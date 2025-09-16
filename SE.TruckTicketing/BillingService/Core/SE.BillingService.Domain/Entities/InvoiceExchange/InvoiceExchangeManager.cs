using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.BillingService.Contracts.Api.Enums;
using SE.Shared.Domain.BusinessStream;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.LegalEntity;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;

namespace SE.BillingService.Domain.Entities.InvoiceExchange;

public class InvoiceExchangeManager : ManagerBase<Guid, InvoiceExchangeEntity>, IInvoiceExchangeManager
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, BusinessStreamEntity> _businessStreamProvider;

    private readonly IProvider<Guid, LegalEntityEntity> _legalEntityProvider;

    public InvoiceExchangeManager(ILog logger,
                                  IProvider<Guid, InvoiceExchangeEntity> provider,
                                  IProvider<Guid, AccountEntity> accountProvider,
                                  IProvider<Guid, LegalEntityEntity> legalEntityProvider,
                                  IProvider<Guid, BusinessStreamEntity> businessStreamProvider,
                                  IValidationManager<InvoiceExchangeEntity> validationManager = null,
                                  IWorkflowManager<InvoiceExchangeEntity> workflowManager = null)
        : base(logger, provider, validationManager, workflowManager)
    {
        _accountProvider = accountProvider;
        _legalEntityProvider = legalEntityProvider;
        _businessStreamProvider = businessStreamProvider;
    }

    public async Task<InvoiceExchangeEntity> GetFinalInvoiceExchangeConfig(string platformCode, Guid customerId)
    {
        // ======================================== FETCH CUSTOMER INFO ======================================== 

        // fetch the customer account
        var customer = await _accountProvider.GetById(customerId);
        if (customer == null)
        {
            return null;
        }

        // fetch the legal entity of the customer
        var legalEntity = await _legalEntityProvider.GetById(customer.LegalEntityId);
        if (legalEntity == null)
        {
            return null;
        }

        var businessStream = await _businessStreamProvider.GetById(legalEntity.BusinessStreamId);
        if (businessStream == null)
        {
            return null;
        }

        // ======================================== FETCH INVOICE CONFIGS ======================================== 

        // fetch the global config for the given platform
        var globalConfig = (await Provider.Get(e => e.IsDeleted == false &&
                                                    e.Type == InvoiceExchangeType.Global &&
                                                    e.PlatformCode == platformCode,
                                               noTracking: true)).MinBy(c => c.CreatedAt);

        // no platform to use for sending = no delivery
        if (globalConfig == null)
        {
            return null;
        }

        // fetch the business stream level for the defined config
        var businessStreamConfig = (await Provider.Get(e => e.IsDeleted == false &&
                                                            e.Type == InvoiceExchangeType.BusinessStream &&
                                                            e.RootInvoiceExchangeId == globalConfig.Id &&
                                                            e.BusinessStreamId == businessStream.Id,
                                                       noTracking: true)).MinBy(c => c.CreatedAt);

        // fetch the legal entity level config
        var legalEntityConfig = (await Provider.Get(e => e.IsDeleted == false &&
                                                         e.Type == InvoiceExchangeType.LegalEntity &&
                                                         e.RootInvoiceExchangeId == globalConfig.Id &&
                                                         e.BusinessStreamId == businessStream.Id &&
                                                         e.LegalEntityId == legalEntity.Id,
                                                    noTracking: true)).MinBy(c => c.CreatedAt);

        // fetch the account config that matches the given platform/root
        var accountConfig = (await Provider.Get(e => e.IsDeleted == false &&
                                                     e.Type == InvoiceExchangeType.Customer &&
                                                     e.RootInvoiceExchangeId == globalConfig.Id &&
                                                     e.BusinessStreamId == businessStream.Id &&
                                                     e.LegalEntityId == legalEntity.Id &&
                                                     e.BillingAccountId == customer.Id,
                                                noTracking: true)).MinBy(c => c.CreatedAt);

        // merge all
        return MergeConfigs(accountConfig, legalEntityConfig, businessStreamConfig, globalConfig);
    }

    public static InvoiceExchangeEntity MergeConfigs(params InvoiceExchangeEntity[] configs)
    {
        // keep only existing configs
        var filteredConfigs = configs.Where(c => c != null).ToDictionary(c => c.Type, c => c);

        // sort by enum value - global, business stream, legal entity, account
        var sortedConfigs = new SortedList<InvoiceExchangeType, InvoiceExchangeEntity>(filteredConfigs);

        // most-narrow is the unchanging base - dequeue it
        var baseConfig = sortedConfigs.Last();
        sortedConfigs.Remove(baseConfig.Key);

        // overlay other configs
        Append(baseConfig.Value, sortedConfigs);

        // resulting config
        return baseConfig.Value;
    }

    private static void Append(InvoiceExchangeEntity config, SortedList<InvoiceExchangeType, InvoiceExchangeEntity> list)
    {
        // append inherited fields based on the destination field ID
        foreach (var pair in list.Reverse())
        {
            AppendInheritedMappings(config.InvoiceDeliveryConfiguration?.MessageAdapterType,
                                    config.InvoiceDeliveryConfiguration?.Mappings,
                                    pair.Value.InvoiceDeliveryConfiguration?.Mappings);

            AppendInheritedMappings(config.FieldTicketsDeliveryConfiguration?.MessageAdapterType,
                                    config.FieldTicketsDeliveryConfiguration?.Mappings,
                                    pair.Value.FieldTicketsDeliveryConfiguration?.Mappings);
        }
    }

    private static void AppendInheritedMappings(MessageAdapterType? messageAdapterType,
                                                List<InvoiceExchangeMessageFieldMappingEntity> accumulator,
                                                List<InvoiceExchangeMessageFieldMappingEntity> addendum)
    {
        if (messageAdapterType == null || accumulator == null || addendum == null)
        {
            return;
        }

        // strategy based on the adapter type
        switch (messageAdapterType)
        {
            case MessageAdapterType.Undefined:
                // e.g. field tickets are not configured
                return;

            case MessageAdapterType.Pidx:
            case MessageAdapterType.MailMessage:
                AppendMappings(e => e.DestinationModelFieldId != null, e => $"{e.DestinationModelFieldId}|{e.DestinationPlacementHint}");
                break;

            case MessageAdapterType.Csv:
                AppendMappings(e => e.DestinationHeaderTitle != null, e => e.DestinationHeaderTitle);
                break;

            case MessageAdapterType.OpenApi:
            case MessageAdapterType.HttpEndpoint:
                throw new NotImplementedException($"{messageAdapterType} is not implemented.");

            default:
                throw new ArgumentOutOfRangeException(nameof(messageAdapterType), messageAdapterType, null);
        }

        void AppendMappings<T>(Func<InvoiceExchangeMessageFieldMappingEntity, bool> isNotNull,
                               Func<InvoiceExchangeMessageFieldMappingEntity, T> getValue)
        {
            // list of destination fields
            var existingFieldIds = accumulator.Where(isNotNull).Select(getValue).ToHashSet();

            // go over all destination fields, consider them for appendage
            foreach (var mapping in addendum.Where(isNotNull))
            {
                // add only new mappings based on the destination field ID
                if (!existingFieldIds.Contains(getValue(mapping)))
                {
                    accumulator.Add(mapping);
                }
            }
        }
    }
}
