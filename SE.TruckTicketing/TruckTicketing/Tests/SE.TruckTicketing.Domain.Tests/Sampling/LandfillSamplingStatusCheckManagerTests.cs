using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Product;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Sampling;
using SE.TruckTicketing.Domain.Entities.FacilityService;
using SE.TruckTicketing.Domain.Entities.Sampling;

using Trident.Contracts;
using Trident.Testing;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.Sampling;

[TestClass]
public class LandfillSamplingStatusCheckManagerTests
{
    [TestMethod]
    [TestCategory(TestCategory.Unit)]
    public async Task LandfillSamplingStatusCheckManager_CheckSamplingStatus_Should_Generate_StatusCheckDto_When_Sampling_Is_Over_Threshold()
    {
        // arrange
        var scope = new DefaultScope { StatusCheckRequestDto = { NetWeight = 100 } };

        // act
        var result = await scope.InstanceUnderTest.GetSamplingStatus(scope.StatusCheckRequestDto);

        // assert
        result.Action.Should().Be("Block");
    }

    [TestMethod]
    [TestCategory(TestCategory.Unit)]
    public async Task LandfillSamplingStatusCheckManager_CheckSamplingStatus_Should_Generate_StatusCheckDto_When_Sampling_Is_Over_WarningThreshold()
    {
        // arrange
        var scope = new DefaultScope { StatusCheckRequestDto = { NetWeight = 18 } };
        // act
        var result = await scope.InstanceUnderTest.GetSamplingStatus(scope.StatusCheckRequestDto);

        // assert
        result.Action.Should().Be("Warn");
    }

    [TestMethod]
    [TestCategory(TestCategory.Unit)]
    public async Task LandfillSamplingStatusCheckManager_CheckSamplingStatus_Should_Generate_StatusCheckDto_When_Sampling_Is_Under_WarningThreshold()
    {
        // arrange
        var scope = new DefaultScope { StatusCheckRequestDto = { NetWeight = 5 } };
        // act
        var result = await scope.InstanceUnderTest.GetSamplingStatus(scope.StatusCheckRequestDto);

        // assert
        result.Action.Should().Be("Allow");
    }

    private class DefaultScope : TestScope<LandfillSamplingStatusCheckManager>
    {
        public DefaultScope()
        {
            FacilityId = Guid.Empty;
            StatusCheckRequestDto = new()
            {
                FacilityId = Guid.NewGuid(),
            };

            SamplingEntities = new()
            {
                new()
                {
                    SamplingRuleType = SamplingRuleType.Weight,
                    Threshold = "20",
                    WarningThreshold = "16",
                    Value = "0",
                },
                new()
                {
                    SamplingRuleType = SamplingRuleType.Load,
                    Threshold = "10",
                    WarningThreshold = "8",
                    Value = "6",
                },
            };

            LandfillSamplingManagerMock.Setup(x => x.Get(It.IsAny<Expression<Func<LandfillSamplingEntity, bool>>>(),
                                                         null,
                                                         It.IsAny<List<string>>(),
                                                         false))
                                       .ReturnsAsync(SamplingEntities);

            FacilityServiceSubstanceManagerMock.Setup(x => x.GetById(It.IsAny<Guid>(), false)).ReturnsAsync(FacilityServiceSubstanceIndex);
            ProductManagerMock.Setup(x => x.GetById(It.IsAny<Guid>(), false)).ReturnsAsync(Product);

            InstanceUnderTest = new(LandfillSamplingManagerMock.Object,
                                    ProductManagerMock.Object,
                                    FacilityServiceSubstanceManagerMock.Object);
        }

        public Mock<IManager<Guid, LandfillSamplingEntity>> LandfillSamplingManagerMock { get; } = new();

        public Mock<IManager<Guid, ProductEntity>> ProductManagerMock { get; } = new();

        public Mock<IManager<Guid, FacilityServiceSubstanceIndexEntity>> FacilityServiceSubstanceManagerMock { get; } = new();

        public List<LandfillSamplingEntity> SamplingEntities { get; }

        public LandfillSamplingStatusCheckRequestDto StatusCheckRequestDto { get; }

        public Guid FacilityId { get; }

        public FacilityServiceSubstanceIndexEntity FacilityServiceSubstanceIndex { get; } = new();

        public ProductEntity Product { get; } = new();
    }
}
