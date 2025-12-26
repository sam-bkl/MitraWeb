using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dapper;
using cos.ViewModels;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace cos.Repositories
{
    public class CscRepository
    {
        private readonly string connectionStringPgSql;

        public CscRepository(IConfiguration configuration)
        {
            connectionStringPgSql = configuration.GetValue<string>("ConnectionStrings:PgSql");
        }

        internal IDbConnection ConnectionPgSql => new NpgsqlConnection(connectionStringPgSql);

        public async Task<User?> GetUserByAccountIdAsync(long accountId)
        {
            try
            {
                const string sql = @"SELECT id, account_id, staff_name, mobile, email, hrno, designation_code,
                                            ssa_code, record_status, created_on, updated_on, deleted_on,
                                            updated_by, changepassword, deleted_by, circle
                                     FROM users
                                     WHERE account_id = @account_id";
                using var db = ConnectionPgSql;
                var result = await db.QueryFirstOrDefaultAsync<User>(sql, new { account_id = accountId });
                return result;
            }
            catch (Exception ex)
            {
                // Log error if needed
                throw new Exception($"Error retrieving user by account ID: {ex.Message}", ex);
            }
        }

        public async Task<CtopMaster?> GetCtopByUsernameAsync(string username)
        {
            try
            {
                const string sql = @"SELECT username, ctopupno, name, dealertype, ssa_code, csccode, circle_code, attached_to,
                                            contact_number, pos_hno, pos_street, pos_landmark, pos_locality, pos_city,
                                            pos_district, pos_state, pos_pincode, created_date, pos_name_ss, pos_owner_name,
                                            pos_code, pos_ctop, circle_name, pos_unique_code, latitude, longitude,
                                            aadhaar_no, zone_code, ctop_type
                                     FROM ctop_master
                                     WHERE username = @username
                                     ORDER BY created_date DESC
                                     LIMIT 1";
                using var db = ConnectionPgSql;
                return await db.QueryFirstOrDefaultAsync<CtopMaster>(sql, new { username });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving CTOP by username: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<CtopMaster>> GetUsersByCtopAsync(string ctopupno)
        {
            try
            {
                const string sql = @"SELECT username, ctopupno, name, dealertype, ssa_code, csccode, circle_code, attached_to,
                                            contact_number, pos_hno, pos_street, pos_landmark, pos_locality, pos_city,
                                            pos_district, pos_state, pos_pincode, created_date, pos_name_ss, pos_owner_name,
                                            pos_code, pos_ctop, circle_name, pos_unique_code, latitude, longitude,
                                            aadhaar_no, zone_code, ctop_type
                                     FROM ctop_master
                                     WHERE ctopupno = @ctopupno";
                using var db = ConnectionPgSql;
                return await db.QueryAsync<CtopMaster>(sql, new { ctopupno });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving users by CTOP: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<CircleOptionVM>> GetCirclesAsync()
        {
            try
            {
                const string sql = @"SELECT id, circle_name, circle_code, zone_code FROM cos_circles ORDER BY circle_name";
                using var db = ConnectionPgSql;
                return await db.QueryAsync<CircleOptionVM>(sql);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving circles: {ex.Message}", ex);
            }
        }

        public async Task<CircleOptionVM?> GetCircleByIdAsync(long id)
        {
            try
            {
                const string sql = @"SELECT id, circle_name, circle_code, zone_code FROM cos_circles WHERE id = @id";
                using var db = ConnectionPgSql;
                return await db.QueryFirstOrDefaultAsync<CircleOptionVM>(sql, new { id });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving circle by ID: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<SsaOptionVM>> GetSsasByCircleIdAsync(long circleId)
        {
            try
            {
                const string sql = @"SELECT id, ssa_name, ssa_code, circle_id
                                     FROM cos_ssas
                                     WHERE circle_id = @circleId
                                     ORDER BY ssa_name";
                using var db = ConnectionPgSql;
                return await db.QueryAsync<SsaOptionVM>(sql, new { circleId });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving SSAs by circle ID: {ex.Message}", ex);
            }
        }

        public async Task<SsaOptionVM?> GetSsaByIdAsync(long id)
        {
            try
            {
                const string sql = @"SELECT id, ssa_name, ssa_code, circle_id
                                     FROM cos_ssas
                                     WHERE id = @id";
                using var db = ConnectionPgSql;
                return await db.QueryFirstOrDefaultAsync<SsaOptionVM>(sql, new { id });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving SSA by ID: {ex.Message}", ex);
            }
        }

        public class InsertResult
        {
            public bool Success { get; set; }
            public int RowsAffected { get; set; }
            public string? ErrorMessage { get; set; }
        }

        public class TempDataResult<T>
        {
            public bool Success { get; set; }
            public T? Data { get; set; }
            public string? Error { get; set; }
        }

        public async Task<InsertResult> InsertCtopAsync(CtopMaster entity, long accountId)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(@"INSERT INTO ctop_master
                            (username, ctopupno, name, dealertype, ssa_code, csccode, circle_code, attached_to,
                             contact_number, pos_hno, pos_street, pos_landmark, pos_locality, pos_city, pos_district,
                             pos_state, pos_pincode, created_date, pos_name_ss, pos_owner_name, pos_code, pos_ctop,
                             circle_name, pos_unique_code, latitude, longitude, aadhaar_no, zone_code, ctop_type,
                             dealercode, ref_dealer_id, master_dealer_id, parent_ctopno, dealer_status, end_date,
                             dealer_id, active, account_id, created_on, updated_on, data_source)
                            VALUES
                            (@username, @ctopupno, @name, @dealertype, @ssa_code, @csccode, @circle_code, @attached_to,
                             @contact_number, @pos_hno, @pos_street, @pos_landmark, @pos_locality, @pos_city, @pos_district,
                             @pos_state, @pos_pincode, @created_date, @pos_name_ss, @pos_owner_name, @pos_code, @pos_ctop,
                             @circle_name, @pos_unique_code, @latitude, @longitude, @aadhaar_no, @zone_code, @ctop_type,
                             @dealercode, @ref_dealer_id, @master_dealer_id, @parent_ctopno, @dealer_status, @end_date,
                             @dealer_id, @active, @account_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, @data_source)");

                using var db = ConnectionPgSql;
                var parameters = new
                {
                    username = entity.username,
                    ctopupno = entity.ctopupno,
                    name = entity.name,
                    dealertype = entity.dealertype,
                    ssa_code = entity.ssa_code,
                    csccode = entity.csccode,
                    circle_code = entity.circle_code,
                    attached_to = entity.attached_to,
                    contact_number = entity.contact_number,
                    pos_hno = entity.pos_hno,
                    pos_street = entity.pos_street,
                    pos_landmark = entity.pos_landmark,
                    pos_locality = entity.pos_locality,
                    pos_city = entity.pos_city,
                    pos_district = entity.pos_district,
                    pos_state = entity.pos_state,
                    pos_pincode = entity.pos_pincode,
                    created_date = entity.created_date,
                    pos_name_ss = entity.pos_name_ss,
                    pos_owner_name = entity.pos_owner_name,
                    pos_code = entity.pos_code,
                    pos_ctop = entity.pos_ctop,
                    circle_name = entity.circle_name,
                    pos_unique_code = entity.pos_unique_code,
                    latitude = entity.latitude,
                    longitude = entity.longitude,
                    aadhaar_no = entity.aadhaar_no,
                    zone_code = entity.zone_code,
                    ctop_type = entity.ctop_type,
                    dealercode = entity.dealercode,
                    ref_dealer_id = entity.ref_dealer_id,
                    master_dealer_id = entity.master_dealer_id,
                    parent_ctopno = entity.parent_ctopno,
                    dealer_status = entity.dealer_status,
                    end_date = entity.end_date,
                    dealer_id = entity.dealer_id,
                    active = entity.active,
                    account_id = accountId,
                    data_source = "MITRA"
                };
                var rowsAffected = await db.ExecuteAsync(sb.ToString(), parameters);
                
                // Log to audit table
                if (rowsAffected > 0)
                {
                    await LogCtopMasterAuditAsync(entity, accountId, "INSERT", null, "New CTOP record inserted");
                }
                
                return new InsertResult { Success = true, RowsAffected = rowsAffected };
            }
            catch (Exception ex)
            {
                return new InsertResult
                {
                    Success = false,
                    RowsAffected = 0,
                    ErrorMessage = $"Database error: {ex.Message}" + (ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "")
                };
            }
        }

        public async Task<InsertResult> UpdateCtopAsync(CtopMaster entity, long accountId)
        {
            try
            {
                using var db = ConnectionPgSql;
                db.Open();
                var transaction = db.BeginTransaction();

                try
                {
                    // First, get the current row to log to audit
                    const string selectSql = @"SELECT username, ctopupno, name, dealertype, ssa_code, csccode, circle_code, attached_to,
                                                      contact_number, pos_hno, pos_street, pos_landmark, pos_locality, pos_city,
                                                      pos_district, pos_state, pos_pincode, created_date, pos_name_ss, pos_owner_name,
                                                      pos_code, pos_ctop, circle_name, pos_unique_code, latitude, longitude,
                                                      aadhaar_no, zone_code, ctop_type, dealercode, ref_dealer_id, master_dealer_id,
                                                      parent_ctopno, dealer_status, end_date, dealer_id, active
                                               FROM ctop_master
                                               WHERE username = @username AND ctopupno = @ctopupno
                                               ORDER BY created_date DESC
                                               LIMIT 1";
                    
                    var currentRow = await db.QueryFirstOrDefaultAsync<CtopMaster>(selectSql, new 
                    { 
                        username = entity.username, 
                        ctopupno = entity.ctopupno 
                    }, transaction);

                    if (currentRow == null)
                    {
                        transaction.Rollback();
                        return new InsertResult
                        {
                            Success = false,
                            RowsAffected = 0,
                            ErrorMessage = "Record not found for update"
                        };
                    }

                    // Log current data to audit table before update
                    await LogCtopMasterAuditAsync(currentRow, accountId, "UPDATE", currentRow.pos_unique_code, 
                        "Record updated from Missing CSC Admin Onboard", transaction);

                    // Update the record
                    var sb = new StringBuilder();
                    sb.Append(@"UPDATE ctop_master SET
                                name = @name, dealertype = @dealertype, ssa_code = @ssa_code, csccode = @csccode, 
                                circle_code = @circle_code, attached_to = @attached_to,
                                contact_number = @contact_number, pos_hno = @pos_hno, pos_street = @pos_street, 
                                pos_landmark = @pos_landmark, pos_locality = @pos_locality, pos_city = @pos_city, 
                                pos_district = @pos_district, pos_state = @pos_state, pos_pincode = @pos_pincode,
                                pos_name_ss = @pos_name_ss, pos_owner_name = @pos_owner_name, pos_code = @pos_code, 
                                pos_ctop = @pos_ctop, circle_name = @circle_name, pos_unique_code = @pos_unique_code,
                                latitude = @latitude, longitude = @longitude, aadhaar_no = @aadhaar_no, 
                                zone_code = @zone_code, ctop_type = @ctop_type, dealercode = @dealercode,
                                ref_dealer_id = @ref_dealer_id, master_dealer_id = @master_dealer_id,
                                parent_ctopno = @parent_ctopno, dealer_status = @dealer_status, end_date = @end_date,
                                dealer_id = @dealer_id, active = @active,
                                updated_on = CURRENT_TIMESTAMP
                                WHERE username = @username AND ctopupno = @ctopupno");

                    var parameters = new
                    {
                        username = entity.username,
                        ctopupno = entity.ctopupno,
                        name = entity.name,
                        dealertype = entity.dealertype,
                        ssa_code = entity.ssa_code,
                        csccode = entity.csccode,
                        circle_code = entity.circle_code,
                        attached_to = entity.attached_to,
                        contact_number = entity.contact_number,
                        pos_hno = entity.pos_hno,
                        pos_street = entity.pos_street,
                        pos_landmark = entity.pos_landmark,
                        pos_locality = entity.pos_locality,
                        pos_city = entity.pos_city,
                        pos_district = entity.pos_district,
                        pos_state = entity.pos_state,
                        pos_pincode = entity.pos_pincode,
                        pos_name_ss = entity.pos_name_ss,
                        pos_owner_name = entity.pos_owner_name,
                        pos_code = entity.pos_code,
                        pos_ctop = entity.pos_ctop,
                        circle_name = entity.circle_name,
                        pos_unique_code = entity.pos_unique_code,
                        latitude = entity.latitude,
                        longitude = entity.longitude,
                        aadhaar_no = entity.aadhaar_no,
                        zone_code = entity.zone_code,
                        ctop_type = entity.ctop_type,
                        dealercode = entity.dealercode,
                        ref_dealer_id = entity.ref_dealer_id,
                        master_dealer_id = entity.master_dealer_id,
                        parent_ctopno = entity.parent_ctopno,
                        dealer_status = entity.dealer_status,
                        end_date = entity.end_date,
                        dealer_id = entity.dealer_id,
                        active = entity.active
                    };

                    var rowsAffected = await db.ExecuteAsync(sb.ToString(), parameters, transaction);
                    
                    // Log updated row to audit
                    await LogCtopMasterAuditAsync(entity, accountId, "UPDATE", currentRow.pos_unique_code, 
                        "Record updated from Missing CSC Admin Onboard", transaction);

                    transaction.Commit();
                    
                    return new InsertResult { Success = true, RowsAffected = rowsAffected };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new InsertResult
                    {
                        Success = false,
                        RowsAffected = 0,
                        ErrorMessage = $"Database error: {ex.Message}" + (ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "")
                    };
                }
            }
            catch (Exception ex)
            {
                return new InsertResult
                {
                    Success = false,
                    RowsAffected = 0,
                    ErrorMessage = $"Database error: {ex.Message}" + (ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "")
                };
            }
        }

        public async Task<IEnumerable<CtopSearchResultVM>> SearchCtopByCtopupnoAsync(string ctopupno)
        {
            try
            {
                const string sql = @"SELECT ctopupno, name, contact_number
                                     FROM ctop_master
                                     WHERE ctopupno LIKE @pattern AND (dealertype = 'CSR' OR dealertype = 'DEPT')
                                     ORDER BY ctopupno
                                     LIMIT 20";
                using var db = ConnectionPgSql;
                var pattern = $"%{ctopupno}%";
                return await db.QueryAsync<CtopSearchResultVM>(sql, new { pattern });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching CTOP: {ex.Message}", ex);
            }
        }

        public async Task<CtopMaster?> GetCtopByCtopupnoAsync(string ctopupno)
        {
            try
            {
                const string sql = @"SELECT username, ctopupno, name, dealertype, ssa_code, csccode, circle_code, attached_to,
                                            contact_number, pos_hno, pos_street, pos_landmark, pos_locality, pos_city,
                                            pos_district, pos_state, pos_pincode, created_date, pos_name_ss, pos_owner_name,
                                            pos_code, pos_ctop, circle_name, pos_unique_code, latitude, longitude,
                                            aadhaar_no, zone_code, ctop_type
                                     FROM ctop_master
                                     WHERE username = @ctopupno
                                     ORDER BY created_date DESC
                                     LIMIT 1";
                using var db = ConnectionPgSql;
                return await db.QueryFirstOrDefaultAsync<CtopMaster>(sql, new { ctopupno });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving CTOP by ctopupno: {ex.Message}", ex);
            }
        }

        // Document Management Methods
        public async Task<IEnumerable<CtopMasterDocVM>> GetDocumentsByUsernameAsync(string username)
        {
            try
            {
                const string sql = @"SELECT id, username, file_name, file_category, file_category_code, 
                                            document_path, created_on, alt_document_path, alt_file_name
                                     FROM ctop_master_docs
                                     WHERE username = @username AND record_status = 'ACTIVE'
                                     ORDER BY created_on DESC";
                using var db = ConnectionPgSql;
                var docs = await db.QueryAsync<CtopMasterDoc>(sql, new { username });
                return docs.Select(d => new CtopMasterDocVM
                {
                    id = d.id,
                    username = d.username,
                    file_name = d.file_name,
                    file_category = d.file_category,
                    file_category_code = d.file_category_code,
                    document_path = d.document_path,
                    created_on = d.created_on,
                    hasDocument = true,
                    alt_document_path = d.alt_document_path,
                    alt_file_name = d.alt_file_name
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving documents by username: {ex.Message}", ex);
            }
        }

        public async Task<CtopMasterDoc?> GetDocumentByUsernameAndCategoryAsync(string username, string fileCategoryCode)
        {
            try
            {
                const string sql = @"SELECT id, username, document_path, file_name, file_category, 
                                            file_category_code, record_status, created_by, updated_by, 
                                            created_on, updated_on, alt_document_path, alt_file_name
                                     FROM ctop_master_docs
                                     WHERE username = @username 
                                       AND file_category_code = @fileCategoryCode 
                                       AND record_status = 'ACTIVE'
                                     ORDER BY created_on DESC
                                     LIMIT 1";
                using var db = ConnectionPgSql;
                return await db.QueryFirstOrDefaultAsync<CtopMasterDoc>(sql, new { username, fileCategoryCode });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving document by username and category: {ex.Message}", ex);
            }
        }

        public async Task<InsertResult> InsertDocumentAsync(CtopMasterDoc doc)
        {
            try
            {
                const string sql = @"INSERT INTO ctop_master_docs
                                    (username, document_path, file_name, file_category, file_category_code, 
                                     record_status, created_by, updated_by, created_on, updated_on,
                                     alt_document_path, alt_file_name)
                                    VALUES
                                    (@username, @document_path, @file_name, @file_category, @file_category_code,
                                     @record_status, @created_by, @updated_by, @created_on, @updated_on,
                                     @alt_document_path, @alt_file_name)
                                    RETURNING id";
                using var db = ConnectionPgSql;
                var id = await db.QuerySingleAsync<long>(sql, doc);
                return new InsertResult { Success = true, RowsAffected = 1 };
            }
            catch (Exception ex)
            {
                return new InsertResult
                {
                    Success = false,
                    RowsAffected = 0,
                    ErrorMessage = $"Database error inserting document: {ex.Message}" + (ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "")
                };
            }
        }

        public async Task<InsertResult> UpdateDocumentStatusAsync(long docId, string recordStatus, long? updatedBy)
        {
            try
            {
                const string sql = @"UPDATE ctop_master_docs
                                    SET record_status = @recordStatus,
                                        updated_by = @updatedBy,
                                        updated_on = @updatedOn
                                    WHERE id = @docId";
                using var db = ConnectionPgSql;
                var rowsAffected = await db.ExecuteAsync(sql, new 
                { 
                    docId, 
                    recordStatus, 
                    updatedBy, 
                    updatedOn = DateTime.UtcNow 
                });
                return new InsertResult { Success = true, RowsAffected = rowsAffected };
            }
            catch (Exception ex)
            {
                return new InsertResult
                {
                    Success = false,
                    RowsAffected = 0,
                    ErrorMessage = $"Database error updating document status: {ex.Message}" + (ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "")
                };
            }
        }

        public async Task<CtopMasterDoc?> GetDocumentByIdAsync(long docId)
        {
            try
            {
                const string sql = @"SELECT id, username, document_path, file_name, file_category, 
                                            file_category_code, record_status, created_by, updated_by, 
                                            created_on, updated_on, alt_document_path, alt_file_name
                                     FROM ctop_master_docs
                                     WHERE id = @docId";
                using var db = ConnectionPgSql;
                return await db.QueryFirstOrDefaultAsync<CtopMasterDoc>(sql, new { docId });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving document by ID: {ex.Message}", ex);
            }
        }

        // Zone-specific CTOP search methods - returns distinct ctopup nos only
        public async Task<IEnumerable<CtopSearchResultVM>> SearchMissingCscCtopByZoneAsync(string ctopupno, string zoneCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(zoneCode))
                {
                    throw new Exception("Zone code is required");
                }

                var zone = zoneCode.ToUpper();
                string sql;
                var pattern = $"%{ctopupno}%";

                switch (zone)
                {
                    case "EZ":
                    case "EAST":
                        sql = @"SELECT DISTINCT ctopupno, name, contact_number
                                FROM ctop_master_ez cme
                                WHERE cme.active = 'A'
                                  AND cme.ctopupno LIKE @pattern
                                ORDER BY ctopupno
                                LIMIT 20";
                        break;
                    case "SZ":
                    case "SOUTH":
                        sql = @"SELECT DISTINCT ctopupno, name, contact_number
                                FROM ctop_master_sz cme
                                WHERE cme.active = 'A' 
                                  AND cme.ctopupno LIKE @pattern
                                ORDER BY ctopupno
                                LIMIT 20";
                        break;
                    case "NZ":
                    case "NORTH":
                        sql = @"SELECT DISTINCT ctopupno, name, contact_number
                                FROM ctop_master_nz cme
                                WHERE cme.active = 'A'
                                  AND cme.ctopupno LIKE @pattern
                                ORDER BY ctopupno
                                LIMIT 20";
                        break;
                    case "WZ":
                    case "WEST":
                        sql = @"SELECT DISTINCT ctopupno, name, contact_number
                                FROM ctop_master_wz cme
                                WHERE cme.active = 'A' 
                                  AND cme.ctopupno LIKE @pattern
                                ORDER BY ctopupno
                                LIMIT 20";
                        break;
                    default:
                        throw new Exception($"Invalid zone code: {zoneCode}");
                }

                using var db = ConnectionPgSql;
                return await db.QueryAsync<CtopSearchResultVM>(sql, new { pattern });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching missing CSC CTOP: {ex.Message}", ex);
            }
        }

        public async Task<MissingCscCtopDetailsVM?> GetMissingCscCtopDetailsByZoneAsync(string ctopupno, string zoneCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(zoneCode))
                {
                    throw new Exception("Zone code is required");
                }

                var zone = zoneCode.ToUpper();
                string sql;

                switch (zone)
                {
                    case "EZ":
                    case "EAST":
                        sql = @"SELECT ctopupno, name, second_name, last_name, dealertype, 
                                       ssa_code, circle_code, csccode, attached_to, contact_number,
                                       dealer_address, dealer_status, parent_ctopno as parent_ctop, active,
                                       dealer_id, master_dealer_id, deact_date
                                FROM ctop_master_ez cme
                                WHERE cme.ctopupno = @ctopupno";
                        break;
                    case "SZ":
                    case "SOUTH":
                        sql = @"SELECT ctopupno, name, second_name, last_name, dealertype, 
                                       ssa_code, circle_code, csccode, attached_to, contact_number,
                                       dealer_address, dealer_status, parent_ctopno as parent_ctop,
                                       aadhaar_no, zone_code, ssa_city, active,
                                       dealer_id, master_dealer_id, deact_date
                                FROM ctop_master_sz cme
                                WHERE cme.ctopupno = @ctopupno";
                        break;
                    case "NZ":
                    case "NORTH":
                        sql = @"SELECT ctopupno, name, second_name, last_name, dealertype, 
                                       ssa_code, circle_code, csccode, attached_to, contact_number,
                                       dealer_address, dealer_status, parent_ctop, ssa_city, active,
                                       dealer_id, master_dealer_id, deact_date
                                FROM ctop_master_nz cme
                                WHERE cme.ctopupno = @ctopupno";
                        break;
                    case "WZ":
                    case "WEST":
                        sql = @"SELECT ctopupno, name, second_name, last_name, dealertype, 
                                       ssa_code, circle_code, csccode, attached_to, contact_number,
                                       dealer_address, dealer_status, parent_ctop, ssa_city, active,
                                       dealer_id, master_dealer_id, deact_date
                                FROM ctop_master_wz cme
                                WHERE cme.ctopupno = @ctopupno";
                        break;
                    default:
                        throw new Exception($"Invalid zone code: {zoneCode}");
                }

                using var db = ConnectionPgSql;
                var result = await db.QueryFirstOrDefaultAsync<MissingCscCtopDetailsVM>(sql, new { ctopupno });
                
                if (result != null)
                {
                    result.zone_code = zoneCode;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving missing CSC CTOP details: {ex.Message}", ex);
            }
        }

        // Check if user exists in ctop_master
        public async Task<CtopMaster?> GetCtopMasterByCtopupnoAsync(string ctopupno)
        {
            try
            {
                const string sql = @"SELECT username, ctopupno, name, dealertype, ssa_code, csccode, circle_code, attached_to,
                                            contact_number, pos_hno, pos_street, pos_landmark, pos_locality, pos_city,
                                            pos_district, pos_state, pos_pincode, created_date, pos_name_ss, pos_owner_name,
                                            pos_code, pos_ctop, circle_name, pos_unique_code, latitude, longitude,
                                            aadhaar_no, zone_code, ctop_type, dealer_status, end_date
                                     FROM ctop_master
                                     WHERE username = @ctopupno
                                     ORDER BY created_date DESC
                                     LIMIT 1";
                using var db = ConnectionPgSql;
                return await db.QueryFirstOrDefaultAsync<CtopMaster>(sql, new { ctopupno });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving CTOP from ctop_master: {ex.Message}", ex);
            }
        }

        // Check count of records in zonal table by ctopupno
        public async Task<int> GetZonalDataCountByCtopupnoAsync(string ctopupno, string zoneCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(zoneCode))
                {
                    throw new Exception("Zone code is required");
                }

                var zone = zoneCode.ToUpper();
                string sql;

                switch (zone)
                {
                    case "EZ":
                    case "EAST":
                        sql = @"SELECT COUNT(1)
                                FROM ctop_master_ez cme
                                WHERE cme.ctopupno = @ctopupno";
                        break;
                    case "SZ":
                    case "SOUTH":
                        sql = @"SELECT COUNT(1)
                                FROM ctop_master_sz cme
                                WHERE cme.ctopupno = @ctopupno";
                        break;
                    case "NZ":
                    case "NORTH":
                        sql = @"SELECT COUNT(1)
                                FROM ctop_master_nz cme
                                WHERE cme.ctopupno = @ctopupno";
                        break;
                    case "WZ":
                    case "WEST":
                        sql = @"SELECT COUNT(1)
                                FROM ctop_master_wz cme
                                WHERE cme.ctopupno = @ctopupno";
                        break;
                    default:
                        throw new Exception($"Invalid zone code: {zoneCode}");
                }

                using var db = ConnectionPgSql;
                return await db.QueryFirstOrDefaultAsync<int>(sql, new { ctopupno });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking count in zonal table: {ex.Message}", ex);
            }
        }

        // Get data from temp_csc_sa_data
        public async Task<TempDataResult<TempCscSaDataVM>> GetTempCscSaDataByPosCtopAsync(string posCtop)
        {
            try
            {
                const string sql = @"SELECT csccode, pos_ctop, dealer_type, ssa_code, circle_code,
                                            pos_hno, pos_street, pos_landmark, pos_locality, pos_city,
                                            pos_district, pos_state, pos_pincode, created_date, pos_name,
                                            pos_name_ss, pos_owner_name, circle_name, pos_unique_code,
                                            latitude, longitude, aadhar_no as aadhaar_no, zone_id, attached_to,
                                            aggrement_through, ins_flag, insert_date
                                     FROM temp_csc_sa_data
                                     WHERE pos_ctop = @posCtop
                                     LIMIT 1";
                using var db = ConnectionPgSql;
                var data = await db.QueryFirstOrDefaultAsync<TempCscSaDataVM>(sql, new { posCtop });
                return new TempDataResult<TempCscSaDataVM>
                {
                    Success = true,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new TempDataResult<TempCscSaDataVM>
                {
                    Success = false,
                    Error = $"Error retrieving temp_csc_sa_data: {ex.Message}"
                };
            }
        }

        // Get data from temp_sa_pos_data
        public async Task<TempDataResult<TempSaPosDataVM>> GetTempSaPosDataByPosCtopAsync(string posCtop)
        {
            try
            {
                const string sql = @"SELECT csccode, pos_ctop, dealer_type, ssa_code, circle_code,
                                            pos_hno, pos_street, pos_landmark, pos_locality, pos_city,
                                            pos_district, pos_state, pos_pincode, created_date, pos_name,
                                            pos_name_ss, pos_owner_name, circle_name, pos_unique_code,
                                            latitude, longitude, aadhar_no as aadhaar_no, zone_id, attached_to,
                                            aggrement_through, ins_flag, insert_date
                                     FROM temp_sa_pos_data
                                     WHERE pos_ctop = @posCtop
                                     LIMIT 1";
                using var db = ConnectionPgSql;
                var data = await db.QueryFirstOrDefaultAsync<TempSaPosDataVM>(sql, new { posCtop });
                return new TempDataResult<TempSaPosDataVM>
                {
                    Success = true,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new TempDataResult<TempSaPosDataVM>
                {
                    Success = false,
                    Error = $"Error retrieving temp_sa_pos_data: {ex.Message}"
                };
            }
        }

        // Check if username+ctopnumber combination already exists in ctop_master
        public async Task<bool> CheckUsernameCtopupnoExistsAsync(string username, string ctopupno)
        {
            try
            {
                const string sql = @"SELECT COUNT(1)
                                     FROM ctop_master
                                     WHERE username = @username AND ctopupno = @ctopupno";
                using var db = ConnectionPgSql;
                var count = await db.QueryFirstOrDefaultAsync<int>(sql, new { username, ctopupno });
                return count > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking username+ctopupno combination: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<CscCodeOptionVM>> GetCscCodesBySsaAsync(string ssaCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ssaCode))
                {
                    return new List<CscCodeOptionVM>();
                }

                const string sql = @"SELECT DISTINCT csccode, csc_name 
                                     FROM csc 
                                     WHERE ssa_code = @ssaCode
                                     ORDER BY csccode";
                using var db = ConnectionPgSql;
                var result = await db.QueryAsync<CscCodeOptionVM>(sql, new { ssaCode });
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving CSC codes by SSA: {ex.Message}", ex);
            }
        }

        // Methods for Agents feature
        public async Task<IEnumerable<CtopSearchResultVM>> SearchRetailerCtopAsync(string ctopupno)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ctopupno))
                {
                    return new List<CtopSearchResultVM>();
                }

                var pattern = $"%{ctopupno}%";
                const string sql = @"SELECT DISTINCT ctopupno, name, contact_number
                                     FROM ctop_master
                                     WHERE dealertype = 'RETAILER'
                                       AND ctopupno LIKE @pattern
                                     ORDER BY ctopupno
                                     LIMIT 20";
                using var db = ConnectionPgSql;
                return await db.QueryAsync<CtopSearchResultVM>(sql, new { pattern });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching retailer CTOP: {ex.Message}", ex);
            }
        }

        public async Task<CtopMaster?> GetRetailerByCtopupnoAsync(string ctopupno)
        {
            try
            {
                const string sql = @"SELECT username, ctopupno, name, dealertype, ssa_code, csccode, circle_code, attached_to,
                                            contact_number, pos_hno, pos_street, pos_landmark, pos_locality, pos_city,
                                            pos_district, pos_state, pos_pincode, created_date, pos_name_ss, pos_owner_name,
                                            pos_code, pos_ctop, circle_name, pos_unique_code, latitude, longitude,
                                            aadhaar_no, zone_code, ctop_type
                                     FROM ctop_master
                                     WHERE ctopupno = @ctopupno
                                       AND dealertype = 'RETAILER'
                                     ORDER BY created_date DESC
                                     LIMIT 1";
                using var db = ConnectionPgSql;
                return await db.QueryFirstOrDefaultAsync<CtopMaster>(sql, new { ctopupno });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving retailer by CTOPUP number: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<CtopMaster>> GetAgentsByRetailerCtopAsync(string retailerCtopupno)
        {
            try
            {
                const string sql = @"SELECT username, ctopupno, name, dealertype, ssa_code, csccode, circle_code, attached_to,
                                            contact_number, pos_hno, pos_street, pos_landmark, pos_locality, pos_city,
                                            pos_district, pos_state, pos_pincode, created_date, pos_name_ss, pos_owner_name,
                                            pos_code, pos_ctop, circle_name, pos_unique_code, latitude, longitude,
                                            aadhaar_no, zone_code, ctop_type
                                     FROM ctop_master
                                     WHERE ctopupno = @retailerCtopupno
                                       AND dealertype = 'AGENT'
                                     ORDER BY created_date DESC";
                using var db = ConnectionPgSql;
                return await db.QueryAsync<CtopMaster>(sql, new { retailerCtopupno });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving agents by retailer CTOP: {ex.Message}", ex);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> UpdatePosUniqueCodeAsync(string ctopupno, string newPosUniqueCode, long updatedByAccountId)
        {
            try
            {
                using var db = ConnectionPgSql;
                db.Open();

                using var transaction = db.BeginTransaction();
                try
                {
                    // First, get the current row to log to audit
                    const string selectSql = @"SELECT username, ctopupno, name, dealertype, ssa_code, csccode, circle_code, attached_to,
                                                      contact_number, pos_hno, pos_street, pos_landmark, pos_locality, pos_city,
                                                      pos_district, pos_state, pos_pincode, created_date, pos_name_ss, pos_owner_name,
                                                      pos_code, pos_ctop, circle_name, pos_unique_code, latitude, longitude,
                                                      aadhaar_no, zone_code, ctop_type, dealercode, ref_dealer_id, master_dealer_id,
                                                      parent_ctopno, dealer_status, end_date, dealer_id, active
                                               FROM ctop_master
                                               WHERE ctopupno = @ctopupno
                                               ORDER BY created_date DESC
                                               LIMIT 1";
                    
                    var currentRow = await db.QueryFirstOrDefaultAsync<CtopMaster>(selectSql, new { ctopupno }, transaction);
                    
                    if (currentRow == null)
                    {
                        transaction.Rollback();
                        return (false, "CTOP record not found.");
                    }

                    // Log current data to audit table before update
                    await LogCtopMasterAuditAsync(currentRow, updatedByAccountId, "UPDATE", currentRow.pos_unique_code, 
                        $"Updating pos_unique_code from '{currentRow.pos_unique_code}' to '{newPosUniqueCode}'", transaction);

                    // Update the pos_unique_code in ctop_master
                    const string updateSql = @"UPDATE ctop_master 
                                                SET pos_unique_code = @new_pos_unique_code,
                                                    updated_on = CURRENT_TIMESTAMP,
                                                    account_id = @account_id,
                                                    data_source = @data_source
                                                WHERE username = @ctopupno";
                    
                    var rowsAffected = await db.ExecuteAsync(updateSql, new
                    {
                        ctopupno,
                        new_pos_unique_code = newPosUniqueCode,
                        account_id = updatedByAccountId,
                        data_source = "MITRA"
                    }, transaction);

                    if (rowsAffected == 0)
                    {
                        transaction.Rollback();
                        return (false, "Failed to update pos_unique_code. No rows affected.");
                    }

                    // Log updated row to audit table - get updated row
                    var updatedRow = await db.QueryFirstOrDefaultAsync<CtopMaster>(
                        @"SELECT username, ctopupno, name, dealertype, ssa_code, csccode, circle_code, attached_to,
                                 contact_number, pos_hno, pos_street, pos_landmark, pos_locality, pos_city,
                                 pos_district, pos_state, pos_pincode, created_date, pos_name_ss, pos_owner_name,
                                 pos_code, pos_ctop, circle_name, pos_unique_code, latitude, longitude,
                                 aadhaar_no, zone_code, ctop_type, dealercode, ref_dealer_id, master_dealer_id,
                                 parent_ctopno, dealer_status, end_date, dealer_id, active
                          FROM ctop_master
                          WHERE ctopupno = @ctopupno
                          ORDER BY created_date DESC
                          LIMIT 1",
                        new { ctopupno }, transaction);
                    
                    if (updatedRow != null)
                    {
                        await LogCtopMasterAuditAsync(updatedRow, updatedByAccountId, "UPDATE", currentRow.pos_unique_code, 
                            $"Updated pos_unique_code from '{currentRow.pos_unique_code}' to '{newPosUniqueCode}'", transaction);
                    }

                    transaction.Commit();
                    return (true, string.Empty);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error updating pos_unique_code: {ex.Message}");
            }
        }

        // Search for unique ctopupnos where ctopupno = username
        public async Task<IEnumerable<string>> SearchCtopupnosWhereUsernameEqualsCtopupnoAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return new List<string>();
                }

                var pattern = $"%{searchTerm}%";
                const string sql = @"SELECT DISTINCT ctopupno
                                     FROM ctop_master
                                     WHERE ctopupno = username
                                       AND ctopupno LIKE @pattern
                                     ORDER BY ctopupno
                                     LIMIT 20";
                using var db = ConnectionPgSql;
                var results = await db.QueryAsync<string>(sql, new { pattern });
                return results;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching CTOPUP numbers: {ex.Message}", ex);
            }
        }

        // Get main user and all child users for a ctopupno
        public async Task<IEnumerable<CtopMaster>> GetMainUserAndChildrenByCtopupnoAsync(string ctopupno)
        {
            try
            {
                const string sql = @"SELECT username, ctopupno, name, dealertype, ssa_code, csccode, circle_code, attached_to,
                                            contact_number, pos_hno, pos_street, pos_landmark, pos_locality, pos_city,
                                            pos_district, pos_state, pos_pincode, created_date, pos_name_ss, pos_owner_name,
                                            pos_code, pos_ctop, circle_name, pos_unique_code, latitude, longitude,
                                            aadhaar_no, zone_code, ctop_type, dealercode, ref_dealer_id, master_dealer_id,
                                            parent_ctopno, dealer_status, end_date, dealer_id, active
                                     FROM ctop_master
                                     WHERE (ctopupno = @ctopupno AND username = @ctopupno)
                                        OR parent_ctopno = @ctopupno
                                     ORDER BY 
                                         CASE WHEN ctopupno = @ctopupno AND username = @ctopupno THEN 0 ELSE 1 END,
                                         created_date DESC";
                using var db = ConnectionPgSql;
                return await db.QueryAsync<CtopMaster>(sql, new { ctopupno });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving main user and children: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Logs audit information for ctop_master table operations (INSERT or UPDATE)
        /// </summary>
        private async Task LogCtopMasterAuditAsync(CtopMaster entity, long accountId, string auditType, string? oldPosUniqueCode = null, string? changeDescription = null, IDbTransaction? transaction = null)
        {
            try
            {
                const string auditSql = @"INSERT INTO ctop_master_audit_log
                    (audit_type, ctopupno, username, name, dealertype, ssa_code, csccode, circle_code, attached_to,
                     contact_number, pos_hno, pos_street, pos_landmark, pos_locality, pos_city, pos_district,
                     pos_state, pos_pincode, created_date, pos_name_ss, pos_owner_name, pos_code, pos_ctop,
                     circle_name, pos_unique_code, latitude, longitude, aadhaar_no, zone_code, ctop_type,
                     dealercode, ref_dealer_id, master_dealer_id, parent_ctopno, dealer_status,
                     account_id, data_source, old_pos_unique_code, change_description, audit_timestamp)
                    VALUES
                    (@audit_type, @ctopupno, @username, @name, @dealertype, @ssa_code, @csccode, @circle_code, @attached_to,
                     @contact_number, @pos_hno, @pos_street, @pos_landmark, @pos_locality, @pos_city, @pos_district,
                     @pos_state, @pos_pincode, @created_date, @pos_name_ss, @pos_owner_name, @pos_code, @pos_ctop,
                     @circle_name, @pos_unique_code, @latitude, @longitude, @aadhaar_no, @zone_code, @ctop_type,
                     @dealercode, @ref_dealer_id, @master_dealer_id, @parent_ctopno, @dealer_status,
                     @account_id, @data_source, @old_pos_unique_code, @change_description, CURRENT_TIMESTAMP)";

                using var db = transaction != null ? null : ConnectionPgSql;
                if (db != null) db.Open();

                var parameters = new
                {
                    audit_type = auditType,
                    ctopupno = entity.ctopupno,
                    username = entity.username,
                    name = entity.name,
                    dealertype = entity.dealertype,
                    ssa_code = entity.ssa_code,
                    csccode = entity.csccode,
                    circle_code = entity.circle_code,
                    attached_to = entity.attached_to,
                    contact_number = entity.contact_number,
                    pos_hno = entity.pos_hno,
                    pos_street = entity.pos_street,
                    pos_landmark = entity.pos_landmark,
                    pos_locality = entity.pos_locality,
                    pos_city = entity.pos_city,
                    pos_district = entity.pos_district,
                    pos_state = entity.pos_state,
                    pos_pincode = entity.pos_pincode,
                    created_date = entity.created_date,
                    pos_name_ss = entity.pos_name_ss,
                    pos_owner_name = entity.pos_owner_name,
                    pos_code = entity.pos_code,
                    pos_ctop = entity.pos_ctop,
                    circle_name = entity.circle_name,
                    pos_unique_code = entity.pos_unique_code,
                    latitude = entity.latitude,
                    longitude = entity.longitude,
                    aadhaar_no = entity.aadhaar_no,
                    zone_code = entity.zone_code,
                    ctop_type = entity.ctop_type,
                    dealercode = entity.dealercode,
                    ref_dealer_id = entity.ref_dealer_id,
                    master_dealer_id = entity.master_dealer_id,
                    parent_ctopno = entity.parent_ctopno,
                    dealer_status = entity.dealer_status,
                    account_id = accountId,
                    data_source = "MITRA",
                    old_pos_unique_code = oldPosUniqueCode,
                    change_description = changeDescription
                };

                if (transaction != null)
                {
                    await ((IDbConnection)transaction.Connection!).ExecuteAsync(auditSql, parameters, transaction);
                }
                else
                {
                    await db!.ExecuteAsync(auditSql, parameters);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the main operation
                // In production, you might want to log this to a separate error log
                System.Diagnostics.Debug.WriteLine($"Failed to log audit entry: {ex.Message}");
            }
        }

        // Account and User management methods for CSC/DEPT
        public class AccountInfo
        {
            public long id { get; set; }
            public string? record_status { get; set; }
        }

        public class UserInfo
        {
            public long id { get; set; }
            public long account_id { get; set; }
            public string? record_status { get; set; }
        }

        public async Task<AccountInfo?> GetAccountByUsernameAsync(string username)
        {
            try
            {
                const string sql = @"SELECT id, record_status FROM accounts WHERE username = @username";
                using var db = ConnectionPgSql;
                return await db.QueryFirstOrDefaultAsync<AccountInfo>(sql, new { username });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving account by username: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateAccountRecordStatusAsync(long accountId, string recordStatus, long updatedBy)
        {
            try
            {
                const string sql = @"UPDATE accounts 
                                    SET record_status = @recordStatus, 
                                        updated_on = CURRENT_TIMESTAMP,
                                        updated_by = @updatedBy
                                    WHERE id = @accountId";
                using var db = ConnectionPgSql;
                var rowsAffected = await db.ExecuteAsync(sql, new { accountId, recordStatus, updatedBy });
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating account record status: {ex.Message}", ex);
            }
        }

        public async Task<UserInfo?> GetUserByMobileAsync(string mobile)
        {
            try
            {
                const string sql = @"SELECT id, account_id, record_status FROM users WHERE mobile = @mobile";
                using var db = ConnectionPgSql;
                return await db.QueryFirstOrDefaultAsync<UserInfo>(sql, new { mobile });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving user by mobile: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateUserRecordStatusAsync(long userId, string recordStatus, long updatedBy)
        {
            try
            {
                const string sql = @"UPDATE users 
                                    SET record_status = @recordStatus, 
                                        updated_on = CURRENT_TIMESTAMP,
                                        updated_by = @updatedBy
                                    WHERE id = @userId";
                using var db = ConnectionPgSql;
                var rowsAffected = await db.ExecuteAsync(sql, new { userId, recordStatus, updatedBy });
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating user record status: {ex.Message}", ex);
            }
        }

        public class InsertAccountUserResult
        {
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
            public long? AccountId { get; set; }
        }

        public async Task<InsertAccountUserResult> InsertAccountAndUserForCscDeptAsync(
            string username, 
            string name, 
            string? mobile, 
            string? ssaCode, 
            string? circleCode,
            long roleId,
            string encryptedPassword,
            string defaultPassword,
            long createdByAccountId,
            string createdByMobile)
        {
            try
            {
                using var db = ConnectionPgSql;
                db.Open();
                var transaction = db.BeginTransaction();

                try
                {
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
                        role_id = roleId,
                        username = username,
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
                        staff_name = name,
                        mobile = mobile,
                        email = (string?)null,
                        hrno = 0L,
                        designation_code = (string?)null,
                        ssa_code = ssaCode,
                        record_status = "ACTIVE",
                        created_on = DateTime.UtcNow,
                        updated_on = DateTime.UtcNow,
                        updated_by = createdByAccountId,
                        changepassword = "0",
                        circle = circleCode
                    }, transaction);
                    
                    transaction.Commit();
                    return new InsertAccountUserResult { Success = true, AccountId = accountId };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new InsertAccountUserResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Failed to create account/user records: {ex.Message}" 
                    };
                }
            }
            catch (Exception ex)
            {
                return new InsertAccountUserResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Error creating account/user: {ex.Message}" 
                };
            }
        }

        public async Task<InsertAccountUserResult> InsertUserForExistingAccountAsync(
            long accountId,
            string name,
            string? mobile,
            string? ssaCode,
            string? circleCode,
            long createdByAccountId)
        {
            try
            {
                using var db = ConnectionPgSql;
                
                var userSql = @"INSERT INTO users 
                              (account_id, staff_name, mobile, email, hrno, designation_code, ssa_code, 
                               record_status, created_on, updated_on, updated_by, changepassword, circle)
                              VALUES 
                              (@account_id, @staff_name, @mobile, @email, @hrno, @designation_code, @ssa_code,
                               @record_status, @created_on, @updated_on, @updated_by, @changepassword, @circle)";
                
                await db.ExecuteAsync(userSql, new
                {
                    account_id = accountId,
                    staff_name = name,
                    mobile = mobile,
                    email = (string?)null,
                    hrno = 0L,
                    designation_code = (string?)null,
                    ssa_code = ssaCode,
                    record_status = "ACTIVE",
                    created_on = DateTime.UtcNow,
                    updated_on = DateTime.UtcNow,
                    updated_by = createdByAccountId,
                    changepassword = "0",
                    circle = circleCode
                });
                
                return new InsertAccountUserResult { Success = true, AccountId = accountId };
            }
            catch (Exception ex)
            {
                return new InsertAccountUserResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Error creating user: {ex.Message}" 
                };
            }
        }

    }
}


