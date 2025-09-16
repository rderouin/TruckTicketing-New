using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.MaterialApproval.Tasks;
using SE.Shared.Domain.Entities.Sequences;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.MaterialApproval;

[TestClass]
public class MaterialApprovalNumberGeneratorTaskTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_OnlyWhenMaterialApprovalBusinessContext_Insert()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidMaterialApprovalContext(scope.ValidMaterialApproval, scope.ValidMaterialApproval);
        context.Operation = Operation.Insert;
        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_IfMaterialApprovalBusinessContext_NotInsert()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidMaterialApprovalContext(new(), scope.ValidMaterialApproval);
        context.Operation = Operation.Update;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_MaterialApproval_FacilityIdDefault()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidMaterialApprovalContext(new(), scope.ValidMaterialApproval);
        context.Operation = Operation.Insert;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_MaterialApproval_ValidFacilityId()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidMaterialApprovalContext(scope.ValidMaterialApproval, scope.ValidMaterialApproval);
        context.Operation = Operation.Insert;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_ValidLegalEntity_MaterialApprovalNumberGenerated()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetSequenceGeneratorManager();
        var context = scope.CreateValidMaterialApprovalContext(scope.ValidMaterialApproval, scope.ValidMaterialApproval);
        scope.SetupExistingFacilities(scope.GenerateFacilityEntity().ToArray());

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        // assert
        result.Should().BeTrue();
        context.Target.MaterialApprovalNumber.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_FacilityLegalEntity_NoRecord_MaterialApprovalNotGenerated()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetSequenceGeneratorManager();
        var cloneEntity = scope.ValidMaterialApproval.Clone();
        cloneEntity.FacilityId = Guid.NewGuid();
        var context = scope.CreateValidMaterialApprovalContext(cloneEntity, scope.ValidMaterialApproval);

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        // assert
        result.Should().BeFalse();
        context.Target.MaterialApprovalNumber.Should().BeNullOrEmpty();
    }

    private class DefaultScope : TestScope<MaterialApprovalNumberGeneratorTask>
    {
        public readonly Mock<IProvider<Guid, FacilityEntity>> FacilityProviderMock = new();

        public readonly MaterialApprovalEntity ValidMaterialApproval =
            new()
            {
                Facility = "Facility1",
                FacilityId = Guid.Parse("ae62f28e-fd0b-4594-b235-0e254bc4771a"),
                FacilityServiceId = Guid.NewGuid(),
                FacilityServiceNumber = "number",
                AccumulatedTonnage = 13,
                FacilityServiceName = "Facility Service 1",
                SubstanceId = Guid.NewGuid(),
                GeneratorId = Guid.NewGuid(),
                GeneratorName = "Generator",
                BillingCustomerId = Guid.NewGuid(),
                BillingCustomerName = "Billing Customer",
                BillingCustomerContactId = Guid.NewGuid(),
                BillingCustomerContact = "Billing Contact",
                BillingCustomerContactAddress = "Billing Address",
                BillingCustomerContactReceiveLoadSummary = true,
                ThirdPartyAnalyticalCompanyId = Guid.NewGuid(),
                ThirdPartyAnalyticalCompanyName = "Third party company",
                ThirdPartyAnalyticalCompanyContactId = Guid.NewGuid(),
                ThirdPartyAnalyticalCompanyContact = " Third Party Contact",
                ThirdPartyAnalyticalContactReceiveLoadSummary = false,
                TruckingCompanyId = Guid.NewGuid(),
                TruckingCompanyName = "Trucking Company",
                TruckingCompanyContactId = Guid.NewGuid(),
                TruckingCompanyContact = "Trucking Contact",
                TruckingCompanyContactReceiveLoadSummary = true,
                HazardousNonhazardous = HazardousClassification.Nonhazardous,
                LoadSummaryReport = true,
                AnalyticalExpiryAlertActive = false,
                AnalyticalExpiryDate = DateTimeOffset.Now,
                SecureRepresentative = "CreatedBy",
                SourceRegion = SourceRegionEnum.InRegion,
                LegalEntity = "Christiansen - Rowe",
                LegalEntityId = Guid.NewGuid(),
            };

        public DefaultScope()
        {
            InstanceUnderTest = new(SequenceNumberGeneratorMock.Object, FacilityProviderMock.Object, UserContextAccessorMock.Object);
        }

        public Mock<ISequenceNumberGenerator> SequenceNumberGeneratorMock { get; } = new();
        public Mock<IUserContextAccessor> UserContextAccessorMock { get; } = new();

        public List<FacilityEntity> GenerateFacilityEntity()
        {
            return new()
            {
                new()
                {
                    Id = Guid.Parse("ae62f28e-fd0b-4594-b235-0e254bc4771a"),
                    SiteId = "TAFA",
                    Name = "Facility1",
                    Type = FacilityType.Lf,
                    LegalEntity = "Canada",
                },
                new()
                {
                    Id = Guid.Parse("42b89075-213e-48ab-9a21-b33e9bfc741d"),
                    SiteId = "DVFST",
                    Name = "Facility2",
                    Type = FacilityType.Fst,
                    LegalEntity = "Canada",
                },
            };
        }

        public void SetupExistingFacilities(params FacilityEntity[] entities)
        {
            FacilityProviderMock.SetupEntities(entities);
        }

        public BusinessContext<MaterialApprovalEntity> CreateValidMaterialApprovalContext(MaterialApprovalEntity target, MaterialApprovalEntity original)
        {
            return new(target, original);
        }

        public void SetSequenceGeneratorManager()
        {
            ConfigureSequenceGeneratorManagerMock(SequenceNumberGeneratorMock);
        }

        private void ConfigureSequenceGeneratorManagerMock(Mock<ISequenceNumberGenerator> mock)
        {
            mock.Setup(sequence => sequence.GenerateSequenceNumbers(It.IsAny<string>(),
                                                                    It.IsAny<string>(),
                                                                    It.IsAny<int>(),
                                                                    It.IsAny<string>(),
                                                                    It.IsAny<string>())).Returns(GenerateSequenceNumber());
        }

        private IAsyncEnumerable<string> GenerateSequenceNumber()
        {
            return new List<string>
            {
                "TAFA-10001-LF",
            }.ToAsyncEnumerable();
        }
    }
}
