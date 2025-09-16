using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;
using SE.Shared.Domain.Entities.SourceLocationType;
using SE.Shared.Domain.Entities.SourceLocationType.Tasks;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.SourceLocation;

[TestClass]
public class SourceLocationTypeUniqueConstraintCheckerTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_OnlyWhenTargetNameDiffersFromOriginalNameAndCountryCodeIsNotNull()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        context.Original.Name = null;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.BeforeUpdate | OperationStage.BeforeInsert);
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_IfTargetNameIsTheSameAsOriginalName()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_IfCountryCodeIsUndefined()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        context.Target.CountryCode = CountryCode.Undefined;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_IfNameIsNull()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        context.Target.Name = null;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldSetResultToTrue_IfNameIsUnique()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        scope.SetupExistingSourceLocationTypes(Array.Empty<SourceLocationTypeEntity>());

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var isUnique = context.GetContextBagItemOrDefault(SourceLocationTypeUniqueConstraintCheckerTask.ResultKey, false);

        // assert
        result.Should().BeTrue();
        isUnique.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldSetResultToTrue_IfNameIsUniqueWithinCountry()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        context.Target.CountryCode = CountryCode.CA;
        var locationTypeWithSameNameDifferentCountry = context.Target.Clone();
        locationTypeWithSameNameDifferentCountry.CountryCode = CountryCode.US;

        scope.SetupExistingSourceLocationTypes(locationTypeWithSameNameDifferentCountry);

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var isUnique = context.GetContextBagItemOrDefault(SourceLocationTypeUniqueConstraintCheckerTask.ResultKey, false);

        // assert
        result.Should().BeTrue();
        isUnique.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldSetResultToFalse_IfNameIsNotUniqueWithinCountry()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        var locationTypeWithSameNameAndCountry = context.Target.Clone();
        locationTypeWithSameNameAndCountry.Id = Guid.NewGuid();

        scope.SetupExistingSourceLocationTypes(locationTypeWithSameNameAndCountry);

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var isUnique = context.GetContextBagItemOrDefault(SourceLocationTypeUniqueConstraintCheckerTask.ResultKey, true);

        // assert
        result.Should().BeTrue();
        isUnique.Should().BeFalse();
    }

    private class DefaultScope : TestScope<SourceLocationTypeUniqueConstraintCheckerTask>
    {
        public readonly Mock<IProvider<Guid, SourceLocationTypeEntity>> SourceLocationTypeProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(SourceLocationTypeProviderMock.Object);
        }

        public SourceLocationTypeEntity ValidSourceTypeEntity =>
            new()
            {
                Name = "UWI NTS",
                Category = SourceLocationTypeCategory.Well,
                CountryCode = CountryCode.CA,
                ShortFormCode = "WI",
                FormatMask = "###/@-###-@/###-@-##/##",
            };

        public void SetupExistingSourceLocationTypes(params SourceLocationTypeEntity[] entities)
        {
            SourceLocationTypeProviderMock.SetupEntities(entities);
        }

        public BusinessContext<SourceLocationTypeEntity> CreateValidSourceLocationTypeContext()
        {
            return new(ValidSourceTypeEntity, ValidSourceTypeEntity);
        }
    }
}
