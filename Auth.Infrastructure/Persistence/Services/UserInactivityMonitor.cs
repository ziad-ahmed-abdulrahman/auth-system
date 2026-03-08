using Auth.Core.Interfaces.Services;
using Auth.Infrastructure.Persistence.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence.Services
{
    public class UserInactivityMonitor : BackgroundService, IUserInactivityMonitor
    {
        private readonly ILogger<UserInactivityMonitor> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        public UserInactivityMonitor(
            ILogger<UserInactivityMonitor> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("User Inactivity Monitor Service is starting.");

            var intervalInHours = _configuration.GetValue<double>("InactivitySettings:CheckIntervalInHours", 24);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking user inactivity at: {time}", DateTimeOffset.Now);

                try
                {
                    await CheckInactiveUserAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while running the inactivity check.");
                }

                await Task.Delay(TimeSpan.FromHours(intervalInHours), stoppingToken);
            }
        }

        private async Task CheckInactiveUserAsync(CancellationToken ct)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();  

                var daysThreshold = _configuration.GetValue<double>("InactivitySettings:InactivityDaysThreshold", 30);
                var thresholdDate = DateTime.UtcNow.AddDays(-daysThreshold);

                var inactiveUserList = await dbContext.Users
                    .Where(u => !u.IsActive
                                && u.InactivityStartDate.HasValue
                                && u.InactivityStartDate.Value <= thresholdDate)
                    .ToListAsync(ct);

                if (inactiveUserList.Any())
                {
                    _logger.LogWarning("Found {Count} inactive user(s) for deletion.", inactiveUserList.Count);

                    foreach (var user in inactiveUserList)
                    {
                        _logger.LogInformation("Removing user: {Email}, Inactive since: {Date}", user.Email, user.InactivityStartDate);
                        dbContext.Users.Remove(user);
                    }

                    await dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("Successfully removed {Count} inactive user(s).", inactiveUserList.Count);
                }
                else
                {
                    _logger.LogInformation("No inactive user found to process.");
                }
            }
        }
    }
}