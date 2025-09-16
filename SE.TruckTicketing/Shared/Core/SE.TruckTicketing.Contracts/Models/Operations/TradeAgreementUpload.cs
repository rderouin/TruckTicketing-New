using System;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class TradeAgreementUpload : GuidApiModelBase
{
    public string Uri { get; set; }

    public string OriginalFileName { get; set; }

    public string UploadFileName { get; set; }

    public string BlobPath { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; }
}
