using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PWSV.Domain.Entities;

namespace PWSV.Infrastructure.Persistence.Configurations;

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("ExchangeRates");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rate)
            .HasColumnType("decimal(18,8)")
            .IsRequired();

        builder.Property(r => r.EffectiveDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(r => r.BaseCurrency)
            .WithMany()
            .HasForeignKey(r => r.BaseCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.QuoteCurrency)
            .WithMany()
            .HasForeignKey(r => r.QuoteCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ExchangeRates_Rate", "[Rate] > 0");
            t.HasCheckConstraint("CK_ExchangeRates_DifferentCurrencies", "[BaseCurrencyId] <> [QuoteCurrencyId]");
        });

        builder.HasIndex(r => new { r.BaseCurrencyId, r.QuoteCurrencyId, r.EffectiveDate })
            .HasDatabaseName("UQ_ExchangeRates")
            .IsUnique();
    }
}
