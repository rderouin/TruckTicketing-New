using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

using SE.TridentContrib.Extensions.Compression.Compressors.Pdf.CompressionStrategies;

using Syncfusion.Pdf;
using Syncfusion.Pdf.Exporting;
using Syncfusion.Pdf.Parsing;

using Trident.Contracts.Configuration;
using Trident.Logging;

namespace SE.TridentContrib.Extensions.Compression.Compressors.Pdf;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "This is not the cross-platform code!")]
[FileCompressor(MediaTypeNames.Application.Pdf)]
public class PdfFileCompressor : IFileCompressor
{
    private readonly IAppSettings _appSettings;

    private readonly ILog _log;

    public PdfFileCompressor(IAppSettings appSettings, ILog log)
    {
        _appSettings = appSettings;
        _log = log;
    }

    public Task<FileCompressionStats> CompressAsync(TargetFileInfo fileInfo,
                                                    Stream srcStream,
                                                    Stream dstStream,
                                                    string contentType,
                                                    string preferredStrategy = null)
    {
        // NOTE: PdfLoadedDocument with the EnableMemoryOptimization option closes the source stream
        var srcStreamLength = srcStream.Length;

        // compress the PDF document
        var compressionTimer = Stopwatch.StartNew();
        Compress(fileInfo, srcStream, dstStream);
        compressionTimer.Stop();

        // return compression stats
        return Task.FromResult(new FileCompressionStats
        {
            FileInfo = fileInfo,
            OriginalSize = srcStreamLength,
            CompressedSize = dstStream.Length,
            TimeTaken = compressionTimer.Elapsed,
        });
    }

    private void Compress(TargetFileInfo fileInfo, Stream srcStream, Stream dstStream)
    {
        // init settings
        var settings = _appSettings.GetSection<PdfFileCompressorSettings>("PDF");
        var shouldCompressImagesCustom = !settings.DisableImageCompressionCustom;
        var shouldCompressImagesNative = !settings.DisableImageCompressionNative;

        // open the PDF doc
        using var pdf = new PdfLoadedDocument(srcStream)
        {
            EnableMemoryOptimization = true,
            Compression = PdfCompressionLevel.Best,
        };

        // compress images
        if (shouldCompressImagesCustom)
        {
            CompressImages(fileInfo, pdf, settings);
        }

        // compress the PDF, running this method also optimizes the replaced images earlier
        pdf.Compress(new()
        {
            RemoveMetadata = true,
            CompressImages = shouldCompressImagesNative,
            ImageQuality = shouldCompressImagesNative ? settings.PdfImageQuality : 100,
            OptimizePageContents = true,
            OptimizeFont = true,
        });

        // save the PDF
        pdf.Save(dstStream);
    }

    private IList<IPdfImageCompressionStrategy> InitStrategies(PdfFileCompressorSettings settings)
    {
        return new IPdfImageCompressionStrategy[]
        {
            new IndexedImageCompressionStrategy(settings.IndexedImageReductionFactor,
                                                settings.ZlibCompressionLevel,
                                                settings.JpegCompressionLevel,
                                                settings.RemoveTransparency,
                                                _log),
            new DefaultCompressionStrategy(settings.ImageReductionFactor,
                                           settings.ZlibCompressionLevel,
                                           settings.JpegCompressionLevel,
                                           settings.RemoveTransparency,
                                           _log),
        };
    }

    private void CompressImages(TargetFileInfo targetFileInfo, PdfLoadedDocument pdfLoadedDocument, PdfFileCompressorSettings settings)
    {
        try
        {
            // init available image compression strategies
            var strategies = InitStrategies(settings);

            // extract all pages
            var pages = pdfLoadedDocument.Pages.Cast<PdfLoadedPage>().ToList();

            // process all pages
            foreach (var (page, pageIndex) in pages.Select((p, i) => (p, i)))
            {
                // extract all images on the page
                var images = page.GetImagesInfo();

                // process each image
                foreach (var (image, imageIndex) in images.Select((i, j) => (i, j)))
                {
                    string contextInfo = null;

                    try
                    {
                        // a context per image
                        using var context = ImageContext.Create(targetFileInfo, image, page, images.Length, pageIndex, imageIndex);
                        contextInfo = context.ToString();

                        // compress the image
                        CompressImage(context, strategies);
                    }
                    catch (Exception x2)
                    {
                        _log.Error(exception: x2, messageTemplate: $"Failed to compress an image. (context: {contextInfo})");
                    }
                }
            }
        }
        catch (Exception x1)
        {
            _log.Error(exception: x1, messageTemplate: $"Failed to compress PDF images. ({targetFileInfo.FileName} @ {targetFileInfo.ToAbsolutePath()})");
        }
    }

    private void CompressImage(ImageContext context, IList<IPdfImageCompressionStrategy> strategies)
    {
        // pick a strategy to compress the image
        var strategy = strategies.FirstOrDefault(s => s.IsApplicable(context));
        if (strategy == null)
        {
            _log.Information(messageTemplate: $"CompressImage: No strategies defined for a given image. ({context})");
            return;
        }

        // the picked strategy may require skipping the compression if the image is already optimized
        if (strategy.IsToSkipImageCompression(context))
        {
            _log.Information(messageTemplate: $"CompressImage: Skipped the image compression as per strategy. ({context})");
            return;
        }

        // compress the image, at this point there are always input and output images
        var newPdfImage = strategy.CompressImage(context);
        if (newPdfImage == null)
        {
            _log.Information(messageTemplate: $"CompressImage: No compressed image to replace with. ({context})");
            return;
        }

        // replace the image
        context.PdfLoadedPage.ReplaceImage(context.PdfImageInfo.Index, newPdfImage);
    }
}
