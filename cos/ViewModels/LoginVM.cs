using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace cos.ViewModels
{
    public class LoginVM
    {
        [DisplayName("Username")]
        [Required(ErrorMessage = "Please enter your username.")]
        public string username { get; set; }

        [DisplayName("Password")]
        [Required(ErrorMessage = "Please enter your password")]
        public string password { get; set; }

        //[DisplayName("OTP")]
        //[Required(ErrorMessage = "Please enter OTP")]
        //[RegularExpression(@"^[0-9]*$", ErrorMessage = "Numbers only")]
        //public string otp { get; set; }
    }
}
