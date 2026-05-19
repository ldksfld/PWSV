namespace PWSV.Client.Models;

public sealed record ProblemDetailsModel
{
    public string? Title { get; init; }
    public string? Detail { get; init; }
    public int? Status { get; init; }
    public Dictionary<string, string[]>? Errors { get; init; }
}
