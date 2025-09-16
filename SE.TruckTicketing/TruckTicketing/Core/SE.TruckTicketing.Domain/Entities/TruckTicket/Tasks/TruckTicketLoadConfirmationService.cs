using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.LegalEntity;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts;
using Trident.Data.Contracts;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public class TruckTicketLoadConfirmationService : ITruckTicketLoadConfirmationService
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, LegalEntityEntity> _legalEntityProvider;

    private readonly IManager<Guid, LoadConfirmationEntity> _loadConfirmationManager;

    private readonly IProvider<Guid, MaterialApprovalEntity> _materialApprovalProvider;

    public TruckTicketLoadConfirmationService(IManager<Guid, LoadConfirmationEntity> loadConfirmationManager,
                                              IProvider<Guid, AccountEntity> accountProvider,
                                              IProvider<Guid, MaterialApprovalEntity> materialApprovalProvider,
                                              IProvider<Guid, LegalEntityEntity> legalEntityProvider)
    {
        _loadConfirmationManager = loadConfirmationManager;
        _accountProvider = accountProvider;
        _materialApprovalProvider = materialApprovalProvider;
        _legalEntityProvider = legalEntityProvider;
    }

    public async Task<LoadConfirmationEntity> GetTruckTicketLoadConfirmation(TruckTicketEntity truckTicket,
                                                                             BillingConfigurationEntity billingConfig,
                                                                             InvoiceEntity invoice,
                                                                             int salesLineCount = 0,
                                                                             double amount = 0)
    {
        var lcDateRange = await GetDefaultLoadConfirmationDateRange(billingConfig, truckTicket);
        if (lcDateRange.EndDate is not null && invoice.InvoiceEndDate < lcDateRange.EndDate)
        {
            lcDateRange = (lcDateRange.StartDate, invoice.InvoiceEndDate?.Date);
        }

        if (invoice.InvoiceStartDate > lcDateRange.StartDate)
        {
            lcDateRange = (invoice.InvoiceStartDate.Date, lcDateRange.EndDate);
        }

        LoadConfirmationEntity loadConfirmation = null;

        var startDate = (truckTicket.EffectiveDate ?? DateTime.Today).Date;
        var endDate = (truckTicket.EffectiveDate ?? DateTime.Today).Date;

        if ((billingConfig.FieldTicketDeliveryMethod ?? FieldTicketDeliveryMethod.LoadConfirmationBatch) is FieldTicketDeliveryMethod.LoadConfirmationBatch)
        {
            var billingConfigFrequency = (billingConfig.LoadConfirmationFrequency ?? LoadConfirmationFrequency.Monthly).ToString();
            var loadConfirmations = await _loadConfirmationManager.Get(lc => lc.BillingConfigurationId == billingConfig.Id &&
                                                                             lc.Status == LoadConfirmationStatus.Open &&
                                                                             lc.InvoiceStatus == InvoiceStatus.UnPosted &&
                                                                             lc.InvoicePermutationId == invoice.InvoicePermutationId &&
                                                                             lc.Frequency == billingConfigFrequency &&
                                                                             lc.FacilityId == truckTicket.FacilityId &&
                                                                             !lc.IsReversed && !lc.IsReversal &&
                                                                             lc.StartDate <= startDate &&
                                                                             (lc.EndDate == null || lc.EndDate >= endDate),
                                                                       lcs => lcs.OrderByDescending(lc => lc.StartDate)); // PK - XP for LC by many parameters

            loadConfirmation = loadConfirmations?.FirstOrDefault();
            
            if (loadConfirmation is not null)
            {
                loadConfirmation.UpdateTicketDateRange(truckTicket.EffectiveDate ?? DateTime.Today);
            }
        }

        // Create new load confirmation if no LC could be found or the existing LC has been closed or ended early.
        if (loadConfirmation is null || (loadConfirmation.EndDate.HasValue && endDate > loadConfirmation.EndDate.Value))
        {
            var lcEndDate = loadConfirmation?.EndDate;
            loadConfirmation = await CreateLoadConfirmation(truckTicket, billingConfig, salesLineCount, amount);
            loadConfirmation.StartDate = lcEndDate?.AddMilliseconds(1) ?? lcDateRange.StartDate;
            loadConfirmation.EndDate = lcEndDate ?? lcDateRange.EndDate;
            loadConfirmation.InvoiceId = invoice.Id;
            loadConfirmation.InvoiceNumber = invoice.ProformaInvoiceNumber;
            loadConfirmation.InvoicePermutationId = invoice.InvoicePermutationId;
        }

        // Update load confirmation customers
        if (loadConfirmation.Generators.All(generator => generator.Id != truckTicket.GeneratorId))
        {
            loadConfirmation.Generators = new()
            {
                new()
                {
                    AccountId = truckTicket.GeneratorId,
                    Name = truckTicket.GeneratorName,
                },
            };
        }

        await _loadConfirmationManager.Save(loadConfirmation, true);

        return loadConfirmation;
    }

    private async Task<(DateTime StartDate, DateTime? EndDate)> GetDefaultLoadConfirmationDateRange(BillingConfigurationEntity billingConfiguration, TruckTicketEntity truckTicket)
    {
        var lcFrequency = billingConfiguration.LoadConfirmationFrequency ?? LoadConfirmationFrequency.Monthly;

        var loadDate = truckTicket.EffectiveDate ?? DateTime.Today;

        var firstDayOfTheWeek = billingConfiguration.FirstDayOfTheWeek ?? DayOfWeek.Sunday;
        var firstDayOfTheMonth = billingConfiguration.FirstDayOfTheMonth ?? 1;

        var dateRange = lcFrequency switch
                        {
                            LoadConfirmationFrequency.Daily => loadDate.DailyDateRange(),
                            LoadConfirmationFrequency.Weekly => loadDate.WeeklyDateRange(firstDayOfTheWeek),
                            LoadConfirmationFrequency.TicketByTicket => loadDate.DailyDateRange(),
                            _ => loadDate.MonthlyDateRange(firstDayOfTheMonth),
                        };

        if (billingConfiguration.FieldTicketDeliveryMethod is FieldTicketDeliveryMethod.TicketByTicket)
        {
            return (dateRange.start, dateRange.end);
        }

        if (truckTicket.MaterialApprovalId == Guid.Empty)
        {
            return dateRange;
        }

        var materialApproval = await _materialApprovalProvider.GetById(truckTicket.MaterialApprovalId);
        var useEndOfJobInvoicing = materialApproval?.EnableEndOfJobInvoicing ?? false;
        return useEndOfJobInvoicing ? (dateRange.start, null) : (dateRange.start, (DateTime?)dateRange.end);
    }

    private async Task<LoadConfirmationEntity> CreateLoadConfirmation(TruckTicketEntity truckTicket, BillingConfigurationEntity billingConfig, int salesLineCount, double amount)
    {
        var customer = await _accountProvider.GetById(billingConfig.BillingCustomerAccountId);
        var currencyCode = await GetCurrencyCode(customer, truckTicket);

        var loadConfirmation = new LoadConfirmationEntity
        {
            Number = billingConfig.FieldTicketDeliveryMethod is FieldTicketDeliveryMethod.TicketByTicket ? truckTicket.TicketNumber : default,
            BillingConfigurationId = billingConfig.Id,
            BillingConfigurationName = billingConfig.Name,
            BillingCustomerName = billingConfig.BillingCustomerName,
            BillingCustomerId = billingConfig.BillingCustomerAccountId,
            BillingCustomerNumber = customer.CustomerNumber,
            BillingCustomerDunsNumber = customer.DUNSNumber,
            FieldTicketsUploadEnabled = billingConfig.FieldTicketsUploadEnabled,
            IsSignatureRequired = billingConfig.IsSignatureRequired,
            Status = LoadConfirmationStatus.Open,
            SalesLineCount = salesLineCount,
            TotalCost = amount,
            FacilityId = truckTicket.FacilityId,
            FacilityName = truckTicket.FacilityName,
            SiteId = truckTicket.SiteId,
            Currency = currencyCode,
            LegalEntity = truckTicket.LegalEntity,
            TicketStartDate = truckTicket.EffectiveDate ?? DateTime.Today,
            TicketEndDate = truckTicket.EffectiveDate ?? DateTime.Today,
            Signatories = billingConfig.Signatories?.Where(s => s.IsAuthorized).ToList() ?? new(),
            Frequency = (billingConfig.LoadConfirmationFrequency ?? LoadConfirmationFrequency.Monthly).ToString(),
        };

        return loadConfirmation;
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
