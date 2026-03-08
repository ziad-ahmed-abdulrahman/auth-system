using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Core.Dtos.User
{
    public class AddUserDto
    {

        [Required(ErrorMessage = "Email is Required")]
        [EmailAddress(ErrorMessage = "wrong format for email")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is Required")]
        public string Password { get; set; } = null!;
        public string? Role { get; set; }
    }
}
