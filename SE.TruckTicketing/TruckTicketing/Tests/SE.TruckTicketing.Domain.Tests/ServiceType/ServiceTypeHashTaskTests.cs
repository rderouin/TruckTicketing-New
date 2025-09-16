using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.ServiceType;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.ServiceType;
using SE.TruckTicketing.Domain.Entities.ServiceType.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Domain;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.ServiceType;

[TestClass]
public class ServiceTypeHashTaskTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task ServiceTypeHashTask_CanBeInstantiatedAsync()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var runOrder = scope.InstanceUnderTest.RunOrder;
        var mustAlwaysRun = await scope.InstanceUnderTest.ShouldRun(scope.CreateContextWithValidServiceTypeEntity());
        var operationStage = scope.InstanceUnderTest.Stage;
        // assert
        runOrder.Should().BePositive();
        mustAlwaysRun.Should().BeTrue();
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task WorkflowTask_ShouldSetHashUniqueFlagToTrue_WhenServiceTypeIsUnique()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidServiceTypeEntity();
        var existing = new ServiceTypeEntity
        {
            Id = Guid.NewGuid(),
            Hash = context.Target.Hash,
        };

        scope.SetExistingServiceTypes(existing);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        context.GetContextBagItemOrDefault(ServiceTypeWorkflowContextBagKeys.ServiceTypeHashIsUnique, false)
               .Should().BeTrue();
    }

    private class DefaultScope : TestScope<ServiceTypeHashTask>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(ServiceTypeProviderMock.Object);
        }

        public Mock<IProvider<Guid, ServiceTypeEntity>> ServiceTypeProviderMock { get; } = new();

        public void SetExistingServiceTypes(params ServiceTypeEntity[] serviceTypes)
        {
            ConfigureProviderMockGetValues(ServiceTypeProviderMock, serviceTypes);
        }

        public BusinessContext<ServiceTypeEntity> CreateContextWithValidServiceTypeEntity()
        {
            return new(new()
            {
                Id = Guid.NewGuid(),
                CountryCode = CountryCode.CA,
                ServiceTypeId = "Id",
                Class = Class.Class2,
                Name = "test1",
                TotalItemName = "Total",
                ReportAsCutType = ReportAsCutTypes.Oil,
                Stream = Stream.Landfill,
                LegalEntityCode = "US",
                LegalEntityId = Guid.Parse("8ef79228-921a-4e47-822e-7999abe71d52"),
            });
        }

        private void ConfigureProviderMockGetValues<TId, TEntity>(Mock<IProvider<TId, TEntity>> mock, TEntity[] entities) where TEntity : EntityBase<TId>
        {
            mock.Setup(x => x.Get(It.IsAny<Expression<Func<TEntity, bool>>>(),
                                  It.IsAny<Func<IQueryable<TEntity>,
                                      IOrderedQueryable<TEntity>>>(),
                                  It.IsAny<IEnumerable<string>>(),
                                  It.IsAny<bool>(),
                                  It.IsAny<bool>(),
                                  It.IsAny<bool>()))
                .ReturnsAsync((Expression<Func<TEntity, bool>> filter,
                               Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> _,
                               List<string> __,
                               bool ___,
                               bool ____,
                               bool _____) => entities.Where(filter.Compile()));

            mock.Setup(x => x.GetByIds(It.IsAny<IEnumerable<TId>>(),
                                       It.IsAny<bool>(),
                                       It.IsAny<bool>(),
                                       It.IsAny<bool>()))
                .ReturnsAsync((IEnumerable<TId> ids, bool _, bool __, bool ___) => entities.Where(entity => ids.Contains(entity.Id)));
        }
    }
}
