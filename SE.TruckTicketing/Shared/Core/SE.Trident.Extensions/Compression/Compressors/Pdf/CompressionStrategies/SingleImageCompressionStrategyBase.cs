using System.IO;

using SE.TridentContrib.Extensions.Compression.Compressors.Pdf.Extensions;
using SE.TridentContrib.Extensions.Compression.Compressors.Pdf.Skia;

using SkiaSharp;

using Trident.Logging;

namespace SE.TridentContrib.Extensions.Compression.Compressors.Pdf.CompressionStrategies;

public abstract class SingleImageCompressionStrategyBase : PdfImageCompressionStrategyBase
{
    private readonly int _quality;

    private readonly float _reductionFactor;

    private readonly bool _removeTransparency;

    private readonly int _zLibLevel;

    protected SingleImageCompressionStrategyBase(float reductionFactor, int zLibLevel, int quality, bool removeTransparency, ILog log) : base(log)
    {
        _reductionFactor = reductionFactor;
        _zLibLevel = zLibLevel;
        _quality = quality;
        _removeTransparency = removeTransparency;
    }

    protected override SKSize CalculateTargetImageSize(SKSize sourceSize, SKSize appliedSize)
    {
        // zero-size image on the page
        if (appliedSize.IsEmpty || appliedSize.IsOnePixel())
        {
            // reduce the size of the image
            return new(sourceSize.Width / _reductionFactor, sourceSize.Height / _reductionFactor);
        }

        // match dimensions on the page for large images, otherwise reduce by a factor
        return appliedSize;
    }

    protected override Stream Resize(Stream stream, SKSizeI size)
    {
        var newStream = ImageTools.Resize(stream, size, _zLibLevel);
        return newStream;
    }

    protected override Stream Compress(Stream stream)
    {
        var newStream = ImageTools.Compress(stream, _quality, _removeTransparency);
        return newStream;
    }
}
