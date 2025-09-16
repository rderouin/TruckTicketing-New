using System;
using System.Collections.Generic;
using System.Linq;

namespace SE.BillingService.Domain.Integrations.OpenInvoice;

public class OpenInvoiceQuery
{
    private static readonly Dictionary<OpenInvoicePath, string> Paths = new()
    {
        [OpenInvoicePath.Receipts] = "/docp/supply-chain/v1/receipts",
    };

    public OpenInvoiceQuery(Uri baseUri, OpenInvoicePath path)
    {
        BaseUri = baseUri;
        Path = path;
    }

    public Uri BaseUri { get; }

    public OpenInvoicePath Path { get; }

    public ICollection<string> Numbers { get; set; }

    public ICollection<string> Fields { get; set; }

    public int? Limit { get; set; }

    public Uri Build()
    {
        var query = new List<string>();

        // columns to return back
        if (Fields?.Any() == true)
        {
            query.Add($"$select={string.Join(",", Fields)}");
        }

        // top N results
        if (Limit.HasValue)
        {
            query.Add($"$top={Limit.Value}");
        }

        // receipt number to search for
        if (Numbers?.Any() == true)
        {
            query.Add($"$filter=receiptNumber in ({string.Join(",", Numbers.Select(t => $"'{t}'"))})");
        }

        // final URL
        return new(BaseUri, $"{Paths[Path]}?{string.Join("&", query)}");
    }

    public override string ToString()
    {
        return Build().ToString();
    }
}

public enum OpenInvoicePath
{
    Undefined = default,

    Receipts,
}

public class OpenInvoiceReceiptsResult
{
    public IList<Receipt> Receipts { get; set; }

    public class Receipt
    {
        public static OpenInvoiceReceiptStatus[] FinalStatuses { get; } = { OpenInvoiceReceiptStatus.Approved, OpenInvoiceReceiptStatus.Disputed, OpenInvoiceReceiptStatus.Cancelled };

        public string ItemId { get; set; }

        public string ReceiptNumber { get; set; }

        public string Status { get; set; }

        public OpenInvoiceReceiptStatus GetEnumStatus()
        {
            if (Enum.TryParse<OpenInvoiceReceiptStatus>(Status, true, out var value))
            {
                return value;
            }

            return default;
        }

        public bool IsInFinalStatus()
        {
            return FinalStatuses.Contains(GetEnumStatus());
        }
    }
}

public enum OpenInvoiceReceiptStatus
{
    Unknown = default,

    Submitted,

    Approved,

    Disputed,

    Cancelled,

    Saved,
}
