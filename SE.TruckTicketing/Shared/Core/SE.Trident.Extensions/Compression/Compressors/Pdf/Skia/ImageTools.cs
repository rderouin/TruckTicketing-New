using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

using SE.TridentContrib.Extensions.Compression.Compressors.Pdf.Extensions;

using SkiaSharp;

namespace SE.TridentContrib.Extensions.Compression.Compressors.Pdf.Skia;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public static class ImageTools
{
    public static Stream ImageToPng(Stream imgStream)
    {
        // read the image
        using var image = Image.FromStream(imgStream);
        imgStream.Reset();

        // convert the image into a bitmap
        using var bitmap = new Bitmap(image);

        // save the bitmap as PNG
        var pngStream = new MemoryStream();
        bitmap.Save(pngStream, ImageFormat.Png);
        pngStream.Reset();

        return pngStream;
    }

    public static Stream Resize(Stream stream, SKSizeI size, int zLibLevel)
    {
        // read the original image
        using var managedStream = new SKManagedStream(stream);
        using var originalBitmap = SKBitmap.Decode(managedStream);

        // resize it
        using var resizedBitmap = originalBitmap.Resize(size, SKFilterQuality.High);

        // encode it to PNG
        using var pixmap = resizedBitmap.PeekPixels();
        var pngEncoderOptions = new SKPngEncoderOptions(SKPngEncoderFilterFlags.AllFilters, zLibLevel);
        using var imageData = pixmap.Encode(pngEncoderOptions);
        var data = imageData.ToArray();

        // create a resulting stream
        return new MemoryStream(data);
    }

    public static Stream Compress(Stream stream, int quality, bool removeTransparency)
    {
        // read the original image
        using var managedStream = new SKManagedStream(stream);
        using var originalBitmap = SKBitmap.Decode(managedStream);

        // detect transparency
        using var originalPixmap = originalBitmap.PeekPixels();
        var hasTransparency = HasTransparency(originalPixmap);

        // remove alpha
        if (hasTransparency && removeTransparency)
        {
            using var newBitmap = new SKBitmap(originalBitmap.Info);
            using var canvas = new SKCanvas(newBitmap);
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(originalBitmap, 0f, 0f);
            newBitmap.CopyTo(originalBitmap);
        }

        // the image has transparency, but the option to remove is disabled, skip it then
        if (hasTransparency && !removeTransparency)
        {
            return null;
        }

        // encode it to JPEG
        using var pixmap = originalBitmap.PeekPixels();
        var jpegEncoderOptions = new SKJpegEncoderOptions(quality, SKJpegEncoderDownsample.Downsample420, SKJpegEncoderAlphaOption.Ignore);
        using var imageData = pixmap.Encode(jpegEncoderOptions);
        var data = imageData.ToArray();

        // create a resulting stream
        return new MemoryStream(data);
    }

    public static bool HasTransparency(SKPixmap pixmap)
    {
        // transparency status
        var hasTransparency = false;

        // process rows in parallel
        Parallel.For(0, pixmap.Height, (y, loopState) =>
                                       {
                                           // iterate through the pixels in each row
                                           for (var x = 0; x < pixmap.Width; x++)
                                           {
                                               if (loopState.IsStopped ||
                                                   loopState.ShouldExitCurrentIteration)
                                               {
                                                   return;
                                               }

                                               // get the pixel color
                                               var color = pixmap.GetPixelColor(x, y);

                                               // check the alpha channel value
                                               if (color.Alpha < 0XFF)
                                               {
                                                   hasTransparency = true;
                                                   loopState.Stop();
                                                   return;
                                               }
                                           }
                                       });

        return hasTransparency;
    }
}
