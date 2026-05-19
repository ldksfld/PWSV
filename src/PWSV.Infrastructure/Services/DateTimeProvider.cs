using PWSV.Application.Common.Interfaces;

namespace PWSV.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly TodayUtc => DateOnly.FromDateTime(DateTime.UtcNow);
}
