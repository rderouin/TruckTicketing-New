using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.BillingService.Domain.InvoiceDelivery;

namespace SE.BillingService.Domain.Tests.InvoiceDelivery;

[TestClass]
public class MustacheParserTests
{
    [DataTestMethod]
    [DataRow("{{type:value:option}}", "type", "value", "option", DisplayName = "MustacheParser.Match: simple")]
    [DataRow(" {{ type : value : option }} ", "type", "value", "option", DisplayName = "MustacheParser.Match: simple with spaces")]
    [DataRow("{{type:value}}", "type", "value", null, DisplayName = "MustacheParser.Match: short")]
    [DataRow(" {{ type : value }} ", "type", "value", null, DisplayName = "MustacheParser.Match: short with spaces")]
    public void MustacheParser_Match_AllSuccessful(string input, string expectedType, string expectedValue, string expectedOption)
    {
        // arrange

        // act
        var (success, type, value, option) = MustacheParser.Match(input);

        // assert
        success.Should().BeTrue();
        type.Should().Be(expectedType);
        value.Should().Be(expectedValue);
        option.Should().Be(expectedOption);
    }
}
