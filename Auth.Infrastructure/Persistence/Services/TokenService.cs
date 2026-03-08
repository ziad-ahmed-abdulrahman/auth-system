using Auth.Core.Aggregates.User;
using Auth.Core.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Infrastructure.Persistence.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;

        public TokenService(IConfiguration configuration, UserManager<User> userManager)
        {
            _configuration = configuration;
            _userManager = userManager;
        }

        public async Task<JwtSecurityToken> CreateToken(User user)
        {
            // 1. Define the user claims
            var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            // 2. Add user roles to claims 
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var roleName in userRoles)
            {
                userClaims.Add(new Claim(ClaimTypes.Role, roleName));
            }

            // 3. Set up the signing credentials
            var symmKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecritKey"]!));
            var signCred = new SigningCredentials(symmKey, SecurityAlgorithms.HmacSha256);

            // 4. Calculate token expiration time
            var expirationMinutes = Convert.ToDouble(_configuration["JWT:ExpirationInMinutes"] ?? "15");
            var expiryDate = DateTime.UtcNow.AddMinutes(expirationMinutes);

            // 5. Generate and return the token response
            var token = new JwtSecurityToken(
                audience: _configuration["JWT:AudienceIP"],
                issuer: _configuration["JWT:IssuerIP"],
                expires: expiryDate,
                claims: userClaims,
                signingCredentials: signCred
            );

            return token;
        }
    }
}
