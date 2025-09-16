using SkiaSharp;

namespace SE.TridentContrib.Extensions.Compression.Compressors.Pdf.Extensions;

public static class SkiaExtensions
{
    public static readonly SKSizeI OnePixel = new(1, 1);

    public static SKSize EnsureNoLessThan(this SKSize size, SKSize lowerBound)
    {
        return new(size.Width < lowerBound.Width ? lowerBound.Width : size.Width,
                   size.Height < lowerBound.Height ? lowerBound.Height : size.Height);
    }

    public static bool IsOnePixel(this SKSize size)
    {
        return size.ToSizeI() == OnePixel;
    }
}
