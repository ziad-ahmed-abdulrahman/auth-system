using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Core.Dtos.Account.ChangePassword
{
    public class VerifyChangePassworCodeDto : BaseChangePasswordDto
    {
        [Required]
        public string Code { get; set; } = null!;

        [Required]
        [PasswordPropertyText]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = null!;

        [Required]
        [PasswordPropertyText]
        [Display(Name = "Old Password")]
        public string OldPassword { get; set; } = null!;
    }
}
