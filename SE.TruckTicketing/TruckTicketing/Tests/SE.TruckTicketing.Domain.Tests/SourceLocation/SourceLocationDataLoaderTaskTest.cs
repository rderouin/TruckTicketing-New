using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Entities.SourceLocation.Tasks;
using SE.Shared.Domain.Entities.SourceLocationType;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.SourceLocation;

[TestClass]
public class SourceLocationDataLoaderTaskTest : TestScope<SourceLocationLookupDataLoaderTask>
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_Always()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.BeforeUpdate | OperationStage.BeforeInsert);
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldEnrichGeneratorInfo()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();

        scope.SetupExistingSourceLocationTypes(scope.SourceLocationType);
        scope.SetupExistingGenerators(scope.Generator);

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var sourceLocation = context.Target;

        // assert
        result.Should().BeTrue();
        sourceLocation.GeneratorName.Should().Be(scope.Generator.Name);
        sourceLocation.GeneratorAccountNumber.Should().Be(scope.Generator.AccountNumber);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldEnrichSourceLocationTypeInfo()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();

        scope.SetupExistingSourceLocationTypes(scope.SourceLocationType);
        scope.SetupExistingGenerators(scope.Generator);

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var sourceLocation = context.Target;

        // assert
        result.Should().BeTrue();
        sourceLocation.SourceLocationTypeCategory.Should().Be(scope.SourceLocationType.Category);
        sourceLocation.SourceLocationTypeName.Should().Be(scope.SourceLocationType.Name);
        sourceLocation.SourceLocationType.Should().Be(scope.SourceLocationType);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldEnrichAssociatedSourceLocationInfo()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();

        scope.SetupExistingSourceLocationTypes(scope.SourceLocationType);
        scope.SetupExistingGenerators(scope.Generator);
        scope.SetupExistingSourceLocations(scope.AssociatedSourceLocation);

        context.Target.AssociatedSourceLocationId = scope.AssociatedSourceLocation.Id;

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var sourceLocation = context.Target;

        // assert
        result.Should().BeTrue();
        sourceLocation.AssociatedSourceLocationIdentifier.Should().Be(scope.AssociatedSourceLocation.Identifier);
        sourceLocation.AssociatedSourceLocationFormattedIdentifier.Should().Be(scope.AssociatedSourceLocation.FormattedIdentifier);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("00000000-0000-0000-0000-000000000000")]
    [DataRow("fe7170cf-e475-4304-9fac-f7d30c418dbe")]
    public async Task Task_ShouldClearAssociationsIfSourceLocationDoesNotExist(string id)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();

        scope.SetupExistingSourceLocationTypes(scope.SourceLocationType);
        scope.SetupExistingGenerators(scope.Generator);
        scope.SetupExistingSourceLocations(scope.SourceLocation);

        context.Target.AssociatedSourceLocationId = Guid.Parse(id);

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var sourceLocation = context.Target;

        // assert
        result.Should().BeTrue();
        sourceLocation.AssociatedSourceLocationIdentifier.Should().BeNull();
        sourceLocation.AssociatedSourceLocationFormattedIdentifier.Should().BeNull();
    }

    private class DefaultScope : TestScope<SourceLocationLookupDataLoaderTask>
    {
        public readonly Mock<IProvider<Guid, AccountEntity>> AccountProviderMock = new();

        public readonly SourceLocationEntity AssociatedSourceLocation =
            new()
            {
                Id = Guid.NewGuid(),
                Identifier = "990/99-90-999-09W9/99",
                FormattedIdentifier = "990999099909W999",
                GeneratorId = Guid.NewGuid(),
                GeneratorStartDate = DateTimeOffset.Now,
                ContractOperatorId = Guid.NewGuid(),
            };

        public readonly AccountEntity Generator =
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Jack Johnson",
            };

        public readonly SourceLocationEntity SourceLocation =
            new()
            {
                Id = Guid.NewGuid(),
                Identifier = "990/99-90-999-09W9/99",
                FormattedIdentifier = "990999099909W999",
                GeneratorId = Guid.NewGuid(),
                GeneratorStartDate = DateTimeOffset.Now,
                ContractOperatorId = Guid.NewGuid(),
            };

        public readonly Mock<IProvider<Guid, SourceLocationEntity>> SourceLocationProviderMock = new();

        public readonly SourceLocationTypeEntity SourceLocationType =
            new()
            {
                Id = Guid.NewGuid(),
                Name = "UWI NTS",
                Category = SourceLocationTypeCategory.Well,
                CountryCode = CountryCode.CA,
                ShortFormCode = "WI",
                FormatMask = "###/@-###-@/###-@-##/##",
            };

        public readonly Mock<IProvider<Guid, SourceLocationTypeEntity>> SourceLocationTypeProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(SourceLocationTypeProviderMock.Object,
                                    AccountProviderMock.Object,
                                    SourceLocationProviderMock.Object);
        }

        public void SetupExistingSourceLocationTypes(params SourceLocationTypeEntity[] entities)
        {
            SourceLocationTypeProviderMock.SetupEntities(entities);
        }

        public void SetupExistingGenerators(params AccountEntity[] entities)
        {
            AccountProviderMock.SetupEntities(entities);
        }

        public void SetupExistingSourceLocations(params SourceLocationEntity[] entities)
        {
            SourceLocationProviderMock.SetupEntities(entities);
        }

        public BusinessContext<SourceLocationEntity> CreateValidSourceLocationTypeContext()
        {
            SourceLocation.GeneratorId = Generator.Id;
            SourceLocation.ContractOperatorId = Generator.Id;
            SourceLocation.SourceLocationTypeId = SourceLocationType.Id;
            SourceLocation.SourceLocationType = SourceLocationType;
            return new(SourceLocation, SourceLocation);
        }
    }
}
