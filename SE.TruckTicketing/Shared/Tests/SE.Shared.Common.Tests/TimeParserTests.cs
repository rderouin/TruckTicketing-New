using System;
using System.Collections.Generic;

using FluentAssertions;

using SE.Shared.Common.Utilities;

namespace SE.Shared.Common.Tests;

[TestClass]
public class TimeParserTests
{
    [DataTestMethod]
    [DataRow("9", DisplayName = "9 => 9:00")]
    [DataRow("09", DisplayName = "09 => 9:00")]
    [DataRow("16", DisplayName = "16 => 16:00")]
    [DataRow("905", DisplayName = "905 => 9:05")]
    [DataRow("0905", DisplayName = "0905 => 9:05")]
    [DataRow("1605", DisplayName = "1605 => 16:05")]
    [DataRow("9:5", DisplayName = "9:5 => 9:05")]
    [DataRow("9:05", DisplayName = "9:05 => 9:05")]
    [DataRow("09:5", DisplayName = "09:5 => 9:05")]
    [DataRow("09:05", DisplayName = "09:05 => 9:05")]
    [DataRow("16:5", DisplayName = "16:5 => 16:05")]
    [DataRow("16:05", DisplayName = "16:05 => 16:05")]
    [DataRow("8pm", DisplayName = "8pm => 20:00")]
    [DataRow("08pm", DisplayName = "08pm => 20:00")]
    [DataRow("11pm", DisplayName = "11pm => 23:00")]
    [DataRow("12pm", DisplayName = "12pm => 12:00")]
    [DataRow("805pm", DisplayName = "805pm => 20:05")]
    [DataRow("1105pm", DisplayName = "1105pm => 23:05")]
    [DataRow("115pm", DisplayName = "115pm => 13:15")]
    [DataRow("0115pm", DisplayName = "0115pm => 13:15")]
    [DataRow("8:5pm", DisplayName = "8:5pm => 20:05")]
    [DataRow("8:05pm", DisplayName = "8:05pm => 20:05")]
    [DataRow("08:5pm", DisplayName = "08:5pm => 20:05")]
    [DataRow("08:05pm", DisplayName = "08:05pm => 20:05")]
    [DataRow("8:5am", DisplayName = "8:5am => 8:05")]
    [DataRow("8:05 a.m.", DisplayName = "8:05 a.m. => 8:05")]
    [DataRow("08:5 a. m.", DisplayName = "08:5 a. m. => 8:05")]
    [DataRow("08:05a.m.", DisplayName = "08:05a.m. => 8:05")]
    [DataRow("15pm", DisplayName = "15pm => 15:00")]
    [DataRow("0", DisplayName = "0 => 00:00")]
    public void TimeParser_Parse(string useCase)
    {
        // arrange
        var useCases = new Dictionary<string, string>
        {
            // digits only
            ["9"] = "9:00",
            ["09"] = "9:00",
            ["16"] = "16:00",
            ["905"] = "9:05",
            ["0905"] = "9:05",
            ["1605"] = "16:05",

            // digits with colon
            ["9:5"] = "9:05",
            ["9:05"] = "9:05",
            ["09:5"] = "9:05",
            ["09:05"] = "9:05",
            ["16:5"] = "16:05",
            ["16:05"] = "16:05",

            // with designator, hours only
            ["8pm"] = "20:00",
            ["08pm"] = "20:00",
            ["11pm"] = "23:00",
            ["12pm"] = "12:00",

            // with designator, hours and minutes
            ["805pm"] = "20:05",
            ["1105pm"] = "23:05",
            ["115pm"] = "13:15",
            ["0115pm"] = "13:15",

            // with designator, hours and minutes, and colon
            ["8:5pm"] = "20:05",
            ["8:05pm"] = "20:05",
            ["08:5pm"] = "20:05",
            ["08:05pm"] = "20:05",

            // with designator, hours and minutes, and colon, at morning
            ["8:5am"] = "8:05",
            ["8:05 a.m."] = "8:05",
            ["08:5 a. m."] = "8:05",
            ["08:05a.m."] = "8:05",

            // edge cases
            ["15pm"] = "15:00",
            ["0"] = "00:00",
        };

        var expected = TimeSpan.Parse(useCases[useCase]);

        // act
        var time = TimeParser.Parse(useCase);

        // assert
        time.Should().Be(expected);
    }
}
