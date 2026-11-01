using PWSV.Application.Common.Mappings;

namespace PWSV.Application.Tests;

internal static class MappingSetup
{
    private static readonly Lazy<bool> Registered = new(() =>
    {
        MapsterConfig.Register();
        return true;
    });

    public static void EnsureRegistered() => _ = Registered.Value;
}
