using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PWSV.Domain.Entities;

namespace PWSV.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts", t => t.HasTrigger("trg_Accounts_PreventDeleteWhenHasTransactions"));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.Balance)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(a => a.AccountNumberCipher)
            .HasMaxLength(512);

        builder.Property(a => a.AccountNumberNonce)
            .HasMaxLength(16);

        builder.Property(a => a.AccountNumberTag)
            .HasMaxLength(16);

        builder.Property(a => a.IsActive)
            .HasDefaultValue(true);

        builder.Property(a => a.RowVersion)
            .IsRowVersion();

        builder.Property(a => a.CreatedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(a => a.UpdatedAt)
            .HasColumnType("datetime2(3)");

        builder.HasOne(a => a.User)
            .WithMany(u => u.Accounts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AccountType)
            .WithMany(t => t.Accounts)
            .HasForeignKey(a => a.AccountTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Currency)
            .WithMany()
            .HasForeignKey(a => a.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_Accounts_UserId")
            .IncludeProperties(a => new { a.Name, a.Balance });

        builder.HasIndex(a => new { a.UserId, a.IsActive })
            .HasDatabaseName("IX_Accounts_UserId_IsActive");
    }
}
