using System.Data;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;
using PWSV.Infrastructure.Persistence.Interceptors;
using PWSV.Infrastructure.Persistence.Seed;

namespace PWSV.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, AuditInterceptor auditInterceptor)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountType> AccountTypes => Set<AccountType>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<KeyDerivationConfig> KeyDerivationConfigs => Set<KeyDerivationConfig>();

    public async Task LockAccountsAsync(IEnumerable<int> accountIds, CancellationToken cancellationToken)
    {
        foreach (var accountId in accountIds.Distinct().Order())
        {
            await Database
                .ExecuteSqlAsync($"SELECT Id FROM dbo.Accounts WITH (UPDLOCK, ROWLOCK) WHERE Id = {accountId}", cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
        return Database.CreateExecutionStrategy().ExecuteAsync(
            async ct =>
            {
                ChangeTracker.Clear();
                await using var transaction = await Database.BeginTransactionAsync(isolationLevel, ct).ConfigureAwait(false);
                var result = await operation(ct).ConfigureAwait(false);
                await transaction.CommitAsync(ct).ConfigureAwait(false);
                return result;
            },
            cancellationToken);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(auditInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        DatabaseSeeder.Seed(modelBuilder);
    }
}
