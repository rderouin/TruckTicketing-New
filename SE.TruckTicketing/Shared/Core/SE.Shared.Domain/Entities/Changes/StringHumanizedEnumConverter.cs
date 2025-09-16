using System;

using Humanizer;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SE.Shared.Domain.Entities.Changes;

public class StringHumanizedEnumConverter : StringEnumConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        try
        {
            var enumValue = (Enum)value;
            var text = enumValue.Humanize();
            writer.WriteValue(text);
        }
        catch (Exception)
        {
            base.WriteJson(writer, value, serializer);
        }
    }
}
