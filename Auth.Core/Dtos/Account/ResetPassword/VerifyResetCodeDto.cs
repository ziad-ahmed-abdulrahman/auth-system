using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Core.Dtos.Account.ResetPassword
{
    public class VerifyResetCodeDto : BaseResetCodeDto
    {
        [Required]
        public string Code { get; set; } = null!;

        [Required]
        [PasswordPropertyText]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = null!;
    }
}
