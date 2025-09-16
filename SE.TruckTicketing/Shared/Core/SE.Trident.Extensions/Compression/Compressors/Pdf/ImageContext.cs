using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;

using Polly;

using SE.TridentContrib.Extensions.Compression.Compressors.Pdf.Extensions;

using Syncfusion.Pdf;
using Syncfusion.Pdf.Exporting;

namespace SE.TridentContrib.Extensions.Compression.Compressors.Pdf;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class ImageContext : IDisposable
{
    private static readonly ResiliencePipeline<Stream> FetchImageStreamPipeline = InitPipeline();

    private ImageContext()
    {
    }

    public TargetFileInfo FileInfo { get; private set; }

    public PdfImageInfo PdfImageInfo { get; private set; }

    public PdfLoadedPage PdfLoadedPage { get; private set; }

    public Stream SourceStream { get; private set; }

    public Image SourceImage { get; private set; }

    public int ImagesOnPage { get; private set; }

    public int PageIndex { get; private set; }

    public int ImageIndex { get; private set; }

    public void Dispose()
    {
        PdfImageInfo?.Dispose();
        SourceStream?.Dispose();
        SourceImage?.Dispose();
    }

    private static ResiliencePipeline<Stream> InitPipeline()
    {
        return new ResiliencePipelineBuilder<Stream>()
              .AddRetry(new()
               {
                   ShouldHandle = new PredicateBuilder<Stream>().HandleResult(r => r == null),
                   MaxRetryAttempts = 10,
                   Delay = TimeSpan.FromMilliseconds(250),
                   BackoffType = DelayBackoffType.Linear,
                   MaxDelay = TimeSpan.FromMinutes(1),
               })
              .Build();
    }

    public static ImageContext Create(TargetFileInfo fileInfo, PdfImageInfo pdfImageInfo, PdfLoadedPage pdfLoadedPage, int imagesOnPage, int pageIndex, int imageIndex)
    {
        // create a source image
        var sourceStream = FetchImageStreamPipeline.Execute(() => pdfImageInfo.ImageStream);
        var sourceImage = Image.FromStream(sourceStream);
        sourceStream.Reset();

        // init
        return new()
        {
            FileInfo = fileInfo,
            PdfImageInfo = pdfImageInfo,
            PdfLoadedPage = pdfLoadedPage,
            SourceStream = sourceStream,
            SourceImage = sourceImage,
            ImagesOnPage = imagesOnPage,
            PageIndex = pageIndex,
            ImageIndex = imageIndex,
        };
    }

    public override string ToString()
    {
        return $"{FileInfo.FileName} [{PageIndex},{ImageIndex}] @ {FileInfo.ToAbsolutePath()}";
    }
}
