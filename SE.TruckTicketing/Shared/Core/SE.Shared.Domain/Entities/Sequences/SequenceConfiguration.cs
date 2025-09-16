namespace SE.Shared.Domain.Entities.Sequences;

public class SequenceConfiguration
{
    public const string Section = "Sequences";

    public int MaxRequestBlockSize { get; set; }

    public long Seed { get; set; }

    public string Infix { get; set; }

    public string Suffix { get; set; }
}
