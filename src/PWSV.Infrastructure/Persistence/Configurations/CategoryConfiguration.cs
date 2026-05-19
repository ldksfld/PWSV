using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PWSV.Domain.Entities;
using PWSV.Domain.Enums;

namespace PWSV.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(c => c.Kind)
            .HasColumnType("char(1)")
            .HasConversion(
                kind => ((char)kind).ToString(),
                value => (CategoryKind)value[0])
            .IsRequired();

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(c => c.User)
            .WithMany(u => u.Categories)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t => t.HasCheckConstraint("CK_Categories_Kind", "[Kind] IN ('I','E')"));

        builder.HasIndex(c => new { c.UserId, c.ParentCategoryId, c.Name })
            .HasDatabaseName("UQ_Categories")
            .IsUnique();

        builder.HasIndex(c => new { c.UserId, c.Kind, c.IsActive })
            .HasDatabaseName("IX_Categories_UserId_Kind");

        builder.HasIndex(c => c.ParentCategoryId)
            .HasDatabaseName("IX_Categories_ParentCategoryId");
    }
}
