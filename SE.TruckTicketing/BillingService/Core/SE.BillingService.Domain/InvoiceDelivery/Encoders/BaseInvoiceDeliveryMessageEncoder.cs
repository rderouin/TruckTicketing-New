using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.Entities.InvoiceExchange;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Common;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Compression;

using Trident.Contracts.Configuration;
using Trident.Logging;

namespace SE.BillingService.Domain.InvoiceDelivery.Encoders;

public abstract class BaseInvoiceDeliveryMessageEncoder : IInvoiceDeliveryMessageEncoder
{
    private readonly IFileCompressorResolver _fileCompressorResolver;

    private readonly Lazy<FeatureToggles> _lazyToggles;

    private readonly ILog _log;

    private readonly IInvoiceAttachmentsBlobStorage _storage;

    protected BaseInvoiceDeliveryMessageEncoder(IInvoiceAttachmentsBlobStorage storage, IFileCompressorResolver fileCompressorResolver, ILog log, IAppSettings appSettings)
    {
        _storage = storage;
        _fileCompressorResolver = fileCompressorResolver;
        _log = log;
        _lazyToggles = FeatureToggles.Init(appSettings);
    }

    public abstract MessageAdapterType SupportedMessageAdapterType { get; }

    public abstract Task<EncodedInvoice> EncodeMessage(InvoiceDeliveryContext context);

    protected async Task<List<EncodedInvoicePart>> FetchAttachments(List<BlobAttachment> attachments, InvoiceExchangeMessageAdapterSettingsEntity messageAdapterSettings)
    {
        // list of parts to be returned
        var parts = new List<EncodedInvoicePart>();

        // may not be any attachments
        if (attachments == null)
        {
            return parts;
        }

        // single attachment check
        if (messageAdapterSettings.SupportsSingleAttachmentOnly && attachments.Count > 1)
        {
            throw new InvalidOperationException("The target gateway supports only 1 attachment.");
        }

        // max allowed size
        var maxAttachmentSize = messageAdapterSettings.MaxAttachmentSizeInMegabytes * 1024 * 1024;

        // process
        foreach (var attachment in attachments)
        {
            MemoryStream srcStream = null;
            MemoryStream dstStream = null;

            // download the attachment
            var storageStream = await _storage.Download(attachment.ContainerName, attachment.BlobPath);
            srcStream = await storageStream.Memorize();

            // source file stats
            var sourceInfo = new
            {
                Attachment = attachment,
                MaxAttachmentSize = maxAttachmentSize,
                OriginalSize = srcStream.Length,
                Message = "Downloaded a file from the blob storage.",
            };

            _log.Information(messageTemplate: JObject.FromObject(sourceInfo).ToString());

            // find the file compressor, use it to compress if one exists
            var fileCompressor = _lazyToggles.Value.DisablePdfCompression ? null : _fileCompressorResolver.Resolve(attachment.ContentType);
            if (fileCompressor != null)
            {
                try
                {
                    if (srcStream.Length > maxAttachmentSize)
                    {
                        // target file info
                        var fileInfo = new TargetFileInfo
                        {
                            FileName = attachment.Filename,
                            BlobPath = attachment.BlobPath,
                            BlobContainer = attachment.ContainerName ?? _storage.DefaultContainerName,
                            BlobAccount = null,
                        };

                        // compress the file 
                        dstStream = new();
                        var compressionStats = await fileCompressor.CompressAsync(fileInfo, srcStream, dstStream, attachment.ContentType);
                        dstStream.Position = 0;

                        // compression stats
                        var stats = new
                        {
                            SourceInfo = sourceInfo,
                            CompressionStats = compressionStats,
                            Compressor = fileCompressor.GetType().Name,
                            attachment.ContentType,
                            Message = "Compression is successful.",
                        };

                        _log.Information(messageTemplate: JObject.FromObject(stats).ToString());
                    }
                    else
                    {
                        _log.Information(messageTemplate: $"File compression is skipped, the source file size ({srcStream.Length}) is less than the maximum allowed size ({maxAttachmentSize}).");
                    }
                }
                catch (Exception e)
                {
                    _log.Error(exception: e, messageTemplate: "Compression has failed!");
                    throw;
                }
            }
            else
            {
                // log compression stats
                _log.Information(messageTemplate: $"Compression is unsuccessful. The compressor is not defined for the content type '{attachment.ContentType}'.");
            }

            // reject the request if the file size exceeds the allowed size
            var finalLength = dstStream?.Length ?? srcStream.Length;
            if (finalLength > maxAttachmentSize)
            {
                throw new InvalidOperationException($"The attachment size ({finalLength}) exceeds the allowed size ({maxAttachmentSize}).");
            }

            // attachment to send
            parts.Add(new()
            {
                DataStream = dstStream ?? srcStream,
                ContentType = attachment.ContentType,
                IsAttachment = true,
                Source = JObject.FromObject(attachment),
                PreferredFileName = attachment.Filename,
            });
        }

        return parts;
    }
}
