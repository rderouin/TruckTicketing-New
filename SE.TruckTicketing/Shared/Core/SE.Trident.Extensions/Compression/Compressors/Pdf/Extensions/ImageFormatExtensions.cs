using System.Diagnostics.CodeAnalysis;
using System.Drawing.Imaging;

namespace SE.TridentContrib.Extensions.Compression.Compressors.Pdf.Extensions;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public static class ImageFormatExtensions
{
    public static bool IsTiff(this ImageFormat imageFormat)
    {
        return ImageFormat.Tiff.Equals(imageFormat);
    }

    public static bool IsPng(this ImageFormat imageFormat)
    {
        return ImageFormat.Png.Equals(imageFormat);
    }

    public static bool IsBmp(this ImageFormat imageFormat)
    {
        return ImageFormat.Bmp.Equals(imageFormat);
    }

    public static bool IsJpeg(this ImageFormat imageFormat)
    {
        return ImageFormat.Jpeg.Equals(imageFormat);
    }
}
