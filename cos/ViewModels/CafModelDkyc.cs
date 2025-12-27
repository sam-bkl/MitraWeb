namespace cos.ViewModels
{
    
    public class CafModelDkyc
    {
        public int Id { get; set; }

        public string? de_csccode { get; set; }
        public string? de_username { get; set; }
        public string? circle_code { get; set; }
        public string? ssa_code { get; set; }
        public string? caf_no { get; set; }
        public string? connection_type { get; set; }
        public string? sim_type { get; set; }
        public string? subscriber_type { get; set; }
        public string? nationality { get; set; }

        public string? gsm_number { get; set; }
        public string? sim_no { get; set; }
        public string? cust_mob_no { get; set; }

        public string? customer_otp { get; set; }
        public DateTime? customer_otp_time { get; set; }
        public string? customer_otp_relation { get; set; }

        public string? tariff_plan { get; set; }
        public string? parent_ctopup_number { get; set; }
        public string? mpin { get; set; }

        public string? subscriber_name { get; set; }
        public string? pwd_status { get; set; }
        public string? father_husband_name { get; set; }
        public string? gender { get; set; }
        public DateTime? date_of_birth { get; set; }
       

        public int? jio_count { get; set; }
        public int? airtel_count { get; set; }
        public int? vi_count { get; set; }
        public int? bsnl_count { get; set; }
        public int? other_count { get; set; }
        // Correspondence Address
        public string? corr_relation_type { get; set; }
        public string? corr_relation_name { get; set; }
        public string? corr_house_details { get; set; }
        public string? corr_street_address { get; set; }
        public string? corr_landmark { get; set; }
        public string? corr_area_locality { get; set; }
        public string? corr_city { get; set; }
        public string? corr_district { get; set; }
        public string? corr_state_ut { get; set; }
        public string? corr_pin_code { get; set; }
        // Permanent Address
        public string? perm_relation_type { get; set; }
        public string? perm_relation_name { get; set; }
        public string? perm_house_details { get; set; }
        public string? perm_street_address { get; set; }
        public string? perm_landmark { get; set; }
        public string? perm_area_locality { get; set; }
        public string? perm_city { get; set; }
        public string? perm_district { get; set; }
        public string? perm_state_ut { get; set; }
        public string? perm_pin_code { get; set; }
        public string? value_added_services { get; set; }
        public string? email_address { get; set; }
        public string? profession { get; set; }
        public string? pan_gir { get; set; }

        public string? alt_home_number { get; set; }
        public string? alt_business_number { get; set; }
        public string? alt_mobile_number { get; set; }

        // POI
        public string? poi_type { get; set; }
        public string? poi_number { get; set; }
        public DateTime? poi_date_of_issue { get; set; }
        public string? poi_place_of_issue { get; set; }
        public string? poi_issue_authority { get; set; }

        // POA
        public string? poa_type { get; set; }
        public string? poa_number { get; set; }
        public DateTime? poa_date_of_issue { get; set; }
        public string? poa_place_of_issue { get; set; }
        public string? poa_issue_authority { get; set; }
        public bool pos_declaration_check { get; set; }
        public bool declaration1 { get; set; }
        public bool declaration2 { get; set; }
        public bool declaration3 { get; set; }

        public string? poi_document_front { get; set; }
        public string? poi_document_back { get; set; }
        public string? poa_document_front { get; set; }
        public string? poa_document_back { get; set; }
        public string? customer_photo { get; set; }
        public string? pos_photo { get; set; }

        public string? pos_otp { get; set; }
        public DateTime? pos_otp_gen_time { get; set; }
        public DateTime? pos_otp_ver_time { get; set; }

        public decimal? pos_lat { get; set; }
        public decimal? pos_lang { get; set; }
        public decimal? cust_lat { get; set; }
        public decimal? cust_lang { get; set; }

        public string? upc_code { get; set; }
        public DateTime? upc_valid_upto { get; set; }
        public string? prev_optr { get; set; }
        public string? prev_optr_area { get; set; }

        public string? payment_mode { get; set; }
        public string? std_isd { get; set; }
        public bool deposit_required { get; set; }
        public string? no_deposit_reason { get; set; }
        public string? payment_method { get; set; }
        public decimal? amount_received { get; set; }

        public string? postpaid_plan_name { get; set; }

        public string? outstation_ref_name { get; set; }
        public string? outstation_mob_no { get; set; }
        public string? outstation_address { get; set; }

        public string? device_ip { get; set; }
        public string? device_mac { get; set; }

        public string? verified_flag { get; set; }
        public string? verified_by { get; set; }
        public DateTime? verified_date { get; set; }

        public string? app_version { get; set; }
        public string? rejection_reason { get; set; }
        public DateTime created_at { get; set; }
          public string Pos_Code { get; set; }

        // 27 - Point of Sale Name
        public string Pos_Sale_Name { get; set; }

        // 28 - POS Agent Name (if available)
        public string Pos_Agent_Name { get; set; }

        // 29A–29G : Complete Address of POS
        public string Pos_Hno { get; set; }         // 29A
        public string Pos_Street { get; set; }      // 29B
        public string Pos_Landmark { get; set; }    // 29C
        public string Pos_Locality { get; set; }    // 29D
        public string Pos_City { get; set; }        // 29E
        public string Pos_District { get; set; }    // 29F
        public string Pos_State { get; set; }       // 29G
        public string Pos_Pincode { get; set; }     // 29H
      

         //for geting plan from simprepaid

        public string Plan_Code { get; set; }   // From simprepaid table
        public string Simnumber { get; set; }   // Needed for lookup

       // public string de_csccode { get; set; } //addiitonal column for print service center

        //public string ssa_code { get; set; }  //addiitonal column for print service center

        public string Print_Service_Center_Id { get; set; } 
       
        //public string Ss_Caf_No { get; set; }
        public int? usimtype { get; set; }

    }

    public class RejectModelDkyc
    {
        public string Gsmnumber { get; set; }
        public string Reason { get; set; }
    }

    public class SeelaterModelDkyc
    {
        public string Gsmnumber { get; set; }
        public string remark { get; set; }
    }

    public class CmfMasterModelDkyc
    {
        public string Circle { get; set; }
        public int? Circle_Code { get; set; }
        public string? Act_Type { get; set; }
        public int? Connection_Type { get; set; }
        public int? Msisdn_Type { get; set; }
        public int? Sim_Type { get; set; }
        public int? Cmf_Mkt_Code { get; set; }
        public int? Zone_Element_Id { get; set; }
        public int? Cmf_Vip_Code { get; set; }
        public int? Cmf_Acct_Seg_Id { get; set; }
        public int? Cmf_Exrate_Class { get; set; }
        public int? Cmf_Rate_Class_Default { get; set; }
        public int? Cmf_Bill_Disp_Meth { get; set; }
        public int? Cmf_Bill_Fmt_Opt { get; set; }
        public int? Invs_Saleschannel_Id { get; set; }
        public int? Emf_Config_Id { get; set; }
        public int? Zone_Id { get; set; }
        public string Cmf_Bill_Period { get; set; }

        public string activation_status { get; set; }

        public int? category_code { get; set; }  //for handling MNP in SZ
      


        


    }

    public class FlagUpdateModelDkyc
    {
        public string Gsmnumber { get; set; }
        public string Reason { get; set; }  // remarks
        public string Flag { get; set; }    // F or D
    }



    public class CafSwapModelDkyc
    {
        // 1
        public string Gsmnumber { get; set; }

        // 1A
        public string Pwd { get; set; }

        // 2
        public string Caf_No { get; set; }

        // 3A
        public string Connection_Type { get; set; }

        // 4
        public string Name { get; set; }
        public string Middle_Name { get; set; }
        public string Last_Name { get; set; }

        // 5A - Not available in table
        public string Pos_Unique_Response_Code { get; set; }

        //5B - Not available in table

        public string Pos_Unique_Response_Code_date { get; set; }

        // 6A - Not available in table
        public string Customer_Unique_Response_Code { get; set; }
        // 6B - Not available in table
        public string Customer_Unique_Response_Code_date { get; set; }

        // 7 & 11
        public string F_H_Name { get; set; }

        // 8
        public string Gender { get; set; }

        // 9
        public DateTime? Date_Of_Birth { get; set; }

        // 10
        public string Customer_Type { get; set; } = "INDIVIDUAL";

        // 11D Permanent Address
        public string Perm_Addr_Hno { get; set; }
        public string Perm_Addr_Street { get; set; }
        public string Perm_Addr_Locality { get; set; }
        public string Perm_Addr_City { get; set; }
        public string Perm_Addr_State { get; set; }
        public string Perm_Addr_Pin { get; set; }

        // 11 Local Address
        public string father_name_adh { get; set; } // father name from uidai
        public string Local_Addr_Hno { get; set; }
        public string Local_Addr_Street { get; set; }
        public string Local_Addr_Locality { get; set; }
        public string Local_Addr_City { get; set; }
        public string Local_Addr_State { get; set; }
        public string Local_Addr_Pin { get; set; }

        //11 E local address of outstation customer
        public string ref_careof_address { get; set; }

        public string subscriber_type { get; set; }


        // 12
        public string Nationality { get; set; } = "Indian";


        // 13
        public string Distinct_Operators { get; set; } //other_connection_details

        // 15
        public string Simstate { get; set; } = "UNIFIED PLAN";

        // 17
        public string Email { get; set; }

        // 18
        public string Alternate_Contact_No { get; set; }

        // 19
        public string Profession { get; set; }

        // 21 (Editable) local reference details

        public string local_ref_name { get; set; }
        public string Local_Ref { get; set; }  //address of local refrence
        public string Local_Ref_Contact { get; set; }

        public string ref_otp { get; set; }
        public string ref_otp_time { get; set; }




        // 22A
        public string Upc_Code { get; set; }

        // 22B
        public string Prev_Optr { get; set; }

        // 23
        public string PaymentType { get; set; }

        // 23B
        public string Bank_Details { get; set; }

        // 24A - Not available in table
        public string Customer_Unique_Code_2 { get; set; }

        // 25
        public string Imsi { get; set; }

        // 26 - Point of Sale Code
        public string Pos_Code { get; set; }

        // 27 - Point of Sale Name
        public string Pos_Sale_Name { get; set; }

        // 28 - POS Agent Name (if available)
        public string Pos_Agent_Name { get; set; }

        // 29A–29G : Complete Address of POS
        public string Pos_Hno { get; set; }         // 29A
        public string Pos_Street { get; set; }      // 29B
        public string Pos_Landmark { get; set; }    // 29C
        public string Pos_Locality { get; set; }    // 29D
        public string Pos_City { get; set; }        // 29E
        public string Pos_District { get; set; }    // 29F
        public string Pos_State { get; set; }       // 29G
        public string Pos_Pincode { get; set; }     // 29H



        // 31A - Not available in table
        public string Pos_Unique_Response_Code_2 { get; set; }

        // 32 (same as CAF serial no)
        public string Caf_No_2 { get; set; }

        // 33
        public string Services { get; set; } = "STD,ISD,National Roaming,Call Transfer,4G";

        // 34
        public string Activation_CscCode { get; set; }

        public byte[] Photo { get; set; }
        public string PhotoBase64 { get; set; }

        public string De_Username { get; set; } // for fetching details from ctopup master

        public DateTime? LivePhotoTime { get; set; }
        public string PhotoUrl { get; set; }

        public int circle_code { get; set; }//for getting circle code which can be used to query postgress master

        //for geting plan from simprepaid

        public string Plan_Code { get; set; }   // From simprepaid table
        public string Simnumber { get; set; }   // Needed for lookup

        public string de_csccode { get; set; } //addiitonal column for print service center

        public string ssa_code { get; set; }  //addiitonal column for print service center

        public string Print_Service_Center_Id { get; set; } // set from cmfmaster

        public string caf_type { get; set; } // for handling mnp

        public string latitude { get; set; }
        public string longitude { get; set; }

        public bool in_process { get; set; }
        public string process_by { get; set; }
        public DateTime? process_at { get; set; }


        // ===== SIM SWAP DETAILS =====

        public string Swap_Reason { get; set; }

        public string Ss_Bill_Fname { get; set; }

        public string Ss_Address1 { get; set; }
        public string Ss_Address2 { get; set; }
        public string Ss_Address3 { get; set; }

        public string Ss_State { get; set; }
        public string Ss_City { get; set; }
        public string Ss_Zip { get; set; }

        public string OldSim { get; set; }          // ss_sim_number
        public String Ss_Amount_Req { get; set; }

        public string Ss_Caf_No { get; set; }

        public string Approved_Csc_Ip { get; set; }  //get approver ip 

        public string Present_Imsi { get; set; }   // IMSI of OLD SIM


        // ===== SIM SWAP DETAILS =====

        public string dealertype { get; set; }  //added for sim swap

        public string dealercode { get; set; }  //added for sim swap

        public string ctopupno { get; set; }  //added for sim swap pyro

        public string? Swap_Activation_Status { get; set; } //for setting IF flag for charge cases

        public string? mpin { get; set; } //for setting mpinn for pyro request


        public void FillLocalAddressIfEmpty()
        {
            Local_Addr_Hno = string.IsNullOrWhiteSpace(Local_Addr_Hno) ? Perm_Addr_Hno : Local_Addr_Hno;
            Local_Addr_Street = string.IsNullOrWhiteSpace(Local_Addr_Street) ? Perm_Addr_Street : Local_Addr_Street;
            Local_Addr_Locality = string.IsNullOrWhiteSpace(Local_Addr_Locality) ? Perm_Addr_Locality : Local_Addr_Locality;
            Local_Addr_City = string.IsNullOrWhiteSpace(Local_Addr_City) ? Perm_Addr_City : Local_Addr_City;
            Local_Addr_State = string.IsNullOrWhiteSpace(Local_Addr_State) ? Perm_Addr_State : Local_Addr_State;
            Local_Addr_Pin = string.IsNullOrWhiteSpace(Local_Addr_Pin) ? Perm_Addr_Pin : Local_Addr_Pin;
        }


    }

    public class PyroApiResponseDkyc
    {
        public int statuscode { get; set; }
        public string status { get; set; }
        public string statusDescription { get; set; }
        public PyroApiData data { get; set; }
    }

    public class PyroApiDataDkyc
    {
        public string transactionId { get; set; }
        public string imsi { get; set; }
        public string msisdn { get; set; }
        public string iccid { get; set; }
    }

    public class InPlanDetailsDkyc
    {
        public string? InPlanId { get; set; }
        public string? SimState { get; set; }
        public decimal? PrimaryTalkValue { get; set; }
    }


}
