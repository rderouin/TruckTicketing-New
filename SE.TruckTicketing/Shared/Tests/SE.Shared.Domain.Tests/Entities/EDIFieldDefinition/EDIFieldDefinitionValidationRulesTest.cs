using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Shared.Domain.Entities.EDIFieldDefinition;
using SE.Shared.Domain.Entities.EDIFieldDefinition.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.Shared.Domain.Tests.Entities.EDIFieldDefinition;

[TestClass]
public class EDIFieldDefinitionValidationRulesTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_ForValidAccount()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidEDIFieldDefinitionContext();
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
    public async Task Rule_ShouldFail_WhenFieldLookupIsEmpty(string lookupId)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidEDIFieldDefinitionContext();
        context.Target.EDIFieldLookupId = lookupId;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EDIFieldDefinitionEntity_FieldLookupRequired));
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(null)]
    public async Task Rule_ShouldFail_WhenFieldNameIsEmpty(string fieldName)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidEDIFieldDefinitionContext();
        context.Target.EDIFieldLookupId = fieldName;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EDIFieldDefinitionEntity_FieldNameRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenValidationPatternIsEmptyAndIsValidationIsTrue()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidEDIFieldDefinitionContext();
        context.Target.ValidationRequired = true;
        context.Target.ValidationPattern = string.Empty;

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EDIFieldDefinitionEntity_ValidationPatternRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenValidationErrorMessageIsEmptyAndIsValidationIsTrue()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidEDIFieldDefinitionContext();
        context.Target.ValidationRequired = true;
        context.Target.ValidationErrorMessage = string.Empty;

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EDIFieldDefinitionEntity_ValidationErrorMessageRequired));
    }

    private class DefaultScope : TestScope<EDIFieldDefinitionValidationRules>
    {
        public readonly Mock<IProvider<Guid, EDIFieldDefinitionEntity>> EDIFieldDefinitionProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(EDIFieldDefinitionProviderMock.Object);
        }

        public EDIFieldDefinitionEntity ValueEDIFieldDefinitionEntity =>
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                EDIFieldLookupId = $"{nameof(EDIFieldDefinitionEntity.EDIFieldLookupId)}",
                EDIFieldName = $"{nameof(EDIFieldDefinitionEntity.EDIFieldName)}",
                DefaultValue = $"{nameof(EDIFieldDefinitionEntity.DefaultValue)}",
                IsRequired = false,
                IsPrinted = false,
                ValidationRequired = false,
                ValidationPatternId = Guid.NewGuid(),
                ValidationPattern = $"{nameof(EDIFieldDefinitionEntity.ValidationPattern)}",
                ValidationErrorMessage = $"{nameof(EDIFieldDefinitionEntity.ValidationErrorMessage)}",
            };

        public BusinessContext<EDIFieldDefinitionEntity> CreateValidEDIFieldDefinitionContext(EDIFieldDefinitionEntity original = null)
        {
            return new(ValueEDIFieldDefinitionEntity, original);
        }
    }
}
