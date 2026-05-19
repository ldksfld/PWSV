using PWSV.Domain.Common;

namespace PWSV.Domain.Entities;

public sealed class User : BaseEntity<int>
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public KeyDerivationConfig? KeyDerivationConfig { get; set; }
    public ICollection<Account> Accounts { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
}
