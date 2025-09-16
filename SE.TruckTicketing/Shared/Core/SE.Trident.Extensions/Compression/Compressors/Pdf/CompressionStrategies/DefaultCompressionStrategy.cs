using System.Diagnostics.CodeAnalysis;

using SE.TridentContrib.Extensions.Compression.Compressors.Pdf.Extensions;

using Trident.Logging;

namespace SE.TridentContrib.Extensions.Compression.Compressors.Pdf.CompressionStrategies;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class DefaultCompressionStrategy : SingleImageCompressionStrategyBase
{
    public DefaultCompressionStrategy(float reductionFactor, int zLibLevel, int quality, bool removeTransparency, ILog log)
        : base(reductionFactor, zLibLevel, quality, removeTransparency, log)
    {
    }

    public override bool IsApplicable(ImageContext context)
    {
        return context.SourceImage.RawFormat.IsJpeg() ||
               context.SourceImage.RawFormat.IsPng() ||
               context.SourceImage.RawFormat.IsBmp();
    }

    public override bool IsToSkipImageCompression(ImageContext context)
    {
        return false;
    }
}
