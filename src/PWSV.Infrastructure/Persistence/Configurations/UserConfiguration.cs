using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PWSV.Domain.Entities;

namespace PWSV.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .HasMaxLength(64)
            .IsUnicode()
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(72)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(u => u.UpdatedAt)
            .HasColumnType("datetime2(3)");

        builder.HasIndex(u => u.Username).IsUnique();

        builder.HasOne(u => u.KeyDerivationConfig)
            .WithOne(k => k.User!)
            .HasForeignKey<KeyDerivationConfig>(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
