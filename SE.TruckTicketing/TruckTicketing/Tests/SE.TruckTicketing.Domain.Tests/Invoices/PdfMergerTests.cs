using System.IO;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.TridentContrib.Extensions.Pdf;

using Syncfusion.Pdf.Parsing;

using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.Invoices;

[TestClass]
public class PdfMergerTests
{
    [TestMethod]
    public async Task PdfMerger_MergePdfDocuments_WithSignatures()
    {
        // arrange
        var scope = new DefaultScope();
        var sourcePdfData = typeof(PdfMergerTests).Assembly.GetResource("SWFST10000277-LC_0.pdf", "TestData");
        await using var sourceStream1 = new MemoryStream(sourcePdfData);
        await using var sourceStream2 = new MemoryStream(sourcePdfData);

        // act
        using var pdfMergingHandler = scope.InstanceUnderTest.StartMerging();
        pdfMergingHandler.Append(sourceStream1);
        pdfMergingHandler.Append(sourceStream2);
        await using var finalStream = new MemoryStream();
        pdfMergingHandler.Save(finalStream);
        finalStream.Position = 0;

        // assert
        var sourcePdf = new PdfLoadedDocument(sourcePdfData);
        var targetPdf = new PdfLoadedDocument(finalStream);
        targetPdf.PageCount.Should().Be(sourcePdf.PageCount * 2);
    }

    private class DefaultScope : TestScope<PdfMerger>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }
    }
}
