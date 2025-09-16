using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Entities.SourceLocation.Rules;
using SE.Shared.Domain.Entities.SourceLocationType;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Tests.SourceLocation;

[TestClass]
public class UsSourceLocationValidationRulesTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_ForValidCASourceLocation()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.CountryCode = CountryCode.CA;
        context.Target.SourceLocationType.CountryCode = CountryCode.CA;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_ForValidUSSourceLocation()
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
    [DataRow(default)]
    public async Task Rule_ShouldFail_WhenSourceLocationNameIsEmpty(string name)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.SourceLocationName = name;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert

        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_NameRequiredForUSLocations));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenSourceLocationNameIsNotUnique()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.IsUnique = false;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_NameMustBeUnique));
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(default)]
    public async Task Rule_ShouldFail_WhenPlsNumberIsRequiredAndIsEmpty(string plsNumber)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.SourceLocationType.RequiresPlsNumber = true;
        context.Target.SourceLocationType.IsPlsNumberVisible = true;

        context.Target.PlsNumber = plsNumber;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert

        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_PlsNumberIsRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_WhenPlsNumberIsRequiredAndNumberIsValid()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.SourceLocationType.RequiresPlsNumber = true;
        context.Target.PlsNumber = "aZa1-12-223-234";
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(default)]
    public async Task Rule_ShouldFail_WhenApiNumberIsRequiredAndIsEmpty(string apiNumber)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.SourceLocationType.RequiresApiNumber = true;
        context.Target.SourceLocationType.IsApiNumberVisible = true;
        context.Target.ApiNumber = apiNumber;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert

        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_ApiNumberIsRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_WhenApiNumberIsRequiredAndNumberIsValid()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.SourceLocationType.RequiresApiNumber = true;
        context.Target.ApiNumber = "2313222563";
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(default)]
    public async Task Rule_ShouldFail_WhenFiveDigitWellFileNumberIsRequiredAndIsEmpty(string wellFileNumber)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.SourceLocationType.RequiresWellFileNumber = true;
        context.Target.SourceLocationType.IsWellFileNumberVisible = true;

        context.Target.ApiNumber = "33-132-22563-35-00";
        context.Target.WellFileNumber = wellFileNumber;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert

        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_WellFileNumberIsRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_WhenFiveDigitWellFileNumberIsRequiredAndNumberIsValid()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.SourceLocationType.RequiresWellFileNumber = true;
        context.Target.ApiNumber = "33-132-22563-35-00";
        context.Target.WellFileNumber = "23421";
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(default)]
    public async Task Rule_ShouldFail_WhenEightDigitWellFileNumberIsRequiredAndIsEmpty(string wellFileNumber)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.SourceLocationType.RequiresWellFileNumber = true;
        context.Target.SourceLocationType.IsWellFileNumberVisible = true;
        context.Target.ApiNumber = "25-132-22563-35-00";
        context.Target.WellFileNumber = wellFileNumber;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert

        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_WellFileNumberIsRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_WhenEightDigitWellFileNumberIsRequiredAndNumberIsValid()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.SourceLocationType.RequiresWellFileNumber = true;
        context.Target.ApiNumber = "25-132-22563-35-00";
        context.Target.WellFileNumber = "23421145";
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(null)]
    public async Task Rule_ShouldFail_WhenCtbNumberIsRequiredAndIsEmpty(string ctbNumber)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.SourceLocationType.RequiresCtbNumber = true;
        context.Target.SourceLocationType.IsCtbNumberVisible = true;

        context.Target.CtbNumber = ctbNumber;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert

        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_CtbNumberIsRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_WhenCtbNumberIsRequiredAndNumberIsValid()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.SourceLocationType.RequiresCtbNumber = true;
        context.Target.CtbNumber = "241390";
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenDownHoleTypeIsUndefined()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.DownHoleType = default;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_DownHoleTypeRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenDeliveryMethodIsUndefined()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.DeliveryMethod = default;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SourceLocation_DeliveryMethodRequired));
    }

    private class DefaultScope : TestScope<UsSourceLocationValidationRules>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public SourceLocationEntity ValidSourceLocationEntity =>
            new()
            {
                SourceLocationName = "Test",
                Identifier = "990/99-90-999-09W9/99",
                GeneratorId = Guid.NewGuid(),
                GeneratorStartDate = DateTimeOffset.Now,
                ContractOperatorId = Guid.NewGuid(),
                SourceLocationType = SourceLocationTypeEntity,
                CountryCode = CountryCode.US,
                IsUnique = true,
                DownHoleType = DownHoleType.Pit,
                DeliveryMethod = DeliveryMethod.Trucked,
            };

        public SourceLocationTypeEntity SourceLocationTypeEntity =>
            new()
            {
                Category = SourceLocationTypeCategory.Well,
                CountryCode = CountryCode.US,
                DefaultDeliveryMethod = DeliveryMethod.Trucked,
                DefaultDownHoleType = DownHoleType.Pit,
                Name = "Test",
                RequiresApiNumber = false,
                RequiresWellFileNumber = false,
                RequiresCtbNumber = false,
                RequiresPlsNumber = false,
            };

        public BusinessContext<SourceLocationEntity> CreateValidSourceLocationContext(SourceLocationEntity original = null)
        {
            return new(ValidSourceLocationEntity, original);
        }
    }
}
