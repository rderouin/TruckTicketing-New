using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.ServiceType;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.ServiceType.Rules;

using Trident.Business;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Tests.ServiceType;

[TestClass]
public class ServiceTypeBasicValidationRuleTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void ServiceTypeBasicValidationRule_CanBeInstantiated()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var runOrder = scope.InstanceUnderTest.RunOrder;

        // assert
        runOrder.Should().BePositive();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task ServiceTypeBasicValidationRule_ShouldPass_ValidServiceTypeEntity()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidServiceTypeEntity();
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task ServiceTypeBasicValidationRule_ShouldFail_WhenCountryCodeIsNullOrEmpty(string countryCode)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidServiceTypeEntity();
        context.Target.CountryCode = CountryCode.Undefined;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.ServiceType_CountryCode));
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task ServiceTypeBasicValidationRule_ShouldFail_WhenServiceTypeIdIsNullOrEmpty(string serviceTypeId)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidServiceTypeEntity();
        context.Target.ServiceTypeId = serviceTypeId;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.ServiceType_ServiceTypeId));
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task ServiceTypeBasicValidationRule_ShouldFail_WhenServiceTypeIdIsTooLong(string serviceTypeId)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidServiceTypeEntity();
        context.Target.ServiceTypeId = serviceTypeId + "NvGbAFAW97RhHt8Df9HAZpwLNSLCTQEVXvcfOKeUkYQJq1e8Cf1234787";
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.ServiceType_ServiceTypeId));
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task ServiceTypeBasicValidationRule_ShouldFail_WhenServiceTypeNameIsNullOrEmpty(string serviceTypeName)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidServiceTypeEntity();
        context.Target.Name = serviceTypeName;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.ServiceType_ServiceTypeName));
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task ServiceTypeBasicValidationRule_ShouldFail_WhenServiceTypeNameIsTooLong(string serviceTypeName)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidServiceTypeEntity();
        context.Target.Name = serviceTypeName + "NvGbAFAW97RhHt8Df9HAZpwLNSLCTQEVXvcfOKeUkYQJq1e8Cf69769";
        ;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.ServiceType_ServiceTypeName));
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task ServiceTypeBasicValidationRule_ShouldFail_WhenClassIsNullOrEmpty(string serviceTypeClass)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidServiceTypeEntity();
        context.Target.Class = Class.Undefined;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.ServiceType_Class));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task ServiceTypeBasicValidationRule_ShouldFail_WhenTotalItemReleasedProductIdIsNotSet()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidServiceTypeEntity();
        context.Target.TotalItemName = null;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.ServiceType_TotalItemProduct));
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task ServiceTypeBasicValidationRule_ShouldFail_WhenReportAsCutTypeIsNullOrEmpty(string reportAsCutType)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidServiceTypeEntity();
        context.Target.ReportAsCutType = ReportAsCutTypes.Undefined;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.ServiceType_ReportAsCutType));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task ServiceTypeBasicValidationRule_ShouldFail_WhenStreamIsNullorEmpty()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidServiceTypeEntity();
        context.Target.Stream = Stream.Undefined;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.ServiceType_Stream));
    }

    [DataTestMethod]
    [DataRow(ReportAsCutTypes.Oil)]
    [DataRow(ReportAsCutTypes.Water)]
    [DataRow(ReportAsCutTypes.Solids)]
    [DataRow(ReportAsCutTypes.Service)]
    public async Task Rule_ShouldFail_IncludesWater_ReportTypeNotAsPerCutsEntered(ReportAsCutTypes cutType)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidServiceTypeEntity();
        context.Target.ReportAsCutType = cutType;
        context.Target.IncludesWater = true;
        context.Target.IncludesOil = false;
        context.Target.IncludesSolids = false;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.ServiceType_InvalidReportAsCutTypeSelected));
    }

    [DataTestMethod]
    [DataRow(ReportAsCutTypes.Oil)]
    [DataRow(ReportAsCutTypes.Water)]
    [DataRow(ReportAsCutTypes.Solids)]
    [DataRow(ReportAsCutTypes.Service)]
    public async Task Rule_ShouldFail_IncludesOil_ReportTypeNotAsPerCutsEntered(ReportAsCutTypes cutType)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidServiceTypeEntity();
        context.Target.ReportAsCutType = cutType;
        context.Target.IncludesWater = false;
        context.Target.IncludesOil = true;
        context.Target.IncludesSolids = false;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.ServiceType_InvalidReportAsCutTypeSelected));
    }

    [DataTestMethod]
    [DataRow(ReportAsCutTypes.Oil)]
    [DataRow(ReportAsCutTypes.Water)]
    [DataRow(ReportAsCutTypes.Solids)]
    [DataRow(ReportAsCutTypes.Service)]
    public async Task Rule_ShouldFail_IncludesSolids_ReportTypeNotAsPerCutsEntered(ReportAsCutTypes cutType)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidServiceTypeEntity();
        context.Target.ReportAsCutType = cutType;
        context.Target.IncludesWater = false;
        context.Target.IncludesOil = false;
        context.Target.IncludesSolids = true;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.ServiceType_InvalidReportAsCutTypeSelected));
    }

    private class DefaultScope : TestScope<ServiceTypeBasicValidationRules>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public BusinessContext<ServiceTypeEntity> CreateContextWithValidServiceTypeEntity()
        {
            return new(new()
            {
                CountryCode = CountryCode.CA,
                ServiceTypeId = "Id",
                Class = Class.Class1,
                Name = "test1",
                TotalItemName = "Total",
                ReportAsCutType = ReportAsCutTypes.Solids,
                Stream = Stream.Landfill,
                TotalMaxValue = 50,
                TotalMinValue = 20,
            });
        }
    }
}
