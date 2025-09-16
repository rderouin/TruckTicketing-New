using SkiaSharp;

using Syncfusion.Drawing;
using Syncfusion.Pdf.Graphics;

namespace SE.TridentContrib.Extensions.Compression.Compressors.Pdf.Extensions;

public static class SyncfusionExtensions
{
    private static readonly float DefaultDpi = 96f;

    public static SKSize ToPixels(this SizeF size)
    {
        var converter = new PdfUnitConverter(DefaultDpi);
        return new(converter.ConvertToPixels(size.Width, PdfGraphicsUnit.Point),
                   converter.ConvertToPixels(size.Height, PdfGraphicsUnit.Point));
    }
}
