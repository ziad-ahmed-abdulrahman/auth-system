using Auth.Core.Aggregates.Token;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Core.Aggregates.User
{
    public class User : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Address? Address { get; set; }
        public bool IsActive { get; set; } = false;
        public string? OneTimeCode { get; set; }
        public DateTime? OneTimeCodeExpiry { get; set; }
        public string? CodeOperation { get; set; }
        public DateTime? InactivityStartDate { get; set; }
        public List<RefreshToken>? RefreshTokens { get; set; } = new(); 
    }
}
