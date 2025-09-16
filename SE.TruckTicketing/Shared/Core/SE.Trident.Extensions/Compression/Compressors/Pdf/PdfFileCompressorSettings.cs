using System.Diagnostics.CodeAnalysis;

namespace SE.TridentContrib.Extensions.Compression.Compressors.Pdf;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class PdfFileCompressorSettings
{
    public bool DisableImageCompressionNative { get; set; } = false;

    public bool DisableImageCompressionCustom { get; set; } = false;

    public int PdfImageQuality { get; set; } = 80;

    public float ImageReductionFactor { get; set; } = 4f;

    public float IndexedImageReductionFactor { get; set; } = 2f;

    public int ZlibCompressionLevel { get; set; } = 3;

    public int JpegCompressionLevel { get; set; } = 50;

    public bool RemoveTransparency { get; set; } = true;
}
