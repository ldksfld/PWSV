namespace PWSV.Application.Auth;

public sealed class DemoAccountOptions
{
    public const string SectionName = "DemoAccount";

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string MasterPassword { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(MasterPassword);
}
