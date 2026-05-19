using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PWSV.Domain.Entities;

namespace PWSV.Infrastructure.Persistence.Configurations;

public sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .HasMaxLength(10)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(c => c.Name)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(c => c.Symbol)
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(c => c.DecimalPlaces)
            .HasColumnType("tinyint");

        builder.ToTable(t => t.HasCheckConstraint("CK_Currencies_DecimalPlaces", "[DecimalPlaces] BETWEEN 0 AND 8"));

        builder.HasIndex(c => c.Code).IsUnique();
    }
}
