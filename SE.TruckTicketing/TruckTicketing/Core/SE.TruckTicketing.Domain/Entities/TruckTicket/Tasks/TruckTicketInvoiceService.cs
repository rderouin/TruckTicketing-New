using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.EDIFieldDefinition;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.LegalEntity;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts;
using Trident.Data.Contracts;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public class TruckTicketInvoiceService : ITruckTicketInvoiceService
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, EDIFieldDefinitionEntity> _ediFieldDefinitionProvider;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IProvider<Guid, InvoiceConfigurationEntity> _invoiceConfigProvider;

    private readonly IManager<Guid, InvoiceEntity> _invoiceManager;

    private readonly IProvider<Guid, LegalEntityEntity> _legalEntityProvider;

    private readonly IProvider<Guid, MaterialApprovalEntity> _materialApprovalProvider;

    public TruckTicketInvoiceService(IManager<Guid, InvoiceEntity> invoiceManager,
                                     IProvider<Guid, InvoiceConfigurationEntity> invoiceConfigProvider,
                                     IProvider<Guid, MaterialApprovalEntity> materialApprovalProvider,
                                     IProvider<Guid, AccountEntity> accountProvider,
                                     IProvider<Guid, EDIFieldDefinitionEntity> ediFieldDefinitionProvider,
                                     IProvider<Guid, FacilityEntity> facilityProvider,
                                     IProvider<Guid, LegalEntityEntity> legalEntityProvider)
    {
        _invoiceManager = invoiceManager;
        _invoiceConfigProvider = invoiceConfigProvider;
        _materialApprovalProvider = materialApprovalProvider;
        _accountProvider = accountProvider;
        _ediFieldDefinitionProvider = ediFieldDefinitionProvider;
        _facilityProvider = facilityProvider;
        _legalEntityProvider = legalEntityProvider;
    }

    public async Task<InvoiceEntity> GetTruckTicketInvoice(TruckTicketEntity truckTicket, BillingConfigurationEntity billingConfig, int salesLineCount = 0, double amount = 0)
    {
        var invoiceConfig = await _invoiceConfigProvider.GetById(billingConfig.InvoiceConfigurationId);
        var invoice = await GetOrCreateInvoice(truckTicket, billingConfig, invoiceConfig, salesLineCount, amount, true);

        return invoice;
    }

    public async Task<string> EvaluateInvoiceConfigurationThreshold(TruckTicketEntity truckTicket, BillingConfigurationEntity billingConfig, int salesLineCount, double amount)
    {
        var invoiceNumber = string.Empty;
        var invoiceConfig = await _invoiceConfigProvider.GetById(billingConfig.InvoiceConfigurationId);
        if (invoiceConfig == null)
        {
            return string.Empty;
        }

        var dollarValueThreshold = invoiceConfig.ThresholdDollarValue.GetValueOrDefault(Double.MaxValue);
        var ticketCountThreshold = invoiceConfig.ThresholdTicketCount.GetValueOrDefault(int.MaxValue);
        var invoiceEntity = await GetOrCreateInvoice(truckTicket, billingConfig, invoiceConfig, salesLineCount, amount);
        var isNewInvoice = invoiceEntity.CreatedAt == default;
        if (!isNewInvoice)
        {
            invoiceNumber = $"#{invoiceEntity.ProformaInvoiceNumber}IP";
            invoiceEntity.InvoiceAmount += amount;
            invoiceEntity.TruckTicketCount += 1;
        }

        //Check for $ Value threshold
        if (invoiceConfig.IsMaximumDollarValueThresholdEnabled is true &&
            invoiceEntity.InvoiceAmount > dollarValueThreshold)
        {
            return $"Perceived invoice {invoiceNumber} must be posted when it reaches ${dollarValueThreshold}. You have met or exceeded this threshold. Invoice must be posted.";
        }

        //Check for Ticket Count
        if (invoiceConfig.IsMaximumTicketsThresholdEnabled is true &&
            invoiceEntity.TruckTicketCount > ticketCountThreshold)
        {
            return $"Perceived invoice {invoiceNumber} must be posted when it reaches {ticketCountThreshold} tickets. You have met or exceeded this threshold. Invoice must be posted.";
        }

        return string.Empty;
    }

    private async Task<InvoiceEntity> GetOrCreateInvoice(TruckTicketEntity truckTicket,
                                                         BillingConfigurationEntity billingConfig,
                                                         InvoiceConfigurationEntity invoiceConfig,
                                                         int salesLineCount = 0,
                                                         double amount = 0,
                                                         bool allowCreateNewInvoice = false)
    {
        var invoicePermutationId = await ComputeInvoicePermutationId(truckTicket, invoiceConfig);
        var invoiceDateRange = await GetDefaultInvoiceDateRange(truckTicket);
        var effectiveDate = truckTicket.EffectiveDate ?? DateTime.Today;
        var endDate = effectiveDate.AddDays(1).AddTicks(-1);
        var invoices = await _invoiceManager.Get(inv => inv.InvoicePermutationId == invoicePermutationId &&
                                                        inv.InvoiceConfigurationId == billingConfig.InvoiceConfigurationId &&
                                                        inv.Status == InvoiceStatus.UnPosted &&
                                                        inv.FacilityId == truckTicket.FacilityId &&
                                                        !inv.IsReversed && !inv.IsReversal &&
                                                        inv.InvoiceStartDate <= effectiveDate &&
                                                        (inv.InvoiceEndDate == null || inv.InvoiceEndDate >= endDate),
                                                 invs => invs.OrderByDescending(inv => inv.InvoiceEndDate)); // PK - XP for invoices by many parameters

        var invoice = invoices?.FirstOrDefault();

        if (invoice is null ||
            (invoice.InvoiceEndDate.HasValue && endDate > invoice.InvoiceEndDate.Value))
        {
            invoice = await CreateInvoice(truckTicket, billingConfig, invoiceConfig, salesLineCount, amount);
            invoice.InvoicePermutationId = invoicePermutationId;
            invoice.InvoiceStartDate = invoiceDateRange.StartDate;
            invoice.InvoiceEndDate = invoiceDateRange.EndDate;

            if (allowCreateNewInvoice)
            {
                await _invoiceManager.Save(invoice, true);
            }
        }

        return invoice;
    }

    private async Task<(DateTime StartDate, DateTime? EndDate)> GetDefaultInvoiceDateRange(TruckTicketEntity truckTicket)
    {
        var loadDate = truckTicket.EffectiveDate ?? DateTime.Today;
        var invoiceDateRange = loadDate.MonthlyDateRange(1);

        if (truckTicket.MaterialApprovalId == Guid.Empty)
        {
            return invoiceDateRange;
        }

        var materialApproval = await _materialApprovalProvider.GetById(truckTicket.MaterialApprovalId);
        var useEndOfJobInvoicing = materialApproval?.EnableEndOfJobInvoicing ?? false;
        return useEndOfJobInvoicing ? (invoiceDateRange.start, null) : invoiceDateRange;
    }

    private async Task<InvoiceEntity> CreateInvoice(TruckTicketEntity truckTicket,
                                                    BillingConfigurationEntity billingConfig,
                                                    InvoiceConfigurationEntity invoiceConfig,
                                                    int salesLineCount,
                                                    double amount)
    {
        var customer = await _accountProvider.GetById(billingConfig.BillingCustomerAccountId);
        var facility = await _facilityProvider.GetById(truckTicket.FacilityId);
        var currencyCode = await GetCurrencyCode(customer, truckTicket);

        return new()
        {
            CustomerId = billingConfig.BillingCustomerAccountId,
            CustomerName = billingConfig.BillingCustomerName,
            CustomerNumber = customer?.CustomerNumber,
            AccountNumber = customer?.AccountNumber,
            FacilityId = truckTicket.FacilityId,
            FacilityName = truckTicket.FacilityName,
            SiteId = truckTicket.SiteId,
            Status = InvoiceStatus.UnPosted,
            SalesLineCount = salesLineCount,
            InvoiceAmount = amount,
            InvoiceConfigurationId = billingConfig.InvoiceConfigurationId,
            InvoiceConfigurationName = invoiceConfig.Name,
            BillingConfigurationId = billingConfig.Id,
            Currency = currencyCode,
            LegalEntity = truckTicket.LegalEntity,
            TicketDateRangeStart = truckTicket.EffectiveDate ?? DateTime.Today,
            TicketDateRangeEnd = truckTicket.EffectiveDate ?? DateTime.Today,
            Generators = new() { List = new() { truckTicket.GeneratorName } },
            Signatories = new() { List = billingConfig.Signatories?.Where(contact => contact.IsAuthorized).Select(contact => contact.FirstName + " " + contact.LastName).ToList() ?? new() },
            BusinessUnit = facility?.BusinessUnitId,
            Division = facility?.Division,
            RequiresPdfRegeneration = true,
            MaxInvoiceAmountThreshold = invoiceConfig.IsMaximumDollarValueThresholdEnabled is true ? invoiceConfig.ThresholdDollarValue.GetValueOrDefault(Double.MaxValue) : null,
            MaxTruckTicketCountThreshold = invoiceConfig.IsMaximumTicketsThresholdEnabled is true ? invoiceConfig.ThresholdTicketCount.GetValueOrDefault(Int32.MaxValue) : null,
            // use the configured billing contact from the invoice configuration
            // if the billing contact isn't configured, take the default billing contact from the customer entity
            BillingContactId = invoiceConfig.BillingContactId ?? customer?.Contacts.FirstOrDefault(c => c.IsPrimaryAccountContact)?.Id,
            BillingContactName = invoiceConfig.BillingContactName ?? customer?.Contacts.FirstOrDefault(c => c.IsPrimaryAccountContact)?.GetFullName(),
            BillingConfigurations = new()
            {
                new()
                {
                    AssociatedSalesLinesCount = salesLineCount,
                    BillingConfigurationId = billingConfig.Id,
                    BillingConfigurationName = billingConfig.Name,
                },
            },
        };
    }

    public async Task<string> ComputeInvoicePermutationId(TruckTicketEntity truckTicket,
                                                          InvoiceConfigurationEntity invoiceConfig)
    {
        var sourceLocationId = invoiceConfig.IsSplitBySourceLocation ? truckTicket.SourceLocationId : Guid.Empty;
        var serviceTypeId = invoiceConfig.IsSplitByServiceType ? truckTicket.ServiceTypeId : Guid.Empty;
        var wellClassification = invoiceConfig.IsSplitByWellClassification ? truckTicket.WellClassification : WellClassifications.Undefined;
        var substanceId = invoiceConfig.IsSplitBySubstance ? truckTicket.SubstanceId : Guid.Empty;
        var facilityId = invoiceConfig.IsSplitByFacility ? truckTicket.FacilityId : Guid.Empty;

        var fields = new List<string>
        {
            "SL" + sourceLocationId,
            "ST" + serviceTypeId,
            "WC" + wellClassification,
            "SB" + substanceId,
            "FC" + facilityId,
        };

        var splitEdiDefinitionIds = invoiceConfig.SplitEdiFieldDefinitions?.List.OrderBy(id => id).ToList() ?? new();
        if (splitEdiDefinitionIds.Any())
        {
            var ediDefinitionMap = (await _ediFieldDefinitionProvider.GetByIds(splitEdiDefinitionIds.Distinct())).ToDictionary(definition => definition.Id);
            var ediDefinitionValueMap = (truckTicket.EdiFieldValues ?? new()).ToDictionary(value => value.EDIFieldDefinitionId, value => value.EDIFieldValueContent);

            foreach (var id in splitEdiDefinitionIds)
            {
                if (ediDefinitionMap.TryGetValue(id, out var ediFieldDefinition))
                {
                    ediDefinitionValueMap.TryGetValue(ediFieldDefinition.Id, out var value);
                    fields.Add(ediFieldDefinition.EDIFieldName + value);
                }
            }
        }

        return string.Join("|", fields);
    }

    private async Task<string> GetCurrencyCode(AccountEntity customer, TruckTicketEntity truckTicket)
    {
        if (customer?.CurrencyCode != null)
        {
            return customer.CurrencyCode.ToString();
        }

        var countryCode = (await _legalEntityProvider.GetById(truckTicket?.LegalEntityId))?.CountryCode;

        return countryCode switch
               {
                   CountryCode.CA => CurrencyCode.CAD.ToString(),
                   CountryCode.US => CurrencyCode.USD.ToString(),
                   _ => null,
               };
    }
}
