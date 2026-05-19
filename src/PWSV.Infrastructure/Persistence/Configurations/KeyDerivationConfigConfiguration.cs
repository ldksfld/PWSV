using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PWSV.Domain.Entities;

namespace PWSV.Infrastructure.Persistence.Configurations;

public sealed class KeyDerivationConfigConfiguration : IEntityTypeConfiguration<KeyDerivationConfig>
{
    public void Configure(EntityTypeBuilder<KeyDerivationConfig> builder)
    {
        builder.ToTable("KeyDerivationConfigs");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Salt)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(k => k.Iterations)
            .HasDefaultValue(100_000);

        builder.Property(k => k.KeyLength)
            .HasDefaultValue(32);

        builder.HasIndex(k => k.UserId).IsUnique();
    }
}
