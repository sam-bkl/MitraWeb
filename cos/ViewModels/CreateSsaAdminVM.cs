using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace cos.ViewModels
{
    public class CreateSsaAdminPageVM
    {
        public IEnumerable<SsaAdminListVM> SsaAdmins { get; set; } = new List<SsaAdminListVM>();
        public IEnumerable<CircleOptionVM> Circles { get; set; } = new List<CircleOptionVM>();
        public IEnumerable<SsaOptionVM> Ssas { get; set; } = new List<SsaOptionVM>();
        public SsaAdminCreateVM NewUser { get; set; } = new SsaAdminCreateVM();
        public string? CircleAdminCircle { get; set; } // circle_code of the logged-in circle_admin
    }

    public class SsaAdminCreateVM
    {
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

    public class SsaAdminListVM
    {
        public long account_id { get; set; }
        public string? staff_name { get; set; }
        public string? mobile { get; set; }
        public string? email { get; set; }
        public long hrno { get; set; }
        public string? designation_code { get; set; }
        public string? ssa_code { get; set; }
        public string? circle_name { get; set; }
        public string? record_status { get; set; }
        public DateTime? created_on { get; set; }
    }
}

