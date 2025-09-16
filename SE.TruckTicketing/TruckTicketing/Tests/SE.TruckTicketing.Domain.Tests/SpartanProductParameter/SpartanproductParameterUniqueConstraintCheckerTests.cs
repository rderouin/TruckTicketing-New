using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Domain.Entities.SpartanProductParameters;
using SE.TruckTicketing.Domain.Entities.SpartanProductParameters.Rules;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Tests.SpartanProductParameter;

[TestClass]
public class SpartanproductParameterUniqueConstraintCheckerTests
{
    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(default)]
    public async Task Rule_ShouldFail_WhenSPPProductNameIsEmpty(string name)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSPPContext(scope.SpartanProductParameterEntity);
        context.Target.ProductName = name;
        var validationResults = new List<ValidationResult>();

        IEnumerable<SpartanProductParameterEntity> getResult = new List<SpartanProductParameterEntity>();

        scope.SpartanProductParameterProviderMock.Setup(x => x.Get(It.IsAny<Expression<Func<SpartanProductParameterEntity, bool>>>(),
                                                                   null, It.IsAny<IEnumerable<string>>(), false, false, true))
             .ReturnsAsync(getResult);

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert

        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SpartanProductParam_ProductNameIsRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenSPPWaterMinGreaterThanMax()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSPPContext(scope.SpartanProductParameterEntity);
        var validationResults = new List<ValidationResult>();
        IEnumerable<SpartanProductParameterEntity> getResult = new List<SpartanProductParameterEntity> { scope.ValidSpartanProductParameterEntity };
        context.Target.MinWaterPercentage = 8.0;
        context.Target.MaxWaterPercentage = 3.0;
        scope.SpartanProductParameterProviderMock.Setup(x => x.Get(It.IsAny<Expression<Func<SpartanProductParameterEntity, bool>>>(),
                                                                   null, It.IsAny<IEnumerable<string>>(), false, false, true))
             .ReturnsAsync(getResult);

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SpartanProductParam_MaxLessThanMin));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenSPPFluidDensityMinGreaterThanMax()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSPPContext(scope.SpartanProductParameterEntity);
        var validationResults = new List<ValidationResult>();
        IEnumerable<SpartanProductParameterEntity> getResult = new List<SpartanProductParameterEntity> { scope.ValidSpartanProductParameterEntity };
        context.Target.MinFluidDensity = 25.0;
        context.Target.MaxFluidDensity = 10.0;
        scope.SpartanProductParameterProviderMock.Setup(x => x.Get(It.IsAny<Expression<Func<SpartanProductParameterEntity, bool>>>(),
                                                                   null, It.IsAny<IEnumerable<string>>(), false, false, true))
             .ReturnsAsync(getResult);

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.SpartanProductParam_MaxLessThanMin));
    }

    private class DefaultScope : TestScope<SpartanProductParameterValidationRules>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public Mock<IProvider<Guid, SpartanProductParameterEntity>> SpartanProductParameterProviderMock { get; } = new();

        public SpartanProductParameterEntity ValidSpartanProductParameterEntity =>
            new()
            {
                ProductName = "Product1",
            };

        public SpartanProductParameterEntity SpartanProductParameterEntity =>
            new()
            {
                ProductName = "Product2",
            };

        public BusinessContext<SpartanProductParameterEntity> CreateValidSPPContext(SpartanProductParameterEntity original = null)
        {
            return new(ValidSpartanProductParameterEntity, original);
        }
    }
}
