using System.Linq;

namespace cos.ViewModels
{
    public class MissingCscCtopDetailsVM
    {
        public string? ctopupno { get; set; }
        public string? name { get; set; }
        public string? second_name { get; set; }
        public string? last_name { get; set; }
        public string? dealertype { get; set; }
        public string? ssa_code { get; set; }
        public string? circle_code { get; set; }
        public string? csccode { get; set; }
        public string? attached_to { get; set; }
        public string? contact_number { get; set; }
        public string? dealer_address { get; set; }
        public string? ssa_city { get; set; }
        public string? aadhaar_no { get; set; }
        public string? zone_code { get; set; }
        public string? dealer_id { get; set; }
        public string? dealercode { get; set; }
        public long? ref_dealer_id { get; set; }
        public long? master_dealer_id { get; set; }
        public string? parent_ctopno { get; set; }
        public string? parent_ctop { get; set; }
        public string? dealer_status { get; set; }
        public string? active { get; set; }
        
        // Computed/combined fields
        public string? full_name 
        { 
            get 
            {
                var parts = new[] { name, second_name, last_name }.Where(x => !string.IsNullOrWhiteSpace(x));
                return string.Join(" ", parts).Trim();
            }
        }
    }
}

