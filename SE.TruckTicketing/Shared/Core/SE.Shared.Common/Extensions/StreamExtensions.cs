using System.IO;
using System.Threading.Tasks;

namespace SE.Shared.Common.Extensions;

public static class StreamExtensions
{
    public static async Task<MemoryStream> Memorize(this Stream stream)
    {
        var memStream = new MemoryStream();
        await stream.CopyToAsync(memStream);
        await stream.FlushAsync();
        memStream.Position = 0;
        return memStream;
    }

    public static async Task<byte[]> ReadAll(this Stream stream)
    {
        await using var memStream = await Memorize(stream);
        return memStream.ToArray();
    }
}
