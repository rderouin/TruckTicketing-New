using System;
using System.Collections.Generic;
using System.Linq;

namespace SE.Shared.Domain.Entities.LoadConfirmation;

public class LoadConfirmationApprovalEmail
{
    public string From { get; set; }

    public string Subject { get; set; }

    [Obsolete("The parsing of the tracking code and the calculation of hash is done in the TT now.")]
    public string Number { get; set; }

    [Obsolete("The parsing of the tracking code and the calculation of hash is done in the TT now.")]
    public string Hash { get; set; }

    public string Body { get; set; }

    public IList<LoadConfirmationApprovalEmailAttachment> Attachments { get; set; }

    public bool HasEncryptedAttachments => Attachments?.Any(a => a.IsEncrypted) == true;
}

public class LoadConfirmationApprovalEmailAttachment
{
    public string BlobContainer { get; set; }

    public string BlobPath { get; set; }

    public string FileName { get; set; }

    public string ContentType { get; set; }

    public bool IsEncrypted { get; set; }
}
