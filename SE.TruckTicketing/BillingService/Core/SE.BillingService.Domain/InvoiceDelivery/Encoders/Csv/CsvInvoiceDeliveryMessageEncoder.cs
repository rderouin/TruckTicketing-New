using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CsvHelper;
using CsvHelper.Configuration;

using Newtonsoft.Json.Linq;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.Entities.InvoiceExchange;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Compression;

using Trident.Contracts.Configuration;
using Trident.Logging;

namespace SE.BillingService.Domain.InvoiceDelivery.Encoders.Csv;

public class CsvInvoiceDeliveryMessageEncoder : BaseInvoiceDeliveryMessageEncoder
{
    public CsvInvoiceDeliveryMessageEncoder(IInvoiceAttachmentsBlobStorage storage, IFileCompressorResolver fileCompressorResolver, ILog log, IAppSettings appSettings)
        : base(storage, fileCompressorResolver, log, appSettings)
    {
    }

    public override MessageAdapterType SupportedMessageAdapterType => MessageAdapterType.Csv;

    public override async Task<EncodedInvoice> EncodeMessage(InvoiceDeliveryContext context)
    {
        // files/requests to send
        var parts = new List<EncodedInvoicePart>();

        // the target invoice exchange can receive attachments
        if (context.DeliveryConfig.MessageAdapterSettings.AcceptsAttachments)
        {
            // add attachments
            parts.AddRange(await FetchAttachments(context.Request.Blobs, context.DeliveryConfig.MessageAdapterSettings));
        }

        // add CSV as a last item to send
        var table = ConvertToDataTable(context.Medium, context.DeliveryConfig);
        var data = await ConvertToCsv(table,
                                      context.DeliveryConfig.MessageAdapterSettings.IncludeHeaderRow,
                                      context.DeliveryConfig.MessageAdapterSettings.AlwaysQuote ?? false);

        parts.Add(new()
        {
            DataStream = new MemoryStream(data),
            ContentType = "text/csv",
            IsAttachment = false,
            Source = context.Medium,
            PreferredFileName = "invoice.csv",
        });

        // finished data blob
        return new() { Parts = parts };
    }

    private async Task<byte[]> ConvertToCsv(DataTable table, bool includeHeaderRow, bool alwaysQuote)
    {
        // CSV config
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture);
        if (alwaysQuote)
        {
            csvConfig.ShouldQuote = _ => true;
        }

        // prep CSV writer
        await using var stream = new MemoryStream();
        await using var streamWriter = new StreamWriter(stream);
        await using var csvWriter = new CsvWriter(streamWriter, csvConfig);

        // write header
        if (includeHeaderRow)
        {
            foreach (DataColumn column in table.Columns)
            {
                csvWriter.WriteField(column.ColumnName);
            }

            await csvWriter.NextRecordAsync();
        }

        // write rows
        foreach (DataRow row in table.Rows)
        {
            for (var i = 0; i < table.Columns.Count; i++)
            {
                csvWriter.WriteField(row[i]);
            }

            await csvWriter.NextRecordAsync();
        }

        // flush all pending ops
        await csvWriter.FlushAsync();
        await streamWriter.FlushAsync();
        await stream.FlushAsync();

        // get the CSV data blob
        return stream.ToArray();
    }

    private DataTable ConvertToDataTable(JObject jsonMedium, InvoiceExchangeDeliveryConfigurationEntity deliveryConfig)
    {
        // prep a blank table
        var table = new DataTable();
        foreach (var mapping in deliveryConfig.Mappings.OrderBy(m => m.DestinationFieldPosition))
        {
            table.Columns.Add(mapping.DestinationHeaderTitle);
        }

        // copy all
        var rows = jsonMedium.SelectTokens("$.Rows[*]");
        foreach (var row in rows)
        {
            if (row is not JObject)
            {
                throw new InvalidOperationException("Medium of the CSV object is defined incorrectly.");
            }

            // copy row
            var dataRow = table.NewRow();
            var properties = row.Children();
            foreach (var property in properties.Cast<JProperty>())
            {
                // add a column if absent
                var columnName = property.Name;
                if (!table.Columns.Contains(columnName))
                {
                    table.Columns.Add(columnName);
                }

                // all must be simple properties
                if (property.Value is not JValue val)
                {
                    throw new InvalidOperationException("Rows in the medium of the CSV object are defined incorrectly.");
                }

                // set the value
                dataRow[columnName] = val.Value!;
            }

            // add the row
            table.Rows.Add(dataRow);
        }

        return table;
    }
}
