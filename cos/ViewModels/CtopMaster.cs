using System;
using System.Linq;

namespace cos.ViewModels
{
    public class CtopMaster
    {
        public string? username { get; set; }
        public string? ctopupno { get; set; }
        public string? name { get; set; }
        public string? dealertype { get; set; }
        public string? ssa_code { get; set; }
        public string? csccode { get; set; }
        public string? circle_code { get; set; }
        public string? attached_to { get; set; }
        public string? contact_number { get; set; }
        public string? pos_hno { get; set; }
        public string? pos_street { get; set; }
        public string? pos_landmark { get; set; }
        public string? pos_locality { get; set; }
        public string? pos_city { get; set; }
        public string? pos_district { get; set; }
        public string? pos_state { get; set; }
        public string? pos_pincode { get; set; }
        public DateTime? created_date { get; set; }
        public string? pos_name_ss { get; set; }
        public string? pos_owner_name { get; set; }
        public string? pos_code { get; set; }
        public string? pos_ctop { get; set; }
        public string? circle_name { get; set; }
        public string? pos_unique_code { get; set; }
        public string? latitude { get; set; }
        public string? longitude { get; set; }
        public string? aadhaar_no { get; set; }
        public string? zone_code { get; set; }
        public string? ctop_type { get; set; }
        public string? dealercode { get; set; }
        public long? ref_dealer_id { get; set; }
        public long? master_dealer_id { get; set; }
        public string? parent_ctopno { get; set; }
        public string? dealer_status { get; set; }
        public DateTime? end_date { get; set; }
        public decimal? dealer_id { get; set; }
        public string? active { get; set; }

        /// <summary>
        /// Builds the POS unique code using the provided Aadhaar number, Aadhaar issue year, and name.
        /// Pattern: last 4 digits of Aadhaar + issue year (YYYY) + first 4 letters of name + last 4 letters of name.
        /// If Aadhaar digits or name are shorter than 4 characters, pad with '0'. Non-alphanumeric characters are stripped.
        /// </summary>
        public static string GeneratePosUniqueCode(string? aadhaarNo, string? aadhaarIssueYear, string? aadhaarName)
        {
            string digits = new string((aadhaarNo ?? string.Empty).Where(char.IsDigit).ToArray());
            string last4Aadhaar = (digits.Length >= 4 ? digits.Substring(digits.Length - 4, 4) : digits.PadLeft(4, '0'));

            string yearPart = (aadhaarIssueYear ?? string.Empty).Trim();
            if (yearPart.Length > 4)
            {
                yearPart = yearPart[^4..];
            }
            yearPart = yearPart.PadLeft(4, '0');

            string cleanName = new string((aadhaarName ?? string.Empty)
                .Where(char.IsLetterOrDigit)
                .ToArray())
                .ToUpperInvariant();

            string first4 = cleanName.Length >= 4 ? cleanName.Substring(0, 4) : cleanName.PadRight(4, '0');
            string last4 = cleanName.Length >= 4 ? cleanName.Substring(cleanName.Length - 4, 4) : cleanName.PadRight(4, '0');

            return $"{last4Aadhaar}{yearPart}{first4}{last4}";
        }

        /// <summary>
        /// Convenience wrapper that uses the current instance fields and an explicit Aadhaar year.
        /// </summary>
        public string GeneratePosUniqueCode(string aadhaarYear)
        {
            return GeneratePosUniqueCode(aadhaar_no, aadhaarYear, name);
        }
    }
}
