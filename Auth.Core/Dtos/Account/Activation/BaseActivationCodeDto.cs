using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Core.Dtos.Account.Activation
{
    public class BaseActivationCodeDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}
