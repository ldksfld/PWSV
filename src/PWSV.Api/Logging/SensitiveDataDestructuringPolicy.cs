using Serilog.Core;
using Serilog.Events;

namespace PWSV.Api.Logging;

public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "OldPassword",
        "NewPassword",
        "MasterPassword",
        "Token",
        "AccessToken",
        "RefreshToken",
        "Secret",
        "SecretKey",
        "AccountNumber",
        "Description",
        "PasswordHash"
    };

    private const string Mask = "***";

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        if (value is null)
        {
            result = null!;
            return false;
        }

        var type = value.GetType();
        if (!type.IsClass || type == typeof(string))
        {
            result = null!;
            return false;
        }

        var properties = type.GetProperties()
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();

        if (properties.Length == 0)
        {
            result = null!;
            return false;
        }

        var members = new List<LogEventProperty>(properties.Length);
        foreach (var property in properties)
        {
            object? memberValue;
            try
            {
                memberValue = property.GetValue(value);
            }
            catch
            {
                continue;
            }

            var stringValue = SensitiveNames.Contains(property.Name)
                ? Mask
                : memberValue;

            members.Add(new LogEventProperty(property.Name, propertyValueFactory.CreatePropertyValue(stringValue)));
        }

        result = new StructureValue(members, type.Name);
        return true;
    }
}
