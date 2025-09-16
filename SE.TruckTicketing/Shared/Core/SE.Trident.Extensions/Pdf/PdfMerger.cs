namespace SE.TridentContrib.Extensions.Pdf;

public class PdfMerger : IPdfMerger
{
    public IPdfMergingHandler StartMerging()
    {
        return new PdfMergingHandler();
    }
}
