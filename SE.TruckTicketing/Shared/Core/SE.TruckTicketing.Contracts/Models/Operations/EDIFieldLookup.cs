using System.Text.Json.Serialization;

using Newtonsoft.Json.Converters;

using SE.Shared.Common.Enums;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class EDIFieldLookup : GuidApiModelBase
{
    public string Name { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public DataTypes DataType { get; set; }
}
