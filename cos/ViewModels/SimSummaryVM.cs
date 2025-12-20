namespace cos.ViewModels
{
    public class SimSummaryVM
    {
        public string? circle { get; set; }
        public int total_kyc { get; set; }
        public int comp_kyc { get; set; }
        public int seelater { get; set; }

        public int rejected { get; set; }
    }





    public class PostpaidSummaryVM
    {
        public string? circle_code { get; set; }
        public string location { get; set; }
        
        public string loccount { get; set; }
    }

    public class PostpaiddetailsVM
    {

        public string? circle_code { get; set; }
        public string location { get; set; }
        public string? simno { get; set; }
        public string imsi { get; set; }

        
    }

}


