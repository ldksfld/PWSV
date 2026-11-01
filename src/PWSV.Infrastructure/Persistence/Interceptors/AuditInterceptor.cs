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

    private readonly List<(AuditEntry Audit, EntityEntry Entry)> _pendingKeys = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is not null && ResolvePendingKeys())
        {
            eventData.Context.SaveChanges();
        }

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null && ResolvePendingKeys())
        {
            await eventData.Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _pendingKeys.Clear();
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _pendingKeys.Clear();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    public override void SaveChangesCanceled(DbContextEventData eventData)
    {
        _pendingKeys.Clear();
        base.SaveChangesCanceled(eventData);
    }

    public override Task SaveChangesCanceledAsync(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        _pendingKeys.Clear();
        return base.SaveChangesCanceledAsync(eventData, cancellationToken);
    }

    private void AddAuditEntries(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var entries = context.ChangeTracker.Entries()
            .Where(e => AuditableEntities.Contains(e.Entity.GetType().Name)
                        && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var audit = BuildAuditEntry(entry);
            context.Set<AuditEntry>().Add(audit);

            if (entry.State == EntityState.Added && HasTemporaryKey(entry))
            {
                _pendingKeys.Add((audit, entry));
            }
        }
    }

    private bool ResolvePendingKeys()
    {
        if (_pendingKeys.Count == 0)
        {
            return false;
        }

        foreach (var (audit, entry) in _pendingKeys)
        {
            audit.EntityKey = ReadKey(entry);
        }

        _pendingKeys.Clear();
        return true;
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

        return new AuditEntry
        {
            EntityName = entry.Entity.GetType().Name,
            EntityKey = HasTemporaryKey(entry) ? string.Empty : ReadKey(entry),
            Action = action,
            OccurredAt = clock.UtcNow,
            UserId = currentUser.UserId
        };
    }

    private static bool HasTemporaryKey(EntityEntry entry) =>
        entry.Metadata.FindPrimaryKey()?.Properties.Any(p => entry.Property(p.Name).IsTemporary) == true;

    private static string ReadKey(EntityEntry entry) =>
        string.Join("|", entry.Metadata.FindPrimaryKey()?.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? string.Empty) ?? []);
}
