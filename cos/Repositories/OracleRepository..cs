using Oracle.ManagedDataAccess.Client;
using Dapper;
using System.Data;
using cos.ViewModels;
using YourProject.Repositories.Interfaces;
using cos.Repositories;
using MpinCrypt;


public class OracleRepository : IOracleRepository
{
    private readonly IConfiguration _config;
    private readonly UserDashRepository _pgRepo;   // or PgMasterRepository if you want

    public OracleRepository(IConfiguration config)
    {
        _config = config;

        // Create PG repository internally (no DI)
        _pgRepo = new UserDashRepository(config);
    }

    private IDbConnection ConnectionOracle =>
        new OracleConnection(_config.GetConnectionString("OracleDb"));

    public async Task<List<BcdVM>> GetBcdDetails()
    {
        List<BcdVM> bcdList = new List<BcdVM>();

        string query = @"SELECT DE_CSCCODE, DE_USERNAME FROM CAF_ADMIN.BCD";

        using (var conn = ConnectionOracle)
        {
            try
            {
                conn.Open();
                var result = await conn.QueryAsync<BcdVM>(query);
                bcdList = result.AsList();
            }
            catch (Exception ex)
            {
                // Optionally log the exception
            }
        }

        return bcdList;
    }


    public async Task<string> TestOracleConnection()
    {
        string connString = _config.GetConnectionString("OracleDb");

        using (var conn = new OracleConnection(connString))
        {
            try
            {
                await conn.OpenAsync();
                return "Oracle Connection Successful";
            }
            catch (Exception ex)
            {
                return "Oracle Connection Failed: " + ex.Message;
            }
        }
    }


    public async Task<bool> InsertBcdRecordAsync(string gsmNumber, string cafSerial, string userId)
    {
        try
        {
            string connString = _config.GetConnectionString("OracleDb");
            using (var con = new OracleConnection(connString))
            {
                await con.OpenAsync();

                string query = @"INSERT INTO cos_bcd 
                             (gsmnumber, caf_serial_no, verified_flag, entrydate, verified_by) 
                             VALUES (:gsm, :caf, 'Y', SYSDATE, :userId)";

                using var cmd = new OracleCommand(query, con);
                cmd.Parameters.Add(new OracleParameter("gsm", gsmNumber));
                cmd.Parameters.Add(new OracleParameter("caf", cafSerial));
                cmd.Parameters.Add(new OracleParameter("userId", userId));

                int rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
        }
        catch (Exception ex)
        {
            // log error
            return false;
        }
    }



   //oracle bcd insertion for ekyc cymn and mnp
    public async Task<bool> InsertBcdFromCafAsync(CafModel model, string userId)
    {
        try
        {
            string connString = _config.GetConnectionString("OracleDb");
            // Auto-fill Local Address from Permanent Address
            model.FillLocalAddressIfEmpty();
            

            //get print service center
            model.Print_Service_Center_Id = await _pgRepo.GetPrintServiceCenterIdAsync( model.circle_code,model.ssa_code);

            // Get in_plan_id,simstate,primary_talk_value from PostgreSQL 

            //string? inPlanId = await _pgRepo.GetInPlanIdAsync(model.Plan_Code, model.circle_code);

            var planDetails = await _pgRepo.GetInPlanDetailsAsync(model.Plan_Code,model.circle_code);
            string inPlanId = planDetails.InPlanId;
            string? simState = planDetails.SimState;
            decimal? primaryTalkValue = planDetails.PrimaryTalkValue;

            // Fetch master values
            var master = await _pgRepo.GetCmfMasterByCircleAsync(model.circle_code,model.Connection_Type , model.caf_type);

            using (var con = new OracleConnection(connString))
            {
                await con.OpenAsync();

                string sql = @"
                INSERT INTO CAF_ADMIN.BCD (
    gsmnumber, simnumber, caf_serial_no, connection_type,
    name, middle_name, last_name, f_h_name, gender, date_of_birth,
    local_addr_hno, local_addr_street, local_addr_locality, local_addr_city,
    local_addr_state, local_addr_pin,
    perm_addr_hno, perm_addr_street, perm_addr_locality, perm_addr_city,
    perm_addr_state, perm_addr_pin,
    customer_type, nationality, email, alternate_contact_no, profession,
    local_ref, local_ref_contact, upc_code,
    doptr,
    prev_optr, paymenttype, bank_details,
    imsi, services, activation_csccode, activation_status,
    circle_code,                        -- MUST BE HERE
    act_type,
    category_code,
    msisdn_type, sim_type, cmf_mkt_code, zone_element_id,
    cmf_vip_code, cmf_acct_seg_id, cmf_exrate_class, cmf_rate_class_default,
    cmf_bill_disp_meth, cmf_bill_fmt_opt, invs_saleschannel_id, emf_config_id,

    in_plan_id,
    simstate,
    primary_talk_value,
    print_service_center_id,
    INS_USR,
    de_csccode, De_Username,plan_code,
    dateallottment,

    verified_flag, inserted_time, verified_by
)
VALUES (
    :gsmnumber, :simnumber, :cafno, :connection_type,
    :name, :middle_name, :last_name, :f_h_name, :gender, :dob,
    :la_hno, :la_street, :la_loc, :la_city, :la_state, :la_pin,
    :pa_hno, :pa_street, :pa_loc, :pa_city, :pa_state, :pa_pin,
    :customer_type, :nationality, :email, :alt_contact, :profession,
    :local_ref, :local_ref_contact, :upc_code,
    :doptr,
    :prev_optr, :paymenttype, :bank_details,
    :imsi, :services, :activation_csccode, :activation_status,
    :circle_code,                        -- FIXED POSITION
    :act_type,
    :category_code,
    :msisdn_type, :sim_type, :cmf_mkt_code, :zone_element_id,
    :cmf_vip_code, :cmf_acct_seg_id, :cmf_exrate_class, :cmf_rate_class_default,
    :cmf_bill_disp_meth, :cmf_bill_fmt_opt, :invs_saleschannel_id, :emf_config_id,

     :in_plan_id,    
     :simstate,
    :primary_talk_value,
    :print_service_center_id,
    :INS_USR,
    :de_csccode, :De_Username,:plan_code,
    :dateallottment,

    'Y', SYSDATE, :verified_by
)";


                using (var cmd = new OracleCommand(sql, con))
                {
                    //CAF Basic
                    cmd.Parameters.Add(new OracleParameter("gsmnumber", model.Gsmnumber));
                    cmd.Parameters.Add(new OracleParameter("simnumber", model.Simnumber));
                    cmd.Parameters.Add(new OracleParameter("cafno", model.Caf_Serial_No));
                    cmd.Parameters.Add(new OracleParameter("connection_type", model.Connection_Type));

                    //Name & KYC
                    cmd.Parameters.Add(new OracleParameter("name", model.Name));
                    cmd.Parameters.Add(new OracleParameter("middle_name", model.Middle_Name));
                    cmd.Parameters.Add(new OracleParameter("last_name", model.Last_Name));
                    cmd.Parameters.Add(new OracleParameter("f_h_name", model.F_H_Name));
                    cmd.Parameters.Add(new OracleParameter("gender", model.Gender));
                    cmd.Parameters.Add(new OracleParameter("dob", model.Date_Of_Birth));

                    //Local Address
                    cmd.Parameters.Add(new OracleParameter("la_hno", model.Local_Addr_Hno));
                    cmd.Parameters.Add(new OracleParameter("la_street", model.Local_Addr_Street));
                    cmd.Parameters.Add(new OracleParameter("la_loc", model.Local_Addr_Locality));
                    cmd.Parameters.Add(new OracleParameter("la_city", model.Local_Addr_City));
                    cmd.Parameters.Add(new OracleParameter("la_state", model.Local_Addr_State));
                    cmd.Parameters.Add(new OracleParameter("la_pin", model.Local_Addr_Pin));

                    //Permanent Address
                    cmd.Parameters.Add(new OracleParameter("pa_hno", model.Perm_Addr_Hno));
                    cmd.Parameters.Add(new OracleParameter("pa_street", model.Perm_Addr_Street));
                    cmd.Parameters.Add(new OracleParameter("pa_loc", model.Perm_Addr_Locality));
                    cmd.Parameters.Add(new OracleParameter("pa_city", model.Perm_Addr_City));
                    cmd.Parameters.Add(new OracleParameter("pa_state", model.Perm_Addr_State));
                    cmd.Parameters.Add(new OracleParameter("pa_pin", model.Perm_Addr_Pin));

                    //Customer Info
                    cmd.Parameters.Add(new OracleParameter("customer_type", model.Customer_Type));
                    cmd.Parameters.Add(new OracleParameter("nationality", model.Nationality));
                    cmd.Parameters.Add(new OracleParameter("email", model.Email));
                    cmd.Parameters.Add(new OracleParameter("alt_contact", model.Alternate_Contact_No));
                    cmd.Parameters.Add(new OracleParameter("profession", model.Profession));

                    //Editable
                    cmd.Parameters.Add(new OracleParameter("local_ref", model.Local_Ref));
                    cmd.Parameters.Add(new OracleParameter("local_ref_contact", model.Local_Ref_Contact));

                    cmd.Parameters.Add(new OracleParameter("upc_code", model.Upc_Code));
                    // Extract first letter of UPC
                    string doptrValue = null;

                    if (!string.IsNullOrWhiteSpace(model.Upc_Code))
                    {
                        doptrValue = model.Upc_Code.Substring(0, 1);  // first character
                    }
                    cmd.Parameters.Add(new OracleParameter("doptr", doptrValue));
                    cmd.Parameters.Add(new OracleParameter("prev_optr", model.Prev_Optr));
                    cmd.Parameters.Add(new OracleParameter("paymenttype", model.PaymentType));
                    cmd.Parameters.Add(new OracleParameter("bank_details", model.Bank_Details));

                    //SIM/Network
                    cmd.Parameters.Add(new OracleParameter("imsi", model.Imsi));
                   // cmd.Parameters.Add(new OracleParameter("stdpco", model.Stdpco));
                    cmd.Parameters.Add(new OracleParameter("services", model.Services));
                    cmd.Parameters.Add(new OracleParameter("activation_csccode", model.Activation_CscCode));
                    cmd.Parameters.Add(new OracleParameter("activation_status", master?.activation_status));
                    cmd.Parameters.Add(new OracleParameter("circle_code", model.circle_code));
                    // Master fields (nullable safe)
                    cmd.Parameters.Add(new OracleParameter("act_type", master?.Act_Type));
                    cmd.Parameters.Add(new OracleParameter("category_code", master?.category_code));
                    cmd.Parameters.Add(new OracleParameter("msisdn_type", master?.Msisdn_Type));
                    cmd.Parameters.Add(new OracleParameter("sim_type", master?.Sim_Type));
                    cmd.Parameters.Add(new OracleParameter("cmf_mkt_code", master?.Cmf_Mkt_Code));
                    cmd.Parameters.Add(new OracleParameter("zone_element_id", master?.Zone_Element_Id));
                    cmd.Parameters.Add(new OracleParameter("cmf_vip_code", master?.Cmf_Vip_Code));
                    cmd.Parameters.Add(new OracleParameter("cmf_acct_seg_id", master?.Cmf_Acct_Seg_Id));
                    cmd.Parameters.Add(new OracleParameter("cmf_exrate_class", master?.Cmf_Exrate_Class));
                    cmd.Parameters.Add(new OracleParameter("cmf_rate_class_default", master?.Cmf_Rate_Class_Default));
                    cmd.Parameters.Add(new OracleParameter("cmf_bill_disp_meth", master?.Cmf_Bill_Disp_Meth));
                    cmd.Parameters.Add(new OracleParameter("cmf_bill_fmt_opt", master?.Cmf_Bill_Fmt_Opt));
                    cmd.Parameters.Add(new OracleParameter("invs_saleschannel_id", master?.Invs_Saleschannel_Id));
                    cmd.Parameters.Add(new OracleParameter("emf_config_id", master?.Emf_Config_Id));

                    // cmd.Parameters.Add(new OracleParameter("zone_id", master?.Zone_Id));
                    // cmd.Parameters.Add(new OracleParameter("cmf_bill_period", master?.Cmf_Bill_Period));


                    //added by sujith
                    cmd.Parameters.Add(new OracleParameter("in_plan_id", inPlanId ?? (object)DBNull.Value));
                    //cmd.Parameters.Add(new OracleParameter("simstate", "UNIFIEDPREACT"));
                    cmd.Parameters.Add(new OracleParameter("simstate", simState ?? simState ?? "UNIFIEDPREACT"));
                    //cmd.Parameters.Add(new OracleParameter("primary_talk_value", "0"));
                    cmd.Parameters.Add(new OracleParameter("primary_talk_value", primaryTalkValue ?? 0));
                    cmd.Parameters.Add(new OracleParameter("print_service_center_id", model.Print_Service_Center_Id));
                    cmd.Parameters.Add(new OracleParameter("INS_USR", userId));
                    cmd.Parameters.Add(new OracleParameter("de_csccode", model.de_csccode));
                    cmd.Parameters.Add(new OracleParameter("De_Username", model.De_Username));
                    cmd.Parameters.Add(new OracleParameter("plan_code", model.Plan_Code));

                    cmd.Parameters.Add(new OracleParameter("dateallottment",model.LivePhotoTime ?? (object)DBNull.Value));


                    cmd.Parameters.Add(new OracleParameter("verified_by", userId));

                    //Console.WriteLine("== SQL INSERT ==");
                    //Console.WriteLine(sql);
                    //int index = 0;
                    //foreach (OracleParameter p in cmd.Parameters)
                    //{
                    //    Console.WriteLine($"{index++} => {p.ParameterName} = {p.Value} ({p.Value?.GetType()})");
                    //}
                    int rows = await cmd.ExecuteNonQueryAsync();
                    return rows > 0;
                }
            }
        }
        catch (Exception ex)
        {
           // _logger.LogError(ex, "Oracle Insert failed");
            return false;
        }
    }


    //oracle bcd insertion for ekyc swap

    public async Task<bool> InsertBcdFromCafSwapAsync(CafSwapModel model, string userId)
    {
        try
        {
            string connString = _config.GetConnectionString("OracleDb");
            // Auto-fill Local Address from Permanent Address
            model.FillLocalAddressIfEmpty();

            //get present imsi
            model.Present_Imsi = await _pgRepo.GetImsiBySimNoAsync(model.OldSim, model.Connection_Type);

            //get print service center
            model.Print_Service_Center_Id = await _pgRepo.GetPrintServiceCenterIdAsync(model.circle_code, model.ssa_code);

            // Get in_plan_id from PostgreSQL

            //string? inPlanId = await _pgRepo.GetInPlanIdAsync(model.Plan_Code, model.circle_code);
            var planDetails = await _pgRepo.GetInPlanDetailsAsync(model.Plan_Code, model.circle_code);
            string inPlanId = planDetails.InPlanId;
            string? simState = planDetails.SimState;
            decimal? primaryTalkValue = planDetails.PrimaryTalkValue;


            // Fetch master values
            var master = await _pgRepo.GetCmfMasterByCircleAsync(model.circle_code, model.Connection_Type, model.caf_type);

            using (var con = new OracleConnection(connString))
            {
                await con.OpenAsync();

                string sql = @"
               INSERT INTO CAF_ADMIN.SIM_SWAP_DATA (
    CSCCODE,
    GSMNUMBER,
    PRESENT_IMSI,
    NEW_IMSI,
    OLD_SIM,
    NEW_SIM,
    CAF_SERIAL_NO,
    CONNECTION_TYPE,
    NAME,

    LOCAL_ADDR_HNO,
    LOCAL_ADDR_STREET,
    LOCAL_ADDR_LOCALITY,
    LOCAL_ADDR_CITY,
    LOCAL_ADDR_STATE,
    LOCAL_ADDR_PIN,

    PERM_ADDR_HNO,
    PERM_ADDR_STREET,
    PERM_ADDR_LOCALITY,
    PERM_ADDR_CITY,
    PERM_ADDR_STATE,
    PERM_ADDR_PIN,

    CUSTOMER_TYPE,
    NATIONALITY,
    EMAIL,
    ALTERNATE_CONTACT_NO,
    PROFESSION,

    LOCAL_REF,
    LOCAL_REF_CONTACT,

    DISTINCT_OPERATORS,
    SERVICES,
    PLAN_CODE,

    INS_USR,
    INS_DATE,
    
    CAF_TYPE,
    SWAP_REMARKS,

    ACTIVATION_STATUS,
    CIRCLE_CODE,

    APPROVED_CSC,
    APPROVED_DATE,
    APPROVED_CSC_IP
)
VALUES (
    :csccode,
    :gsmnumber,
    :present_imsi,
    :new_imsi,
    :old_sim,
    :new_sim,
    :caf_serial_no,
    :connection_type,
    :name,

    :la_hno,
    :la_street,
    :la_locality,
    :la_city,
    :la_state,
    :la_pin,

    :pa_hno,
    :pa_street,
    :pa_locality,
    :pa_city,
    :pa_state,
    :pa_pin,

    :customer_type,
    :nationality,
    :email,
    :alt_contact,
    :profession,

    :local_ref,
    :local_ref_contact,

    :distinct_operators,
    :services,
    :plan_code,

    :ins_usr,
    SYSDATE,

   
    :caf_type,
    :swap_remarks,

    :activation_status,
    :circle_code,

    :approved_csc,
    SYSDATE,
    :approved_csc_ip
)";


                using (var cmd = new OracleCommand(sql, con))
                {
                    cmd.Parameters.Add("csccode", model.de_csccode);
                    cmd.Parameters.Add("gsmnumber", model.Gsmnumber);
                    cmd.Parameters.Add("present_imsi", model.Present_Imsi);
                    cmd.Parameters.Add("new_imsi", model.Imsi);
                    cmd.Parameters.Add("old_sim", model.OldSim);
                    cmd.Parameters.Add("new_sim", model.Simnumber);
                    cmd.Parameters.Add("caf_serial_no", model.Caf_Serial_No);
                    cmd.Parameters.Add("connection_type", model.Connection_Type);
                    cmd.Parameters.Add("name", model.Name);

                    // Local Address
                    cmd.Parameters.Add("la_hno", model.Local_Addr_Hno);
                    cmd.Parameters.Add("la_street", model.Local_Addr_Street);
                    cmd.Parameters.Add("la_locality", model.Local_Addr_Locality);
                    cmd.Parameters.Add("la_city", model.Local_Addr_City);
                    cmd.Parameters.Add("la_state", model.Local_Addr_State);
                    cmd.Parameters.Add("la_pin", model.Local_Addr_Pin);

                    // Permanent Address
                    cmd.Parameters.Add("pa_hno", model.Perm_Addr_Hno);
                    cmd.Parameters.Add("pa_street", model.Perm_Addr_Street);
                    cmd.Parameters.Add("pa_locality", model.Perm_Addr_Locality);
                    cmd.Parameters.Add("pa_city", model.Perm_Addr_City);
                    cmd.Parameters.Add("pa_state", model.Perm_Addr_State);
                    cmd.Parameters.Add("pa_pin", model.Perm_Addr_Pin);

                    // Customer
                    cmd.Parameters.Add("customer_type", model.Customer_Type);
                    cmd.Parameters.Add("nationality", model.Nationality);
                    cmd.Parameters.Add("email", model.Email);
                    cmd.Parameters.Add("alt_contact", model.Alternate_Contact_No);
                    cmd.Parameters.Add("profession", model.Profession);

                    // Local Reference
                    cmd.Parameters.Add("local_ref", model.Local_Ref);
                    cmd.Parameters.Add("local_ref_contact", model.Local_Ref_Contact);

                    cmd.Parameters.Add("distinct_operators", model.Distinct_Operators);
                    cmd.Parameters.Add("services", model.Services);
                    cmd.Parameters.Add(new OracleParameter("plan_code", model.Plan_Code));

                    // Audit
                    cmd.Parameters.Add("ins_usr", model.De_Username);

                    // Swap
                    //cmd.Parameters.Add("swap_type", "SIM_SWAP");
                    cmd.Parameters.Add("caf_type", model.caf_type);
                    cmd.Parameters.Add("swap_remarks", model.Swap_Reason);

                    //handling IF flag for swap charges
                    string activationStatus = !string.IsNullOrWhiteSpace(model.Swap_Activation_Status)? model.Swap_Activation_Status : master?.activation_status; 
                    cmd.Parameters.Add("activation_status", activationStatus);
                    cmd.Parameters.Add("circle_code", model.circle_code);

                    // Approval block
                    cmd.Parameters.Add("approved_csc", userId);
                    cmd.Parameters.Add("approved_csc_ip", model.Approved_Csc_Ip);

                    //Console.WriteLine("== SQL INSERT ==");
                    //Console.WriteLine(sql);
                    //int index = 0;
                    //foreach (OracleParameter p in cmd.Parameters)
                    //{
                    //    Console.WriteLine($"{index++} => {p.ParameterName} = {p.Value} ({p.Value?.GetType()})");
                    //}
                    int rows = await cmd.ExecuteNonQueryAsync();
                    return rows > 0;
                }
            }
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "Oracle Insert failed");
            return false;
        }
    }

    //pyro inegration for sim swap
    public async Task<bool> InsertSimSwapAmountDeductRequestAsync(
    CafSwapModel model,
    decimal amount,
    string userId
)
    {
        try
        {
            string connString = _config.GetConnectionString("OracleDb");

            using (var con = new OracleConnection(connString))
            {
                await con.OpenAsync();

                string sql = @"
INSERT INTO CAF_ADMIN.SIMSWAP_AMOUNT_DEDUCT_REQUESTS
(
    SS_REQUEST_ID,    
    CIRCLE_CODE,
    DEALERCODE,
    CTOPUPNO,
    DEALERTYPE,
    FRANCHISEE,
    GSMNUMBER,
    SIMNUMBER,
    AMOUNT,
    REQUEST_DATE,
    SWAP_TYPE,
    MPIN,
    MPIN_LENGTH,
    SOURCE    
   )
VALUES
(
    :ss_request_id,  
    :circle_code,
    :dealercode,
    :ctopupno,
    :dealertype,
    :franchisee,
    :gsmnumber,
    :simnumber,
    :amount,
    SYSDATE,
    :swap_type,
    :mpin,
    :mpin_length,
    :source
)";



                // 1. Get sequence
                long sequenceNo;
                using (var seqCmd = new OracleCommand(
                    "SELECT CAF_ADMIN.SS_REQUEST_SEQ.NEXTVAL FROM DUAL", con))
                {
                    sequenceNo = Convert.ToInt64(await seqCmd.ExecuteScalarAsync());
                }


                using (var cmd = new OracleCommand(sql, con))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ss_request_id", sequenceNo);
                    cmd.Parameters.Add("circle_code", model.circle_code);
                    cmd.Parameters.Add("dealercode", model.de_csccode);
                    cmd.Parameters.Add("ctopupno", model.ctopupno);
                    cmd.Parameters.Add("dealertype", model.dealertype);
                    cmd.Parameters.Add("franchisee", model.dealertype);
                    cmd.Parameters.Add("gsmnumber", model.Gsmnumber);
                    cmd.Parameters.Add("simnumber", model.Simnumber);
                    cmd.Parameters.Add("amount", amount);


                    cmd.Parameters.Add("swap_type", "Normal");
                    cmd.Parameters.Add("mpin", OracleDesCrypto.Encrypt( model.mpin));
                    cmd.Parameters.Add("mpin_length", model.mpin?.Length ?? 0);
                    cmd.Parameters.Add("source", "KYC_PORTAL");
                    
                   

                    int rows = await cmd.ExecuteNonQueryAsync();
                    return rows > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "ERROR-InsertSimSwapAmountDeductRequestAsync: " + ex.Message
            );
            return false;
        }
    }



   



}
