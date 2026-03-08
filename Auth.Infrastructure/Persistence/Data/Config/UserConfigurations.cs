using Auth.Core.Aggregates.User;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence.Data.Config
{
    internal class UserConfigurations : IEntityTypeConfiguration<User>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<User> builder)
        {
            builder.Property(u => u.FirstName)
                    .IsRequired(false)    // optional
                    .HasMaxLength(20);

            // LastName
            builder.Property(propertyExpression: u => u.LastName)
                   .IsRequired(false)
                   .HasMaxLength(20);

            // One-to-One relationship with Address
            builder.HasOne(u => u.Address)
                   .WithOne(a => a.User)
                   .HasForeignKey<Address>(a => a.UserId);


        }

    }
}
