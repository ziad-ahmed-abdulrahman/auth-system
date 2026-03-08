using Auth.Api.Helper;
using Auth.Api.Middlewares;
using Auth.Core.Aggregates.User;
using Auth.Core.Interfaces.Services;
using Auth.Core.Shared;
using Auth.Infrastructure.Persistence.Data;
using Auth.Infrastructure.Persistence.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

namespace Auth.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            #region Default API Services

            // AddControllers Service
            builder.Services.AddControllers();

            // AddSwaggerGen & AddEndpointsApiExplorer Service
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            #endregion

            #region DbContext&Identity

            builder.Services
    .AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            #endregion

            #region Maps email sender settings from appsettings.json to the EmailSenderSettings class.

            builder.Services.Configure<GmailSettings>(builder.Configuration.GetSection("GmailSettings"));

            builder.Services.Configure<SendGridSettings>(builder.Configuration.GetSection("SendGridSettings")
);
            #endregion

            #region Dependency Injection

            builder.Services.AddScoped<IEmailSender, SendEmailViaSendGridAdsync>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddHostedService<UserInactivityMonitor>();

            #endregion

            #region Authentication & JWT
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; // check by token 
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; //unauthorize
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JWT:IssuerIP"],

                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JWT:AudienceIP"],

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecritKey"]!)),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,

                    RequireExpirationTime = true,
                };
            });
            #endregion

            #region Rate Limiter
            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("AuthPolicy", partitioner: httpContext =>
                {
                    var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(remoteIp, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 15,
                        Window = TimeSpan.FromSeconds(10),
                        QueueLimit = 0
                    });
                });

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";
                    var response = new APIResponse<object>(429, "Too many requests. Please slow down.");
                    await context.HttpContext.Response.WriteAsJsonAsync(response, token);
                };
            });
            #endregion

            #region Cors
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", policy =>
                {
                    if (builder.Environment.IsDevelopment())
                    {

                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    }
                    else
                    {

                        var origin1 = builder.Configuration["Cors:FirstOrgin"] ?? "https://waiting-for-origin.com";

                        policy.WithOrigins(origin1)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    }
                });
            });

            #endregion

            var app = builder.Build();


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMiddleware<ExceptionMiddleware>();

            app.UseHttpsRedirection();

            app.UseCors("AllowSpecificOrigin");

            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
