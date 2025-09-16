using System.IO;
using System.Threading.Tasks;

namespace SE.TridentContrib.Extensions.Compression;

public interface IFileCompressor
{
    Task<FileCompressionStats> CompressAsync(TargetFileInfo fileInfo,
                                             Stream srcStream,
                                             Stream dstStream,
                                             string contentType,
                                             string preferredStrategy = null);
}
