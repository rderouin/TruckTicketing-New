namespace SE.TridentContrib.Extensions.Compression;

public interface IFileCompressorResolver
{
    IFileCompressor Resolve(string contentType, string preferredStrategy = null);
}
