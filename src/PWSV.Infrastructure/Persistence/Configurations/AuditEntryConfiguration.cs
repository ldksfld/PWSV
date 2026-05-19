using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PWSV.Domain.Entities;

namespace PWSV.Infrastructure.Persistence.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityName)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.EntityKey)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.Action)
            .HasMaxLength(16)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(a => a.OccurredAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.OccurredAt)
            .HasDatabaseName("IX_AuditEntries_OccurredAt")
            .IsDescending(true);
    }
}
