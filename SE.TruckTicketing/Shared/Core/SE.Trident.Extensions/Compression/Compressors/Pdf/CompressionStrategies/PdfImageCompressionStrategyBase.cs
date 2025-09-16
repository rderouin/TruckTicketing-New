using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;

using SE.TridentContrib.Extensions.Compression.Compressors.Pdf.Extensions;
using SE.TridentContrib.Extensions.Compression.Compressors.Pdf.Skia;

using SkiaSharp;

using Syncfusion.Pdf.Graphics;

using Trident.Logging;

namespace SE.TridentContrib.Extensions.Compression.Compressors.Pdf.CompressionStrategies;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public abstract class PdfImageCompressionStrategyBase : IPdfImageCompressionStrategy
{
    public static SKSize OnePixel = new(1f, 1f);

    private readonly ILog _log;

    protected PdfImageCompressionStrategyBase(ILog log)
    {
        _log = log;
    }

    public abstract bool IsApplicable(ImageContext context);

    public abstract bool IsToSkipImageCompression(ImageContext context);

    public PdfImage CompressImage(ImageContext context)
    {
        // preprocess the image
        var sourceStream = context.SourceStream;
        var imageSize = new SKSize(context.SourceImage.Width, context.SourceImage.Height);
        var pngStream = EnsurePngStream(context);

        // resize the image
        var appliedSize = context.PdfImageInfo.Bounds.Size.ToPixels();
        var resizedImage = ResizeImage(pngStream, imageSize, appliedSize, context) ?? pngStream;

        // compress the image
        var compressedImage = Compress(resizedImage) ?? resizedImage;

        // validate the compression, for larger images do not do anything
        if (compressedImage.Length >= sourceStream.Length)
        {
            _log.Information(messageTemplate: $"The compressed image ({compressedImage.Length}) is larger than source ({sourceStream.Length}). ({context})");
            return null;
        }

        // create a new Syncfusion PDF image from the stream
        PdfImage pdfImage = IsTiff(compressedImage)
                                ? new PdfTiffImage(compressedImage)
                                : new PdfBitmap(compressedImage);

        return pdfImage;
    }

    private Stream EnsurePngStream(ImageContext context)
    {
        if (context.SourceImage.RawFormat.IsPng())
        {
            return context.SourceStream;
        }

        return ImageTools.ImageToPng(context.SourceStream);
    }

    private Stream ResizeImage(Stream stream, SKSize imageSize, SKSize appliedSize, ImageContext context)
    {
        // fetch the target image size
        var targetSize = CalculateTargetImageSize(imageSize, appliedSize);

        // edge case: the image size may become less than a pixel-size
        // solution: preserve the lower boundary (lower bound restriction)
        targetSize = targetSize.EnsureNoLessThan(OnePixel);

        // rounded/trimmed values
        var roundedTargetSize = targetSize.ToSizeI();
        var roundedImageSize = imageSize.ToSizeI();

        // skip resizing if the target size is already the same
        if (roundedTargetSize == roundedImageSize)
        {
            _log.Information(messageTemplate: $"The image has the target size already. ({context})");
            return null;
        }

        // resize
        var newStream = Resize(stream, roundedTargetSize);
        return newStream;
    }

    protected abstract SKSize CalculateTargetImageSize(SKSize sourceSize, SKSize appliedSize);

    protected abstract Stream Resize(Stream stream, SKSizeI size);

    protected abstract Stream Compress(Stream stream);

    private bool IsTiff(Stream stream)
    {
        var isTiff = Image.FromStream(stream).RawFormat.IsTiff();
        stream.Reset();
        return isTiff;
    }
}
