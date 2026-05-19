using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PWSV.Client.Services;

public sealed class MoneyJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => decimal.Parse(reader.GetString()!, NumberStyles.Number, CultureInfo.InvariantCulture),
            JsonTokenType.Number => reader.GetDecimal(),
            _ => throw new JsonException($"Очікувалось значення суми, отримано {reader.TokenType}.")
        };
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("F2", CultureInfo.InvariantCulture));
    }
}

public sealed class NullableMoneyJsonConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDecimal();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString();
            return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        throw new JsonException($"Очікувалось значення суми, отримано {reader.TokenType}.");
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString("F2", CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
