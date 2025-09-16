using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.TruckTicketing.Api.Functions;
using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Api.Tests.Functions;

[TestClass]
public class TicketScanMetadataTests
{
    [TestMethod]
    public void FromBlobUrl_WithValidBlobUrl_ShouldReturnTicketScanMetadata()
    {
        // Arrange
        var validBlobUrl = "https://test.blob.core.windows.net/container1/KIFST123243-SP-INT.pdf";

        // Act
        var metadata = TicketScanMetadata.FromBlobUrl(validBlobUrl);

        // Assert
        metadata.Should().NotBeNull();
        metadata.File.Should().Be("KIFST123243-SP-INT.pdf");
        metadata.Path.Should().Be("KIFST123243-SP-INT.pdf");
        metadata.Container.Should().Be("container1");
        metadata.TicketNumber.Should().Be("KIFST123243-SP");
        metadata.AttachmentSuffix.Should().Be("INT");
    }
    
    [DataTestMethod]
    [DataRow("https://test.blob.core.windows.net/container1/nested-folder/KIFST123243-SP-INT.pdf", "nested-folder/KIFST123243-SP-INT.pdf")]
    [DataRow("https://test.blob.core.windows.net/container1/nested-folder/nested-folder2/KIFST123243-SP-INT.pdf", "nested-folder/nested-folder2/KIFST123243-SP-INT.pdf")]
    public void FromBlobUrl_WithValidBlobUrlNestedFolder_ShouldReturnTicketScanMetadata(string absoluteBlobUrl, string expectedPath)
    {
        // Act
        var metadata = TicketScanMetadata.FromBlobUrl(absoluteBlobUrl);

        // Assert
        metadata.Should().NotBeNull();
        metadata.Path.Should().Be(expectedPath);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow("https://test.invalid.blob.core.windows.net/container1/invalid.pdf")]
    [DataRow("https://test.invalid.blob.core.windows.net/container1/SA-DLD1289212.SP-IN.pdf")]
    [DataRow("https://test.invalid.blob.core.windows.net/container1/SADLD1289212.SP.pdf")]
    public void FromBlobUrl_WithInvalidBlobUrl_ShouldReturnNull(string invalidBlobUrl)
    {
        // Act
        var metadata = TicketScanMetadata.FromBlobUrl(invalidBlobUrl);

        // Assert
        metadata.Should().BeNull();
    }

    [DataTestMethod]
    [DataRow(TruckTicketType.WT, CountryCode.US, "int", AttachmentType.Internal)]
    [DataRow(TruckTicketType.WT, CountryCode.US, "ext", AttachmentType.External)]
    [DataRow(TruckTicketType.WT, CountryCode.US, "unknown", AttachmentType.Internal)]
    [DataRow(TruckTicketType.WT, CountryCode.CA, "int", AttachmentType.External)]
    [DataRow(TruckTicketType.WT, CountryCode.CA, "ext", AttachmentType.External)]
    [DataRow(TruckTicketType.WT, CountryCode.CA, "unknown", AttachmentType.External)]
    [DataRow(TruckTicketType.SP, CountryCode.US, "int", AttachmentType.Internal)]
    [DataRow(TruckTicketType.SP, CountryCode.US, "ext", AttachmentType.External)]
    [DataRow(TruckTicketType.SP, CountryCode.US, "unknown", AttachmentType.External)]
    [DataRow(TruckTicketType.SP, CountryCode.CA, "int", AttachmentType.Internal)]
    [DataRow(TruckTicketType.SP, CountryCode.CA, "ext", AttachmentType.External)]
    [DataRow(TruckTicketType.SP, CountryCode.CA, "unknown", AttachmentType.External)]
    [DataRow(TruckTicketType.LF, CountryCode.US, "int", AttachmentType.Internal)]
    [DataRow(TruckTicketType.LF, CountryCode.US, "ext", AttachmentType.External)]
    [DataRow(TruckTicketType.LF, CountryCode.US, "unknown", AttachmentType.External)]
    [DataRow(TruckTicketType.LF, CountryCode.CA, "int", AttachmentType.Internal)]
    [DataRow(TruckTicketType.LF, CountryCode.CA, "ext", AttachmentType.External)]
    [DataRow(TruckTicketType.LF, CountryCode.CA, "unknown", AttachmentType.External)]
    public void GetComputedAttachmentType_WithVariousInputs_ShouldReturnCorrectAttachmentType(TruckTicketType ticketType, CountryCode countryCode, string attachmentSuffix, AttachmentType expectedType)
    {
        // Arrange
        var metadata = new TicketScanMetadata
        {
            TicketType = ticketType,
            AttachmentSuffix = attachmentSuffix,
        };

        // Act
        var actualType = metadata.GetComputedAttachmentType(countryCode);

        // Assert
        actualType.Should().Be(expectedType);
    }
}
