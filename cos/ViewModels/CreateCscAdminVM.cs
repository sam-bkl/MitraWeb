using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace cos.ViewModels
{
    public class CreateCscAdminPageVM
    {
        public IEnumerable<CircleOptionVM> Circles { get; set; } = new List<CircleOptionVM>();
        public IEnumerable<SsaOptionVM> Ssas { get; set; } = new List<SsaOptionVM>();
        public CscAdminCreateVM NewUser { get; set; } = new CscAdminCreateVM();
        public CtopMaster? SelectedCtop { get; set; }
        public ExistingUserVM? ExistingUser { get; set; }
        public bool AccountExists { get; set; }
    }

    public class CscAdminCreateVM
    {
        [Required]
        [Display(Name = "CTOPUP No")]
        public string? ctopupno { get; set; }

        [Required]
        [Display(Name = "Staff Name")]
        public string? staff_name { get; set; }

        [Required]
        [Display(Name = "Mobile")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Mobile must be 10 digits")]
        public string? mobile { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? email { get; set; }

        [Required]
        [Display(Name = "HR No")]
        public long hrno { get; set; }

        [Display(Name = "Designation Code")]
        public string? designation_code { get; set; }

        [Required]
        [Display(Name = "SSA Code")]
        public string? ssa_code { get; set; }

        [Required]
        [Display(Name = "Circle")]
        public string? circle { get; set; }

        public long? circle_id { get; set; }
        public long? ssa_id { get; set; }
        public long? role_id { get; set; }
    }

    public class ExistingUserVM
    {
        public string? staff_name { get; set; }
        public string? mobile { get; set; }
        public string? email { get; set; }
        public long hrno { get; set; }
        public string? designation_code { get; set; }
        public string? ssa_code { get; set; }
        public string? circle_name { get; set; }
        public string? record_status { get; set; }
    }

    public class CtopSearchResultVM
    {
        public string? ctopupno { get; set; }
        public string? name { get; set; }
        public string? contact_number { get; set; }
    }
}

