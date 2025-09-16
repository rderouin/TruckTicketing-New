using System.Diagnostics.CodeAnalysis;
using System.Drawing.Imaging;

using SE.TridentContrib.Extensions.Compression.Compressors.Pdf.Extensions;

using Trident.Logging;

namespace SE.TridentContrib.Extensions.Compression.Compressors.Pdf.CompressionStrategies;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class IndexedImageCompressionStrategy : SingleImageCompressionStrategyBase
{
    public IndexedImageCompressionStrategy(float reductionFactor, int zLibLevel, int quality, bool removeTransparency, ILog log)
        : base(reductionFactor, zLibLevel, quality, removeTransparency, log)
    {
    }

    public override bool IsApplicable(ImageContext context)
    {
        var pixelFormat = context.SourceImage.PixelFormat;
        var imageFormat = context.SourceImage.RawFormat;

        var indexed = pixelFormat.HasFlag(PixelFormat.Indexed);
        var paletteBased = imageFormat.IsTiff() || imageFormat.IsPng() || imageFormat.IsBmp();

        return indexed && paletteBased;
    }

    public override bool IsToSkipImageCompression(ImageContext context)
    {
        // a single large image on the page should be compressible to a gray scale image
        var single = context.ImagesOnPage == 1;
        var large =
            context.SourceImage.Width > context.PdfLoadedPage.Size.Width ||
            context.SourceImage.Height > context.PdfLoadedPage.Size.Height;

        // skip if not single & large
        var shouldCompressImage = single && large;
        return !shouldCompressImage;
    }
}
