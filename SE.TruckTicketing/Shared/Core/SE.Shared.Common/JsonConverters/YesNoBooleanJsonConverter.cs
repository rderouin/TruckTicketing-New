using System;

using Newtonsoft.Json;

namespace SE.Shared.Common.JsonConverters;

public class YesNoBooleanJsonConverter : JsonConverter<bool?>
{
    private const string TrueValue = "Yes";

    private const string FalseValue = "No";

    private readonly StringComparer _comparer = StringComparer.OrdinalIgnoreCase;

    public override void WriteJson(JsonWriter writer, bool? value, JsonSerializer serializer)
    {
        if (value == true)
        {
            writer.WriteValue(TrueValue);
            return;
        }

        if (value == false)
        {
            writer.WriteValue(FalseValue);
            return;
        }

        writer.WriteValue(string.Empty);
    }

    public override bool? ReadJson(JsonReader reader, Type objectType, bool? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var value = reader.Value;

        if (_comparer.Equals(value, TrueValue))
        {
            return true;
        }

        if (_comparer.Equals(value, FalseValue))
        {
            return false;
        }

        return null;
    }
}
