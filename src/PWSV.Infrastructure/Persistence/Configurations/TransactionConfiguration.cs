using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PWSV.Domain.Entities;
using PWSV.Domain.Enums;

namespace PWSV.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions", t =>
        {
            t.HasTrigger("trg_Transactions_AfterInsert");
            t.HasTrigger("trg_Transactions_AfterUpdate");
            t.HasTrigger("trg_Transactions_AfterDelete");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Kind)
            .HasColumnType("char(1)")
            .HasConversion(
                kind => ((char)kind).ToString(),
                value => (TransactionKind)value[0])
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(t => t.OccurredAt)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.Property(t => t.DescriptionCipher)
            .HasMaxLength(2048);

        builder.Property(t => t.DescriptionNonce)
            .HasMaxLength(16);

        builder.Property(t => t.DescriptionTag)
            .HasMaxLength(16);

        builder.Property(t => t.Counterparty)
            .HasMaxLength(256);

        builder.Property(t => t.CreatedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(t => t.UpdatedAt)
            .HasColumnType("datetime2(3)");

        builder.HasOne(t => t.Account)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(t => t.LinkedTransaction)
            .WithMany()
            .HasForeignKey(t => t.LinkedTransactionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Transactions_Kind", "[Kind] IN ('I','E','T')");
            t.HasCheckConstraint("CK_Transactions_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_Transactions_TransferShape",
                "([Kind] = 'T' AND [CategoryId] IS NULL) OR ([Kind] IN ('I','E') AND [CategoryId] IS NOT NULL)");
        });

        builder.HasIndex(t => new { t.AccountId, t.OccurredAt })
            .HasDatabaseName("IX_Transactions_AccountId_OccurredAt")
            .IsDescending(false, true);

        builder.HasIndex(t => t.CategoryId)
            .HasDatabaseName("IX_Transactions_CategoryId")
            .HasFilter("[CategoryId] IS NOT NULL");

        builder.HasIndex(t => t.OccurredAt)
            .HasDatabaseName("IX_Transactions_OccurredAt")
            .IsDescending(true);

        builder.HasIndex(t => t.LinkedTransactionId)
            .HasDatabaseName("IX_Transactions_LinkedTransactionId")
            .HasFilter("[LinkedTransactionId] IS NOT NULL");
    }
}
