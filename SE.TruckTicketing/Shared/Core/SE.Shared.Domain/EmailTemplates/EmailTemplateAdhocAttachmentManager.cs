using System;
using System.Web;

using SE.TridentContrib.Extensions.Azure.Blobs;

namespace SE.Shared.Domain.EmailTemplates;

public class EmailTemplateAttachmentManager : IEmailTemplateAttachmentManager
{
    private readonly IEmailTemplateAttachmentBlobStorage _blobStorage;

    public EmailTemplateAttachmentManager(IEmailTemplateAttachmentBlobStorage blobStorage)
    {
        _blobStorage = blobStorage;
    }

    public AdHocAttachment GetUploadUrl(AdHocAttachment attachment)
    {
        var path = GetBlobPath(attachment);
        var uri = _blobStorage.GetUploadUri(_blobStorage.DefaultContainerName, path);
        attachment.Uri = uri.ToString();
        return attachment;
    }

    private string GetBlobPath(AdHocAttachment attachment)
    {
        var now = DateTime.UtcNow;
        var eventName = HttpUtility.UrlEncode(attachment.EventName);
        var fileName = HttpUtility.UrlEncode(attachment.FileName);
        var requestId = HttpUtility.UrlEncode(attachment.RequestId);
        return $"adhoc/{eventName}/{now.Year}/{now.Month}/{requestId}/{fileName}";
    }
}

public interface IEmailTemplateAttachmentManager
{
    AdHocAttachment GetUploadUrl(AdHocAttachment attachment);
}

public interface IEmailTemplateAttachmentBlobStorage : IBlobStorage
{
}

public class EmailTemplateAttachmentBlobStorage : BlobStorage, IEmailTemplateAttachmentBlobStorage
{
    public EmailTemplateAttachmentBlobStorage(string connectionString, string containerName) : base(connectionString, containerName)
    {
    }

    protected override string Prefix { get; }
}
