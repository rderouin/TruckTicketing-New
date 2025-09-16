using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Entities.SourceLocation.Rules;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Tests.SourceLocation;

[TestClass]
public class SourceLocationValidationRulesTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_ForValidCASourceLocation()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
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
    public async Task Rule_ShouldFail_WhenIdentifierIsEmpty(string identifier)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.Identifier = identifier;
        context.Target.CountryCode = CountryCode.CA;
        var validationResults = new List<ValidationResult>();
        context.Target.IsUnique = true;
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert

        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_IdentifierRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenGeneratorIdIsEmpty()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.GeneratorId = default;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert

        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_GeneratorRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenGeneratorStartDateIsEmpty()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.GeneratorStartDate = default;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert

        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_GeneratorStartDateRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenGeneratorStartIsBeforePreviousStartDate()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext(scope.ValidSourceLocationEntity);
        context.Target.GeneratorStartDate = context.Original.GeneratorStartDate.AddMinutes(-1);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert

        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_GeneratorStartDateMustBeLaterThanPreviousStartDate));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenContractorIdIsEmpty()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.ContractOperatorId = default;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert

        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_ContractOperatorRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_ForLicenseNumberBetween5And20CharactersLong()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.LicenseNumber = "L12345";
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ForLicenseNumberWithLessThan5Characters()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.LicenseNumber = "L123";
        context.Target.CountryCode = CountryCode.CA;
        var validationResults = new List<ValidationResult>();
        context.Target.IsUnique = true;
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_LicenseNumberInvalid));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ForUnspecifiedProvinces()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.ProvinceOrState = StateProvince.Unspecified;
        context.Target.CountryCode = CountryCode.CA;
        var validationResults = new List<ValidationResult>();
        context.Target.IsUnique = true;
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocationType_ProvinceRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_ForInvalidLicenseNumberWhenCountryIsUS()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.LicenseNumber = "L1234L1234L1234L12341";
        context.Target.CountryCode = CountryCode.US;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ForLicenseNumberWithMoreThan20Characters()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.LicenseNumber = "L1234L1234L1234L12341";
        context.Target.CountryCode = CountryCode.CA;
        var validationResults = new List<ValidationResult>();
        context.Target.IsUnique = true;
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_LicenseNumberInvalid));
    }

    private class DefaultScope : TestScope<SourceLocationValidationRules>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public SourceLocationEntity ValidSourceLocationEntity =>
            new()
            {
                Identifier = "990/99-90-999-09W9/99",
                GeneratorId = Guid.NewGuid(),
                GeneratorStartDate = DateTimeOffset.Now,
                ContractOperatorId = Guid.NewGuid(),
                SourceLocationCode = "1234567890",
                ProvinceOrState = StateProvince.AB,
                FormattedIdentifier = "990/99-90-999-09W9/99",
            };

        public BusinessContext<SourceLocationEntity> CreateValidSourceLocationContext(SourceLocationEntity original = null)
        {
            return new(ValidSourceLocationEntity, original);
        }
    }
}
