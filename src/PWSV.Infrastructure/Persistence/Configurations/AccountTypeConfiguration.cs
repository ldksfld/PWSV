using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PWSV.Domain.Entities;

namespace PWSV.Infrastructure.Persistence.Configurations;

public sealed class AccountTypeConfiguration : IEntityTypeConfiguration<AccountType>
{
    public void Configure(EntityTypeBuilder<AccountType> builder)
    {
        builder.ToTable("AccountTypes");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code)
            .HasMaxLength(32)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(t => t.DisplayName)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(t => t.Code).IsUnique();
    }
}
