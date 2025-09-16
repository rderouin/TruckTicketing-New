using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.MaterialApproval.Tasks;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.ManualTruckTicket.Tasks;

[TestClass]
public class MaterialApprovalContactIndexerTest
{
    [TestMethod]
    public async Task Task_ShouldNotRun_WhenTargetIsNull()
    {
        // arrange
        var scope = new DefaultScope();
        var context = new BusinessContext<MaterialApprovalEntity>(null);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenInsert()
    {
        // arrange
        var scope = new DefaultScope();
        var context = new BusinessContext<MaterialApprovalEntity>(scope.ValidMaterialApprovalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenBillingCustomerContactUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidMaterialApprovalEntity;
        var targetEntity = scope.ValidMaterialApprovalEntity.Clone();
        targetEntity.BillingCustomerId = Guid.NewGuid();
        targetEntity.BillingCustomerContactId = Guid.NewGuid();
        var context = new BusinessContext<MaterialApprovalEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenThirdPartyAnalyticalContactUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidMaterialApprovalEntity;
        var targetEntity = scope.ValidMaterialApprovalEntity.Clone();
        targetEntity.ThirdPartyAnalyticalCompanyId = Guid.NewGuid();
        targetEntity.ThirdPartyAnalyticalCompanyContactId = Guid.NewGuid();
        var context = new BusinessContext<MaterialApprovalEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenTruckingCompanyContactUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidMaterialApprovalEntity;
        var targetEntity = scope.ValidMaterialApprovalEntity.Clone();
        targetEntity.TruckingCompanyId = Guid.NewGuid();
        targetEntity.TruckingCompanyContactId = Guid.NewGuid();
        var context = new BusinessContext<MaterialApprovalEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_WhenNoContactUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidMaterialApprovalEntity;
        var targetEntity = originalEntity.Clone();
        var context = new BusinessContext<MaterialApprovalEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_WhenNoContactSelected()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidMaterialApprovalEntity;
        originalEntity.ThirdPartyAnalyticalCompanyId = Guid.Empty;
        originalEntity.GeneratorId = Guid.Empty;
        originalEntity.TruckingCompanyId = Guid.Empty;

        originalEntity.ThirdPartyAnalyticalCompanyContactId = Guid.Empty;
        originalEntity.TruckingCompanyContactId = Guid.Empty;
        originalEntity.GeneratorRepresenativeId = Guid.Empty;

        var targetEntity = originalEntity.Clone();
        var context = new BusinessContext<MaterialApprovalEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenBillingCustomerContactIsSet()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidMaterialApprovalEntity;
        var targetEntity = originalEntity.Clone();
        targetEntity.BillingCustomerId = Guid.NewGuid();
        targetEntity.BillingCustomerContactId = Guid.NewGuid();
        var context = new BusinessContext<MaterialApprovalEntity>(targetEntity, scope.ValidMaterialApprovalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<AccountContactReferenceIndexEntity>(index => IsBillingCustomerContactMatch(targetEntity, index)), It.IsAny<bool>()));
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenThirdPartyAnalyticalContactIsSet()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidMaterialApprovalEntity;
        var targetEntity = originalEntity.Clone();
        targetEntity.ThirdPartyAnalyticalCompanyId = Guid.NewGuid();
        targetEntity.ThirdPartyAnalyticalCompanyContactId = Guid.NewGuid();
        var context = new BusinessContext<MaterialApprovalEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<AccountContactReferenceIndexEntity>(index => IsThirdPartyAnalyticalContactMatch(targetEntity, index)), It.IsAny<bool>()));
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenTruckingCompanyContactIsSet()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidMaterialApprovalEntity;
        var targetEntity = originalEntity.Clone();
        targetEntity.TruckingCompanyId = Guid.NewGuid();
        targetEntity.TruckingCompanyContactId = Guid.NewGuid();
        var context = new BusinessContext<MaterialApprovalEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<AccountContactReferenceIndexEntity>(index => IsTruckingCompanyContactMatch(targetEntity, index)), It.IsAny<bool>()));
    }

    private bool IsBillingCustomerContactMatch(MaterialApprovalEntity entity, AccountContactReferenceIndexEntity index)
    {
        return entity.Id == index.ReferenceEntityId &&
               entity.BillingCustomerId == index.AccountId &&
               entity.BillingCustomerContactId == index.AccountContactId;
    }

    private bool IsThirdPartyAnalyticalContactMatch(MaterialApprovalEntity entity, AccountContactReferenceIndexEntity index)
    {
        return entity.Id == index.ReferenceEntityId &&
               entity.ThirdPartyAnalyticalCompanyId == index.AccountId &&
               entity.ThirdPartyAnalyticalCompanyContactId == index.AccountContactId;
    }

    private bool IsTruckingCompanyContactMatch(MaterialApprovalEntity entity, AccountContactReferenceIndexEntity index)
    {
        return entity.Id == index.ReferenceEntityId &&
               entity.TruckingCompanyId == index.AccountId &&
               entity.TruckingCompanyContactId == index.AccountContactId;
    }

    public class DefaultScope : TestScope<MaterialApprovalContactIndexer>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(IndexProviderMock.Object);
        }

        public Mock<IProvider<Guid, AccountContactReferenceIndexEntity>> IndexProviderMock { get; } = new();

        public MaterialApprovalEntity ValidMaterialApprovalEntity =>
            new()
            {
                Facility = "Facility1",
                FacilityId = Guid.NewGuid(),
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
    }
}
