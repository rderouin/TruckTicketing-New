using Syncfusion.Pdf.Graphics;

namespace SE.TridentContrib.Extensions.Compression.Compressors.Pdf;

public interface IPdfImageCompressionStrategy
{
    bool IsApplicable(ImageContext context);

    bool IsToSkipImageCompression(ImageContext context);

    PdfImage CompressImage(ImageContext context);
}
