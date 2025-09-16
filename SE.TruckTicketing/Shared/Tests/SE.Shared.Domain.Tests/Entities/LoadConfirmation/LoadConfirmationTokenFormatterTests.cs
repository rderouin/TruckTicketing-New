using FluentAssertions;

using SE.Shared.Domain.Entities.LoadConfirmation;

namespace SE.Shared.Domain.Tests.Entities.LoadConfirmation;

[TestClass]
public class LoadConfirmationTokenFormatterTests
{
    private const string NullString = null;

    [DataTestMethod]
    [DataRow("GLFST10000100-LC", "1FFC7D", "(GLFST10000100-LC:1FFC7D)")]
    [DataRow("GLFST10000100-LC", "1FFC7DAB", "(GLFST10000100-LC:1FFC7DAB)")]
    [DataRow("", "", "(:)")]
    [DataRow(NullString, NullString, "(:)")]
    public void LoadConfirmationTokenFormatter_Format(string number, string hash, string expected)
    {
        // arrange

        // act
        var actual = LoadConfirmationTokenFormatter.Format(number, hash);

        // assert
        actual.Should().Be(expected);
    }

    [DataTestMethod]
    [DataRow("Re: Secure Load Confirmation:  Secure Energy (Drilling Services) Inc. 8/29/2023-8/29/2023 (HPFST10000285-LC:50678C)", "HPFST10000285-LC", "50678C", true, false)]
    [DataRow("SECURE Load Confirmation: Obsidian Energy Ltd. 4/16/2023 to 4/16/2023 (GLFST10000100-LC:1FFC7D)", "GLFST10000100-LC", "1FFC7D", true, false)]
    [DataRow("blahblahblah(GLFST10000100-LC:1FFC7D)blahblahblah", "GLFST10000100-LC", "1FFC7D", true, false)]
    [DataRow("blahblahblah(GLFST10000100-LC:1FFC7DAB)blahblahblah", "GLFST10000100-LC", "1FFC7DAB", true, false)]
    [DataRow("blahblahblah(GLFST10000100-LC:)blahblahblah", NullString, NullString, false, false)]
    [DataRow("blahblahblah(:)blahblahblah", NullString, NullString, false, false)]
    [DataRow("blahblahblahblahblahblah", NullString, NullString, false, false)]
    [DataRow(NullString, NullString, NullString, false, false)]
    [DataRow("SECURE Load Confirmation: Obsidian Energy Ltd. 4/16/2023 to 4/16/2023 93C2C830979635DA BCFST10000129-LC", "BCFST10000129-LC", "93C2C830979635DA", true, true)]
    [DataRow("SECURE Load Confirmation: Obsidian Energy Ltd. 4/16/2023 to 4/16/2023 93C2C830979635DA BCFST10000129-LC   ", "BCFST10000129-LC", "93C2C830979635DA", true, true)]
    [DataRow("[EXTERNAL]93C2C830979635DA BCFST10000129-LC", "BCFST10000129-LC", "93C2C830979635DA", true, true)]
    [DataRow("SECURE Load Confirmation: Obsidian Energy Ltd. 4/16/2023 to 4/16/2023 93C2C830979635DX BCFST10000129-LC", NullString, NullString, false, true)]
    [DataRow("SECURE Load Confirmation: Obsidian Energy Ltd. 4/16/2023 to 4/16/2023 93C2C830979635D BCFST10000129-LC", NullString, NullString, false, true)]
    [DataRow("blahblahblah 93C2C830979635DA BCFST10000129-LC blahblahblah", NullString, NullString, false, true)]
    [DataRow("blahblahblah(GLFST10000100-LC:)blahblahblah", NullString, NullString, false, true)]
    [DataRow("blahblahblah(:)blahblahblah", NullString, NullString, false, true)]
    [DataRow("blahblahblahblahblahblah", NullString, NullString, false, true)]
    [DataRow(NullString, NullString, NullString, false, true)]
    public void LoadConfirmationTokenFormatter_Parse(string input, string expectedNumber, string expectedHash, bool expectedSuccess, bool isLegacy)
    {
        // arrange
        var strategy = isLegacy switch
                       {
                           true => LoadConfirmationHashStrategy.Version1L16,
                           false => LoadConfirmationHashStrategy.Version2L6,
                       };

        // act
        var (actualNumber, actualHash, isSuccess) = LoadConfirmationTokenFormatter.Parse(input, strategy);

        // assert
        isSuccess.Should().Be(expectedSuccess);
        actualNumber.Should().Be(expectedNumber);
        actualHash.Should().Be(expectedHash);
    }
}
