using System;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;

namespace SE.TridentContrib.Extensions.Compression.Compressors;

[FileCompressor(MediaTypeNames.Application.Zip)]
public class ZipFileCompressor : IFileCompressor
{
    public Task<FileCompressionStats> CompressAsync(TargetFileInfo fileInfo, Stream srcStream, Stream dstStream, string contentType, string preferredStrategy = null)
    {
        throw new NotImplementedException();
    }
}
