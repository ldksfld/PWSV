using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;

namespace PWSV.Application.Tests;

public sealed class TestApplicationDbContext : DbContext, IApplicationDbContext
{
    public TestApplicationDbContext(DbContextOptions<TestApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountType> AccountTypes => Set<AccountType>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<KeyDerivationConfig> KeyDerivationConfigs => Set<KeyDerivationConfig>();

    public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }
}
