using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Domain.Entities.FacilityService;
using SE.TruckTicketing.Domain.Entities.FacilityService.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.FacilityService;

[TestClass]
public class FacilityServiceDataLoaderTaskTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_Always()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateFacilityServiceBusinessContext();

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.BeforeUpdate | OperationStage.BeforeInsert);
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldReturnFalse_ForNullTarget()
    {
        // arrange
        var scope = new DefaultScope();
        var context = new BusinessContext<FacilityServiceEntity>(null);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldEnrichFacilityData()
    {
        // arrange
        var scope = new DefaultScope();
        scope.FacilityProviderMock.SetupEntities(new[] { scope.Facility });
        var context = scope.CreateFacilityServiceBusinessContext();

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var expectedServiceNumber = $"{scope.Facility.SiteId}-{scope.FacilityService.ServiceNumber}";

        // assert
        result.Should().BeTrue();
        context.Target.SiteId.Should().Be(scope.Facility.SiteId);
        context.Target.FacilityServiceNumber.Should().Be(expectedServiceNumber);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldEnrichUniqueFlag_ForUnique()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateFacilityServiceBusinessContext();

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        context.Target.IsUnique.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldEnrichUniqueFlag_ForDuplicate()
    {
        // arrange
        var scope = new DefaultScope();
        var duplicate = scope.FacilityService.Clone();
        duplicate.Id = Guid.NewGuid();
        scope.FacilityServiceProviderMock.SetupEntities(new[] { duplicate });
        var context = scope.CreateFacilityServiceBusinessContext();

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        context.Target.IsUnique.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldEnrichServiceTypeData()
    {
        // arrange
        var scope = new DefaultScope();
        scope.FacilityProviderMock.SetupEntities(new[] { scope.Facility });
        var context = scope.CreateFacilityServiceBusinessContext();

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var expectedServiceNumber = $"{scope.Facility.SiteId}-{scope.FacilityService.ServiceNumber}";

        // assert
        result.Should().BeTrue();
        context.Target.SiteId.Should().Be(scope.Facility.SiteId);
        context.Target.FacilityServiceNumber.Should().Be(expectedServiceNumber);
    }

    private class DefaultScope : TestScope<FacilityServiceDataLoaderTask>
    {
        public readonly FacilityEntity Facility = new()
        {
            Id = Guid.Parse("686c82ac-7e1e-46dc-956c-742bc8a248ce"),
            SiteId = "DCFST",
        };

        public readonly FacilityServiceEntity FacilityService =
            new()
            {
                Id = Guid.Parse("686c82ac-7e1e-46dc-956c-742bc8a248cd"),
                FacilityId = Guid.Parse("686c82ac-7e1e-46dc-956c-742bc8a248ce"),
                ServiceNumber = 1,
            };

        public DefaultScope()
        {
            InstanceUnderTest = new(FacilityProviderMock.Object,
                                    FacilityServiceProviderMock.Object,
                                    ServiceTypeProviderMock.Object);
        }

        public Mock<IProvider<Guid, FacilityEntity>> FacilityProviderMock { get; } = new();

        public Mock<IProvider<Guid, FacilityServiceEntity>> FacilityServiceProviderMock { get; } = new();

        public Mock<IProvider<Guid, ServiceTypeEntity>> ServiceTypeProviderMock { get; } = new();

        public BusinessContext<FacilityServiceEntity> CreateFacilityServiceBusinessContext()
        {
            return new(FacilityService);
        }
    }
}
