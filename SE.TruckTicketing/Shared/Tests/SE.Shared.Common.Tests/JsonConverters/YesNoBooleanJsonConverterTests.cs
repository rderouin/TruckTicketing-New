using System.IO;
using System.Text;

using FluentAssertions;

using Newtonsoft.Json;

using SE.Shared.Common.JsonConverters;

namespace SE.Shared.Common.Tests.JsonConverters;

[TestClass]
public class YesNoBooleanJsonConverterTests
{
    private readonly YesNoBooleanJsonConverter _converter;

    public YesNoBooleanJsonConverterTests()
    {
        _converter = new();
    }

    [DataTestMethod]
    [DataRow(true, DisplayName = "YesNoBooleanJsonConverter can write the 'true' value.")]
    [DataRow(false, DisplayName = "YesNoBooleanJsonConverter can write the 'false' value.")]
    [DataRow(null!, DisplayName = "YesNoBooleanJsonConverter can write the null value.")]
    public void YesNoBooleanJsonConverter_WriteJson(bool? value)
    {
        // arrange
        var expected = value == true ? "Yes" : value == false ? "No" : "";
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);
        var jsonTextWriter = new JsonTextWriter(stringWriter);
        var jsonSerializer = new JsonSerializer();

        // act
        _converter.WriteJson(jsonTextWriter, value, jsonSerializer);

        // assert
        var output = stringBuilder.ToString();
        output.Should()!.Be($@"""{expected}""");
    }

    [DataTestMethod]
    [DataRow("Yes", DisplayName = "YesNoBooleanJsonConverter can read the 'Yes' value.")]
    [DataRow("No", DisplayName = "YesNoBooleanJsonConverter can read the 'No' value.")]
    [DataRow("", DisplayName = "YesNoBooleanJsonConverter can read the blank value.")]
    [DataRow(null!, DisplayName = "YesNoBooleanJsonConverter can read the null value.")]
    public void YesNoBooleanJsonConverter_ReadJson(string value)
    {
        // arrange
        bool? expected = value == "Yes" ? true : value == "No" ? false : null;
        var stringReader = new StringReader($@"""{value}""");
        var jsonTextReader = new JsonTextReader(stringReader);
        var jsonSerializer = new JsonSerializer();

        // act
        jsonTextReader.Read();
        var output = _converter.ReadJson(jsonTextReader, typeof(bool?), null, jsonSerializer)!;

        // assert
        output.Should()!.Be(expected!);
    }
}
