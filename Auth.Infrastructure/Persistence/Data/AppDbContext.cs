using Auth.Core.Aggregates.Token;
using Auth.Core.Aggregates.User;
using Auth.Infrastructure.Persistence.Data.Config;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection;


namespace Auth.Infrastructure.Persistence.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        private readonly IConfiguration _configuration;

        public AppDbContext(DbContextOptions<AppDbContext> options , IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<RefreshToken> RefreshTokens {  get; set; }
        public DbSet<Address> Addresses { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // User table
            modelBuilder.Entity<User>(b =>
            {
                b.ToTable("Users"); 
            });

            // Role table
            modelBuilder.Entity<IdentityRole>(b =>
            {
                b.ToTable("Roles"); 
            });

            // UserRoles
            modelBuilder.Entity<IdentityUserRole<string>>(b =>
            {
                b.ToTable("UserRoles");
            });

            // UserClaims
            modelBuilder.Entity<IdentityUserClaim<string>>(b =>
            {
                b.ToTable("UserClaims");
            });

            // RoleClaims
            modelBuilder.Entity<IdentityRoleClaim<string>>(b =>
            {
                b.ToTable("RoleClaims");
            });

            // UserLogins
            modelBuilder.Entity<IdentityUserLogin<string>>(b =>
            {
                b.ToTable("UserLogins");
            });

            // UserTokens
            modelBuilder.Entity<IdentityUserToken<string>>(b =>
            {
                b.ToTable("UserTokens");
            });

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());



            var MANAGER_ROLE_ID = "c55e8d1a-8c7e-4b4e-8f9d-1a2b3c4d5e6f";
            var ADMIN_ROLE_ID = "fab4fac1-c546-41de-aebc-a14da6895711";
            var USER_ROLE_ID = "b74ddd14-6340-4840-95c2-db12554843e5";

            var MANAGER_ID = "7d8e9f0a-b1c2-3d4e-5f6a-7b8c9d0e1f2a";
            var ADMIN_ID = "5f2d9e8a-b1c4-4e7a-9d6b-3f2a1c5d8e90";
            var USER_ID = "a1b2c3d4-e5f6-4a5b-bc6d-7e8f9a0b1c2d";

            var managerEmail = _configuration["UsersData:Manager:Email"]!;
            var managerPass = _configuration["UsersData:Manager:Password"]!;
            var adminEmail = _configuration["UsersData:Admin:Email"]!;
            var adminPass = _configuration["UsersData:Admin:Password"]!;
            var userEmail = _configuration["UsersData:User:Email"]!;
            var userPass = _configuration["UsersData:User:Password"]!;

            // 1. Roles Seed
            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = MANAGER_ROLE_ID, Name = "Manager", NormalizedName = "MANAGER" },
                new IdentityRole { Id = ADMIN_ROLE_ID, Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = USER_ROLE_ID, Name = "User", NormalizedName = "USER" }
            );

            var ph = new PasswordHasher<User>();

            // 2. Manager Seed
            var manager = new User
            {
                Id = MANAGER_ID,
                UserName = managerEmail.Split('@')[0],
                NormalizedUserName = managerEmail.Split('@')[0].ToUpper(),
                Email = managerEmail,
                NormalizedEmail = managerEmail.ToUpper(),
                EmailConfirmed = true,
                IsActive = true,
                SecurityStamp = "STATIC_STAMP_ADMIN_1",
                ConcurrencyStamp = "STATIC_CONCURRENCY_1"
            };
            manager.PasswordHash = ph.HashPassword(manager, managerPass);

            // 2. Admin Seed
            var admin = new User
            {
                Id = ADMIN_ID,
                UserName = adminEmail.Split('@')[0],
                NormalizedUserName = adminEmail.Split('@')[0].ToUpper(),
                Email = adminEmail,
                NormalizedEmail = adminEmail.ToUpper(),
                EmailConfirmed = true,
                IsActive = true,
                SecurityStamp = "STATIC_STAMP_ADMIN_1",
                ConcurrencyStamp = "STATIC_CONCURRENCY_1"
            };
            admin.PasswordHash = ph.HashPassword(admin, adminPass);

            // 3. User Seed
            var user = new User
            {
                Id = USER_ID,
                UserName = userEmail.Split('@')[0],
                NormalizedUserName = userEmail.Split('@')[0].ToUpper(),
                Email = userEmail,
                NormalizedEmail = userEmail.ToUpper(),
                EmailConfirmed = true,
                IsActive = true,
                SecurityStamp = "STATIC_STAMP_ADMIN_2",
                ConcurrencyStamp = "STATIC_CONCURRENCY_2"
            };
            user.PasswordHash = ph.HashPassword(user, userPass);

            modelBuilder.Entity<User>().HasData(manager, admin, user);

            // 4. Map Roles
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string> { RoleId = MANAGER_ROLE_ID, UserId = MANAGER_ID },
                new IdentityUserRole<string> { RoleId = ADMIN_ROLE_ID, UserId = ADMIN_ID },
                new IdentityUserRole<string> { RoleId = USER_ROLE_ID, UserId = USER_ID }
            );
        }



    }
}
