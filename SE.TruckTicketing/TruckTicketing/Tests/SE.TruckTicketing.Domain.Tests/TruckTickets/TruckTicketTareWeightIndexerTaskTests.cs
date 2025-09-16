using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.TruckTickets;

[TestClass]
public class TruckTicketTareWeightIndexerTaskTests
{
    [TestMethod]
    public async Task Task_ShouldNotRun_WhenTargetIsNull()
    {
        // arrange
        var scope = new DefaultScope();
        var context = new BusinessContext<TruckTicketEntity>(null);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_When_FacilityId_IsNotSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.FacilityId = default;
        entity.FacilityType = FacilityType.Lf;
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_When_TruckingCompanyName_IsNotSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.TruckingCompanyName = default;
        entity.FacilityType = FacilityType.Lf;
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_When_TruckNumber_IsNotSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.TruckNumber = default;
        entity.FacilityType = FacilityType.Lf;
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_When_LoadDate_IsNotSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.LoadDate = default;
        entity.FacilityType = FacilityType.Lf;
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_When_TrailerNumber_IsNotSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.TrailerNumber = default;
        entity.FacilityType = FacilityType.Lf;
        entity.LoadDate = new(DateTimeOffset.UtcNow.Date.AddHours(7));
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_TruckingCompanyNameIsSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.TruckingCompanyName = "TruckingCompanyName";
        entity.FacilityType = FacilityType.Lf;
        entity.LoadDate = new(DateTimeOffset.UtcNow.Date.AddHours(7));
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
    }

    [TestMethod]
    public async Task Task_ShouldRun_TruckNumberIsSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.TruckNumber = "12345";
        entity.FacilityType = FacilityType.Lf;
        entity.LoadDate = new(DateTimeOffset.UtcNow.Date.AddHours(7));
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
    }

    [TestMethod]
    public async Task Task_ShouldRun_FacilityIdIsSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.FacilityId = Guid.NewGuid();
        entity.FacilityType = FacilityType.Lf;
        entity.LoadDate = new(DateTimeOffset.UtcNow.Date.AddHours(7));
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenFacilityIdAndTruckingCompanyAndTruckNumberAreSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.FacilityId = Guid.NewGuid();
        entity.FacilityType = FacilityType.Lf;
        entity.TruckingCompanyName = "TruckingCompanyName";
        entity.TruckNumber = "12345";
        entity.LoadDate = DateTimeOffset.UtcNow;
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<TruckTicketTareWeightEntity>(index => IsMatch(entity, index)), It.IsAny<bool>()));
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenFacilityIdAndTruckingCompanyAndTruckNumberAreSet_LoadDateIsNotInRange()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.FacilityId = Guid.NewGuid();
        entity.FacilityType = FacilityType.Lf;
        entity.TruckingCompanyName = "TruckingCompanyName";
        entity.TruckNumber = "12345";
        entity.LoadDate = DateTimeOffset.UtcNow.AddDays(-5);
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    private bool IsMatch(TruckTicketEntity entity, TruckTicketTareWeightEntity index)
    {
        return entity.FacilityId == index.FacilityId &&
               entity.FacilityName == index.FacilityName &&
               entity.TruckingCompanyName == index.TruckingCompanyName &&
               entity.TruckNumber == index.TruckNumber &&
               entity.TrailerNumber == index.TrailerNumber &&
               entity.Id == index.TicketId &&
               entity.TruckingCompanyId == index.TruckingCompanyId &&
               entity.TareWeight == index.TareWeight;
    }

    public class DefaultScope : TestScope<TruckTicketTareWeightIndexerTask>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(IndexProviderMock.Object);
        }

        public Mock<IProvider<Guid, TruckTicketTareWeightEntity>> IndexProviderMock { get; } = new();
    }
}
