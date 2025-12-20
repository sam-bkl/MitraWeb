namespace cos.ViewModels
{
    public class AccountVM
    {
        public long id { get; set; }
        public string? staff_name { get; set; }
        public string? user_name { get; set; }
        public long mobile { get; set; }
        public int hrno { get; set; }
        public string? role_name { get; set; }
        public string? designation_code { get; set; }
        public string? ssa_code { get; set; }

        public string? circle { get; set; }
        public string is_verified { get; set; } = "VERIFIED";
        public string? record_status { get; set; }

        public string? changepassword { get; set; }

        public string? reset_on { get; set; }

    }
}
