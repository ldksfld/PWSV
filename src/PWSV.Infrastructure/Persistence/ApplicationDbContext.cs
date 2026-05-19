using System.Data;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
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

    public async Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        return await Database.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
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
