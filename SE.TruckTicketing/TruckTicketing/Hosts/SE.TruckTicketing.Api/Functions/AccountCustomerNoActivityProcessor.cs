using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.LegalEntity;
using SE.TridentContrib.Extensions.Managers;
using SE.TruckTicketing.Domain.Configuration;
using SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

using Trident.Contracts;
using Trident.Logging;
using Trident.Search;

namespace SE.TruckTicketing.Api.Functions;

public class AccountCustomerNoActivityProcessor
{
    private readonly IManager<Guid, AccountEntity> _accountManager;

    private readonly IAccountSettingsConfiguration _accountSettingsConfiguration;

    private readonly IManager<Guid, LegalEntityEntity> _legalEntityManager;

    private readonly ILog _logger;

    public AccountCustomerNoActivityProcessor(ILog logger,
                                              IManager<Guid, AccountEntity> accountManager,
                                              IAccountSettingsConfiguration accountSettingsConfiguration,
                                              IManager<Guid, LegalEntityEntity> legalEntityManager)
    {
        _logger = logger;
        _accountManager = accountManager;
        _accountSettingsConfiguration = accountSettingsConfiguration;
        _legalEntityManager = legalEntityManager;
    }

    /// <summary>
    ///     Runs the specified timer.
    /// </summary>
    /// <param name="timer">The timer.</param>
    /// <param name="context">The context.</param>
    [Function("AccountCustomerNoActivityProcessorTimer")]
    public async Task Run([TimerTrigger("0 30 1 * * *", RunOnStartup = false)] TimerInfo timer,
                          FunctionContext context)
    {
        try
        {
            await RunAccountCustomerNoActivityProcess();
        }
        catch (Exception ex)
        {
            // log exception and throw to put the message back on the bus for retry.
            _logger.Error(exception: ex, messageTemplate: "Account Customer No Activity Processor - Exception");
        }
        finally
        {
            _logger.Information(messageTemplate: "Account Customer No Activity Processor - Complete");
        }
    }

    private async Task RunAccountCustomerNoActivityProcess()
    {
        try
        {
            var legalEntities = await _legalEntityManager.Get(legalEntity => legalEntity.ShowAccountsInTruckTicketing == true);

            foreach (var legalEntity in legalEntities)
            {
                await ProcessLegalEntity(legalEntity);
            }

            _logger.Information(messageTemplate: "RunAccountCustomerNoActivityProcess - Finished", propertyValues:
                                new Dictionary<string, object> { { "Details", string.Empty } });
        }
        catch (Exception ex)
        {
            _logger.Error(exception: ex, messageTemplate: "RunAccountCustomerNoActivityProcess - Exception");
            throw;
        }
    }

    private async Task ProcessLegalEntity(LegalEntityEntity legalEntity)
    {
        try
        {
            if (legalEntity == null)
            {
                return;
            }

            var currentEntityThreshold = legalEntity.CreditExpirationThreshold < 1 ? 365 : legalEntity.CreditExpirationThreshold;
            var thresholdDaysDatetimeOffset = DateTimeOffset.UtcNow.AddDays(-currentEntityThreshold);
            var lastTransactionDateFilter = AxiomFilterBuilder.CreateFilter()
                                                              .StartGroup()
                                                              .AddAxiom(new()
                                                               {
                                                                   Field = nameof(AccountEntity.LastTransactionDate),
                                                                   Key = nameof(AccountEntity.LastTransactionDate) + "1",
                                                                   Operator = CompareOperators.lt,
                                                                   Value = thresholdDaysDatetimeOffset,
                                                               })
                                                              .Or()
                                                              .AddAxiom(new()
                                                               {
                                                                   Field = nameof(AccountEntity.LastTransactionDate),
                                                                   Key = nameof(AccountEntity.LastTransactionDate) + "2",
                                                                   Operator = CompareOperators.eq,
                                                                   Value = null,
                                                               })
                                                              .EndGroup()
                                                              .Build();

            var validCreditStatuses = new[] { CreditStatus.Approved.ToString(), CreditStatus.ProvisionalApproval.ToString(), CreditStatus.New.ToString() };

            var criteria = new SearchCriteria
            {
                Filters = new()
                {
                    [nameof(AccountEntity.LegalEntityId)] = legalEntity.Id,
                    [nameof(AccountEntity.CreditStatus)] = validCreditStatuses.AsInclusionAxiomFilter(nameof(AccountEntity.CreditStatus), CompareOperators.eq),
                    [nameof(AccountEntity.LastTransactionDate)] = lastTransactionDateFilter,
                    [nameof(AccountEntity.AccountTypes).AsPrimitiveCollectionFilterKey()] = new Compare
                    {
                        Operator = CompareOperators.contains,
                        Value = AccountTypes.Customer.ToString(),
                    },
                },
            };

            async Task UpdateCreditStatus(AccountEntity[] accounts)
            {
                foreach (var account in accounts)
                {
                    account.CreditStatus = CreditStatus.RequiresRenewal;
                }

                await _accountManager.BulkSave(accounts);
            }

            await _accountManager.RunBatchedAction(criteria, UpdateCreditStatus);
        }
        catch (Exception ex)
        {
            _logger.Error(exception: ex, messageTemplate: "RunAccountCustomerNoActivityProcess - Account Update- Exception");
            throw;
        }
    }
}
