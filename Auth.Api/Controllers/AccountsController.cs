using Auth.Api.Helper;
using Auth.Core.Aggregates.Token;
using Auth.Core.Aggregates.User;
using Auth.Core.Dtos.Account.Activation;
using Auth.Core.Dtos.Account.ChangePassword;
using Auth.Core.Dtos.Account.Login;
using Auth.Core.Dtos.Account.Registration;
using Auth.Core.Dtos.Account.ResetPassword;
using Auth.Core.Dtos.Token;
using Auth.Core.Dtos.User;
using Auth.Core.Interfaces.Services;
using Auth.Core.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using SendGrid.Helpers.Mail;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace Auth.Api.Controllers
{
    [Route("api/accounts")]
    [EnableRateLimiting("AuthPolicy")]
    [ApiController]
    public class AccountsController : ControllerBase
    {

        #region private fields and ctor 


        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailSender _emailSender;
        private readonly ITokenService _tokenService;
        public AccountsController(IConfiguration configuration,
            IEmailSender emailSender,
            ITokenService tokenService,
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            IWebHostEnvironment env)
        {
            _configuration = configuration;
            _emailSender = emailSender;
            _tokenService = tokenService;
            _userManager = userManager;
            _env = env;
        }
        #endregion

        #region Guest 

        #region Regsiter
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDto regsiterDto)
        {

            var userFromDb = await _userManager.FindByEmailAsync(regsiterDto.Email);

            if (userFromDb != null)
                return BadRequest(new APIResponse<object>(400, "User already exists."));

            var user = new User
            {
                UserName = regsiterDto.Email.Split('@')[0],
                Email = regsiterDto.Email,
                IsActive = false,
                InactivityStartDate = DateTime.UtcNow,
            };

            var result = await _userManager.CreateAsync(user, regsiterDto.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();

                return BadRequest(new APIResponse<object>
                (
                    400,
                    $"Registration failed : {string.Join(", ", errors)}"
                ));
            }

            var userName = regsiterDto.Email.Split('@')[0];

            var templatePath = Path.Combine(_env.ContentRootPath, "Templates/Registration", "AccountCreatedSuccessfully.html");
            var body = await System.IO.File.ReadAllTextAsync(templatePath);
            body = body
                 .Replace("{{username}}", userName)
                 .Replace("{{currentyear}}", "2026");



            var subject = "Activate Your Account";
            await _emailSender.SendEmailAsync(regsiterDto.Email, subject, body);

            return Ok(value: new APIResponse<object>(200, "Account Created Successfully. Activation Requierd"));
        }
        #endregion

        #region Send code to activae account

        [HttpPost("account-activations")]
        public async Task<IActionResult> SendActivationCode(SendActivationCodeDto sendActivationCodeDto)
        {
            var userFromDb = await _userManager.FindByEmailAsync(sendActivationCodeDto.Email);

            if (userFromDb == null)
                return BadRequest(new APIResponse<object>(400, "No account found with this email."));

            if (userFromDb.IsActive == true)
                return BadRequest(new APIResponse<object>(400, "Account already activated"));


            var oneTimeCode = GenerateCode.Generate();

            double codeExpiryMinutes = Convert.ToDouble(_configuration["OTPSetting:codeExpiryMinutes"]);

            userFromDb.OneTimeCode = oneTimeCode;
            userFromDb.OneTimeCodeExpiry = DateTime.UtcNow.AddMinutes(codeExpiryMinutes);
            userFromDb.CodeOperation = "activate";

            await _userManager.UpdateAsync(userFromDb);

            var templatePath = Path.Combine(_env.ContentRootPath, "Templates/Activation", "SendActivationCode.html");
            var body = await System.IO.File.ReadAllTextAsync(templatePath);
            body = body
                 .Replace("{{username}}", userFromDb.UserName)
                 .Replace("{{currentyear}}", "2026")
                 .Replace("{{activationcode}}", oneTimeCode)
                 .Replace("{{activationcodeexpiryminutes}}", codeExpiryMinutes!.ToString());

            var subject = "Activate Your Account";


            await _emailSender.SendEmailAsync(sendActivationCodeDto.Email, subject, body);

            return Ok(new APIResponse<object>(200, "Activation code sent successfully."));
        }

        #endregion

        #region verify activation code 

        [HttpPost("account-activations/verify")]
        public async Task<IActionResult> verifyActivationCode(VerifyActivationCodeDto verifyActivationCodeDto)
        {
            var userFromDb = await _userManager.FindByEmailAsync(verifyActivationCodeDto.Email);


            if (userFromDb == null)
                return BadRequest(new APIResponse<object>(400, "No account found with this email."));

            if (userFromDb.CodeOperation != "activate")
                return BadRequest(new APIResponse<object>(400, "Invalid operation for this code."));

            if (userFromDb.OneTimeCode != verifyActivationCodeDto.Code)
                return BadRequest(new APIResponse<object>(400, "Invalid code for this operation."));

            if (userFromDb.OneTimeCodeExpiry < DateTime.UtcNow)
            {
                userFromDb.OneTimeCode = null;
                userFromDb.OneTimeCodeExpiry = null;
                await _userManager.UpdateAsync(userFromDb);
                return BadRequest(new APIResponse<object>(400, "code expired. Please request a new code."));
            }

            userFromDb.IsActive = true;
            userFromDb.InactivityStartDate = null;
            userFromDb.OneTimeCodeExpiry = null;
            userFromDb.OneTimeCode = null;

            await _userManager.UpdateAsync(userFromDb);

            var templatePath = Path.Combine(_env.ContentRootPath, "Templates/Activation", "AccountActivatedSuccessfully.html");
            var body = await System.IO.File.ReadAllTextAsync(templatePath);
            body = body
                 .Replace("{{username}}", userFromDb.UserName)
                 .Replace("{{currentyear}}", "2026");

            var subject = "Account Activated Successfully";

            await _emailSender.SendEmailAsync(verifyActivationCodeDto.Email, subject, body);

            return Ok(new APIResponse<object>(200, ("Account activated successfully.")));

        }

        #endregion

        #region Send code to Reset Password

        [HttpPost("account-resets")]
        public async Task<IActionResult> SendResetCode(SendResetCodeDto sendResetCodeDto)
        {
            var userFromDb = await _userManager.FindByEmailAsync(sendResetCodeDto.Email);

            if (userFromDb == null)
                return BadRequest(new APIResponse<object>(400, "No account found with this email."));

            if (userFromDb.IsActive == false)
                return BadRequest(new APIResponse<object>(400, "you must activate account first"));


            var oneTimeCode = GenerateCode.Generate();

            double codeExpiryMinutes = Convert.ToDouble(_configuration["OTPSetting:codeExpiryMinutes"]);

            userFromDb.OneTimeCode = oneTimeCode;
            userFromDb.OneTimeCodeExpiry = DateTime.UtcNow.AddMinutes(codeExpiryMinutes);
            userFromDb.CodeOperation = "reset_password";

            await _userManager.UpdateAsync(userFromDb);

            var templatePath = Path.Combine(_env.ContentRootPath, "Templates/ResetPassword", "SendResetCode.html");
            var body = await System.IO.File.ReadAllTextAsync(templatePath);
            body = body
                 .Replace("{{username}}", userFromDb.UserName)
                 .Replace("{{currentyear}}", "2026")
                 .Replace("{{resetcode}}", oneTimeCode)
                 .Replace("{{resetcodeexpiryminutes}}", codeExpiryMinutes!.ToString());

            var subject = "Reset Your Password";
            await _emailSender.SendEmailAsync(sendResetCodeDto.Email, subject, body);

            return Ok(new APIResponse<object>(200, "Password reset code sent successfully."));
        }

        #endregion

        #region verify reset code 

        [HttpPost("account-resets/verify")]
        public async Task<IActionResult> verifyResetCode(VerifyResetCodeDto verifyResetCodeDto)
        {

            var userFromDb = await _userManager
               .Users.Include(u => u.RefreshTokens)
               .FirstOrDefaultAsync(u => u.Email == verifyResetCodeDto.Email);

            if (userFromDb == null)
                return BadRequest(new APIResponse<object>(400, "No account found with this email."));

            if (userFromDb.IsActive == false)
                return BadRequest(new APIResponse<object>(400, "you must activate account first"));

            if (userFromDb.CodeOperation != "reset_password")
                return BadRequest(new APIResponse<object>(400, "Invalid operation for this code."));

            if (userFromDb.OneTimeCode != verifyResetCodeDto.Code)
                return BadRequest(new APIResponse<object>(400, "Invalid code for this operation."));

            if (userFromDb.OneTimeCodeExpiry < DateTime.UtcNow)
            {
                userFromDb.OneTimeCode = null;
                userFromDb.OneTimeCodeExpiry = null;
                await _userManager.UpdateAsync(userFromDb);
                return BadRequest(new APIResponse<object>(400, "code expired. Please request a new code."));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(userFromDb);
            var resetResult = await _userManager.ResetPasswordAsync(userFromDb, token, verifyResetCodeDto.NewPassword);


            if (!resetResult.Succeeded)
            {
                var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                return BadRequest(new APIResponse<object>(400, $"Password reset failed: {string.Join(", ", errors)}"));
            }

            userFromDb.OneTimeCodeExpiry = null;
            userFromDb.OneTimeCode = null;

            userFromDb.RefreshTokens?.Clear();
            userFromDb.SecurityStamp = Guid.NewGuid().ToString();

            await _userManager.UpdateAsync(userFromDb);

            var templatePath = Path.Combine(_env.ContentRootPath, "Templates/ResetPassword", "PasswordResetedSuccessfully.html");
            var body = await System.IO.File.ReadAllTextAsync(templatePath);
            body = body
                 .Replace("{{username}}", userFromDb.UserName)
                 .Replace("{{currentyear}}", "2026");

            var subject = "🔑 Password Reset Successfully";

            await _emailSender.SendEmailAsync(verifyResetCodeDto.Email, subject, body);

            return Ok(new APIResponse<object>(200, "Your password has been reset successfully!"));

        }

        #endregion

        #region Send code to Change Password

        [HttpPost("account-passwordchanges")]
        public async Task<IActionResult> SendChangePasswordCode(SendChangePasswordCodeDto sendChangePasswordCodeDto)
        {
            var userFromDb = await _userManager.FindByEmailAsync(sendChangePasswordCodeDto.Email);

            if (userFromDb == null)
                return BadRequest(new APIResponse<object>(400, "No account found with this email."));

            if (userFromDb.IsActive == false)
                return BadRequest(new APIResponse<object>(400, "you must activate account first"));

            var oneTimeCode = GenerateCode.Generate();

            double codeExpiryMinutes = Convert.ToDouble(_configuration["OTPSetting:codeExpiryMinutes"]);

            userFromDb.OneTimeCode = oneTimeCode;
            userFromDb.OneTimeCodeExpiry = DateTime.UtcNow.AddMinutes(codeExpiryMinutes);
            userFromDb.CodeOperation = "change_password";

            await _userManager.UpdateAsync(userFromDb);

            var templatePath = Path.Combine(_env.ContentRootPath, "Templates/ChangePassword", "SendChangePasswordCode.html");
            var body = await System.IO.File.ReadAllTextAsync(templatePath);
            body = body
                 .Replace("{{username}}", userFromDb.UserName)
                 .Replace("{{currentyear}}", "2026")
                 .Replace("{{change_password_code}}", oneTimeCode)
                 .Replace("{{change_password_code_expiryMinutes}}", codeExpiryMinutes!.ToString());

            var subject = "Change Your Password";
            await _emailSender.SendEmailAsync(sendChangePasswordCodeDto.Email, subject, body);

            return Ok(new APIResponse<object>(200, "Password Change code sent successfully."));
        }

        #endregion

        #region verify Change Password code  

        [HttpPost("account-passwordchanges/verify")]
        public async Task<IActionResult> verifyCahngePasswordCode(VerifyChangePassworCodeDto verifyChangePassworCodeDto)
        {
            var userFromDb = await _userManager
                          .Users.Include(u => u.RefreshTokens)
                          .FirstOrDefaultAsync(u => u.Email == verifyChangePassworCodeDto.Email);
            if (userFromDb == null)
                return BadRequest(new APIResponse<object>(400, "No account found with this email."));

            if (userFromDb.IsActive == false)
                return BadRequest(new APIResponse<object>(400, "you must activate account first"));

            if (userFromDb.CodeOperation != "change_password")
                return BadRequest(new APIResponse<object>(400, "Invalid operation for this code."));

            if (userFromDb.OneTimeCode != verifyChangePassworCodeDto.Code)
                return BadRequest(new APIResponse<object>(400, "Invalid code for this operation."));

            if (userFromDb.OneTimeCodeExpiry < DateTime.UtcNow)
            {
                userFromDb.OneTimeCode = null;
                userFromDb.OneTimeCodeExpiry = null;
                await _userManager.UpdateAsync(userFromDb);
                return BadRequest(new APIResponse<object>(400, "code expired. Please request a new code."));
            }


            var resetResult = await _userManager.ChangePasswordAsync(userFromDb, verifyChangePassworCodeDto.OldPassword, verifyChangePassworCodeDto.NewPassword);

            if (!resetResult.Succeeded)
            {
                var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                return BadRequest(new APIResponse<object>(400, $"Password reset failed: {string.Join(", ", errors)}"));
            }

            userFromDb.OneTimeCodeExpiry = null;
            userFromDb.OneTimeCode = null;

            userFromDb.RefreshTokens?.Clear();
            userFromDb.SecurityStamp = Guid.NewGuid().ToString();
            await _userManager.UpdateAsync(userFromDb);

            var templatePath = Path.Combine(_env.ContentRootPath, "Templates/ChangePassword", "PasswordChangedSuccessfully.html");
            var body = await System.IO.File.ReadAllTextAsync(templatePath);
            body = body
                 .Replace("{{username}}", userFromDb.UserName)
                 .Replace("{{currentyear}}", "2026");

            var subject = "🔑 Password Changed Successfully";

            await _emailSender.SendEmailAsync(verifyChangePassworCodeDto.Email, subject, body);

            return Ok(new APIResponse<object>(200, "Your password has been Changed successfully!"));

        }

        #endregion

        #region Login

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto userFromRequest)
        {
            var user = await _userManager
                .Users.Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Email == userFromRequest.Email);

            if (user == null)
                return BadRequest(new APIResponse<object>(400, "Email or Password is incorrect"));

            bool passwordValid = await _userManager.CheckPasswordAsync(user, userFromRequest.Password);

            if (!passwordValid)
                return BadRequest(new APIResponse<object>(400, "Email or Password is incorrect"));

            var daysThreshold = _configuration["InactivitySettings:InactivityDaysThreshold"] ?? "30";

            if (user.IsActive == false)
                return BadRequest(new APIResponse<object>(400,
                  $"This account is currently deactivated. You have a {daysThreshold}-day grace period " +
        $"to recover it by verifying a new activation code. " +
        $"After {daysThreshold} days, the account and all its data will be permanently deleted."));

            var activeTokens = user.RefreshTokens?.OrderBy(t => t.CreatedOn).ToList();
            if (activeTokens?.Count >= 5)
            { 
                var oldestToken = activeTokens[0];
                user.RefreshTokens!.Remove(oldestToken); 
            }

            var refreshTokenExpirationInDays = Convert.ToDouble(_configuration["JWT:refreshTokenExpirationInDays"] ?? "7");
            var newRefreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresOn = DateTime.UtcNow.AddDays(refreshTokenExpirationInDays),
                CreatedOn = DateTime.UtcNow,
                UserId = user.Id
            };

            user.RefreshTokens!.Add(newRefreshToken);
            await _userManager.UpdateAsync(user);

            var jwtToken = await  _tokenService.CreateToken(user);

            return Ok(new APIResponse<object>(200, "Login successful", new TokenResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                RefreshToken = newRefreshToken.Token,
                ExpiryDate = jwtToken.ValidTo
            }));
        }
        #endregion

        #region Refresh Token

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequestDto dto)
        {
            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.RefreshTokens!.Any(t => t.Token == dto.RefreshToken));

            if (user == null)
                return BadRequest(new APIResponse<object>(400, "Invalid Refresh Token."));

            var daysThreshold = _configuration["InactivitySettings:InactivityDaysThreshold"] ?? "30";

            if (user.IsActive == false)
                return BadRequest(new APIResponse<object>(400,
                  $"This account is currently deactivated. You have a {daysThreshold}-day grace period " +
        $"to recover it by verifying a new activation code. " +
        $"After {daysThreshold} days, the account and all its data will be permanently deleted."));

            var currentToken = user.RefreshTokens?.Single(t => t.Token == dto.RefreshToken);

            if (!currentToken!.IsActive)
                return BadRequest(new APIResponse<object>(400, "Refresh Token is expired or revoked."));

            // 1. Create a new refresh token
            var refreshTokenExpirationInDays = Convert.ToDouble(_configuration["JWT:refreshTokenExpirationInDays"] ?? "7");
            var newRefreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresOn = DateTime.UtcNow.AddDays(refreshTokenExpirationInDays),
                CreatedOn = DateTime.UtcNow,
                UserId = user.Id
            };

            // 2. Token Rotation: Remove the used token and add a new one directly to the user's list
            user.RefreshTokens?.Remove(currentToken);
            user.RefreshTokens?.Add(newRefreshToken);

            // 3. Persist changes (EF will handle deleting the old token and adding the new one automatically)
            await _userManager.UpdateAsync(user);

            await _userManager.UpdateAsync(user);

            // 4. Use the unified TokenService to generate a new Access Token (JWT)
            var jwtToken = await _tokenService.CreateToken(user);

            return Ok(new APIResponse<object>(200, "refreshed successful", new TokenResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                RefreshToken = newRefreshToken.Token,
                ExpiryDate = jwtToken.ValidTo
            }));
        }
        #endregion

        #endregion

        #region User

        #region Get Me
        [HttpGet("me")]
        [Authorize]

        public async Task<IActionResult> GetMe()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (claim == null)
                return StatusCode(401, new APIResponse<object>(401, "Your session has expired. Please log in again."));

            var user = await _userManager.Users
             .Include(u => u.Address)
             .FirstOrDefaultAsync(u => u.Email == claim);

            if (user == null)
                return NotFound(new APIResponse<string>(404, "User not found."));

            if (user.IsActive == false)
            {
                return BadRequest(new APIResponse<object>(400, "Your account is not active. You should activate it first!"));
            }

            if (user.Address == null)
                user.Address = new Address();


            return Ok(new APIResponse<object>(200, "User data retrieved successfully", new UserDto
            {
                Email = user?.Email,
                UserName = user?.UserName,
                Id = user?.Id,
                FirstName = user?.FirstName,
                LastName = user?.LastName,
                PhoneNumber = user?.PhoneNumber,
                Address = new AddressDto
                {
                    Street = user?.Address?.Street,
                    City = user?.Address?.City,
                    State = user?.Address?.State,
                    ZipCode = user?.Address?.ZipCode,
                    Country = user?.Address?.Country,
                    Government = user?.Address?.Government,
                }
            }));

        }


        #endregion

        #region Update Me
        [HttpPatch("me")]
        [Authorize]

        public async Task<IActionResult> UpdateMe(UpdateMeDto updateMeDto)
        {
            if (updateMeDto == null)
                return BadRequest(new APIResponse<object>(400, "Empty data: please provide the fields to be updated"));

            var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (claim == null)
                return StatusCode(401, new APIResponse<object>(401, "Your session has expired. Please log in again."));

            var user = await _userManager.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.Email == claim);


            if (user == null)
                return NotFound(new APIResponse<string>(404, "User not found."));

            if (user.IsActive == false)
            {
                return BadRequest(new APIResponse<object>(400, "Your account is not active. You should activate it first!"));
            }

            if (user.Address == null)
                user.Address = new Address();

            user.Address.Street = updateMeDto.Street ?? user.Address.Street;
            user.Address.City = updateMeDto.City ?? user.Address.City;
            user.Address.State = updateMeDto.State ?? user.Address.State;
            user.Address.ZipCode = updateMeDto.ZipCode ?? user.Address.ZipCode;
            user.Address.Country = updateMeDto.Country ?? user.Address.Country;
            user.Address.Government = updateMeDto.Government ?? user.Address.Government;

            user.FirstName = updateMeDto.FirstName ?? user.FirstName;
            user.LastName = updateMeDto.LastName ?? user.LastName;
            user.PhoneNumber = updateMeDto.PhoneNumber ?? user.PhoneNumber;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(e => e.Description).ToList();

                return BadRequest(new APIResponse<object>
                (
                    400,
                    $"Update failed : {string.Join(", ", errors)}"
                ));
            }


            return Ok(new APIResponse<object>(200, "Updated Successfully"));
        }

        #endregion

        #region Delete Me => Soft Delete (Deactivate Account - Permanently deleted after 30 days)

        [HttpDelete("me")]
        [Authorize]
        public async Task<IActionResult> DeleteMe()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (claim == null)
                return StatusCode(401, new APIResponse<object>(401, "Your session has expired. Please log in again."));

            var user = await _userManager.FindByEmailAsync(claim);
            if (user == null || !user.IsActive)
            {
                return BadRequest(new APIResponse<object>(400, message: "This account is deactivated and scheduled for permanent deletion. " +
                    "You have 30 days from the deactivation date to recover it," +
                    " or it will be removed permanently."));
            }

            // soft delete
            user.IsActive = false;
            user.InactivityStartDate = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(e => e.Description).ToList();

                return BadRequest(new APIResponse<object>
                (
                    400,
                    $"Deletion failed : {string.Join(", ", errors)}"
                ));
            }

            return Ok(new APIResponse<object>(200, "Deleted successfully"));


        }

        #endregion

        #endregion

        #region Admin & Manager

        #region  Add User

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AddUser(AddUserDto userFromRequest)
        {
            if (userFromRequest == null)
                return BadRequest(new APIResponse<object>(400, "Empty data: please provide user details"));

            if (string.IsNullOrEmpty(userFromRequest.Role))
                userFromRequest.Role = "User";

            var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (claim == null)
                return StatusCode(401, new APIResponse<object>(401, "Your session has expired. Please log in again."));

            var currentUser = await _userManager.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.Email == claim);


            var roles = await _userManager.GetRolesAsync(currentUser!);

            var existingEmailUser = await _userManager.FindByEmailAsync(userFromRequest.Email);


            if (existingEmailUser != null)
                return BadRequest(new APIResponse<object>(400, "Email is already registered"));

            if (roles.Contains("Admin") && userFromRequest.Role == "Manager")
            {
                return BadRequest(new APIResponse<object>(404, "Admins cannot create Manager accounts."));
            }

            var user = new User()
            {
                UserName = userFromRequest.Email.Split('@')[0],
                Email = userFromRequest.Email,
                IsActive = true
            };

            IdentityResult CreateUserResult = await _userManager.CreateAsync(user, userFromRequest.Password);

            if (CreateUserResult.Succeeded)
            {
                var AddRoleResult = await _userManager.AddToRoleAsync(user, userFromRequest.Role);

                if (AddRoleResult.Succeeded)
                {
                    return Ok(new APIResponse<object>(200, "Account created and activated successfully!"));
                }
                // Using string.Join to keep the message property a clean string
                var roleErrors = string.Join(", ", AddRoleResult.Errors.Select(e => e.Description));
                return BadRequest(new APIResponse<object>(400, roleErrors));
            }

            var userErrors = string.Join(", ", CreateUserResult.Errors.Select(e => e.Description));
            return BadRequest(error: new APIResponse<object>(400, userErrors));
        }
        #endregion

        #region Get User
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new APIResponse<object>(400, "Invalid ID"));

            var user = await _userManager.Users
            .Include(u => u.Address)
            .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound(new APIResponse<object>(404, "user not found"));

            return Ok(new APIResponse<object>(200, "User data retrieved successfully", new UserForAdminDto
            {
                Email = user.Email,
                UserName = user.UserName,
                Id = user.Id,
                IsActive = user.IsActive,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Address = new AddressDto
                {
                    Street = user?.Address?.Street,
                    City = user?.Address?.City,
                    State = user?.Address?.State,
                    ZipCode = user?.Address?.ZipCode,
                    Country = user?.Address?.Country,
                    Government = user?.Address?.Government,
                }
            }));

        }

        #endregion

        #region Get all Users (support sort, search, pagination)

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")] // السماح للأدمن والمانجر معاً
        public async Task<IActionResult> GetAll([FromQuery] UserParams getUsersParams)
        {
            // Base query on the Users table
            var query = _userManager.Users
                        .AsNoTracking()
                        .AsQueryable();

            // Filter by search keyword
            if (!string.IsNullOrEmpty(getUsersParams.Search))
            {
                var searchWords = getUsersParams.Search.Split(' ');
                query = query.Where(u => searchWords.All(word =>
                (u.UserName != null && u.UserName.ToLower().Contains(word.ToLower())) ||
                (u.Email != null && u.Email.ToLower().Contains(word.ToLower()))
  ));
            }

            // Sorting
            if (!string.IsNullOrEmpty(getUsersParams.Sort))
            {
                query = getUsersParams.Sort switch
                {
                    "NameAsc" => query.OrderBy(u => u.UserName),
                    "NameDesc" => query.OrderByDescending(u => u.UserName),
                    "EmailAsc" => query.OrderBy(u => u.Email),
                    "EmailDesc" => query.OrderByDescending(u => u.Email),
                    _ => query.OrderBy(u => u.UserName) // Default sort
                };
            }
            else
            {
                query = query.OrderBy(u => u.UserName); // Default sort
            }

            // Total count before pagination
            var totalCount = await query.CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / getUsersParams.PageSize);

            // Pagination
            var users = await query
                .Skip((getUsersParams.PageNumber - 1) * getUsersParams.PageSize)
                .Take(getUsersParams.PageSize)
                .ToListAsync();

            // Map users to DTO (or anonymous object)
            var usersDto = users.Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.IsActive
            }).ToList();

            var message = totalCount == 0
    ? "No users found."
    : (getUsersParams.PageNumber > totalPages && totalPages > 0
        ? $"You've reached the end. Only {totalPages} page exists."
        : "User retrieved successfully!");

            // Return the result
            return Ok(new APIResponse<object>(200,
                  message,
            new
            {
                TotalCount = totalCount,
                PageSize = getUsersParams.PageSize,
                TotalPages = totalPages,
                PageNumber = getUsersParams.PageNumber,
                Data = usersDto
            }));
        }

        #endregion

        #region Update User

        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateUser([FromRoute] string id, UpdateUserDto updateUserDto)
        {
            if (updateUserDto == null)
                return BadRequest(new APIResponse<object>(400, "empty data"));

            if (id == null)
                return BadRequest(new APIResponse<object>(400, "Invalid Id"));

            var user = await _userManager.Users
              .Include(u => u.Address)
              .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound(new APIResponse<object>(404, "User not found"));

            var isManager = await _userManager.IsInRoleAsync(user, "Manager");

            if (isManager)
            {
                return Forbid();
            }

            var isActiveBeforeUpdate = user.IsActive;
            var isActiveAfterUpdate = updateUserDto.IsActive;


            if (user.Address == null)
                user.Address = new Address();

            user.Address.Street = updateUserDto.Street ?? user.Address.Street;
            user.Address.City = updateUserDto.City ?? user.Address.City;
            user.Address.State = updateUserDto.State ?? user.Address.State;
            user.Address.ZipCode = updateUserDto.ZipCode ?? user.Address.ZipCode;
            user.Address.Country = updateUserDto.Country ?? user.Address.Country;
            user.Address.Government = updateUserDto.Government ?? user.Address.Government;

            user.FirstName = updateUserDto.FirstName ?? user.FirstName;
            user.LastName = updateUserDto.LastName ?? user.LastName;
            user.PhoneNumber = updateUserDto.PhoneNumber ?? user.PhoneNumber;
            user.IsActive = updateUserDto.IsActive ?? user.IsActive;

            if (isActiveBeforeUpdate && !user.IsActive)
                user.InactivityStartDate = DateTime.UtcNow;

            if (!isActiveBeforeUpdate && user.IsActive)
                user.InactivityStartDate = null;


            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(e => e.Description).ToList();

                return BadRequest(new APIResponse<object>
                (
                    400,
$"Update failed: {string.Join(", ", errors)}"));
            }


            return Ok(new APIResponse<object>(200, "updated successfully"));

        }

        #endregion

        #region Delete User

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]

        public async Task<IActionResult> HardDeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new APIResponse<object>(404, "User not found"));

            var isManager = await _userManager.IsInRoleAsync(user, "Manager");

            if (isManager)
            {
                return Forbid();
            }

            // hard delete
            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new APIResponse<object>(400, errors));
            }

            return Ok(new APIResponse<object>(200, "Deleted successfully"));
        }

        #endregion

        #endregion

    }
}
