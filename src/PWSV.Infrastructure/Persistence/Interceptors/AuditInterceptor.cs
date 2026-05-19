using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;

namespace PWSV.Infrastructure.Persistence.Interceptors;

public sealed class AuditInterceptor(ICurrentUserService currentUser, IDateTimeProvider clock) : SaveChangesInterceptor
{
    private static readonly HashSet<string> AuditableEntities = new(StringComparer.Ordinal)
    {
        nameof(Account),
        nameof(Category),
        nameof(Transaction),
        nameof(ExchangeRate),
        nameof(Currency)
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var entries = context.ChangeTracker.Entries()
            .Where(e => AuditableEntities.Contains(e.Entity.GetType().Name)
                        && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        foreach (var entry in entries)
        {
            context.Set<AuditEntry>().Add(BuildAuditEntry(entry));
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private AuditEntry BuildAuditEntry(EntityEntry entry)
    {
        var action = entry.State switch
        {
            EntityState.Added => "INSERT",
            EntityState.Modified => "UPDATE",
            EntityState.Deleted => "DELETE",
            _ => "UNKNOWN"
        };

        var key = entry.Metadata.FindPrimaryKey()?.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? string.Empty)
            .FirstOrDefault() ?? string.Empty;

        return new AuditEntry
        {
            EntityName = entry.Entity.GetType().Name,
            EntityKey = key,
            Action = action,
            OccurredAt = clock.UtcNow,
            UserId = currentUser.UserId
        };
    }
}
