using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace cos.ViewModels
{

    public class LgchkVM
    {
        [Required(ErrorMessage = "Please enter your username.")]
        public string uid { get; set; }

        
        [Required(ErrorMessage = "Please enter your password")]
        public string pid { get; set; }

        public long mobile { get; set; }

        public int hrno { get; set; }
        public long id { get; set; }
    }
}
