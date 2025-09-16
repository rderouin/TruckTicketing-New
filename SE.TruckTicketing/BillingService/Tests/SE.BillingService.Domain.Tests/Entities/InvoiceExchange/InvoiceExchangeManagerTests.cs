using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.Entities.InvoiceExchange;
using SE.Shared.Domain.BusinessStream;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.LegalEntity;

using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Testing.TestScopes;
using Trident.Validation;
using Trident.Workflow;

namespace SE.BillingService.Domain.Tests.Entities.InvoiceExchange;

[TestClass]
public class InvoiceExchangeManagerTests
{
    [TestMethod]
    public void InvoiceExchangeManager_ValidateOrder()
    {
        // arrange
        var orderedList = new[] { InvoiceExchangeType.Unknown, InvoiceExchangeType.Global, InvoiceExchangeType.BusinessStream, InvoiceExchangeType.LegalEntity, InvoiceExchangeType.Customer };

        // act
        var sorted = Enum.GetValues<InvoiceExchangeType>().OrderBy(v => v).ToList();

        // assert
        for (var i = 0; i < orderedList.Length; i++)
        {
            sorted[i].Should().Be(orderedList[i]);
        }
    }

    [TestMethod]
    public async Task InvoiceExchangeManager_GetFinalInvoiceExchangeConfig_Basic()
    {
        // arrange
        var scope = new DefaultScope();
        var existingI = Guid.NewGuid();
        var existingF = Guid.NewGuid();
        var newAccI = Guid.NewGuid();
        var newAccF = Guid.NewGuid();
        var newBuI = Guid.NewGuid();
        var newBuF = Guid.NewGuid();
        var newGlobalI = Guid.NewGuid();
        var newGlobalF = Guid.NewGuid();
        var bs = new BusinessStreamEntity();
        var le = new LegalEntityEntity { BusinessStreamId = bs.Id };
        var acc = new AccountEntity { LegalEntityId = le.Id };
        var accountConfig = new InvoiceExchangeEntity
        {
            PlatformCode = "OI",
            Type = InvoiceExchangeType.Customer,
            InvoiceDeliveryConfiguration = new()
            {
                MessageAdapterType = MessageAdapterType.Pidx,
                Mappings = new()
                {
                    new()
                    {
                        DestinationModelFieldId = existingI,
                    },
                    new()
                    {
                        DestinationModelFieldId = newAccI,
                    },
                },
            },
            FieldTicketsDeliveryConfiguration = new()
            {
                MessageAdapterType = MessageAdapterType.Pidx,
                Mappings = new()
                {
                    new()
                    {
                        DestinationModelFieldId = existingF,
                    },
                    new()
                    {
                        DestinationModelFieldId = newAccF,
                    },
                },
            },
        };

        var leConfig = new InvoiceExchangeEntity
        {
            PlatformCode = "OI",
            Type = InvoiceExchangeType.LegalEntity,
        };

        var buConfig = new InvoiceExchangeEntity
        {
            PlatformCode = "OI",
            Type = InvoiceExchangeType.BusinessStream,
            InvoiceDeliveryConfiguration = new()
            {
                MessageAdapterType = MessageAdapterType.Pidx,
                Mappings = new()
                {
                    new()
                    {
                        DestinationModelFieldId = existingI,
                    },
                    new()
                    {
                        DestinationModelFieldId = newBuI,
                    },
                },
            },
            FieldTicketsDeliveryConfiguration = new()
            {
                MessageAdapterType = MessageAdapterType.Pidx,
                Mappings = new()
                {
                    new()
                    {
                        DestinationModelFieldId = existingF,
                    },
                    new()
                    {
                        DestinationModelFieldId = newBuF,
                    },
                },
            },
        };

        var globalConfig = new InvoiceExchangeEntity
        {
            PlatformCode = "OI",
            Type = InvoiceExchangeType.Global,
            InvoiceDeliveryConfiguration = new()
            {
                MessageAdapterType = MessageAdapterType.Pidx,
                Mappings = new()
                {
                    new()
                    {
                        DestinationModelFieldId = existingI,
                    },
                    new()
                    {
                        DestinationModelFieldId = newGlobalI,
                    },
                },
            },
            FieldTicketsDeliveryConfiguration = new()
            {
                MessageAdapterType = MessageAdapterType.Pidx,
                Mappings = new()
                {
                    new()
                    {
                        DestinationModelFieldId = existingF,
                    },
                    new()
                    {
                        DestinationModelFieldId = newGlobalF,
                    },
                },
            },
        };

        var callNum = 0;
        scope.Provider.Setup(e => e.Get(It.IsAny<Expression<Func<InvoiceExchangeEntity, bool>>>(),
                                        It.IsAny<Func<IQueryable<InvoiceExchangeEntity>, IOrderedQueryable<InvoiceExchangeEntity>>>(),
                                        It.IsAny<IEnumerable<string>>(),
                                        It.IsAny<bool>(),
                                        It.IsAny<bool>(),
                                        It.IsAny<bool>()))
             .ReturnsAsync(() =>
                           {
                               callNum++;
                               if (callNum == 1)
                               {
                                   return new List<InvoiceExchangeEntity> { globalConfig };
                               }

                               if (callNum == 2)
                               {
                                   return new List<InvoiceExchangeEntity> { buConfig };
                               }

                               if (callNum == 3)
                               {
                                   return new List<InvoiceExchangeEntity> { leConfig };
                               }

                               if (callNum == 4)
                               {
                                   return new List<InvoiceExchangeEntity> { accountConfig };
                               }

                               return null;
                           });

        scope.AccountProvider.Setup(p => p.GetById(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(acc);
        scope.LegalEntityProvider.Setup(p => p.GetById(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(le);
        scope.BusinessStreamProvider.Setup(p => p.GetById(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(bs);

        // act
        var config = await scope.InstanceUnderTest.GetFinalInvoiceExchangeConfig("OI", acc.Id);

        // assert
        config.InvoiceDeliveryConfiguration.Mappings.Count.Should().Be(4);
        config.FieldTicketsDeliveryConfiguration.Mappings.Count.Should().Be(4);
        config.InvoiceDeliveryConfiguration.Mappings.Select(m => m.DestinationModelFieldId.GetValueOrDefault()).ToList().Should().BeEquivalentTo(new List<Guid>
        {
            existingI,
            newAccI,
            newBuI,
            newGlobalI,
        });

        config.FieldTicketsDeliveryConfiguration.Mappings.Select(m => m.DestinationModelFieldId.GetValueOrDefault()).ToList().Should().BeEquivalentTo(new List<Guid>
        {
            existingF,
            newAccF,
            newBuF,
            newGlobalF,
        });
    }

    private static bool Add(List<string> s, string ss)
    {
        s.Add(ss);
        return true;
    }

    private class DefaultScope : TestScope<InvoiceExchangeManager>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(Logger.Object,
                                    Provider.Object,
                                    AccountProvider.Object,
                                    LegalEntityProvider.Object,
                                    BusinessStreamProvider.Object,
                                    ValidationManager.Object,
                                    WorkflowManager.Object);
        }

        public Mock<ILog> Logger { get; } = new();

        public Mock<IProvider<Guid, InvoiceExchangeEntity>> Provider { get; } = new();

        public Mock<IProvider<Guid, BusinessStreamEntity>> BusinessStreamProvider { get; } = new();

        public Mock<IProvider<Guid, LegalEntityEntity>> LegalEntityProvider { get; } = new();

        public Mock<IProvider<Guid, AccountEntity>> AccountProvider { get; } = new();

        public Mock<IValidationManager<InvoiceExchangeEntity>> ValidationManager { get; } = new();

        public Mock<IWorkflowManager<InvoiceExchangeEntity>> WorkflowManager { get; } = new();
    }
}
