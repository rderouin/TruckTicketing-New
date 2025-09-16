using System;
using System.IO;

namespace SE.TridentContrib.Extensions.Pdf;

public interface IPdfMergingHandler : IDisposable
{
    void Append(byte[] data)
    {
        using var stream = new MemoryStream(data);
        Append(stream);
    }

    void Append(Stream sourceStream);

    void Save(Stream targetStream);

    byte[] ToByteArray()
    {
        using var stream = new MemoryStream();
        Save(stream);
        return stream.ToArray();
    }
}
