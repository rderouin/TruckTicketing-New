using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.LegalEntity;
using SE.TruckTicketing.Api.Functions;
using SE.TruckTicketing.Domain.Configuration;

using Trident.Contracts;
using Trident.Logging;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Api.Tests.Functions;

[TestClass]
public class AccountCustomerNoActvityProcessorTests
{
    [TestMethod]
    public async Task AccountCustomerNoActvityProcessor_RegenerateInvitationLinks_DelegatesToGenerateInvitation()
    {
        // arrange
        var scope = new DefaultScope();
        var LegalEntityId = Guid.NewGuid();
        var legalEntity = new List<LegalEntityEntity>
        {
            new()
            {
                Name = "Legal Entity",
                Id = LegalEntityId,
            },
        };

        var entityList = new List<AccountEntity>();
        for (var i = 0; i < 200; i++)
        {
            var entity = new AccountEntity
            {
                Name = "Mickey",
                NickName = "Mouse",
                AccountTypes = new()
                {
                    List = new() { "Customer" },
                },
                LastTransactionDate = DateTimeOffset.Now.AddDays(-400),
                LegalEntityId = LegalEntityId,
                CreditStatus = CreditStatus.Approved,
            };

            entityList.Add(entity);
        }

        scope.MockAccountSettingsConfiguration.SetupGet(x => x.RunAccountCustomerNoActivityProcessor).Returns(true);
        scope.MockLegalEntityManager.Setup(x => x.Search(It.IsAny<SearchCriteria<LegalEntityEntity>>(), false))
             .ReturnsAsync((SearchCriteria<LegalEntityEntity> searchCriteria, bool loadChildren) =>
                           {
                               var morePages = searchCriteria.PageSize.GetValueOrDefault() * searchCriteria.CurrentPage.GetValueOrDefault() < 1;
                               var results = new SearchResults<LegalEntityEntity, SearchCriteria>
                               {
                                   Results = legalEntity
                                            .Skip(searchCriteria.PageSize.GetValueOrDefault() * (searchCriteria.CurrentPage.GetValueOrDefault() - 1))
                                            .Take(searchCriteria.PageSize.GetValueOrDefault()),
                                   Info = new()
                                   {
                                       TotalRecords = 1,
                                       NextPageCriteria = morePages ? new SearchCriteria(searchCriteria) { CurrentPage = searchCriteria.CurrentPage + 1 } : null,
                                   },
                               };

                               return results;
                           });

        scope.MockAccountEntityManager.Setup(x => x.Search(It.IsAny<SearchCriteria<AccountEntity>>(), false))
             .ReturnsAsync((SearchCriteria<AccountEntity> searchCriteria, bool loadChildren) =>
                           {
                               var morePages = searchCriteria.PageSize.GetValueOrDefault() * searchCriteria.CurrentPage.GetValueOrDefault() < entityList.Count;
                               var results = new SearchResults<AccountEntity, SearchCriteria>
                               {
                                   Results = entityList
                                            .Skip(searchCriteria.PageSize.GetValueOrDefault() * (searchCriteria.CurrentPage.GetValueOrDefault() - 1))
                                            .Take(searchCriteria.PageSize.GetValueOrDefault()),
                                   Info = new()
                                   {
                                       TotalRecords = entityList.Count,
                                       NextPageCriteria = morePages ? new SearchCriteria(searchCriteria) { CurrentPage = searchCriteria.CurrentPage + 1 } : null,
                                   },
                               };

                               return results;
                           });

        entityList.ForEach(entity => entity.CreditStatus = CreditStatus.RequiresRenewal);
        scope.MockAccountEntityManager.Setup(x => x.BulkSave(It.IsAny<List<AccountEntity>>()))
             .ReturnsAsync(entityList);

        // act
        await scope.InstanceUnderTest.Run(scope.MyTimerMock, scope.MyFunctionContext);

        // assert
        Assert.IsTrue(entityList.FirstOrDefault().CreditStatus == CreditStatus.RequiresRenewal);
    }

    [TestMethod]
    public async Task AccountCustomerNoActvityProcessor_RegenerateInvitationLinks_ConfigurationDisablesRun()
    {
        // arrange
        var scope = new DefaultScope();
        var LegalEntityId = Guid.NewGuid();
        var legalEntity = new List<LegalEntityEntity>
        {
            new()
            {
                Name = "Legal Entity",
                Id = LegalEntityId,
            },
        };

        var entityList = new List<AccountEntity>();
        for (var i = 0; i < 200; i++)
        {
            var entity = new AccountEntity
            {
                Name = "Mickey",
                NickName = "Mouse",
                AccountTypes = new()
                {
                    List = new() { "Customer" },
                },
                LastTransactionDate = DateTimeOffset.Now.AddDays(-400),
                LegalEntityId = LegalEntityId,
                CreditStatus = CreditStatus.Approved,
            };

            entityList.Add(entity);
        }

        scope.MockAccountSettingsConfiguration.SetupGet(x => x.RunAccountCustomerNoActivityProcessor).Returns(false);
        scope.MockLegalEntityManager.Setup(x => x.Search(It.IsAny<SearchCriteria<LegalEntityEntity>>(), false))
             .ReturnsAsync((SearchCriteria<LegalEntityEntity> searchCriteria, bool loadChildren) =>
                           {
                               var morePages = searchCriteria.PageSize.GetValueOrDefault() * searchCriteria.CurrentPage.GetValueOrDefault() < 1;
                               var results = new SearchResults<LegalEntityEntity, SearchCriteria>
                               {
                                   Results = legalEntity
                                            .Skip(searchCriteria.PageSize.GetValueOrDefault() * (searchCriteria.CurrentPage.GetValueOrDefault() - 1))
                                            .Take(searchCriteria.PageSize.GetValueOrDefault()),
                                   Info = new()
                                   {
                                       TotalRecords = 1,
                                       NextPageCriteria = morePages ? new SearchCriteria(searchCriteria) { CurrentPage = searchCriteria.CurrentPage + 1 } : null,
                                   },
                               };

                               return results;
                           });

        scope.MockAccountEntityManager.Setup(x => x.Search(It.IsAny<SearchCriteria<AccountEntity>>(), false))
             .ReturnsAsync((SearchCriteria<AccountEntity> searchCriteria, bool loadChildren) =>
                           {
                               var morePages = searchCriteria.PageSize.GetValueOrDefault() * searchCriteria.CurrentPage.GetValueOrDefault() < entityList.Count;
                               var results = new SearchResults<AccountEntity, SearchCriteria>
                               {
                                   Results = entityList
                                            .Skip(searchCriteria.PageSize.GetValueOrDefault() * (searchCriteria.CurrentPage.GetValueOrDefault() - 1))
                                            .Take(searchCriteria.PageSize.GetValueOrDefault()),
                                   Info = new()
                                   {
                                       TotalRecords = entityList.Count,
                                       NextPageCriteria = morePages ? new SearchCriteria(searchCriteria) { CurrentPage = searchCriteria.CurrentPage + 1 } : null,
                                   },
                               };

                               return results;
                           });

        scope.MockAccountEntityManager.Setup(x => x.BulkSave(It.IsAny<List<AccountEntity>>()))
             .ReturnsAsync(entityList);

        // act
        await scope.InstanceUnderTest.Run(scope.MyTimerMock, scope.MyFunctionContext);

        // assert
        Assert.IsTrue(entityList.All(x => x.CreditStatus == CreditStatus.Approved));
    }

    private class DefaultScope : TestScope<AccountCustomerNoActivityProcessor>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(AppLoggerMock.Object, MockAccountEntityManager.Object, MockAccountSettingsConfiguration.Object
                                  , MockLegalEntityManager.Object);
        }

        public TimerInfo MyTimerMock { get; }

        public FunctionContext MyFunctionContext { get; }

        public Mock<ILog> AppLoggerMock { get; } = new();

        public Mock<IAccountSettingsConfiguration> MockAccountSettingsConfiguration { get; } = new();

        public Mock<IManager<Guid, LegalEntityEntity>> MockLegalEntityManager { get; } = new();

        public Mock<IManager<Guid, AccountEntity>> MockAccountEntityManager { get; } = new();
    }
}
