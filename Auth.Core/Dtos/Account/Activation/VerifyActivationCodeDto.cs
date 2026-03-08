using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Core.Dtos.Account.Activation
{
    public class VerifyActivationCodeDto : BaseActivationCodeDto
    {
        [Required]
        public string Code { get; set; } = null!;
    }
}
