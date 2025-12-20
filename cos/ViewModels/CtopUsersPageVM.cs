using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace cos.ViewModels
{
    public class CtopUsersPageVM
    {
        public string? AdminMobile { get; set; }
        public CtopMaster? AdminCtop { get; set; }
        public IEnumerable<CtopMaster> Users { get; set; } = new List<CtopMaster>();
        public IEnumerable<CircleOptionVM> Circles { get; set; } = new List<CircleOptionVM>();
        public IEnumerable<SsaOptionVM> Ssas { get; set; } = new List<SsaOptionVM>();
        public CtopMasterCreateVM NewCtop { get; set; } = new CtopMasterCreateVM();
    }

    public class CtopMasterCreateVM
    {
        [Required]
        [Display(Name = "Name")]
        public string? name { get; set; }

        [Display(Name = "Dealer Type")]
        public string? dealertype { get; set; }

        [Display(Name = "Designation")]
        public string? designation { get; set; }

        [Display(Name = "SSA Code")]
        public string? ssa_code { get; set; }

        [Display(Name = "CSC Code")]
        public string? csccode { get; set; }

        [Display(Name = "Circle Code")]
        public string? circle_code { get; set; }

        [Display(Name = "Attached To")]
        public string? attached_to { get; set; }

        [Display(Name = "Contact Number")]
        public string? contact_number { get; set; }

        [Display(Name = "POS House No")]
        public string? pos_hno { get; set; }

        [Display(Name = "POS Street")]
        public string? pos_street { get; set; }

        [Display(Name = "POS Landmark")]
        public string? pos_landmark { get; set; }

        [Display(Name = "POS Locality")]
        public string? pos_locality { get; set; }

        [Display(Name = "POS City")]
        public string? pos_city { get; set; }

        [Display(Name = "POS District")]
        public string? pos_district { get; set; }

        [Display(Name = "POS State")]
        public string? pos_state { get; set; }

        [Display(Name = "POS PIN Code")]
        public string? pos_pincode { get; set; }

        [Display(Name = "POS Name As")]
        public string? pos_name_ss { get; set; }

        [Display(Name = "POS Owner Name")]
        public string? pos_owner_name { get; set; }

        [Display(Name = "POS Code")]
        public string? pos_code { get; set; }

        [Display(Name = "Cirle Name")]
        public string? circle_name { get; set; }

        [Display(Name = "Aadhar No")]
        public string? aadhaar_no { get; set; }

        [Display(Name = "Zone Code")]
        public string? zone_code { get; set; }

        [Display(Name = "Ctop Type")]
        public string? ctop_type { get; set; } = "INDIRECT";

        public long? circle_id { get; set; }
        public long? ssa_id { get; set; }

        [Required]
        [Display(Name = "Aadhaar Issue Year (YYYY)")]
        [StringLength(4, MinimumLength = 1, ErrorMessage = "Year must be up to 4 digits.")]
        public string? aadhaar_issue_year { get; set; }

        // CSC staff POS registration documents
        [Required(ErrorMessage = "BA Head approved letter is required.")]
        [Display(Name = "BA Head Approved Letter")]
        public IFormFile? BaApprovalLetter { get; set; }

        [Required(ErrorMessage = "Employee ID Card is required.")]
        [Display(Name = "Employee ID Card")]
        public IFormFile? EmployeeIdCard { get; set; }

        [Required(ErrorMessage = "Aadhaar Card is required.")]
        [Display(Name = "Aadhaar Card")]
        public IFormFile? AadhaarCard { get; set; }

        [Display(Name = "PAN Card (optional)")]
        public IFormFile? PanCard { get; set; }

        [Required(ErrorMessage = "Photo is required.")]
        [Display(Name = "Photo")]
        public IFormFile? Photo { get; set; }
    }

    public class CircleOptionVM
    {
        public long id { get; set; }
        public string circle_name { get; set; }
        public string circle_code { get; set; }
        public string zone_code { get; set; }
    }

    public class SsaOptionVM
    {
        public long id { get; set; }
        public string ssa_name { get; set; }
        public string ssa_code { get; set; }
        public long circle_id { get; set; }
    }
}