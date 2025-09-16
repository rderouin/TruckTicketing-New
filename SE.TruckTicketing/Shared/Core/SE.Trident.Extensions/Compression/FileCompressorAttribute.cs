using System;

namespace SE.TridentContrib.Extensions.Compression;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class FileCompressorAttribute : Attribute
{
    public FileCompressorAttribute(string contentType)
    {
        ContentType = contentType;
    }

    public string ContentType { get; set; }

    public string Strategy { get; set; }
}
