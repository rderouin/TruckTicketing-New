using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.SourceLocationType;
using SE.Shared.Domain.Entities.SourceLocationType.Rules;
using SE.Shared.Domain.Entities.SourceLocationType.Tasks;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Tests.SourceLocation;

[TestClass]
public class SourceLocationTypePropertyValidationRulesTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_ForValidCASourceLocationType()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_ForValidUSSourceLocationType()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        context.Target.CountryCode = CountryCode.US;
        context.Target.DefaultDeliveryMethod = DeliveryMethod.Pipeline;
        context.Target.DefaultDownHoleType = DownHoleType.Pit;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        validationResults.Should().BeEmpty();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(null)]
    public async Task Rule_ShouldFail_WhenNameIsNullOrWhitespace(string name)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        context.Target.Name = name;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocationType_NameRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenCountryCodeIsUndefined()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        context.Target.CountryCode = CountryCode.Undefined;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocationType_CountryCodeRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenCategoryIsUndefined()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        context.Target.Category = SourceLocationTypeCategory.Undefined;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocationType_CategoryRequired));
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(null)]
    public async Task Rule_ShouldFail_ShortFormCodeIsNullOrWhitespace_ForCanadianTypes(string shortFormCode)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        context.Target.ShortFormCode = shortFormCode;
        context.Target.CountryCode = CountryCode.CA;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocationType_ShortFormCodeRequired));
    }
    
    
    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(null)]
    public async Task Rule_ShouldPass_ShortFormCodeIsNullOrWhitespace_ForUSTypes(string shortFormCode)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        context.Target.DefaultDownHoleType = DownHoleType.Pit;
        context.Target.DefaultDeliveryMethod = DeliveryMethod.Pipeline;
        context.Target.ShortFormCode = shortFormCode;
        context.Target.CountryCode = CountryCode.US;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenCountryCodeIsNotUnique()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        context.ContextBag.Add(SourceLocationTypeUniqueConstraintCheckerTask.ResultKey, false);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocationType_NameMustBeUniqueInEachCountry));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_IfDownHoleTypeDefaultIsUndefinedForUS()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        context.Target.DefaultDownHoleType = DownHoleType.Undefined;
        context.Target.CountryCode = CountryCode.US;
        context.Target.DefaultDeliveryMethod = DeliveryMethod.Trucked;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocationType_DownHoleTypeDefaultRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_IfDeliveryMethodIsUndefinedForUS()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationTypeContext();
        context.Target.DefaultDeliveryMethod = DeliveryMethod.Undefined;
        context.Target.CountryCode = CountryCode.US;
        context.Target.DefaultDownHoleType = DownHoleType.Well;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocationType_DeliveryMethodDefaultRequired));
    }

    private class DefaultScope : TestScope<SourceLocationTypeValidationRules>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public BusinessContext<SourceLocationTypeEntity> CreateValidSourceLocationTypeContext()
        {
            return new(new()
            {
                Name = "UWI NTS",
                Category = SourceLocationTypeCategory.Well,
                CountryCode = CountryCode.CA,
                ShortFormCode = "WI",
                FormatMask = "###/@-###-@/###-@-##/##",
            });
        }
    }
}
