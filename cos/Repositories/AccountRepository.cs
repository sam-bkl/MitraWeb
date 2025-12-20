using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using cos.ViewModels;
using cos.Helpers;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace cos.Repositories
{
    public class AccountRepository
    {
        private readonly string connectionStringPgSql;

        public AccountRepository(IConfiguration configuration)
        {
            connectionStringPgSql = configuration.GetValue<string>("ConnectionStrings:PgSql");
        }

        internal IDbConnection ConnectionPgSql => new NpgsqlConnection(connectionStringPgSql);

        public class InsertResult
        {
            public bool Success { get; set; }
            public int RowsAffected { get; set; }
            public string? ErrorMessage { get; set; }
            public long? AccountId { get; set; }
        }

        public async Task<long?> GetAccountIdByUsernameAsync(string username)
        {
            try
            {
                const string sql = @"SELECT id FROM accounts WHERE username = @username AND record_status = 'ACTIVE'";
                using var db = ConnectionPgSql;
                return await db.QueryFirstOrDefaultAsync<long?>(sql, new { username });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking account existence: {ex.Message}", ex);
            }
        }

        public async Task<ExistingUserVM?> GetExistingUserDetailsAsync(long accountId)
        {
            try
            {
                const string sql = @"SELECT u.staff_name, u.mobile, u.email, u.hrno, u.designation_code, 
                                            u.ssa_code, c.circle_name, u.record_status
                                     FROM users u
                                     LEFT JOIN cos_circles c ON u.circle = c.circle_code
                                     WHERE u.account_id = @account_id";
                using var db = ConnectionPgSql;
                return await db.QueryFirstOrDefaultAsync<ExistingUserVM>(sql, new { account_id = accountId });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving existing user details: {ex.Message}", ex);
            }
        }

        public async Task<long?> GetRoleIdByNameAsync(string roleName)
        {
            try
            {
                const string sql = @"SELECT id FROM roles WHERE role_name = @roleName";
                using var db = ConnectionPgSql;
                return await db.QueryFirstOrDefaultAsync<long?>(sql, new { roleName });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving role ID: {ex.Message}", ex);
            }
        }

        public async Task<InsertResult> CreateCscAdminAccountAsync(CscAdminCreateVM model, long createdByAccountId, string createdByMobile, CscRepository cscRepository)
        {
            try
            {
                using var db = ConnectionPgSql;
                db.Open();
                var transaction = db.BeginTransaction();

                try
                {
                    // Get role_id for csc_admin
                    var roleId = await GetRoleIdByNameAsync("csc_admin");
                    if (!roleId.HasValue)
                    {
                        transaction.Rollback();
                        return new InsertResult
                        {
                            Success = false,
                            ErrorMessage = "Role 'csc_admin' not found in database."
                        };
                    }

                    // Get circle_code from circle_id if needed
                    string? circleCode = model.circle;
                    if (string.IsNullOrEmpty(circleCode) && model.circle_id.HasValue)
                    {
                        var circle = await cscRepository.GetCircleByIdAsync(model.circle_id.Value);
                        if (circle != null)
                        {
                            circleCode = circle.circle_code;
                        }
                    }

                    // Get ssa_code from ssa_id if needed
                    string? ssaCode = model.ssa_code;
                    if (string.IsNullOrEmpty(ssaCode) && model.ssa_id.HasValue)
                    {
                        var ssa = await cscRepository.GetSsaByIdAsync(model.ssa_id.Value);
                        if (ssa != null)
                        {
                            ssaCode = ssa.ssa_code;
                        }
                    }

                    // Default password
                    const string defaultPassword = "Bsnl@123";
                    string encryptedPassword = PasswordHelper.ComputeSha256Hash(defaultPassword);

                    // Insert into accounts table
                    var accountSql = @"INSERT INTO accounts 
                                      (role_id, username, user_password_safe, user_password, is_verified, record_status, 
                                       created_on, updated_on, updated_by, reset_by, reset_on)
                                      VALUES 
                                      (@role_id, @username, @user_password_safe, @user_password, @is_verified, @record_status,
                                       @created_on, @updated_on, @updated_by, @reset_by, @reset_on)
                                      RETURNING id";

                    var accountId = await db.QuerySingleAsync<long>(accountSql, new
                    {
                        role_id = roleId.Value,
                        username = model.mobile,
                        user_password_safe = encryptedPassword,
                        user_password = defaultPassword,
                        is_verified = "VERIFIED",
                        record_status = "ACTIVE",
                        created_on = DateTime.UtcNow,
                        updated_on = DateTime.UtcNow,
                        updated_by = createdByAccountId,
                        reset_by = createdByMobile,
                        reset_on = DateTime.UtcNow
                    }, transaction);

                    // Insert into users table
                    var userSql = @"INSERT INTO users 
                                   (account_id, staff_name, mobile, email, hrno, designation_code, ssa_code, 
                                    record_status, created_on, updated_on, updated_by, changepassword, circle)
                                   VALUES 
                                   (@account_id, @staff_name, @mobile, @email, @hrno, @designation_code, @ssa_code,
                                    @record_status, @created_on, @updated_on, @updated_by, @changepassword, @circle)";

                    await db.ExecuteAsync(userSql, new
                    {
                        account_id = accountId,
                        staff_name = model.staff_name,
                        mobile = model.mobile,
                        email = model.email,
                        hrno = model.hrno,
                        designation_code = model.designation_code,
                        ssa_code = ssaCode ?? model.ssa_code,
                        record_status = "ACTIVE",
                        created_on = DateTime.UtcNow,
                        updated_on = DateTime.UtcNow,
                        updated_by = createdByAccountId,
                        changepassword = "0",
                        circle = circleCode ?? model.circle
                    }, transaction);

                    transaction.Commit();
                    return new InsertResult { Success = true, RowsAffected = 1, AccountId = accountId };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new InsertResult
                    {
                        Success = false,
                        ErrorMessage = $"Error creating account: {ex.Message}" + (ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "")
                    };
                }
            }
            catch (Exception ex)
            {
                return new InsertResult
                {
                    Success = false,
                    ErrorMessage = $"Database error: {ex.Message}" + (ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "")
                };
            }
        }

        public async Task<IEnumerable<SsaAdminListVM>> GetSsaAdminsByCircleAsync(string circleCode)
        {
            try
            {
                const string sql = @"SELECT u.account_id, u.staff_name, u.mobile, u.email, u.hrno, 
                                            u.designation_code, u.ssa_code, c.circle_name, 
                                            u.record_status, u.created_on
                                     FROM users u
                                     INNER JOIN accounts a ON u.account_id = a.id
                                     INNER JOIN roles r ON a.role_id = r.id
                                     LEFT JOIN cos_circles c ON u.circle = c.circle_code
                                     WHERE u.circle = @circleCode 
                                       AND r.role_name = 'ba_admin'
                                       AND u.record_status = 'ACTIVE'
                                       AND a.record_status = 'ACTIVE'
                                     ORDER BY u.created_on DESC";
                using var db = ConnectionPgSql;
                return await db.QueryAsync<SsaAdminListVM>(sql, new { circleCode });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving SSA admins by circle: {ex.Message}", ex);
            }
        }

        public async Task<InsertResult> CreateSsaAdminAccountAsync(SsaAdminCreateVM model, long createdByAccountId, string createdByMobile, CscRepository cscRepository)
        {
            try
            {
                using var db = ConnectionPgSql;
                db.Open();
                var transaction = db.BeginTransaction();

                try
                {
                    // Get role_id for ba_admin
                    var roleId = await GetRoleIdByNameAsync("ba_admin");
                    if (!roleId.HasValue)
                    {
                        transaction.Rollback();
                        return new InsertResult
                        {
                            Success = false,
                            ErrorMessage = "Role 'ba_admin' not found in database."
                        };
                    }

                    // Get circle_code from circle_id if needed
                    string? circleCode = model.circle;
                    if (string.IsNullOrEmpty(circleCode) && model.circle_id.HasValue)
                    {
                        var circle = await cscRepository.GetCircleByIdAsync(model.circle_id.Value);
                        if (circle != null)
                        {
                            circleCode = circle.circle_code;
                        }
                    }

                    // Get ssa_code from ssa_id if needed
                    string? ssaCode = model.ssa_code;
                    if (string.IsNullOrEmpty(ssaCode) && model.ssa_id.HasValue)
                    {
                        var ssa = await cscRepository.GetSsaByIdAsync(model.ssa_id.Value);
                        if (ssa != null)
                        {
                            ssaCode = ssa.ssa_code;
                        }
                    }

                    // Default password
                    const string defaultPassword = "Bsnl@123";
                    string encryptedPassword = PasswordHelper.ComputeSha256Hash(defaultPassword);

                    // Insert into accounts table
                    var accountSql = @"INSERT INTO accounts 
                                      (role_id, username, user_password_safe, user_password, is_verified, record_status, 
                                       created_on, updated_on, updated_by, reset_by, reset_on)
                                      VALUES 
                                      (@role_id, @username, @user_password_safe, @user_password, @is_verified, @record_status,
                                       @created_on, @updated_on, @updated_by, @reset_by, @reset_on)
                                      RETURNING id";

                    var accountId = await db.QuerySingleAsync<long>(accountSql, new
                    {
                        role_id = roleId.Value,
                        username = model.mobile,
                        user_password_safe = encryptedPassword,
                        user_password = defaultPassword,
                        is_verified = "VERIFIED",
                        record_status = "ACTIVE",
                        created_on = DateTime.UtcNow,
                        updated_on = DateTime.UtcNow,
                        updated_by = createdByAccountId,
                        reset_by = createdByMobile,
                        reset_on = DateTime.UtcNow
                    }, transaction);

                    // Insert into users table
                    var userSql = @"INSERT INTO users 
                                   (account_id, staff_name, mobile, email, hrno, designation_code, ssa_code, 
                                    record_status, created_on, updated_on, updated_by, changepassword, circle)
                                   VALUES 
                                   (@account_id, @staff_name, @mobile, @email, @hrno, @designation_code, @ssa_code,
                                    @record_status, @created_on, @updated_on, @updated_by, @changepassword, @circle)";

                    await db.ExecuteAsync(userSql, new
                    {
                        account_id = accountId,
                        staff_name = model.staff_name,
                        mobile = model.mobile,
                        email = model.email,
                        hrno = model.hrno,
                        designation_code = model.designation_code,
                        ssa_code = ssaCode ?? model.ssa_code,
                        record_status = "ACTIVE",
                        created_on = DateTime.UtcNow,
                        updated_on = DateTime.UtcNow,
                        updated_by = createdByAccountId,
                        changepassword = "0",
                        circle = circleCode ?? model.circle
                    }, transaction);

                    transaction.Commit();
                    return new InsertResult { Success = true, RowsAffected = 1, AccountId = accountId };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new InsertResult
                    {
                        Success = false,
                        ErrorMessage = $"Error creating account: {ex.Message}" + (ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "")
                    };
                }
            }
            catch (Exception ex)
            {
                return new InsertResult
                {
                    Success = false,
                    ErrorMessage = $"Database error: {ex.Message}" + (ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "")
                };
            }
        }
    }
}

