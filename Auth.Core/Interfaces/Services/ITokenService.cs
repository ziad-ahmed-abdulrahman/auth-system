using Auth.Core.Aggregates.User;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Core.Interfaces.Services
{
    public interface ITokenService
    {
        Task<JwtSecurityToken> CreateToken(User user);
    }
}
