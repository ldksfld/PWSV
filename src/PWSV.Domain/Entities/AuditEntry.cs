namespace PWSV.Domain.Entities;

public sealed class AuditEntry
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityKey { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }

    public User? User { get; set; }
}
