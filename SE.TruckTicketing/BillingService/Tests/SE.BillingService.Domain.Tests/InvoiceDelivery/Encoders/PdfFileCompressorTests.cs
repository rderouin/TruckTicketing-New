using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.TridentContrib.Extensions.Compression;
using SE.TridentContrib.Extensions.Compression.Compressors.Pdf;

using Syncfusion.Pdf.Parsing;

using Trident.Contracts.Configuration;
using Trident.Logging;
using Trident.Testing.TestScopes;

namespace SE.BillingService.Domain.Tests.InvoiceDelivery.Encoders;

[TestClass]
public class PdfFileCompressorTests
{
    [TestMethod]
    public async Task PdfFileCompressor_CompressAsync_Basic()
    {
        // arrange
        var scope = new DefaultScope();
        var fileInfo = new TargetFileInfo { FileName = "uncompressed-with-images.pdf" };
        var compressionSettings = new PdfFileCompressorSettings();
        scope.AppSettings.Setup(s => s.GetSection<PdfFileCompressorSettings>(It.IsAny<string>())).Returns(compressionSettings);
        var pdfData = typeof(PdfFileCompressorTests).Assembly.GetResource("uncompressed-with-images.pdf", "TestData");
        await using var sourceStream = new MemoryStream(pdfData);
        await using var targetStream = new MemoryStream();

        // act
        var stats = await scope.InstanceUnderTest.CompressAsync(fileInfo, sourceStream, targetStream, MediaTypeNames.Application.Pdf);

        // assert
        targetStream.Position = 0;
        using var targetDocument = new PdfLoadedDocument(targetStream);
        using var sourceDocument = new PdfLoadedDocument(pdfData);
        targetStream.Length.Should().BeLessThan(pdfData.Length);
        stats.CompressedSize.Should().BeLessThan(stats.OriginalSize);
        targetDocument.PageCount.Should().Be(sourceDocument.PageCount);
        compressionSettings.DisableImageCompressionNative.Should().BeFalse();
        compressionSettings.DisableImageCompressionCustom.Should().BeFalse();
        compressionSettings.PdfImageQuality.Should().Be(80);
        compressionSettings.ImageReductionFactor.Should().BeApproximately(4f, 0.1f);
        compressionSettings.IndexedImageReductionFactor.Should().BeApproximately(2f, 0.1f);
        compressionSettings.ZlibCompressionLevel.Should().Be(3);
        compressionSettings.JpegCompressionLevel.Should().Be(50);
        compressionSettings.RemoveTransparency.Should().BeTrue();
    }

    public class DefaultScope : TestScope<PdfFileCompressor>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(AppSettings.Object, Log.Object);
        }

        public Mock<IAppSettings> AppSettings { get; set; } = new();

        public Mock<ILog> Log { get; set; } = new();
    }
}
