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
                             dealercode, ref_dealer_id, master_dealer_id, parent_ctopno, dealer_status,
                             account_id, created_on, updated_on, data_source)
                            VALUES
                            (@username, @ctopupno, @name, @dealertype, @ssa_code, @csccode, @circle_code, @attached_to,
                             @contact_number, @pos_hno, @pos_street, @pos_landmark, @pos_locality, @pos_city, @pos_district,
                             @pos_state, @pos_pincode, @created_date, @pos_name_ss, @pos_owner_name, @pos_code, @pos_ctop,
                             @circle_name, @pos_unique_code, @latitude, @longitude, @aadhaar_no, @zone_code, @ctop_type,
                             @dealercode, @ref_dealer_id, @master_dealer_id, @parent_ctopno, @dealer_status,
                             @account_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, @data_source)");

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
                                     WHERE ctopupno = @ctopupno
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

        // Zone-specific CTOP search methods
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
                        sql = @"SELECT ctopupno, name, contact_number
                                FROM ctop_master_ez cme
                                WHERE cme.dealer_status = 'Active' 
                                  AND cme.dealertype = 'CSR'
                                  AND cme.ctopupno = cme.contact_number
                                  AND cme.ctopupno LIKE @pattern
                                  AND cme.ctopupno NOT IN (SELECT DISTINCT username FROM ctop_master)
                                ORDER BY cme.ctopupno
                                LIMIT 20";
                        break;
                    case "SZ":
                    case "SOUTH":
                        sql = @"SELECT ctopupno, name, contact_number
                                FROM ctop_master_sz cme
                                WHERE cme.dealer_status = 'Active' 
                                  AND cme.dealertype = 'CSR'
                                  AND cme.ctopupno = cme.contact_number
                                  AND cme.ctopupno LIKE @pattern
                                  AND cme.ctopupno NOT IN (SELECT DISTINCT username FROM ctop_master)
                                ORDER BY cme.ctopupno
                                LIMIT 20";
                        break;
                    case "NZ":
                    case "NORTH":
                        sql = @"SELECT ctopupno, name, contact_number
                                FROM ctop_master_nz cme
                                WHERE cme.dealer_status = 'Active' 
                                  AND cme.dealertype = 'DEPT'
                                  AND cme.ctopupno = cme.contact_number
                                  AND cme.ctopupno LIKE @pattern
                                  AND cme.ctopupno NOT IN (SELECT DISTINCT username FROM ctop_master)
                                ORDER BY cme.ctopupno
                                LIMIT 20";
                        break;
                    case "WZ":
                    case "WEST":
                        sql = @"SELECT ctopupno, name, contact_number
                                FROM ctop_master_wz cme
                                WHERE cme.dealer_status = 'Active' 
                                  AND cme.dealertype = 'CSR'
                                  AND cme.ctopupno = cme.contact_number
                                  AND cme.ctopupno LIKE @pattern
                                  AND cme.ctopupno NOT IN (SELECT DISTINCT username FROM ctop_master)
                                ORDER BY cme.ctopupno
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
                                       dealer_address, dealer_status, parent_ctopno as parent_ctop
                                FROM ctop_master_ez cme
                                WHERE cme.ctopupno = @ctopupno
                                  AND cme.dealer_status = 'Active'
                                  AND cme.dealertype = 'CSR'
                                  AND cme.ctopupno = cme.contact_number
                                  AND cme.ctopupno NOT IN (SELECT DISTINCT username FROM ctop_master)";
                        break;
                    case "SZ":
                    case "SOUTH":
                        sql = @"SELECT ctopupno, name, second_name, last_name, dealertype, 
                                       ssa_code, circle_code, csccode, attached_to, contact_number,
                                       dealer_address, dealer_status, parent_ctopno as parent_ctop,
                                       aadhaar_no, zone_code, ssa_city
                                FROM ctop_master_sz cme
                                WHERE cme.ctopupno = @ctopupno
                                  AND cme.dealer_status = 'Active'
                                  AND cme.dealertype = 'CSR'
                                  AND cme.ctopupno = cme.contact_number
                                  AND cme.ctopupno NOT IN (SELECT DISTINCT username FROM ctop_master)";
                        break;
                    case "NZ":
                    case "NORTH":
                        sql = @"SELECT ctopupno, name, second_name, last_name, dealertype, 
                                       ssa_code, circle_code, csccode, attached_to, contact_number,
                                       dealer_address, dealer_status, parent_ctop, ssa_city
                                FROM ctop_master_nz cme
                                WHERE cme.ctopupno = @ctopupno
                                  AND cme.dealer_status = 'Active'
                                  AND cme.dealertype = 'DEPT'
                                  AND cme.ctopupno = cme.contact_number
                                  AND cme.ctopupno NOT IN (SELECT DISTINCT username FROM ctop_master)";
                        break;
                    case "WZ":
                    case "WEST":
                        sql = @"SELECT ctopupno, name, second_name, last_name, dealertype, 
                                       ssa_code, circle_code, csccode, attached_to, contact_number,
                                       dealer_address, dealer_status, parent_ctop, ssa_city
                                FROM ctop_master_wz cme
                                WHERE cme.ctopupno = @ctopupno
                                  AND cme.dealer_status = 'Active'
                                  AND cme.dealertype = 'CSR'
                                  AND cme.ctopupno = cme.contact_number
                                  AND cme.ctopupno NOT IN (SELECT DISTINCT username FROM ctop_master)";
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
                    // First, get the current row to backup
                    const string selectSql = @"SELECT username, ctopupno, name, dealertype, ssa_code, csccode, circle_code, attached_to,
                                                      contact_number, pos_hno, pos_street, pos_landmark, pos_locality, pos_city,
                                                      pos_district, pos_state, pos_pincode, created_date, pos_name_ss, pos_owner_name,
                                                      pos_code, pos_ctop, circle_name, pos_unique_code, latitude, longitude,
                                                      aadhaar_no, zone_code, ctop_type, dealercode, ref_dealer_id, master_dealer_id,
                                                      parent_ctopno, dealer_status
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

                    // Backup the current row to backup table
                    const string backupSql = @"INSERT INTO ctop_master_pos_unique_code_backup 
                                                (username, ctopupno, name, dealertype, ssa_code, csccode, circle_code, attached_to,
                                                 contact_number, pos_hno, pos_street, pos_landmark, pos_locality, pos_city,
                                                 pos_district, pos_state, pos_pincode, created_date, pos_name_ss, pos_owner_name,
                                                 pos_code, pos_ctop, circle_name, pos_unique_code, latitude, longitude,
                                                 aadhaar_no, zone_code, ctop_type, updated_by_account_id, updated_at, new_pos_unique_code)
                                                VALUES 
                                                (@username, @ctopupno, @name, @dealertype, @ssa_code, @csccode, @circle_code, @attached_to,
                                                 @contact_number, @pos_hno, @pos_street, @pos_landmark, @pos_locality, @pos_city,
                                                 @pos_district, @pos_state, @pos_pincode, @created_date, @pos_name_ss, @pos_owner_name,
                                                 @pos_code, @pos_ctop, @circle_name, @pos_unique_code, @latitude, @longitude,
                                                 @aadhaar_no, @zone_code, @ctop_type, @updated_by_account_id, CURRENT_TIMESTAMP, @new_pos_unique_code)";
                    
                    await db.ExecuteAsync(backupSql, new
                    {
                        username = currentRow.username,
                        ctopupno = currentRow.ctopupno,
                        name = currentRow.name,
                        dealertype = currentRow.dealertype,
                        ssa_code = currentRow.ssa_code,
                        csccode = currentRow.csccode,
                        circle_code = currentRow.circle_code,
                        attached_to = currentRow.attached_to,
                        contact_number = currentRow.contact_number,
                        pos_hno = currentRow.pos_hno,
                        pos_street = currentRow.pos_street,
                        pos_landmark = currentRow.pos_landmark,
                        pos_locality = currentRow.pos_locality,
                        pos_city = currentRow.pos_city,
                        pos_district = currentRow.pos_district,
                        pos_state = currentRow.pos_state,
                        pos_pincode = currentRow.pos_pincode,
                        created_date = currentRow.created_date,
                        pos_name_ss = currentRow.pos_name_ss,
                        pos_owner_name = currentRow.pos_owner_name,
                        pos_code = currentRow.pos_code,
                        pos_ctop = currentRow.pos_ctop,
                        circle_name = currentRow.circle_name,
                        pos_unique_code = currentRow.pos_unique_code, // OLD value
                        latitude = currentRow.latitude,
                        longitude = currentRow.longitude,
                        aadhaar_no = currentRow.aadhaar_no,
                        zone_code = currentRow.zone_code,
                        ctop_type = currentRow.ctop_type,
                        updated_by_account_id = updatedByAccountId,
                        new_pos_unique_code = newPosUniqueCode
                    }, transaction);

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

                    // Log to audit table - get updated row
                    var updatedRow = await db.QueryFirstOrDefaultAsync<CtopMaster>(
                        @"SELECT username, ctopupno, name, dealertype, ssa_code, csccode, circle_code, attached_to,
                                 contact_number, pos_hno, pos_street, pos_landmark, pos_locality, pos_city,
                                 pos_district, pos_state, pos_pincode, created_date, pos_name_ss, pos_owner_name,
                                 pos_code, pos_ctop, circle_name, pos_unique_code, latitude, longitude,
                                 aadhaar_no, zone_code, ctop_type, dealercode, ref_dealer_id, master_dealer_id,
                                 parent_ctopno, dealer_status
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

    }
}


