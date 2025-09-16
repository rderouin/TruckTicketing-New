namespace SE.TridentContrib.Extensions.Compression;

public class TargetFileInfo
{
    public string FileName { get; set; }

    public string BlobAccount { get; set; }

    public string BlobContainer { get; set; }

    public string BlobPath { get; set; }

    public string ToAbsolutePath()
    {
        return $"{BlobAccount}/{BlobContainer}/{BlobPath}";
    }
}
