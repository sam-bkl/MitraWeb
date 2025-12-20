namespace cos.ViewModels
{
    public class User
    {
        public long id { get; set; }
        public long account_id { get; set; }
        public string? staff_name { get; set; }
        public string? mobile { get; set; }
        public string? email { get; set; }
        public long hrno { get; set; }
        public string? designation_code { get; set; }
        public string? ssa_code { get; set; }
        public string? record_status { get; set; }
        public DateTime? created_on { get; set; }
        public DateTime? updated_on { get; set; }
        public DateTime? deleted_on { get; set; }
        public long updated_by { get; set; }
        public string? changepassword { get; set; }
        public string? deleted_by { get; set; }
        public string? circle { get; set; }
        public string? ctopupno { get; set; }
    }
}
