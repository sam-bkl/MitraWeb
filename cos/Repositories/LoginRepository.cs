using Dapper;
using cos.ViewModels;
using Npgsql;
using System.Data;
using System.Text;

namespace cos.Repositories
{
    public class LoginRepository
    {
        private readonly string connectionStringPgSql;

        public LoginRepository(IConfiguration configuration)
        {
            connectionStringPgSql = configuration.GetValue<string>("ConnectionStrings:PgSql");
        }

        internal IDbConnection ConnectionPgSql
        {
            get
            {
                return new NpgsqlConnection(connectionStringPgSql);
            }
        }

        public async Task<AccountVM> Authenticate(LoginVM postData)
        {
            AccountVM userAccount = null;

            StringBuilder query = new StringBuilder();
            query.Append("SELECT b.id, a.staff_name, a.email, a.mobile, a.hrno, c.role_name, ");
            query.Append("a.designation_code, a.ssa_code, a.changepassword,a.circle, ");
            query.Append("TO_CHAR(b.reset_on, 'DD/MM/YYYY') AS reset_on, b.is_verified, ");
            query.Append("b.username AS user_name ");
            query.Append("FROM users a ");
            query.Append("LEFT JOIN accounts b ON a.account_id = b.id ");
            query.Append("LEFT JOIN roles c ON b.role_id = c.id ");
            query.Append("WHERE a.record_status = :param_record_status ");
            query.Append("AND b.username = :param_user_name ");
            query.Append("AND b.user_password = :param_password");

            //StringBuilder otpQuery = new StringBuilder();
            //otpQuery.Append("SELECT expiry_date ");
            //otpQuery.Append("FROM otp_entrylog ");
            //otpQuery.Append("WHERE username = :param_username ");
            //otpQuery.Append("AND otp = :param_otp ");
            //otpQuery.Append("ORDER BY entry_date DESC ");
            //otpQuery.Append("LIMIT 1");

            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();

                try
                {
                    var result = await dbConnection.QueryAsync<AccountVM>(query.ToString(), new
                    {
                        param_record_status = "ACTIVE",
                        param_user_name = postData.username,
                        param_password = postData.password
                    });

                    userAccount = result.FirstOrDefault();

                    if (userAccount != null)
                    {
                        //var otpResult = await dbConnection.QueryFirstOrDefaultAsync<DateTime?>(otpQuery.ToString(), new
                        //{
                        //    param_username = postData.username,
                        //    param_otp = postData.otp
                        //});

                        //if (otpResult.HasValue && otpResult.Value > DateTime.Now)
                        //{
                            return userAccount;
                        //}

                        // If OTP is expired or null, treat as authentication failure
                        //return null;
                    }

                    // User not found
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Authentication error: " + ex.Message);
                    return new AccountVM();
                }
            }
        }


        public async Task<string> LogEntry(long accountid, string user_name,string clientip)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder query = new StringBuilder();
            AccountVM userAccount = new AccountVM();
            query.Append("select staff_name from users where account_id = :param_accountid ");

            sb.Append("insert into entrylog(account_id,staff_name,username,clientip,entry_date) values( ");
            sb.Append(" @parm_user_account_id,@parm_staff_name,@parm_username,@parm_clientip,now() )");


            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();
                var transaction = dbConnection.BeginTransaction();
                try
                {
                    var result = await dbConnection.QueryAsync<AccountVM>(query.ToString(), new
                    {
                        param_accountid = accountid

                    });
                    userAccount = result.FirstOrDefault();
                    if (userAccount != null)
                    {

                        var result_insert = await dbConnection.ExecuteAsync(sb.ToString(), new
                        {
                           
                            parm_user_account_id = accountid,
                            parm_staff_name = userAccount.staff_name,
                            parm_username = user_name,
                            parm_clientip = clientip
                        });

                        transaction.Commit();

                        return "success";
                    }
                    else
                    {
                        return "failure";
                    }

                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                    return "failure";
                }
            }
        }

        public async Task<string> PwdExpiryupdt(int nid)
        {

            StringBuilder sbPwd = new StringBuilder();
            sbPwd.Append("update users set changepassword='1' where account_id=@parm_nid ");

            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();
                var transaction = dbConnection.BeginTransaction();

                try
                {

                    var resultpwd = await dbConnection.ExecuteAsync(sbPwd.ToString(), new
                    {
                        parm_nid = nid
                    });
                    transaction.Commit();
                    return "success";

                }
                catch (Exception err)
                {
                    //transaction.Rollback();
                    return "error-" + err.Message;
                }

            }
        }

        public async Task<LgchkVM> ChkData(LgchkVM postdata)
        {

            LgchkVM lgchk = new LgchkVM();

            StringBuilder query = new StringBuilder();
            query.Append("select a.id,b.hrno,b.mobile,a.username uid");
            query.Append(" from accounts  a left join  users b on (a.id=b.account_id)");
            query.Append(" where a.record_status = :param_record_status and a.username = :param_user_name and a.user_password = :param_password");



            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();

                try
                {
                    var result = await dbConnection.QueryAsync<LgchkVM>(query.ToString(), new
                    {
                        param_record_status = "ACTIVE",
                        param_user_name = postdata.uid,
                        param_password = postdata.pid
                    });

                    lgchk = result.FirstOrDefault();
                    return lgchk;
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                    return new LgchkVM();
                }


            }

        }

        public async Task<string> OtpLogEntry(LgchkVM lgchk, string clientip, string otp)
        {
            StringBuilder sb = new StringBuilder();

            // First, expire any previous OTPs for this user to avoid multiple active tokens
            await ExpirePreviousOtpsAsync(lgchk.id);

            sb.Append("insert into otp_entrylog(username,clientip,entry_date,expiry_date,mobileno,otp,user_account_id,");
            sb.Append("is_verified, verified_at, verification_attempts, is_expired) values( ");
            sb.Append(" @parm_username,@parm_clientip,now(), now() + (5 ||' minutes')::interval,@parm_mob,@parm_otp,@parm_id,");
            sb.Append(" false, NULL, 0, false)");


            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();
                var transaction = dbConnection.BeginTransaction();
                try
                {

                    var result_insert = await dbConnection.ExecuteAsync(sb.ToString(), new
                    {
                        parm_username = lgchk.uid,
                        parm_clientip = clientip,
                        parm_mob = lgchk.mobile,
                        parm_otp = otp,
                        parm_id = lgchk.id
                    });

                    transaction.Commit();

                    return "success";
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                    return "failure";
                }
            }
        }

        /// <summary>
        /// Force-expire all previous OTPs for this account.
        /// </summary>
        public async Task ExpirePreviousOtpsAsync(long accountId)
        {
            const string sql = @"UPDATE otp_entrylog
                                 SET is_expired = true
                                 WHERE user_account_id = @accountId
                                   AND is_expired = false";

            using var dbConnection = ConnectionPgSql;
            await dbConnection.ExecuteAsync(sql, new { accountId });
        }

        /// <summary>
        /// Mark the latest matching OTP as verified.
        /// </summary>
        public async Task MarkOtpVerifiedAsync(long accountId, string otp)
        {
            const string sql = @"
                WITH latest AS (
                    SELECT id
                    FROM otp_entrylog
                    WHERE user_account_id = @accountId
                      AND otp = @otp
                      AND is_expired = false
                    ORDER BY entry_date DESC
                    LIMIT 1
                )
                UPDATE otp_entrylog
                SET is_verified = true,
                    verified_at = NOW(),
                    is_expired = false
                WHERE id IN (SELECT id FROM latest)";

            using var dbConnection = ConnectionPgSql;
            await dbConnection.ExecuteAsync(sql, new { accountId, otp });
        }

        /// <summary>
        /// Increment verification attempts for the latest active OTP.
        /// </summary>
        public async Task IncrementOtpAttemptAsync(long accountId)
        {
            const string sql = @"
                WITH latest AS (
                    SELECT id
                    FROM otp_entrylog
                    WHERE user_account_id = @accountId
                      AND is_expired = false
                    ORDER BY entry_date DESC
                    LIMIT 1
                )
                UPDATE otp_entrylog
                SET verification_attempts = COALESCE(verification_attempts, 0) + 1
                WHERE id IN (SELECT id FROM latest)";

            using var dbConnection = ConnectionPgSql;
            await dbConnection.ExecuteAsync(sql, new { accountId });
        }

        /// <summary>
        /// Expire the current active OTP (e.g., on timeout).
        /// </summary>
        public async Task ExpireCurrentOtpAsync(long accountId)
        {
            const string sql = @"
                WITH latest AS (
                    SELECT id
                    FROM otp_entrylog
                    WHERE user_account_id = @accountId
                      AND is_expired = false
                    ORDER BY entry_date DESC
                    LIMIT 1
                )
                UPDATE otp_entrylog
                SET is_expired = true
                WHERE id IN (SELECT id FROM latest)";

            using var dbConnection = ConnectionPgSql;
            await dbConnection.ExecuteAsync(sql, new { accountId });
        }

        /// <summary>
        /// Gets account information by account ID without requiring password authentication.
        /// Used for retrieving account details after OTP verification.
        /// </summary>
        public async Task<AccountVM?> GetAccountByIdAsync(long accountId)
        {
            AccountVM? userAccount = null;

            StringBuilder query = new StringBuilder();
            query.Append("SELECT b.id, a.staff_name, a.email, a.mobile, a.hrno, c.role_name, ");
            query.Append("a.designation_code, a.ssa_code, a.changepassword, a.circle, ");
            query.Append("TO_CHAR(b.reset_on, 'DD/MM/YYYY') AS reset_on, b.is_verified, ");
            query.Append("b.username AS user_name ");
            query.Append("FROM users a ");
            query.Append("LEFT JOIN accounts b ON a.account_id = b.id ");
            query.Append("LEFT JOIN roles c ON b.role_id = c.id ");
            query.Append("WHERE a.record_status = :param_record_status ");
            query.Append("AND b.id = :param_account_id ");
            query.Append("AND b.record_status = :param_record_status");

            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();

                try
                {
                    var result = await dbConnection.QueryAsync<AccountVM>(query.ToString(), new
                    {
                        param_record_status = "ACTIVE",
                        param_account_id = accountId
                    });

                    userAccount = result.FirstOrDefault();
                    return userAccount;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GetAccountByIdAsync error: {ex.Message}");
                    return null;
                }
            }
        }
    }
}
