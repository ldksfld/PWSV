namespace PWSV.Domain.Common;

public abstract class BaseEntity<TId>
    where TId : struct
{
    public TId Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
