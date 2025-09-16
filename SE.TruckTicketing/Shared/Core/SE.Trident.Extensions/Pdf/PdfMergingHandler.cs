using System.IO;

using Syncfusion.Pdf;

namespace SE.TridentContrib.Extensions.Pdf;

public class PdfMergingHandler : IPdfMergingHandler
{
    private readonly PdfDocument _pdfDocument;

    public PdfMergingHandler()
    {
        _pdfDocument = new()
        {
            EnableMemoryOptimization = true,
            Compression = PdfCompressionLevel.Best,
        };
    }

    public void Dispose()
    {
        _pdfDocument?.Dispose();
    }

    public void Append(Stream sourceStream)
    {
        if (sourceStream.Length > 0)
        {
            PdfDocumentBase.Merge(_pdfDocument, sourceStream);
        }
    }

    public void Save(Stream targetStream)
    {
        _pdfDocument.Save(targetStream);
    }
}
