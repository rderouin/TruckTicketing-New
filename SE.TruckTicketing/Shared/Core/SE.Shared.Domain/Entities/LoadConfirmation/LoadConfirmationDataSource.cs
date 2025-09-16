using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Humanizer;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.EDIFieldValue;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.SalesLine;
using SE.TridentContrib.Extensions.Azure.Blobs;
using SE.TruckTicketing.Contracts.Lookups;

// ReSharper disable UnusedAutoPropertyAccessor.Global - POCOs are to be consumed by the RDL
// ReSharper disable InconsistentNaming - POCOs are to be consumed by the RDL
// ReSharper disable MemberCanBePrivate.Global - POCOs are to be consumed by the RDL

namespace SE.Shared.Domain.Entities.LoadConfirmation;

public class LoadConfirmationDataSource
{
    public string TemplateKey { get; set; }

    public Dictionary<string, string> Parameters { get; } = new();

    public Dictionary<string, IEnumerable> DataSets { get; } = new();

    public List<byte[]> Attachments { get; } = new();

    public bool IsBlank { get; set; }

    public static async Task<LoadConfirmationDataSource> Create(LoadConfirmationEntity loadConfirmation,
                                                                BillingConfigurationEntity billingConfiguration,
                                                                FacilityEntity facility,
                                                                AccountEntity customerAccount,
                                                                IList<SalesLineEntity> salesLines,
                                                                IBlobStorage blobStorage,
                                                                AttachmentIndicatorType? attachmentTypeOverride)
    {
        // prep the input data
        salesLines = salesLines.Where(sl => sl.Status != SalesLineStatus.Void)
                               .OrderBy(sl => sl.TruckTicketDate)
                               .ToList();

        // the data source
        var dataSource = new LoadConfirmationDataSource
        {
            TemplateKey = facility.Type switch
                          {
                              FacilityType.Lf => "LoadConfirmationReportLF",
                              _ => "LoadConfirmationReport",
                          },

            // as of now: all types of LC PDF will have the cover and details page
            IsBlank = false,
        };

        // LC PDF attachment settings
        var includeExternalAttachmentInLC = attachmentTypeOverride.HasValue
                                                ? attachmentTypeOverride.Value == AttachmentIndicatorType.External ||
                                                  attachmentTypeOverride.Value == AttachmentIndicatorType.InternalExternal
                                                : billingConfiguration.IncludeExternalAttachmentInLC;

        var includeInternalAttachmentInLC = attachmentTypeOverride.HasValue
                                                ? attachmentTypeOverride.Value == AttachmentIndicatorType.Internal ||
                                                  attachmentTypeOverride.Value == AttachmentIndicatorType.InternalExternal
                                                : billingConfiguration.IncludeInternalAttachmentInLC;

        // all edi values in a single line
        var allEdi = FormatEdiFieldsV2(CombineEdiForLoadConfirmation(salesLines));

        // find the right contact
        var contactName = string.Empty;
        var contactAddress = string.Empty;
        if (billingConfiguration.BillingContactId.HasValue)
        {
            var billingContact = customerAccount.Contacts.Where(c => c.Id == billingConfiguration.BillingContactId).FirstOrDefault();
            contactName = $"{billingContact.Name} {billingContact.LastName}";
            contactAddress = billingContact.AccountContactAddress.GetFullAddress(false);
        }
        else
        {
            var primaryContact = customerAccount.Contacts.FirstOrDefault(c => c.IsPrimaryAccountContact) ?? customerAccount.Contacts.FirstOrDefault();
            if (primaryContact != null)
            {
                contactName = $"{primaryContact.Name} {primaryContact.LastName}";
                contactAddress = primaryContact.AccountContactAddress.GetFullAddress(false);
            }
        }

        // report parameters
        dataSource.Parameters["IncludeSaleTaxInformation"] = billingConfiguration.IncludeSaleTaxInformation.ToString();

        // report summary header
        dataSource.DataSets["HeaderDS"] = new Header[]
        {
            new()
            {
                // title bar
                TaxRegistrationNumber = customerAccount.GSTNumber ?? string.Empty,

                // billing customer info
                CustomerName = customerAccount.Name,
                CustomerAddress = contactAddress,
                AttnTo = contactName,

                // LC range
                LoadsFrom = salesLines.MinBy(sl => sl.TruckTicketDate)?.TruckTicketDate.DateTime ?? DateTimeOffset.Now.ToAlbertaOffset().DateTime,
                LoadsTo = salesLines.MaxBy(sl => sl.TruckTicketDate)?.TruckTicketDate.DateTime ?? DateTimeOffset.Now.ToAlbertaOffset().DateTime,

                // LC identification
                Number = loadConfirmation.Number,
                Date = DateTimeOffset.Now.Date,
                FacilityName = facility.Name,
                FacilityUWI = facility.LocationCode,

                // billing coding - edi
                Coding = allEdi,
            },
        };

        dataSource.DataSets["FooterDS"] = new Footer[]
        {
            new()
            {
                AdminName = $"{facility.InvoiceContactFirstName} {facility.InvoiceContactLastName}",
                AdminEmail = facility.InvoiceContactEmailAddress,
                AdminPhone = facility.InvoiceContactPhoneNumber,
                Signatories = string.Join(Environment.NewLine,
                                          loadConfirmation.Signatories?.Select(signatory => signatory.FirstName + " " + signatory.LastName + "  " + signatory.Email + "  " + signatory.PhoneNumber) ??
                                          Enumerable.Empty<string>()),
            },
        };

        // data sets - summary page
        var summaryRows = new Dictionary<string, ServicesSummary>();
        foreach (var salesLine in salesLines)
        {
            // no zero-rate items on the summary page
            if (salesLine.Rate < 0.01 && facility.Type != FacilityType.Lf)
            {
                continue;
            }

            var sourceLocation = salesLine.SourceLocationFormattedIdentifier.HasText() ? salesLine.SourceLocationFormattedIdentifier : salesLine.SourceLocationIdentifier;
            var sourceLocationFull = $"{sourceLocation} / {salesLine.GeneratorName}";
            var hashString = $"{sourceLocationFull}|{salesLine.MaterialApprovalNumber}|{salesLine.ProductName}|{salesLine.CutType}|{salesLine.Rate}";

            // if a similar line exist, add up values, otherwise add a new summary row
            if (summaryRows.TryGetValue(hashString, out var row))
            {
                row.Volume += salesLine.Quantity;
                row.GrossWeight += salesLine.GrossWeight;
                row.TareWeight += salesLine.TareWeight;
                row.NetWeight += salesLine.GrossWeight - salesLine.TareWeight;
                row.Amount += salesLine.TotalValue;
            }
            else
            {
                summaryRows.Add(hashString, new()
                {
                    Category = (int?)salesLine.CutType ?? 100,
                    SourceLocation = sourceLocationFull,
                    MaterialApproval = salesLine.MaterialApprovalNumber ?? String.Empty,
                    ServiceName = salesLine.ProductName,
                    Volume = salesLine.Quantity,
                    Rate = salesLine.Rate,
                    Units = salesLine.UnitOfMeasure,
                    Amount = salesLine.TotalValue,
                    GrossWeight = salesLine.GrossWeight,
                    TareWeight = salesLine.TareWeight,
                    NetWeight = salesLine.GrossWeight - salesLine.TareWeight,
                    IsCutLine = salesLine.IsCutLine,
                    ServiceType = salesLine.ServiceTypeName,
                });
            }
        }

        // aggregated values for the summary page
        dataSource.DataSets["ServicesSummaryDS"] = summaryRows.Values.ToList();

        // data sets - details page
        var detailsDS = new List<ServicesDetails>();
        var attachedAttachments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var salesLine in salesLines)
        {
            var sourceLocation = salesLine.SourceLocationFormattedIdentifier.HasText() ? salesLine.SourceLocationFormattedIdentifier : salesLine.SourceLocationIdentifier;
            var sourceLocationFull = $"{sourceLocation} / {salesLine.GeneratorName}";
            var appendSubstance = facility.Type == FacilityType.Lf && salesLine.Substance.HasText();

            detailsDS.Add(new()
            {
                // grouping fields
                Coding = FormatEdiFieldsForSalesLines(salesLine.EdiFieldValues),
                SourceLocation = sourceLocationFull,
                ServiceType = salesLine.ServiceTypeName,

                // details
                Date = salesLine.TruckTicketDate.Date,
                TruckingCompany = salesLine.TruckingCompanyName,
                ManifestNumber = salesLine.ManifestNumber,
                BillOfLading = salesLine.BillOfLading,
                MeterOrScaleReceipt = salesLine.TruckTicketNumber,
                ServiceName = appendSubstance ? $"{salesLine.ProductName} - {salesLine.Substance}" : salesLine.ProductName,
                GrossWeight = salesLine.GrossWeight,
                TareWeight = salesLine.TareWeight,
                NetWeight = salesLine.GrossWeight - salesLine.TareWeight,
                Units = salesLine.UnitOfMeasure,
                PercentOfLoad = salesLine.QuantityPercent / 100.0,
                ShowPercentOfLoad = facility.Type == FacilityType.Lf || salesLine.CutType == SalesLineCutType.Water,
                Rate = salesLine.Rate,
                Amount = salesLine.TotalValue,
                Volume = salesLine.Quantity,
                GlobalSort = (10000 - salesLine.EdiFieldValues?.Count ?? 10000) + ((int?)salesLine.CutType ?? 100),
                IsBolded = salesLine.CutType == SalesLineCutType.Total,
                IsCutLine = salesLine.IsCutLine,
                MaterialApproval = salesLine.MaterialApprovalNumber ?? string.Empty,
            });

            // read attachments
            foreach (var attachment in salesLine.Attachments.OrderBy(GetRank))
            {
                if ((includeExternalAttachmentInLC && attachment.IsExternalAttachment()) ||
                    (includeInternalAttachmentInLC && attachment.IsInternalAttachment()))
                {
                    // skip duplicates
                    var key = $"{attachment.Container}||{attachment.Path}";
                    if (attachedAttachments.Contains(key))
                    {
                        continue;
                    }

                    // added!
                    attachedAttachments.Add(key);

                    // attach it to this PDF
                    var pdf = await DownloadBlobFile(blobStorage, attachment.Container, attachment.Path);
                    dataSource.Attachments.Add(pdf);
                }
            }
        }

        dataSource.DataSets["ServicesDetailsDS"] = detailsDS;

        return dataSource;

        int GetRank(SalesLineAttachmentEntity attachmentEntity)
        {
            return attachmentEntity.IsInternalAttachment() ? 1 : attachmentEntity.IsExternalAttachment() ? 2 : 100;
        }
    }

    public static async Task<byte[]> DownloadBlobFile(IBlobStorage blobStorage, string container, string path)
    {
        await using var stream = await blobStorage.Download(container, path);
        await using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        await stream.FlushAsync();
        await memory.FlushAsync();
        return memory.ToArray();
    }

    public static Dictionary<string, string> CombineEdiForLoadConfirmation(IList<SalesLineEntity> salesLines)
    {
        return salesLines.Any() ? CombineEdiForLoadConfirmationV2(salesLines) : new();
    }

    public static Dictionary<string, string> CombineEdiForLoadConfirmationV2(IList<SalesLineEntity> salesLines)
    {
        // goal - to show all EDI fields that exist on all Sales Lines

        // compare edis from the first sales line to the rest of the sales lines
        var refValues = salesLines.First().EdiFieldValues.ToDictionary(v => v.EDIFieldName, v => v.EDIFieldValueContent);

        // index for quicker search
        var indexedEdis = salesLines.Select(sl => sl.EdiFieldValues.ToDictionary(v => v.EDIFieldName, v => v.EDIFieldValueContent)).ToList();

        // process all fields
        var lcEdis = new Dictionary<string, string>();
        foreach (var fieldName in refValues.Keys)
        {
            // get a reference value for the field, ensure it has contents
            if (refValues.TryGetValue(fieldName, out var valueToCompare) && valueToCompare.HasText())
            {
                // validate that all sales lines have the same value
                if (indexedEdis.All(sl => sl.TryGetValue(fieldName, out var v) && v == valueToCompare))
                {
                    // this EDI field has the identical contents on all sales lines, show at the top of the LC
                    lcEdis[fieldName] = valueToCompare;
                }
            }
        }

        return lcEdis;
    }

    public static Dictionary<string, string> CombineEdiForLoadConfirmationV1(IList<SalesLineEntity> salesLines)
    {
        // goal - to show all EDI fields that have only a single value for all Sales Lines
        //  - flatten all the EDIs into a simple array
        //  - make the array struct-based for simple ops
        //  - create a lookup by name to work on each group separately
        //  - remove duplicates from each group
        //  - select EDIs that have only a single value
        //  - order the new set
        return salesLines.SelectMany(salesLine => (IEnumerable<EDIFieldValueEntity>)salesLine.EdiFieldValues ?? Array.Empty<EDIFieldValueEntity>())
                         .Select(ediValue => (ediValue.EDIFieldName, ediValue.EDIFieldValueContent))
                         .ToLookup(tuple => tuple.EDIFieldName)
                         .Select(grouping => new
                          {
                              GroupingKey = grouping.Key,
                              Set = grouping.ToHashSet(),
                          })
                         .Where(deduped => deduped.Set.Count == 1)
                         .Select(grouping => (name: grouping.GroupingKey, value: grouping.Set.First().EDIFieldValueContent))
                         .ToDictionary(g => g.name, g => g.value);
    }

    public static string FormatEdiFieldsV2(Dictionary<string, string> ediCoding)
    {
        var lines = new List<string>();
        foreach (var edi in ediCoding.OrderBy(item => item.Key))
        {
            if (!edi.Value.HasText() || edi.Key.Humanize(LetterCasing.Title) == "Requisitioner")
            {
                continue;
            }

            var formattedValue = $"{edi.Key.Humanize(LetterCasing.Title)} : {edi.Value}";
            lines.Add(formattedValue);
        }

        return string.Join(Environment.NewLine, lines);
    }

    public static string FormatEdiFieldsForSalesLines(List<EDIFieldValueEntity> ediFieldValues)
    {
        return string.Join(", ", (ediFieldValues ?? new()).Where(x => x.EDIFieldValueContent.HasText() && x.EDIFieldName.Humanize(LetterCasing.Title) != "Requisitioner")
                                                          .Select(x => string.Concat(x.EDIFieldName.Humanize(LetterCasing.Title), ": ", x.EDIFieldValueContent)));
    }

    public static string FormatEdiFields(Dictionary<string, string> ediCoding, string separator)
    {
        var lines = new List<string>();
        var sb = new StringBuilder();
        var isFirstValue = true;
        foreach (var edi in ediCoding.OrderBy(item => item.Key))
        {
            if (!edi.Value.HasText())
            {
                continue;
            }

            var formattedValue = $"{edi.Key.Humanize(LetterCasing.Title)}: {edi.Value}";
            if (sb.Length + formattedValue.Length + separator.Length > 100)
            {
                lines.Add(sb.ToString());
                sb = new();
                isFirstValue = true;
            }

            if (isFirstValue == false)
            {
                sb.Append(separator);
            }

            sb.Append(formattedValue);
            isFirstValue = false;
        }

        lines.Add(sb.ToString());
        return string.Join(Environment.NewLine, lines);
    }

    public class Header
    {
        public string Number { get; set; }

        public DateTime? Date { get; set; }

        public string CustomerName { get; set; }

        public string CustomerAddress { get; set; }

        public string AttnTo { get; set; }

        public string TaxRegistrationNumber { get; set; }

        public DateTime LoadsFrom { get; set; }

        public DateTime LoadsTo { get; set; }

        public string FacilityName { get; set; }

        public string FacilityUWI { get; set; }

        public string Coding { get; set; }
    }

    public class Footer
    {
        public string AdminName { get; set; }

        public string AdminPhone { get; set; }

        public string AdminEmail { get; set; }

        public string Signatories { get; set; }
    }

    public class ServicesSummary
    {
        public int Category { get; set; }

        public string ServiceName { get; set; }

        public double Volume { get; set; }

        public double Rate { get; set; }

        public double Amount { get; set; }

        public string Units { get; set; }

        public string SourceLocation { get; set; }

        public string MaterialApproval { get; set; }

        public double GrossWeight { get; set; }

        public double TareWeight { get; set; }

        public double NetWeight { get; set; }

        public bool IsCutLine { get; set; }

        public string ServiceType { get; set; }
    }

    public class ServicesDetails
    {
        public int GlobalSort { get; set; }

        public string Coding { get; set; }

        public string SourceLocation { get; set; }

        public string ServiceType { get; set; }

        public DateTime Date { get; set; }

        public string TruckingCompany { get; set; }

        public string ManifestNumber { get; set; }

        public string BillOfLading { get; set; }

        public string MeterOrScaleReceipt { get; set; }

        public string ServiceName { get; set; }

        public double GrossWeight { get; set; }

        public double TareWeight { get; set; }

        public double NetWeight { get; set; }

        public string Units { get; set; }

        public double PercentOfLoad { get; set; }

        public bool ShowPercentOfLoad { get; set; }

        public double Rate { get; set; }

        public double Amount { get; set; }

        public double Volume { get; set; }

        public bool IsBolded { get; set; }

        public bool IsCutLine { get; set; }

        public string MaterialApproval { get; set; }
    }
}
