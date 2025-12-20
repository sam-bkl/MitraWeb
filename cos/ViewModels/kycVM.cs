namespace cos.ViewModels
{
    public class kycVM
    {
        public string? circle { get; set; }
        public string name { get; set; }
        public string gsmnumber { get; set; }
        public string simnumber { get; set; }

        public string cafslno { get; set; }

        public string caftype { get; set; }

        public string ssa_code { get; set; }

        public string alternate_contact_no { get; set; }

        public string de_username { get; set; }

        public string de_csccode { get; set; }

        public string live_photo_date { get;  set; }

    }

    public class kycstatusVM
    {
        public string? circle { get; set; }
        public string name { get; set; }
        public string gsmnumber { get; set; }
        public string simnumber { get; set; }

        public string cafslno { get; set; }

        public string status { get; set; }

        public string verified_date { get; set; }

        public string verified_by { get; set; }

        public string? reason { get; set; } 


    }


}
