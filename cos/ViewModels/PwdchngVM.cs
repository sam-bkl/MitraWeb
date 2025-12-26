using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace cos.ViewModels
{
    public class PwdchngVM
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Newpassword { get; set; }

        [Required]
        [Compare("Newpassword")]
        public string ConfirmPassword { get; set; }
        public string? msg { get; set; }

        [ValidateNever]
        public long update_by {  get; set; }

        [ValidateNever]
        public string? reset_by_User { get; set; }

        public string? modeofoperation { get; set; }
    }
}
