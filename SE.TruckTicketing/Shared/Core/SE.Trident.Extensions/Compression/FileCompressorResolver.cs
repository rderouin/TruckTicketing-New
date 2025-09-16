using System.Collections.Generic;
using System.Reflection;

using Autofac;

using Trident.IoC;

namespace SE.TridentContrib.Extensions.Compression;

public class FileCompressorResolver : IFileCompressorResolver
{
    private readonly IIoCServiceLocator _serviceLocator;

    public FileCompressorResolver(IIoCServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public IFileCompressor Resolve(string contentType, string preferredStrategy = null)
    {
        // get the right context
        var context = _serviceLocator.Get<IComponentContext>();

        // find the suitable compressors by content type
        var targetedCompressors = context.ResolveNamed<IEnumerable<IFileCompressor>>(contentType);
        var fileCompressorsByStrategy = ToLookupByStrategy(targetedCompressors);

        // target the most relevant to the task
        if (fileCompressorsByStrategy.TryGetValue(preferredStrategy ?? "", out var specificCompressor))
        {
            return specificCompressor;
        }

        // try a generic version
        if (fileCompressorsByStrategy.TryGetValue("", out var genericCompressor))
        {
            return genericCompressor;
        }

        return null;
    }

    private Dictionary<string, IFileCompressor> ToLookupByStrategy(IEnumerable<IFileCompressor> compressors)
    {
        var lookup = new Dictionary<string, IFileCompressor>();

        foreach (var compressor in compressors)
        {
            var attributes = compressor.GetType().GetCustomAttributes<FileCompressorAttribute>();
            foreach (var attribute in attributes)
            {
                lookup.Add(attribute.Strategy ?? "", compressor);
            }
        }

        return lookup;
    }
}
