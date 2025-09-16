using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Domain;
using Trident.Search;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.Account.Tasks;

public class CustomerAccountNameReferenceUpdateTask : WorkflowTaskBase<BusinessContext<AccountEntity>>
{
    private readonly IProvider<Guid, BillingConfigurationEntity> _billingConfigProvider;

    private readonly IProvider<Guid, InvoiceConfigurationEntity> _invoiceConfigProvider;

    private readonly IProvider<Guid, InvoiceEntity> _invoiceProvider;

    private readonly IProvider<Guid, LoadConfirmationEntity> _loadConfirmationProvider;

    private readonly IProvider<Guid, MaterialApprovalEntity> _materialApprovalProvider;

    private readonly IProvider<Guid, SalesLineEntity> _salesLineProvider;

    private readonly IProvider<Guid, SourceLocationEntity> _sourceLocationProvider;

    private readonly IProvider<Guid, TruckTicketEntity> _truckTicketProvider;

    public CustomerAccountNameReferenceUpdateTask(IProvider<Guid, TruckTicketEntity> truckTicketProvider,
                                                  IProvider<Guid, SalesLineEntity> salesLineProvider,
                                                  IProvider<Guid, BillingConfigurationEntity> billingConfigProvider,
                                                  IProvider<Guid, InvoiceConfigurationEntity> invoiceConfigProvider,
                                                  IProvider<Guid, SourceLocationEntity> sourceLocationProvider,
                                                  IProvider<Guid, MaterialApprovalEntity> materialApprovalProvider,
                                                  IProvider<Guid, LoadConfirmationEntity> loadConfirmationProvider,
                                                  IProvider<Guid, InvoiceEntity> invoiceProvider)
    {
        _truckTicketProvider = truckTicketProvider;
        _salesLineProvider = salesLineProvider;
        _billingConfigProvider = billingConfigProvider;
        _invoiceConfigProvider = invoiceConfigProvider;
        _sourceLocationProvider = sourceLocationProvider;
        _materialApprovalProvider = materialApprovalProvider;
        _loadConfirmationProvider = loadConfirmationProvider;
        _invoiceProvider = invoiceProvider;
    }

    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<AccountEntity> context)
    {
        var account = context.Target;
        await Run(() => UpdateOpenSalesLines(account), account, AccountTypes.Customer);
        await Run(() => UpdateOpenTruckTickets(account), account, AccountTypes.Customer, AccountTypes.Generator, AccountTypes.TruckingCompany);
        await Run(() => UpdateInvoiceConfigurations(account), account, AccountTypes.Customer);
        await Run(() => UpdateBillingConfigurations(account), account, AccountTypes.Customer, AccountTypes.Generator);
        await Run(() => UpdateSourceLocations(account), account, AccountTypes.Generator);
        await Run(() => UpdateMaterialApprovals(account), account, AccountTypes.Generator, AccountTypes.Customer, AccountTypes.TruckingCompany, AccountTypes.ThirdPartyAnalytical);
        await Run(() => UpdateLoadConfirmations(account), account, AccountTypes.Customer);
        await Run(() => UpdateInvoices(account), account, AccountTypes.Customer);

        return true;
    }

    private async Task Run(Func<Task> operation, AccountEntity account, params string[] accountTypes)
    {
        if (!account.AccountTypes.List.Intersect(accountTypes).Any())
        {
            return;
        }

        await operation();
    }

    private async Task UpdateOpenSalesLines(AccountEntity account)
    {
        var criteria = new SearchCriteria
        {
            Filters = new()
            {
                [nameof(SalesLineEntity.CustomerId)] = account.Id.ToString(),
                [nameof(SalesLineEntity.Status)] = AxiomFilterBuilder.CreateFilter()
                                                                     .StartGroup()
                                                                     .AddAxiom(new()
                                                                      {
                                                                          Key = "Status1",
                                                                          Field = nameof(TruckTicketEntity.Status),
                                                                          Operator = CompareOperators.ne,
                                                                          Value = SalesLineStatus.Posted.ToString(),
                                                                      })
                                                                     .And()
                                                                     .AddAxiom(new()
                                                                      {
                                                                          Key = "Status2",
                                                                          Field = nameof(TruckTicketEntity.Status),
                                                                          Operator = CompareOperators.ne,
                                                                          Value = SalesLineStatus.Void.ToString(),
                                                                      })
                                                                     .EndGroup()
                                                                     .Build(),
            },
        };

        async Task UpdateCustomerNames(SalesLineEntity[] salesLines)
        {
            for (var index = 0; index < salesLines.Length; index++)
            {
                var salesLine = salesLines[index];
                if (salesLine.GeneratorId == account.Id)
                {
                    salesLine.GeneratorName = account.Name;
                }

                if (salesLine.CustomerId == account.Id)
                {
                    salesLine.CustomerName = account.Name;
                }

                await _salesLineProvider.Update(salesLine, index != salesLines.Length - 1);
            }
        }

        await RunBatchedAction(_salesLineProvider, criteria, UpdateCustomerNames);
    }

    private async Task UpdateOpenTruckTickets(AccountEntity account)
    {
        var criteria = new SearchCriteria
        {
            Filters = new()
            {
                [nameof(TruckTicketEntity.BillingCustomerId)] = AxiomFilterBuilder.CreateFilter()
                                                                                  .StartGroup()
                                                                                  .AddAxiom(new()
                                                                                   {
                                                                                       Key = nameof(TruckTicketEntity.BillingCustomerId),
                                                                                       Field = nameof(TruckTicketEntity.BillingCustomerId),
                                                                                       Value = account.Id.ToString(),
                                                                                       Operator = CompareOperators.eq,
                                                                                   })
                                                                                  .Or()
                                                                                  .AddAxiom(new()
                                                                                   {
                                                                                       Key = nameof(TruckTicketEntity.GeneratorId),
                                                                                       Field = nameof(TruckTicketEntity.GeneratorId),
                                                                                       Value = account.Id.ToString(),
                                                                                       Operator = CompareOperators.eq,
                                                                                   })
                                                                                  .Or()
                                                                                  .AddAxiom(new()
                                                                                   {
                                                                                       Key = nameof(TruckTicketEntity.TruckingCompanyId),
                                                                                       Field = nameof(TruckTicketEntity.TruckingCompanyId),
                                                                                       Value = account.Id.ToString(),
                                                                                       Operator = CompareOperators.eq,
                                                                                   })
                                                                                  .EndGroup()
                                                                                  .Build(),
                [nameof(TruckTicketEntity.Status)] = AxiomFilterBuilder.CreateFilter()
                                                                       .StartGroup()
                                                                       .AddAxiom(new()
                                                                        {
                                                                            Key = "Status1",
                                                                            Field = nameof(TruckTicketEntity.Status),
                                                                            Operator = CompareOperators.eq,
                                                                            Value = TruckTicketStatus.Open.ToString(),
                                                                        })
                                                                       .Or()
                                                                       .AddAxiom(new()
                                                                        {
                                                                            Key = "Status2",
                                                                            Field = nameof(TruckTicketEntity.Status),
                                                                            Operator = CompareOperators.eq,
                                                                            Value = TruckTicketStatus.Hold.ToString(),
                                                                        })
                                                                       .Or()
                                                                       .AddAxiom(new()
                                                                        {
                                                                            Key = "Status3",
                                                                            Field = nameof(TruckTicketEntity.Status),
                                                                            Operator = CompareOperators.eq,
                                                                            Value = TruckTicketStatus.Approved.ToString(),
                                                                        })
                                                                       .EndGroup()
                                                                       .Build(),
            },
        };

        async Task UpdateCustomerNames(TruckTicketEntity[] truckTickets)
        {
            for (var index = 0; index < truckTickets.Length; index++)
            {
                var truckTicket = truckTickets[index];

                if (truckTicket.BillingCustomerId == account.Id)
                {
                    truckTicket.BillingCustomerName = account.Name;
                }

                if (truckTicket.GeneratorId == account.Id)
                {
                    truckTicket.GeneratorName = account.Name;
                }

                if (truckTicket.TruckingCompanyId == account.Id)
                {
                    truckTicket.TruckingCompanyName = account.Name;
                }

                await _truckTicketProvider.Update(truckTicket, index != truckTickets.Length - 1);
            }
        }

        await RunBatchedAction(_truckTicketProvider, criteria, UpdateCustomerNames);
    }

    private async Task UpdateInvoiceConfigurations(AccountEntity account)
    {
        async Task UpdateCustomerNames(InvoiceConfigurationEntity[] invoiceConfigurations)
        {
            for (var index = 0; index < invoiceConfigurations.Length; index++)
            {
                var invoiceConfiguration = invoiceConfigurations[index];
                invoiceConfiguration.CustomerName = account.Name;
                await _invoiceConfigProvider.Update(invoiceConfiguration, index != invoiceConfigurations.Length - 1);
            }
        }

        var criteria = new SearchCriteria
        {
            Filters = new()
            {
                { nameof(InvoiceConfigurationEntity.CustomerId), account.Id.ToString() },
            },
        };

        await RunBatchedAction(_invoiceConfigProvider, criteria, UpdateCustomerNames);
    }

    private async Task UpdateBillingConfigurations(AccountEntity account)
    {
        async Task UpdateCustomerNames(BillingConfigurationEntity[] billingConfigurations)
        {
            for (var index = 0; index < billingConfigurations.Length; index++)
            {
                var billingConfig = billingConfigurations[index];

                if (billingConfig.CustomerGeneratorId == account.Id)
                {
                    billingConfig.CustomerGeneratorName = account.Name;
                }

                if (billingConfig.BillingCustomerAccountId == account.Id)
                {
                    billingConfig.BillingCustomerName = account.Name;
                }

                await _billingConfigProvider.Update(billingConfig, index != billingConfigurations.Length - 1);
            }
        }

        var criteria = new SearchCriteria
        {
            Filters = new()
            {
                [nameof(BillingConfigurationEntity.BillingCustomerAccountId)] = AxiomFilterBuilder.CreateFilter()
                                                                                                  .StartGroup()
                                                                                                  .AddAxiom(new()
                                                                                                   {
                                                                                                       Key = nameof(BillingConfigurationEntity.CustomerGeneratorId),
                                                                                                       Field = nameof(BillingConfigurationEntity.CustomerGeneratorId),
                                                                                                       Value = account.Id.ToString(),
                                                                                                       Operator = CompareOperators.eq,
                                                                                                   })
                                                                                                  .Or()
                                                                                                  .AddAxiom(new()
                                                                                                   {
                                                                                                       Key = nameof(BillingConfigurationEntity.BillingCustomerAccountId),
                                                                                                       Field = nameof(BillingConfigurationEntity.BillingCustomerAccountId),
                                                                                                       Value = account.Id.ToString(),
                                                                                                       Operator = CompareOperators.eq,
                                                                                                   })
                                                                                                  .EndGroup()
                                                                                                  .Build(),
            },
        };

        await RunBatchedAction(_billingConfigProvider, criteria, UpdateCustomerNames);
    }

    private async Task UpdateSourceLocations(AccountEntity account)
    {
        if (!account.AccountTypes.List.Contains(AccountTypes.Generator))
        {
            return;
        }

        async Task UpdateNameReferences(SourceLocationEntity[] sourceLocations)
        {
            for (var index = 0; index < sourceLocations.Length; index++)
            {
                var sourceLocation = sourceLocations[index];

                if (sourceLocation.GeneratorId == account.Id)
                {
                    sourceLocation.GeneratorName = account.Name;
                }

                if (sourceLocation.ContractOperatorId == account.Id)
                {
                    sourceLocation.ContractOperatorName = account.Name;
                }

                await _sourceLocationProvider.Update(sourceLocation, index != sourceLocations.Length - 1);
            }
        }

        var criteria = new SearchCriteria
        {
            Filters = new()
            {
                [nameof(SourceLocationEntity.GeneratorId)] = AxiomFilterBuilder.CreateFilter()
                                                                               .StartGroup()
                                                                               .AddAxiom(new()
                                                                                {
                                                                                    Key = nameof(SourceLocationEntity.GeneratorId),
                                                                                    Field = nameof(SourceLocationEntity.GeneratorId),
                                                                                    Value = account.Id.ToString(),
                                                                                    Operator = CompareOperators.eq,
                                                                                })
                                                                               .Or()
                                                                               .AddAxiom(new()
                                                                                {
                                                                                    Key = nameof(SourceLocationEntity.ContractOperatorId),
                                                                                    Field = nameof(SourceLocationEntity.ContractOperatorId),
                                                                                    Value = account.Id.ToString(),
                                                                                    Operator = CompareOperators.eq,
                                                                                })
                                                                               .EndGroup()
                                                                               .Build(),
            },
        };

        await RunBatchedAction(_sourceLocationProvider, criteria, UpdateNameReferences);
    }

    private async Task UpdateMaterialApprovals(AccountEntity account)
    {
        async Task UpdateNameReferences(MaterialApprovalEntity[] materialApprovals)
        {
            for (var index = 0; index < materialApprovals.Length; index++)
            {
                var materialApproval = materialApprovals[index];

                if (materialApproval.BillingCustomerId == account.Id)
                {
                    materialApproval.BillingCustomerName = account.Name;
                }

                if (materialApproval.GeneratorId == account.Id)
                {
                    materialApproval.GeneratorName = account.Name;
                }

                if (materialApproval.TruckingCompanyId == account.Id)
                {
                    materialApproval.TruckingCompanyName = account.Name;
                }

                if (materialApproval.ThirdPartyAnalyticalCompanyId == account.Id)
                {
                    materialApproval.ThirdPartyAnalyticalCompanyName = account.Name;
                }

                await _materialApprovalProvider.Update(materialApproval, index != materialApprovals.Length - 1);
            }
        }

        var criteria = new SearchCriteria
        {
            Filters = new()
            {
                [nameof(MaterialApprovalEntity.BillingCustomerId)] = AxiomFilterBuilder.CreateFilter()
                                                                                       .StartGroup()
                                                                                       .AddAxiom(new()
                                                                                        {
                                                                                            Key = nameof(MaterialApprovalEntity.BillingCustomerId),
                                                                                            Field = nameof(MaterialApprovalEntity.BillingCustomerId),
                                                                                            Value = account.Id.ToString(),
                                                                                            Operator = CompareOperators.eq,
                                                                                        })
                                                                                       .Or()
                                                                                       .AddAxiom(new()
                                                                                        {
                                                                                            Key = nameof(MaterialApprovalEntity.GeneratorId),
                                                                                            Field = nameof(MaterialApprovalEntity.GeneratorId),
                                                                                            Value = account.Id.ToString(),
                                                                                            Operator = CompareOperators.eq,
                                                                                        })
                                                                                       .Or()
                                                                                       .AddAxiom(new()
                                                                                        {
                                                                                            Key = nameof(MaterialApprovalEntity.TruckingCompanyId),
                                                                                            Field = nameof(MaterialApprovalEntity.TruckingCompanyId),
                                                                                            Value = account.Id.ToString(),
                                                                                            Operator = CompareOperators.eq,
                                                                                        })
                                                                                       .Or()
                                                                                       .AddAxiom(new()
                                                                                        {
                                                                                            Key = nameof(MaterialApprovalEntity.ThirdPartyAnalyticalCompanyId),
                                                                                            Field = nameof(MaterialApprovalEntity.ThirdPartyAnalyticalCompanyId),
                                                                                            Value = account.Id.ToString(),
                                                                                            Operator = CompareOperators.eq,
                                                                                        })
                                                                                       .EndGroup()
                                                                                       .Build(),
            },
        };

        await RunBatchedAction(_materialApprovalProvider, criteria, UpdateNameReferences);
    }

    private async Task UpdateInvoices(AccountEntity account)
    {
        async Task UpdateCustomerNames(InvoiceEntity[] invoiceEntities)
        {
            for (var index = 0; index < invoiceEntities.Length; index++)
            {
                var invoice = invoiceEntities[index];
                invoice.CustomerName = account.Name;
                await _invoiceProvider.Update(invoice, index != invoiceEntities.Length - 1);
            }
        }

        var criteria = new SearchCriteria
        {
            Filters = new()
            {
                [nameof(InvoiceEntity.CustomerId)] = account.Id.ToString(),
                [nameof(InvoiceEntity.Status)] = InvoiceStatus.UnPosted.ToString(),
            },
        };

        await RunBatchedAction(_invoiceProvider, criteria, UpdateCustomerNames);
    }

    private async Task UpdateLoadConfirmations(AccountEntity account)
    {
        async Task UpdateNameReferences(LoadConfirmationEntity[] loadConfirmations)
        {
            for (var index = 0; index < loadConfirmations.Length; index++)
            {
                var loadConfirmation = loadConfirmations[index];
                loadConfirmation.BillingCustomerName = account.Name;

                await _loadConfirmationProvider.Update(loadConfirmation, index != loadConfirmations.Length - 1);
            }
        }

        var criteria = new SearchCriteria
        {
            Filters = new()
            {
                [nameof(LoadConfirmationEntity.BillingCustomerId)] = account.Id.ToString(),
                [nameof(LoadConfirmationEntity.InvoiceStatus)] = InvoiceStatus.UnPosted,
                [nameof(LoadConfirmationEntity.Status)] = new Compare
                {
                    Value = LoadConfirmationStatus.Void.ToString(),
                    Operator = CompareOperators.ne,
                },
            },
        };

        await RunBatchedAction(_loadConfirmationProvider, criteria, UpdateNameReferences);
    }

    private async Task RunBatchedAction<TEntity>(IProvider<Guid, TEntity> provider, SearchCriteria criteria, Func<TEntity[], Task> action)
        where TEntity : EntityBase<Guid>
    {
        criteria.PageSize = 500;

        SearchResults<TEntity, SearchCriteria> search;

        do
        {
            search = await provider.Search(criteria);
            var items = search?.Results.ToArray();
            if (items is null || items.Length == 0)
            {
                return;
            }

            await action(items);
            criteria = search.Info.NextPageCriteria;
        } while (search.Info.CurrentPage < (search.Info.NextPageCriteria.CurrentPage ?? 0));
    }

    public override Task<bool> ShouldRun(BusinessContext<AccountEntity> context)
    {
        var shouldRun = context.Operation == Operation.Update &&
                        context.Original.Name != context.Target.Name;

        return Task.FromResult(shouldRun);
    }
}
