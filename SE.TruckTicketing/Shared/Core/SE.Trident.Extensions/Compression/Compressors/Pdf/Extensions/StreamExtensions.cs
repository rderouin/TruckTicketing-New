using System.IO;

namespace SE.TridentContrib.Extensions.Compression.Compressors.Pdf.Extensions;

public static class StreamExtensions
{
    public static void Reset(this Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Seek(0L, SeekOrigin.Begin);
        }
    }
}
