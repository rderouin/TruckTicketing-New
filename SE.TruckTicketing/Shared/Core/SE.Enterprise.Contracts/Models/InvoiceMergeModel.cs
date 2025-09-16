using System;

namespace SE.Enterprise.Contracts.Models;

public class InvoiceMergeModel
{
    public Guid InvoiceId { get; set; }

    public InvoiceMergeModelBlob InvoiceBlob { get; set; }
}

public class InvoiceMergeModelBlob
{
    public string ContainerName { get; set; }

    public string BlobPath { get; set; }

    public string FileName { get; set; }

    public string ContentType { get; set; }
}
