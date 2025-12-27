using Dapper;
using cos.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Linq;
using System.Text;
using CosApp.PyroUsim;


namespace cos.Repositories
{
    public class UserDkycDashRepository
    {
        private readonly string connectionStringPgSql;

        public UserDkycDashRepository(IConfiguration configuration)
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

        public async Task<InventoryVM> GetInventory(string customer, string circle, string role,string ssa)
        {

            InventoryVM inventories = new InventoryVM();

            StringBuilder query = new StringBuilder();

            if (role == "cc_admin")
            {
                query.Append("select count(*) total_kyc,COUNT(CASE WHEN verified_flag  = 'Y' then 1 ELSE NULL END) comp_kyc,");
                query.Append("COUNT(CASE WHEN verified_flag  = 'F' then 1 ELSE NULL END) seelater , COUNT(CASE WHEN verified_flag  = 'R' then 1 ELSE NULL END) rejected ");;
                query.Append(" from cos_bcd_dkyc cb   ");
            }
            else if (role == "circle_admin")
            {
                query.Append("select count(*) total_kyc,COUNT(CASE WHEN verified_flag  = 'Y' then 1 ELSE NULL END) comp_kyc,");
                query.Append("COUNT(CASE WHEN verified_flag  = 'F' then 1 ELSE NULL END) seelater , COUNT(CASE WHEN verified_flag  = 'R' then 1 ELSE NULL END) rejected ");
                query.Append(" from cos_bcd_dkyc cb where circle_code = @param_circle ::int ");
            }
            else
            {
                query.Append("select count(*) total_kyc,COUNT(CASE WHEN verified_flag  = 'Y' then 1 ELSE NULL END) comp_kyc,");
                query.Append("COUNT(CASE WHEN verified_flag  = 'F' then 1 ELSE NULL END) seelater , COUNT(CASE WHEN verified_flag  = 'R' then 1 ELSE NULL END) rejected ");
                query.Append(" from cos_bcd_dkyc cb where circle_code = @param_circle ::int  and ssa_code = @oa ");
            }


                using (IDbConnection dbConnection = ConnectionPgSql)
                {
                    dbConnection.Open();

                    try
                    {
                        var result = await dbConnection.QueryAsync<InventoryVM>(query.ToString(), new
                        {
                            param_customer = customer,
                            param_circle = circle,
                            oa = ssa
                        });

                        return result.FirstOrDefault();

                    }
                    catch (Exception)
                    {
                        return inventories;
                    }


                }

        }


        public async Task<List<SimSummaryVM>> GetkycSummary(string circle, string role, string ssa)
        {

            List<SimSummaryVM> simSummary = new List<SimSummaryVM>();

            StringBuilder query = new StringBuilder();

            if (role == "cc_admin")
            {
                query.Append("select count(*) total_kyc,COUNT(CASE WHEN verified_flag  = 'Y' then 1 ELSE NULL END) comp_kyc,");
                query.Append("COUNT(CASE WHEN verified_flag  = 'F' then 1 ELSE NULL END) seelater , COUNT(CASE WHEN verified_flag  = 'R' then 1 ELSE NULL END) rejected ");
                query.Append(" from cos_bcd_dkyc cb   ");
            }
            else if (role == "circle_admin")
            {
                query.Append("select count(*) total_kyc,COUNT(CASE WHEN verified_flag  = 'Y' then 1 ELSE NULL END) comp_kyc,");
                query.Append("COUNT(CASE WHEN verified_flag  = 'F' then 1 ELSE NULL END) seelater ,  COUNT(CASE WHEN verified_flag  = 'R' then 1 ELSE NULL END) rejected ");
                query.Append(" from cos_bcd_dkyc cb where circle_code = @param_circle ::int ");
            }
            else
            {
                query.Append("select count(*) total_kyc,COUNT(CASE WHEN verified_flag  = 'Y' then 1 ELSE NULL END) comp_kyc,");
                query.Append("COUNT(CASE WHEN verified_flag  = 'F' then 1 ELSE NULL END) seelater ,  COUNT(CASE WHEN verified_flag  = 'R' then 1 ELSE NULL END) rejected ");
                query.Append(" from cos_bcd_dkyc cb where circle_code = @param_circle ::int and ssa_code = @oa ");
            }

            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();

                try
                {
                    var result = await dbConnection.QueryAsync<SimSummaryVM>(query.ToString(), new { circle = circle, oa = ssa });

                    simSummary = result.AsList();
                    return simSummary;
                }
                catch (Exception)
                {
                    return simSummary;
                }


            }

        }


    //DKYC Functions find pending activation requests
            public async Task<List<kycVM>> GetDkycdetails(string circle, string role, string ssa, string cafType)
            {
                List<kycVM> kycdetails = new List<kycVM>();

                StringBuilder query = new StringBuilder();



           query.Append(@"SELECT circle_code AS circle,
                      sim_type AS caftype,
                      subscriber_name AS name,
                     cust_mob_no AS gsmnumber,
                      sim_no As simnumber,caf_no  AS cafslno,
                      ssa_code,alt_mobile_number AS alternate_contact_no,de_username,
                      de_csccode,TO_CHAR(created_at, 'DD-MM-YYYY') AS live_photo_date
               FROM cos_bcd_dkyc
               WHERE (verified_flag IS NULL OR verified_flag = '') and sim_type <> 'simswap' ");

if (role != "cc_admin")
{
    query.Append(" AND circle_code = @circle");
}
            if (role == "ba_admin")
            {
                query.Append(" AND ssa_code = @ssa ");
            }

            if (!string.IsNullOrEmpty(cafType) && cafType != "ALL")
{
    query.Append(" AND caf_type = @cafType ");
}

                using (IDbConnection dbConnection = ConnectionPgSql)
                {
                    dbConnection.Open();

                    try
                    {
                        var result = await dbConnection.QueryAsync<kycVM>(
                            query.ToString(),
                            new { circle = circle, cafType = cafType, ssa = ssa} // IMPORTANT FIX
                        );
//circle = Convert.ToInt32(circle)
                        kycdetails = result.AsList();
                        return kycdetails;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return kycdetails;
                    }
                }
        }

 public async Task<bool> DkycUpdateCafFlagAsync(string gsm, string remark, string flag, string loggedin)
        {
            string sql = @"
        UPDATE cos_bcd_dkyc
        SET verified_flag = @Flag,
            rejection_reason = @Remark,
            verified_by = @Loggedin,
            verified_date = NOW()
        WHERE gsmnumber = @Gsmnumber and verified_flag is null;
    ";

            using (IDbConnection db = ConnectionPgSql)
            {
                db.Open();
                int rows = await db.ExecuteAsync(sql, new
                {
                    Gsmnumber = gsm,
                    Remark = remark,
                    Flag = flag,
                    Loggedin = loggedin
                });

                return rows > 0;
            }
        }
//End DKYC Functions





























        //find pending activation requests
        public async Task<List<kycVM>> Getkycdetails(string circle, string role, string ssa, string cafType)
        {
            List<kycVM> kycdetails = new List<kycVM>();

            StringBuilder query = new StringBuilder();



           query.Append(@"SELECT circle_code AS circle,
                      caf_type AS caftype,
                      name,
                      gsmnumber,
                      simnumber,caf_no AS cafslno,
                      ssa_code,alternate_contact_no,de_username,
                      de_csccode,TO_CHAR(live_photo_time, 'DD-MM-YYYY') AS live_photo_date
               FROM cos_bcd_dkyc
               WHERE (verified_flag IS NULL OR verified_flag = '') and caf_type <> 'simswap' ");

if (role != "cc_admin")
{
    query.Append(" AND circle_code = @circle::int ");
}
            if (role == "ba_admin")
            {
                query.Append(" AND ssa_code = @ssa ");
            }

            if (!string.IsNullOrEmpty(cafType) && cafType != "ALL")
{
    query.Append(" AND caf_type = @cafType ");
}
                using (IDbConnection dbConnection = ConnectionPgSql)
                {
                    dbConnection.Open();

                    try
                    {
                        var result = await dbConnection.QueryAsync<kycVM>(
                            query.ToString(),
                            new { circle = Convert.ToInt32(circle), cafType = cafType, ssa = ssa} // IMPORTANT FIX
                        );

                        kycdetails = result.AsList();
                        return kycdetails;
                    }
                    catch (Exception)
                    {
                        return kycdetails;
                    }
                }
        }


        //find pending activation requests sim swap
        public async Task<List<kycVM>> GetkycdetailsSwap(string circle, string role, string ssa)
        {
            List<kycVM> kycdetails = new List<kycVM>();

            StringBuilder query = new StringBuilder();

            //condition s removed to '' blank

            query.Append(@"SELECT circle_code AS circle,
                      caf_type AS caftype,
                      name,
                      gsmnumber,
                      simnumber,
                      caf_no AS cafslno
               FROM cos_bcd_dkyc
               WHERE (verified_flag IS NULL OR verified_flag = '') and caf_type = 'simswap' ");

            if (role != "cc_admin")
            {
                query.Append(" AND circle_code = @circle::int ");
            }
            if (role == "ba_admin")
            {
                query.Append(" AND ssa_code = @ssa ");
            }

          
            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();

                try
                {
                    var result = await dbConnection.QueryAsync<kycVM>(
                        query.ToString(),
                        new { circle = Convert.ToInt32(circle),  ssa = ssa } 
                    );

                    kycdetails = result.AsList();
                    return kycdetails;
                }
                catch (Exception)
                {
                    return kycdetails;
                }
            }
        }


        //show status of activations
        public async Task<List<kycstatusVM>> Getkycstatusdetails(string circle, string role, string ssa)
        {

            List<kycstatusVM> kycststusdetails = new List<kycstatusVM>();

            StringBuilder query = new StringBuilder();

            if (role == "cc_admin")
            {
                query.Append("select circle_code circle ,name,gsmnumber,simnumber,caf_no cafslno,verified_flag status,to_char(verified_date, 'DD-MM-YY HH24:MI:SS') as verified_date,verified_by  ");
                query.Append("from cos_bcd_dkyc where  verified_flag = 'Y'");
            }
            else if (role == "circle_admin")
            {
                query.Append("select circle_code circle ,name,gsmnumber,simnumber,caf_no cafslno,verified_flag status,to_char(verified_date, 'DD-MM-YY HH24:MI:SS') as verified_date,verified_by  ");
                query.Append("from cos_bcd_dkyc where  verified_flag = 'Y' AND circle_code = @circle::int ");
            }
            else
            {
                query.Append("select circle_code circle ,name,gsmnumber,simnumber,caf_no cafslno,verified_flag status,to_char(verified_date, 'DD-MM-YY HH24:MI:SS') as verified_date,verified_by  ");
                query.Append("from cos_bcd_dkyc where  verified_flag = 'Y' AND circle_code = @circle::int and ssa_code = @oa");
            }

                using (IDbConnection dbConnection = ConnectionPgSql)
                {
                    dbConnection.Open();

                    try
                    {
                        var result = await dbConnection.QueryAsync<kycstatusVM>(query.ToString(), new { circle = circle, oa = ssa });

                        kycststusdetails = result.AsList();
                        return kycststusdetails;
                    }
                    catch (Exception)
                    {
                        return kycststusdetails;
                    }


                }

        }


        //show status of see later
        public async Task<List<kycstatusVM>> Getkycstatusseelater(string circle, string role, string ssa)
        {

            List<kycstatusVM> kycststusdetails = new List<kycstatusVM>();

            StringBuilder query = new StringBuilder();

            if (role == "cc_admin")
            {
                query.Append("select circle_code circle ,name,gsmnumber,simnumber,caf_no cafslno,verified_flag status,to_char(verified_date, 'DD-MM-YY HH24:MI:SS') as verified_date,verified_by,rejection_reason reason ");
                query.Append("from cos_bcd_dkyc where  verified_flag = 'F' ");
            }
            else if (role == "circle_admin")
            {
                query.Append("select circle_code circle ,name,gsmnumber,simnumber,caf_no cafslno,verified_flag status, to_char(verified_date, 'DD-MM-YY HH24:MI:SS') as verified_date,verified_by,rejection_reason reason ");
                query.Append("from cos_bcd_dkyc where  verified_flag = 'F' AND circle_code = @circle::int");
            }
            else
            {
                query.Append("select circle_code circle ,name,gsmnumber,simnumber,caf_no cafslno,verified_flag status,to_char(verified_date, 'DD-MM-YY HH24:MI:SS') as verified_date,verified_by,rejection_reason reason ");
                query.Append("from cos_bcd_dkyc where  verified_flag = 'F' AND circle_code = @circle::int and ssa_code = @oa");
            }

            using (IDbConnection dbConnection = ConnectionPgSql)
                {
                    dbConnection.Open();

                    try
                    {
                        var result = await dbConnection.QueryAsync<kycstatusVM>(query.ToString(), new { circle = circle, oa = ssa });

                        kycststusdetails = result.AsList();
                        return kycststusdetails;
                    }
                    catch (Exception)
                    {
                        return kycststusdetails;
                    }


                }

        }

        //show status of see later
        public async Task<List<kycstatusVM>> Getkycstatusrejected(string circle, string role, string ssa)
        {

            List<kycstatusVM> kycststusdetails = new List<kycstatusVM>();

            StringBuilder query = new StringBuilder();

            if (role == "cc_admin")
            {
                query.Append("select circle_code circle ,name,gsmnumber,simnumber,caf_no cafslno,verified_flag status,to_char(verified_date, 'DD-MM-YY HH24:MI:SS') as verified_date,verified_by,rejection_reason reason ");
                query.Append("from cos_bcd_dkyc where  verified_flag = 'R'");
            }
            else if (role == "circle_admin")
            {

                query.Append("select circle_code circle ,name,gsmnumber,simnumber,caf_no cafslno,verified_flag status,to_char(verified_date, 'DD-MM-YY HH24:MI:SS') as verified_date,verified_by,rejection_reason reason ");
                query.Append("from cos_bcd_dkyc where  verified_flag = 'R' AND circle_code = @circle::int");
            }
            else
            {
                query.Append("select circle_code circle ,name,gsmnumber,simnumber,caf_no cafslno,verified_flag status,to_char(verified_date, 'DD-MM-YY HH24:MI:SS') as verified_date,verified_by,rejection_reason reason ");
                query.Append("from cos_bcd_dkyc where  verified_flag = 'R' AND circle_code = @circle::int and ssa_code = @oa");

            }

                using (IDbConnection dbConnection = ConnectionPgSql)
                {
                    dbConnection.Open();

                    try
                    {
                        var result = await dbConnection.QueryAsync<kycstatusVM>(query.ToString(), new { circle = circle, oa = ssa });

                        kycststusdetails = result.AsList();
                        return kycststusdetails;
                    }
                    catch (Exception)
                    {
                        return kycststusdetails;
                    }


                }

        }
        public async Task<bool> UpdateSACustomerIdAsync(string gsmnumber)
        {
            string query = @"
        UPDATE cos_bcd_dkyc
        SET verified_flag = 'Y' 
        WHERE gsmnumber = @gsmnumber;
    ";

            using (IDbConnection dbConnection = ConnectionPgSql)
            {
                dbConnection.Open();
                try
                {
                    int rowsAffected = await dbConnection.ExecuteAsync(
                        query,
                        new { gsmnumber } // parameter name must match SQL
                    );

                    return rowsAffected > 0;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }



        public async Task<CafModelDkyc> GetCAFDataAsync(string cafslno)
        {
            string sql = @"SELECT
   id, 
   de_csccode , 
   de_username, 
   circle_code, 
   ssa_code, caf_no, 
   connection_type, sim_type, 
   subscriber_type, nationality, gsmnumber, sim_no, cust_mob_no,
    customer_otp, customer_otp_time, customer_otp_relation, 
    tariff_plan, parent_ctopup_number, mpin, subscriber_name, 
    pwd_status, father_husband_name, gender, 
    date_of_birth  AS date_of_birth, 
    jio_count, airtel_count, vi_count, bsnl_count, other_count, corr_relation_type, 
    corr_relation_name, corr_house_details, corr_street_address, corr_landmark, 
    corr_area_locality, corr_city, corr_district, corr_state_ut, corr_pin_code, 
    perm_relation_type, perm_relation_name, perm_house_details, perm_street_address, 
    perm_landmark, perm_area_locality, perm_city, 
    perm_district, perm_state_ut, perm_pin_code, value_added_services, email_address, 
    profession, pan_gir, alt_home_number, alt_business_number, alt_mobile_number, 
    poi_type, poi_number, poi_date_of_issue, poi_place_of_issue, poi_issue_authority, 
    poa_type, poa_number, poa_date_of_issue, poa_place_of_issue, poa_issue_authority, 
    pos_declaration_check, declaration1, declaration2, declaration3, poi_document_front, 
    poi_document_back, poa_document_front, poa_document_back, customer_photo,
     pos_photo, pos_otp, pos_otp_gen_time, pos_otp_ver_time, 
     pos_lat, pos_lang, cust_lat, cust_lang, upc_code, upcvalidupto, 
     prev_optr, prev_optr_area, payment_mode, std_isd, deposit_required, 
     no_deposit_reason, payment_method, amount_received, postpaid_plan_name, 
     outstation_ref_name, outstation_mob_no, outstation_address, device_ip, device_mac,
      verified_flag, verified_by, verified_date, app_version, rejection_reason, created_at
FROM cos_bcd_dkyc
WHERE caf_no = @cafslno;";

            using (IDbConnection db = ConnectionPgSql)
            {
                db.Open();

                // Load base CAF data
                var model = await db.QueryFirstOrDefaultAsync<CafModelDkyc>(sql, new { cafslno });

                
                if (model == null)
                    return null;

                // Load POS Master Info
                if (!string.IsNullOrEmpty(model.de_username))
                {
                    string posSql = @"
                SELECT 
                    pos_unique_code AS Pos_Code,
                    name AS Pos_Sale_Name,
                    pos_hno AS Pos_Hno,
                    pos_street AS Pos_Street,
                    pos_landmark AS Pos_Landmark,
                    pos_locality AS Pos_Locality,
                    pos_city AS Pos_City,
                    pos_district AS Pos_District,
                    pos_state AS Pos_State,
                    pos_pincode AS Pos_Pincode,
                    ssa_code
                FROM ctop_master
                WHERE username = @username
                LIMIT 1;";

                    var posData = await db.QueryFirstOrDefaultAsync(posSql,
                                    new { username = model.de_username });

                    if (posData != null)
                    {
                        model.Pos_Code = posData.pos_code;
                        model.Pos_Sale_Name = posData.pos_sale_name;
                        model.Pos_Hno = posData.pos_hno;
                        model.Pos_Street = posData.pos_street;
                        model.Pos_Landmark = posData.pos_landmark;
                        model.Pos_Locality = posData.pos_locality;
                        model.Pos_City = posData.pos_city;
                        model.Pos_District = posData.pos_district;
                        model.Pos_State = posData.pos_state;
                        model.Pos_Pincode = posData.pos_pincode;
                        model.ssa_code = posData.ssa_code;
                    }
                }

                // Fetch IMSI + PLAN_CODE from simprepaid using SIMNUMBER //postpaid and prepaid table selection handled here
                try
                {
                    if (!string.IsNullOrEmpty(model.Simnumber))
                    {
                        string simSql;
                        if (model.connection_type == "1")
                        {
                            simSql = @"
                    SELECT 
                        imsi::text AS imsi,
                        plan_code 
                    FROM simprepaid_sold
                    WHERE simno = @simno
                    LIMIT 1;";
                        }
                        else
                        {
                            simSql = @"
                    SELECT 
                        imsi::text AS imsi,
                        plan_code 
                    FROM simpostpaid_sold
                    WHERE simno = @simno
                    LIMIT 1;";
                        }

                            var simData = await db.QueryFirstOrDefaultAsync(simSql,
                                            new { simno = model.Simnumber });

                        if (simData != null)
                        {
                            model.sim_no = simData.imsi; // override with actual IMSI
                            model.Plan_Code = simData.plan_code?.ToString();
                        }
                        else
                        {
                            Console.WriteLine($"No simprepaid record found for SIMNO: {model.Simnumber}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error fetching IMSI/plan_code: " + ex.Message);
                }



                return model;
            }
        }


        //save cos bcd after oracle insertion
        public async Task<bool> SaveCAFEditableFieldsAsync(CafModelDkyc model, string loggedin,string userid)
        {
            bool status = false;

            //        string sql = @"
            //    UPDATE cos_bcd
            //    SET local_ref_name = @local_ref_name,
            //        local_ref_contact = @Local_Ref_Contact,
            //        verified_flag = 'Y',
            //        verified_date = now(),
            //        verified_by = @loggedin
            //    WHERE gsmnumber = @Gsmnumber and verified_flag is null;
            //";

            string sql = @"
        UPDATE cos_bcd_dkyc
        SET f_h_name = @F_H_Name,
            other_connection_det = @Distinct_Operators,
            local_ref_name = @local_ref_name,
            local_ref_contact = @Local_Ref_Contact,
            verified_flag = 'Y',
            verified_date = NOW(),
            verified_by = @loggedin,
            ins_usr = @userid,
            in_process = false
        WHERE gsmnumber = @Gsmnumber
          AND verified_flag IS NULL
          AND in_process = true
          AND process_by = @loggedin;
    ";

            using (IDbConnection db = ConnectionPgSql)
            {
                db.Open();

                try
                {
                    int rows = await db.ExecuteAsync(sql, new
                    {
                        model.father_husband_name,
                        model.prev_optr,
                        model.outstation_ref_name,
                        model.outstation_mob_no,
                        model.gsm_number,
                        loggedin,
                        userid
                    });

                    status = rows > 0;
                    return status;
                }
                catch (Exception ex)
                {
                    // OPTIONAL: Log error (recommended)
                    Console.WriteLine("ERROR-SaveCAFEditableFieldsAsync: " + ex.Message);
                    return status; // false
                }
            }
        }

        //save cos bcd after oracle insertion for swap cases
        public async Task<bool> SaveCAFEditableFieldsSwapAsync(CafSwapModel model, string loggedin, string userid)
        {
            bool status = false;

            //        string sql = @"
            //    UPDATE cos_bcd
            //    SET local_ref_name = @local_ref_name,
            //        local_ref_contact = @Local_Ref_Contact,
            //        verified_flag = 'Y',
            //        verified_date = now(),
            //        verified_by = @loggedin
            //    WHERE gsmnumber = @Gsmnumber and verified_flag is null;
            //";

            string sql = @"
                UPDATE cos_bcd_dkyc
                SET f_h_name = @F_H_Name,
                    other_connection_det = @Distinct_Operators,
                    local_ref_name = @local_ref_name,
                    local_ref_contact = @Local_Ref_Contact,
                    verified_flag = 'Y',
                    verified_date = NOW(),
                    verified_by = @loggedin,
                    ins_usr = @userid,
                    in_process = false
                WHERE gsmnumber = @Gsmnumber
                  AND verified_flag IS NULL
                  AND in_process = true
                  AND process_by = @loggedin;
            ";


            //string sql = @"
            //UPDATE cos_bcd
            //SET local_ref_name = @local_ref_name,
            //    local_ref_contact = @Local_Ref_Contact,
            //    verified_flag = 'Y',
            //    verified_date = NOW(),
            //    verified_by = @loggedin,
            //    ins_usr = @userid,
            //    in_process = false
            //WHERE gsmnumber = @Gsmnumber
            //  AND verified_flag = 'S'
            //  AND in_process = true
            //  AND process_by = @loggedin;";

            using (IDbConnection db = ConnectionPgSql)
            {
                db.Open();

                try
                {
                    int rows = await db.ExecuteAsync(sql, new
                    {
                        model.F_H_Name,
                        model.Distinct_Operators,
                        model.local_ref_name,
                        model.Local_Ref_Contact,
                        model.Gsmnumber,
                        loggedin,
                        userid
                    });

                    status = rows > 0;
                    return status;
                }
                catch (Exception ex)
                {
                    // OPTIONAL: Log error (recommended)
                    Console.WriteLine("ERROR -SaveCAFEditableFieldsAsync: " + ex.Message);
                    return status; // false
                }
            }
        }

        public async Task<bool> RejectCAFAsync(string gsm, string reason, string loggedin,string userid)
{
            //string sql = @"
            //    UPDATE cos_bcd
            //    SET verified_flag = 'R',
            //        rejection_reason = @Reason,
            //        verified_by = @loggedin,
            //        verified_date = NOW()
            //    WHERE gsmnumber = @Gsmnumber
            //      AND verified_flag IS NULL
            //  ";


            string sql = @"
        UPDATE cos_bcd_dkyc
        SET verified_flag = 'R',
            rejection_reason = @Reason,
            verified_by = @loggedin,
            verified_date = NOW(),
            in_process = false,
            ins_usr = @userid,
            process_by = @loggedin,
            process_at = NOW()
        WHERE gsmnumber = @Gsmnumber
          AND verified_flag IS NULL
          AND (in_process = false OR in_process IS NULL);
    ";



            using var db = ConnectionPgSql;
    return await db.ExecuteAsync(sql, new { Gsmnumber = gsm, Reason = reason, loggedin , userid }) == 1;
}


        //see later
        public async Task<bool> SeelaterCAFAsync(string gsm, string remark, string loggedin,string userid)
        {
            string sql = @"
        UPDATE cos_bcd_dkyc
        SET verified_flag = 'F',
            rejection_reason = @Reason,
            ins_usr = @userid,
            verified_by = @loggedin,
            verified_date = NOW()
        WHERE gsmnumber = @Gsmnumber and verified_flag is null;
    ";

            using (IDbConnection db = ConnectionPgSql)
            {
                db.Open();
                try
                {
                    int rows = await db.ExecuteAsync(sql, new { Gsmnumber = gsm, Reason = remark, loggedin ,userid});
                    return rows > 0;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }


        public async Task<bool> UpdateCafFlagAsync(string gsm, string remark, string flag, string loggedin)
        {
            string sql = @"
        UPDATE cos_bcd_dkyc
        SET verified_flag = @Flag,
            rejection_reason = @Remark,
            verified_by = @Loggedin,
            verified_date = NOW()
        WHERE gsmnumber = @Gsmnumber and verified_flag is null;
    ";

            using (IDbConnection db = ConnectionPgSql)
            {
                db.Open();
                int rows = await db.ExecuteAsync(sql, new
                {
                    Gsmnumber = gsm,
                    Remark = remark,
                    Flag = flag,
                    Loggedin = loggedin
                });

                return rows > 0;
            }
        }


        ///get act_type from cmfmaster
        public async Task<CmfMasterModel> GetCmfMasterByCircleAsync(int circle,string Connection_Type, string act_type)
        {


            string sql = @"
        SELECT circle, circle_code, act_type, connection_type, msisdn_type, sim_type,
               cmf_mkt_code, zone_element_id, cmf_vip_code, cmf_acct_seg_id,
               cmf_exrate_class, cmf_rate_class_default, cmf_bill_disp_meth,
               cmf_bill_fmt_opt, invs_saleschannel_id, emf_config_id,
               zone_id, cmf_bill_period,activation_status,
               category_code
        FROM cmfmaster_sz
        WHERE circle_code = @circle and act_type = UPPER(@act_type) and connection_type = @ Connection_Type
        LIMIT 1;
    ";

            using (var db = ConnectionPgSql)
            {
                db.Open();

                try
                {
                    var result = await db.QueryFirstOrDefaultAsync<CmfMasterModel>(sql, new { circle, act_type , Connection_Type });
                    return result;
                }
                catch (Exception ex)
                {
                    // OPTIONAL: Log the error
                    Console.WriteLine("ERROR-GetCmfMasterByCircleAsync: " + ex.Message);
                    return null;  // return null on failure
                }
            }
        }



        public async Task<string?> GetPrintServiceCenterIdAsync(int circleCode, string ssaCode)
        {
            string query = @"
        SELECT print_service_center_id 
        FROM print_service 
        WHERE circle_code = @circle 
          AND ssa_code = upper(@ssa)
        LIMIT 1;
    ";

            using (var con = new NpgsqlConnection(connectionStringPgSql))
            {
                await con.OpenAsync();

                try
                {
                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@circle", circleCode);
                        cmd.Parameters.AddWithValue("@ssa", ssaCode);

                        var result = await cmd.ExecuteScalarAsync();
                        return result?.ToString();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR-GetPrintServiceCenterIdAsync: " + ex.Message);
                    return null; // Return null if error
                }
            }
        }


        //    public async Task<string?> GetInPlanIdAsync(string planCode, int circleCode)
        //    {
        //        string sql = @"
        //    SELECT TCOSP_ID 
        //    FROM in_plan_details 
        //    WHERE ss_plan_code = @plan 
        //      AND circle_code = @circle
        //    LIMIT 1;
        //";

        //        using (var con = new NpgsqlConnection(connectionStringPgSql))
        //        {
        //            await con.OpenAsync();

        //            try
        //            {
        //                using (var cmd = new NpgsqlCommand(sql, con))
        //                {
        //                    cmd.Parameters.AddWithValue("@plan", planCode);
        //                    cmd.Parameters.AddWithValue("@circle", circleCode);

        //                    var result = await cmd.ExecuteScalarAsync();
        //                    return result?.ToString();
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine("ERROR-GetInPlanIdAsync: " + ex.Message);
        //                return null;
        //            }
        //        }
        //    }

        public async Task<InPlanDetails?> GetInPlanDetailsAsync(string planCode, int circleCode)
        {
            string sql = @"
        SELECT 
            TCOSP_ID,
            sim_state       AS simstate,
            prim_balance    AS primary_talk_value
        FROM in_plan_details 
        WHERE ss_plan_code = @plan 
          AND circle_code = @circle
        LIMIT 1;
    ";

            using (var con = new NpgsqlConnection(connectionStringPgSql))
            {
                await con.OpenAsync();

                try
                {
                    using (var cmd = new NpgsqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@plan", planCode);
                        cmd.Parameters.AddWithValue("@circle", circleCode);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new InPlanDetails
                                {
                                    InPlanId = reader["tcosp_id"]?.ToString(),
                                    SimState = reader["simstate"]?.ToString(),
                                    PrimaryTalkValue = reader["primary_talk_value"] == DBNull.Value
                                        ? null
                                        : Convert.ToDecimal(reader["primary_talk_value"])
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR-GetInPlanDetailsAsync: " + ex.Message);
                }
            }

            return null;
        }



        //lock method
        public async Task<bool> LockCAFAsync(string cafSerialNo, string gsm, string loggedin)
        {
            string sql = @"
        UPDATE cos_bcd_dkyc
        SET in_process = true,
            process_by = @loggedin,
            process_at = NOW()
        WHERE TRIM(gsmnumber) = @GsmNumber
          AND TRIM(caf_no) = @cafNo
          AND verified_flag IS NULL
          AND (in_process = false OR in_process IS NULL);
    ";

            try
            {
                using var db = ConnectionPgSql;

                gsm = gsm?.Trim();
                cafSerialNo = cafSerialNo?.Trim();
                loggedin = loggedin?.Trim();

                var param = new DynamicParameters();
                param.Add("GsmNumber", gsm, DbType.String);
                param.Add("cafNo", cafSerialNo, DbType.String);
                param.Add("loggedin", loggedin, DbType.String);

                int rows = await db.ExecuteAsync(sql, param);
                return rows == 1;
            }
            catch (Exception ex)
            {
                // log ex
                return false;
            }
        }




        //unlock method
        public async Task ReleaseLockAsync(string cafSerialNo,string gsm, string loggedin)
        {
            string sql = @"
        UPDATE cos_bcd_dkyc
        SET in_process = false
        WHERE gsmnumber = @Gsmnumber and caf_no = @cafSerialNo 
          AND process_by = @loggedin;
    ";

            using var db = ConnectionPgSql;
            await db.ExecuteAsync(sql, new { Gsmnumber = gsm, loggedin , cafSerialNo });
        }

        //get simswap caf data model
//         public async Task<CafSwapModel> GetCAFDataSwapAsync(string cafslno)
//         {
//             string sql = @"SELECT
//     de_username ,
//     gsmnumber as Gsmnumber,
//     pwd as Pwd,
//     caf_no as Caf_No,
//     connection_type as Connection_Type,
//     name as Name,
//     middle_name as Middle_Name,
//     last_name as Last_Name,
//     f_h_name as F_H_Name,
//     gender as Gender,
//     date_of_birth as Date_Of_Birth,
//     customer_type as Customer_Type,
//     perm_addr_hno as Perm_Addr_Hno,
//     perm_addr_street as Perm_Addr_Street,
//     perm_addr_locality as Perm_Addr_Locality,
//     perm_addr_city as Perm_Addr_City,
//     perm_addr_state as Perm_Addr_State,
//     perm_addr_pin as Perm_Addr_Pin,
//     local_addr_hno as Local_Addr_Hno,
//     local_addr_street as Local_Addr_Street,
//     local_addr_locality as Local_Addr_Locality,
//     local_addr_city as Local_Addr_City,
//     local_addr_state as Local_Addr_State,
//     local_addr_pin as Local_Addr_Pin,
//     nationality as Nationality,
//     other_connection_det as Distinct_Operators,
//     simstate as Simstate,
//     email as Email,
//     alternate_contact_no as Alternate_Contact_No,
//     profession as Profession,
//     subscriber_type ,ref_careof_address,
//     local_ref_name,
//     local_ref as Local_Ref,
//     local_ref_contact as Local_Ref_Contact,ref_otp ,ref_otp_time ,
//     upc_code as Upc_Code,
//     prev_optr as Prev_Optr,
//     paymenttype as PaymentType,
//     bank_details as Bank_Details,
//     imsi as Imsi,
//     stdpco as Stdpco,
//     services as Services,
//     activation_csccode as Activation_CscCode,
//     photo,
//     unq_resp_code_pos as Pos_Unique_Response_Code,
//     unq_resp_date_pos Pos_Unique_Response_Code_date,
//     unq_resp_code_cust Customer_Unique_Response_Code,
//     unq_resp_date_cust Customer_Unique_Response_Code_date,
//     live_photo_time as LivePhotoTime,
//     pos_adh_name Pos_Agent_Name,
//     circle_code as Circle_Code,
//     simnumber as Simnumber,
//     de_csccode,
//     caf_type,
//     father_name_adh,latitude ,longitude ,frc_ctopup_number_mpin mpin 
// FROM cos_bcd_dkyc
// WHERE caf_no = @cafslno;";

//             using (IDbConnection db = ConnectionPgSql)
//             {
//                 db.Open();

//                 // Load base CAF data
//                 var model = await db.QueryFirstOrDefaultAsync<CafSwapModelDkyc>(sql, new { cafslno });

//                 if (model == null)
//                     return null;

//                 // Load POS Master Info
//                 if (!string.IsNullOrEmpty(model.De_Username))
//                 {
//                     string posSql = @"
//                 SELECT 
//                     pos_unique_code AS Pos_Code,
//                     name AS Pos_Sale_Name,
//                     pos_hno AS Pos_Hno,
//                     pos_street AS Pos_Street,
//                     pos_landmark AS Pos_Landmark,
//                     pos_locality AS Pos_Locality,
//                     pos_city AS Pos_City,
//                     pos_district AS Pos_District,
//                     pos_state AS Pos_State,
//                     pos_pincode AS Pos_Pincode,
//                     dealertype,dealercode,ctopupno,
//                     ssa_code
//                 FROM ctop_master
//                 WHERE username = @username
//                 LIMIT 1;";

//                     var posData = await db.QueryFirstOrDefaultAsync(posSql,
//                                     new { username = model.De_Username });

//                     if (posData != null)
//                     {
//                         model.Pos_Code = posData.pos_code;
//                         model.Pos_Sale_Name = posData.pos_sale_name;
//                         model.Pos_Hno = posData.pos_hno;
//                         model.Pos_Street = posData.pos_street;
//                         model.Pos_Landmark = posData.pos_landmark;
//                         model.Pos_Locality = posData.pos_locality;
//                         model.Pos_City = posData.pos_city;
//                         model.Pos_District = posData.pos_district;
//                         model.Pos_State = posData.pos_state;
//                         model.Pos_Pincode = posData.pos_pincode;
//                         model.ssa_code = posData.ssa_code;
//                         model.dealercode = posData.dealercode;
//                         model.dealertype = posData.dealertype;
//                         model.ctopupno = posData.ctopupno;
//                     }
//                 }

//                 // Fetch IMSI + PLAN_CODE from simprepaid using SIMNUMBER //postpaid and prepaid table selection handled here
//                 try
//                 {
//                     if (!string.IsNullOrEmpty(model.Simnumber))
//                     {
//                         string simSql;
//                         if (model.Connection_Type == "1")
//                         {
//                             simSql = @"
//                     SELECT 
//                         imsi::text AS imsi,
//                         plan_code 
//                     FROM simprepaid_sold
//                     WHERE simno = @simno
//                     LIMIT 1;";
//                         }
//                         else
//                         {
//                             simSql = @"
//                     SELECT 
//                         imsi::text AS imsi,
//                         plan_code 
//                     FROM simpostpaid_sold
//                     WHERE simno = @simno
//                     LIMIT 1;";
//                         }

//                         var simData = await db.QueryFirstOrDefaultAsync(simSql,
//                                         new { simno = model.Simnumber });

//                         if (simData != null)
//                         {
//                             model.Imsi = simData.imsi; // override with actual IMSI
//                             model.Plan_Code = simData.plan_code?.ToString();
//                         }
//                         else
//                         {
//                             Console.WriteLine($"No simprepaid record found for SIMNO: {model.Simnumber}");
//                         }
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     Console.WriteLine("Error fetching IMSI/plan_code: " + ex.Message);
//                 }


//                 //  Load SIM SWAP DETAILS (ADD HERE)

//                 if (model.caf_type?.Equals("simswap", StringComparison.OrdinalIgnoreCase) == true)
//                 {
//                     try
//                     {
//                         string simSwapSql = @"
//             SELECT
//                 swap_reason      AS Swap_Reason,
//                 ss_bill_fname    AS Ss_Bill_Fname,
//                 ss_address1      AS Ss_Address1,
//                 ss_address2      AS Ss_Address2,
//                 ss_address3      AS Ss_Address3,
//                 ss_state         AS Ss_State,
//                 ss_city          AS Ss_City,
//                 ss_zip           AS Ss_Zip,
//                 ss_sim_number    AS OldSim,
//                 ss_amount_req    AS Ss_Amount_Req,
//                 ss_caf_no AS Ss_Caf_No,
//                 ss_sim_number   AS OldSim
//             FROM caf_sim_swap_details
//             WHERE caf_id = @cafslno
//             LIMIT 1;";

//                         var swapData = await db.QueryFirstOrDefaultAsync<CafSwapModelDkyc>(
//                                             simSwapSql,
//                                             new { cafslno });

//                         //  NO ROW FOUND → FAIL CAF LOAD
//                         if (swapData == null)
//                         {
//                             throw new ApplicationException(
//                                 $"SIM swap details not found for CAF: {cafslno}"
//                             );
//                         }

//                         model.Swap_Reason = swapData.Swap_Reason;
//                         model.Ss_Bill_Fname = swapData.Ss_Bill_Fname;
//                         model.Ss_Address1 = swapData.Ss_Address1;
//                         model.Ss_Address2 = swapData.Ss_Address2;
//                         model.Ss_Address3 = swapData.Ss_Address3;
//                         model.Ss_State = swapData.Ss_State;
//                         model.Ss_City = swapData.Ss_City;
//                         model.Ss_Zip = swapData.Ss_Zip;
//                         model.OldSim = swapData.OldSim;
//                         model.Ss_Amount_Req = swapData.Ss_Amount_Req;
//                         model.Ss_Caf_No = swapData.Ss_Caf_No;
//                     }
//                     catch (Exception ex)
//                     {
//                         Console.WriteLine(
//                             "ERROR-GetCAFSimSwapDetails: " + ex.Message
//                         );

//                         // STOP CAF LOADING
//                         throw;   // rethrow to caller
//                     }
//                 }


//                 return model;
//             }
//         }

        public async Task<decimal?> GetSimSwapChargeAsync(int circleCode)
        {
            const string sql = @"
        SELECT charge_amount
        FROM caf_swap_charge
        WHERE circlecode = @circleCode
        LIMIT 1;";

            using (IDbConnection db = ConnectionPgSql)
            {
                return await db.ExecuteScalarAsync<decimal?>(
                    sql,
                    new { circleCode }
                );
            }
        }


        //get ims of old sim
 public async Task<string?> GetImsiBySimNoAsync(string simNo,string connectionType)
        {
            if (string.IsNullOrWhiteSpace(simNo))
                return null;

            string sql = connectionType == "1"
                ? @"
            SELECT imsi::text
            FROM simprepaid_sold
            WHERE simno = @simNo
            LIMIT 1;"
                : @"
            SELECT imsi::text
            FROM simpostpaid_sold
            WHERE simno = @simNo
            LIMIT 1;";

            using (IDbConnection db = ConnectionPgSql)
            {
                return await db.ExecuteScalarAsync<string?>(
                    sql,
                    new { simNo }
                );
            }
        }


        //fetch api for usim imsi
        //public async Task<string> GetImsiFromUsimApiAsync(CafModel model)
        //{
        //    try
        //    {
        //        using var client = new PyroUsimSimSaleApiClient();

        //        var request = new PyroUsimSimSaleRequest
        //        {
        //            SimVendor = "Idemia",                 // or from config
        //            CircleId = model.circle_code.ToString(),
        //            Msisdn =  model.Gsmnumber,
        //            Iccid =  model.Simnumber,            // NEW SIM ICCID
        //            Brand = "BSNL",
        //            International = false,
        //            SimType = model.Connection_Type == "1" ? "Prepaid" : "Postpaid",
        //            ChannelName = "SancharMitra",
        //            MethodName = "New"
        //        };

        //        var response = await client.SubmitAsync(request);

        //        if (!response.IsSuccess)
        //        {
        //            throw new Exception(
        //                $"USIM API failed. Status={response.Status}, Desc={response.StatusDescription}"
        //            );
        //        }

        //        if (response.Data == null || string.IsNullOrWhiteSpace(response.Data.Imsi))
        //        {
        //            throw new Exception("USIM API success but IMSI not returned");
        //        }

        //        // ✅ SUCCESS
        //        return response.Data.Imsi;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("ERROR-GetImsiFromUsimApiAsync: " + ex.Message);
        //        throw; // 🔥 IMPORTANT: rethrow to STOP CAF flow
        //    }
        //}

        // fetch api for usim imsi (WITH DETAILED SERVER LOGGING)
        public async Task<string> GetImsiFromUsimApiAsync(CafModelDkyc model)
        {
            Console.WriteLine("========== USIM IMSI FETCH START ==========");
            Console.WriteLine($"[INPUT] GSM      : {model.gsm_number}");
            Console.WriteLine($"[INPUT] SIM(ICCID): {model.Simnumber}");
            Console.WriteLine($"[INPUT] Circle   : {model.circle_code}");
            Console.WriteLine($"[INPUT] ConnType : {model.connection_type}");
            Console.WriteLine($"[INPUT] UsimType : {model.usimtype}");
            Console.WriteLine($"[TIME ] Start    : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            try
            {
                using var client = new PyroUsimSimSaleApiClient();

                var request = new PyroUsimSimSaleRequest
                {
                    SimVendor = "Idemia",
                    CircleId = model.circle_code.ToString(),
                    Msisdn = model.gsm_number,
                    Iccid = model.Simnumber,
                    Brand = "BSNL",
                    International = false,
                    SimType = model.connection_type == "1" ? "Prepaid" : "Postpaid",
                    ChannelName = "SancharMitra",
                    MethodName = "New"
                };

                Console.WriteLine("----- USIM API REQUEST BUILT -----");
                Console.WriteLine($"Vendor   : {request.SimVendor}");
                Console.WriteLine($"CircleId : {request.CircleId}");
                Console.WriteLine($"Msisdn   : {request.Msisdn}");
                Console.WriteLine($"Iccid    : {request.Iccid}");
                Console.WriteLine($"SimType  : {request.SimType}");

                Console.WriteLine(">>> Calling USIM API ...");

                var response = await client.SubmitAsync(request);

                Console.WriteLine("<<< USIM API RESPONSE RECEIVED");
                Console.WriteLine($"HTTP StatusCode : {response.StatusCode}");
                Console.WriteLine($"Status          : {response.Status}");
                Console.WriteLine($"Description     : {response.StatusDescription}");
                Console.WriteLine($"IsSuccess       : {response.IsSuccess}");

                if (!response.IsSuccess)
                {
                    Console.WriteLine("❌ USIM API FAILURE");
                    throw new Exception(
                        $"USIM API failed. Status={response.Status}, Desc={response.StatusDescription}"
                    );
                }

                if (response.Data == null)
                {
                    Console.WriteLine("❌ USIM API SUCCESS BUT DATA IS NULL");
                    throw new Exception("USIM API success but data object is null");
                }

                Console.WriteLine("----- USIM API DATA -----");
                Console.WriteLine($"TransactionId : {response.Data.TransactionId}");
                Console.WriteLine($"MSISDN        : {response.Data.Msisdn}");
                Console.WriteLine($"ICCID         : {response.Data.Iccid}");

                if (string.IsNullOrWhiteSpace(response.Data.Imsi))
                {
                    Console.WriteLine("❌ IMSI RETURNED NULL/EMPTY FROM API");
                    throw new Exception("IMSI returned null/empty");
                }

                // Mask IMSI for logs
                string maskedImsi =
                    response.Data.Imsi.Length > 6
                        ? response.Data.Imsi[..3] + "******" + response.Data.Imsi[^3..]
                        : "INVALID";

                Console.WriteLine($"IMSI (masked)  : {maskedImsi}");
                Console.WriteLine("✅ USIM IMSI FETCH SUCCESS");
                Console.WriteLine("========== USIM IMSI FETCH END ==========");

                return response.Data.Imsi;
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 ERROR IN USIM IMSI FETCH 🔥");
                Console.WriteLine($"[ERROR] GSM      : {model.gsm_number}");
                Console.WriteLine($"[ERROR] SIM(ICCID): {model.Simnumber}");
                Console.WriteLine($"[ERROR] Circle   : {model.circle_code}");
                Console.WriteLine($"[ERROR] Message  : {ex.Message}");
                Console.WriteLine($"[ERROR] Stack    : {ex.StackTrace}");
                Console.WriteLine("========== USIM IMSI FETCH FAILED ==========");

                throw; // 🚨 STOP CAF FLOW
            }
        }

    }
}
