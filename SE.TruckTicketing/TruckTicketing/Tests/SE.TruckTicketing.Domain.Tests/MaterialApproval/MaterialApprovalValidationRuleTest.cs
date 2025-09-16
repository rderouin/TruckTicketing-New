using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.MaterialApproval.Rules;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Tests.MaterialApproval;

[TestClass]
public class MaterialApprovalValidationRuleTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public void MaterialApprovalValidationRule_CanBeInstantiated()
    {
        //arrange
        var scope = new DefaultScope();

        //act
        var runOrder = scope.InstanceUnderTest.RunOrder;

        //assert
        runOrder.Should().BePositive();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MaterialApprovalValidationRule_ShouldPass_ValidData()
    {
        //arrange
        var scope = new DefaultScope();
        var context = DefaultScope.CreateNonHazardousMaterialApprovalContext();
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MaterialApprovalValidationRule_ShouldFail_WhenFacilityIdIsNullOrEmpty()
    {
        //arrange
        var scope = new DefaultScope();
        var context = DefaultScope.CreateNonHazardousMaterialApprovalContext();
        context.Target.FacilityId = default;
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.MaterialApproval_Facility));
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task MaterialApprovalValidationRule_ShouldFail_WhenDescriptionIsLong(string description)
    {
        description = "JFXv1p7u9CD1Dr4MHSr05OQpTMr1dkmGMFt3BFTwSnhc7m0ku95du4O1Aord";
        var scope = new DefaultScope();
        var context = DefaultScope.CreateNonHazardousMaterialApprovalContext();
        context.Target.Description = description;
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert 
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.MaterialApproval_Description));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MaterialApprovalValidationRule_ShouldFail_WhenGeneratorIdIsNullOrEmpty()
    {
        //arrange
        var scope = new DefaultScope();
        var context = DefaultScope.CreateNonHazardousMaterialApprovalContext();
        context.Target.GeneratorId = default;
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.MaterialApproval_Generator));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MaterialApprovalValidationRule_ShouldFail_WhenBillingCustomerIdIsNullOrEmpty()
    {
        //arrange
        var scope = new DefaultScope();
        var context = DefaultScope.CreateNonHazardousMaterialApprovalContext();
        context.Target.BillingCustomerId = default;
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.MaterialApproval_BillingCustomer));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MaterialApprovalValidationRule_ShouldFail_WhenBillingContactIdIsNullOrEmpty()
    {
        //arrange
        var scope = new DefaultScope();
        var context = DefaultScope.CreateNonHazardousMaterialApprovalContext();
        context.Target.BillingCustomerContactId = null;
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.MaterialApproval_BillingCustomerContact));
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task MaterialApprovalValidationRule_ShouldFail_WhenBillingContactAddIsNullOrEmpty(string address)
    {
        //arrange
        address = string.Empty;
        var scope = new DefaultScope();
        var context = DefaultScope.CreateNonHazardousMaterialApprovalContext();
        context.Target.BillingCustomerContactAddress = address;
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.MaterialApproval_BillingCustomerContactAddress));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MaterialApprovalValidationRule_ShouldFail_WhenFacilityNumberIsNullOrEmpty()
    {
        //arrange
        var scope = new DefaultScope();
        var context = DefaultScope.CreateNonHazardousMaterialApprovalContext();
        context.Target.FacilityServiceNumber = default;
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.MaterialApproval_FacilityServiceNumber));
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task MaterialApprovalValidationRule_ShouldFail_WhenFacilityServiceNameIsNullOrEmpty(string facilityServiceName)
    {
        //arrange
        facilityServiceName = string.Empty;
        var scope = new DefaultScope();
        var context = DefaultScope.CreateNonHazardousMaterialApprovalContext();
        context.Target.FacilityServiceName = facilityServiceName;
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.MaterialApproval_FacilityServiceName));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MaterialApprovalValidationRule_ShouldFail_WhenAnalyticalExpiryDateIsNullOrEmpty()
    {
        //arrange
        var scope = new DefaultScope();
        var context = DefaultScope.CreateNonHazardousMaterialApprovalContext();
        scope.SetupExistingFacilities(DefaultScope.GenerateOneLandfillFacility().ToArray());

        context.Target.AnalyticalExpiryDate = default;
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.MaterialApproval_AnalyticalExpiryDate));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MaterialApprovalValidationRule_ShouldFail_WhenFacilityIsLandfillAndHazNonHazIsUndefined_ShowHazFalse()
    {
        //setup
        var scope = new DefaultScope();
        var landfillFacility = DefaultScope.GenerateOneLandfillFacility();
        landfillFacility[0].ShowHazNonHaz = false;
        scope.SetupExistingFacilities(landfillFacility.ToArray());
        var context = DefaultScope.CreateUndefinedHazardousMaterialApprovalContext(landfillFacility[0].Id);
        var validationResults = new List<ValidationResult>();

        //execute
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MaterialApprovalValidationRule_ShouldFail_WhenFacilityIsLandfillAndHazNonHazIsUndefined_ShowHazTrue()
    {
        //setup
        var scope = new DefaultScope();
        var landfillFacility = DefaultScope.GenerateOneLandfillFacility();
        scope.SetupExistingFacilities(landfillFacility.ToArray());
        var context = DefaultScope.CreateUndefinedHazardousMaterialApprovalContext(landfillFacility[0].Id);
        var validationResults = new List<ValidationResult>();

        //execute
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.MaterialApproval_HazardousClassification));
    }

    private class DefaultScope : TestScope<MaterialApprovalBasicValidationRules>
    {
        public readonly Mock<IProvider<Guid, FacilityEntity>> FacilityProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(FacilityProviderMock.Object);
        }

        public static List<FacilityEntity> GenerateOneLandfillFacility()
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
                    ShowHazNonHaz = true,
                }
            };
        }

        public void SetupExistingFacilities(params FacilityEntity[] entities)
        {
            FacilityProviderMock.SetupEntities(entities);
        }

        public static BusinessContext<MaterialApprovalEntity> CreateUndefinedHazardousMaterialApprovalContext(Guid facilityId)
        {
            return new(new()
            {
                Facility = "Facility1",
                FacilityId = facilityId,
                FacilityServiceId = Guid.NewGuid(),
                FacilityServiceNumber = "number 2355",
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
                HazardousNonhazardous = HazardousClassification.Undefined,
                LoadSummaryReport = true,
                AnalyticalExpiryAlertActive = false,
                AnalyticalExpiryDate = DateTimeOffset.Now,
                SecureRepresentative = "CreatedBy",
                SourceRegion = SourceRegionEnum.InRegion,
                WasteCodeName = "AER",
            });
        }

        public static BusinessContext<MaterialApprovalEntity> CreateNonHazardousMaterialApprovalContext()
        {
            return new(new()
            {
                Facility = "Facility1",
                FacilityId = Guid.NewGuid(),
                FacilityServiceId = Guid.NewGuid(),
                FacilityServiceNumber = "number 2355",
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
                WasteCodeName = "AER",
            });
        }
    }
}
