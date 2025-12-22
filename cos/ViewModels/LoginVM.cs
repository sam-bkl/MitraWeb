using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace cos.ViewModels
{
    public class LoginVM
    {
        [DisplayName("Username")]
        [Required(ErrorMessage = "Please enter your username.")]
        public string username { get; set; } = string.Empty;

        [DisplayName("Password")]
        [Required(ErrorMessage = "Please enter your password")]
        public string password { get; set; } = string.Empty;

        public string? captcha_input { get; set; }

        public string? otp { get; set; }
        
        public bool otp_sent { get; set; } = false;
    }
}
