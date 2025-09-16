using System;

namespace SE.TridentContrib.Extensions.Compression;

public class FileCompressionStats
{
    public TargetFileInfo FileInfo { get; set; }

    public long OriginalSize { get; set; }

    public long CompressedSize { get; set; }

    public TimeSpan TimeTaken { get; set; }
}
