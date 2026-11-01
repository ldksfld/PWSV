using Microsoft.EntityFrameworkCore;
using PWSV.Domain.Entities;

namespace PWSV.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Account> Accounts { get; }
    DbSet<AccountType> AccountTypes { get; }
    DbSet<Currency> Currencies { get; }
    DbSet<ExchangeRate> ExchangeRates { get; }
    DbSet<Category> Categories { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<AuditEntry> AuditEntries { get; }
    DbSet<KeyDerivationConfig> KeyDerivationConfigs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task LockAccountsAsync(IEnumerable<int> accountIds, CancellationToken cancellationToken);

    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        System.Data.IsolationLevel isolationLevel,
        CancellationToken cancellationToken);
}
