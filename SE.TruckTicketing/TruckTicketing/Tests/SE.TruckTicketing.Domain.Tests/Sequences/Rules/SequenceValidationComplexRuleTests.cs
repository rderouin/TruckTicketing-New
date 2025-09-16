using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.Sequences;
using SE.Shared.Domain.Entities.Sequences.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;
using Trident.Contracts.Configuration;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Tests.Sequences.Rules;

[TestClass]
public class SequenceValidationComplexRuleTests
{
    private const int SequenceGenerationSeed = 10000;

    private const int SequenceGenerationBlockSize = 50;

    private const int Offset = 10;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
    [TestMethod]
    [TestCategory("Unit")]
    public void SequenceValidationBusinessProfileRule_Inherits()
    {
        // arrange
        var scope = new DefaultScope();
        // act / assert
        Assert.IsInstanceOfType(scope.InstanceUnderTest, typeof(ValidationRuleBase<BusinessContext<SequenceEntity>>));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SequenceValidationComplexBusinessProfileRule_Run_Exist_ExpectPass()
    {
        // arrange
        var scope = new DefaultScope();
        var errors = new List<ValidationResult>();
        scope.TestContext.Target.Type = "Test";
        scope.TestContext.Target.Prefix = "Test";
        scope.TestContext.Target.LastNumber = 10005;
        // act
        await scope.InstanceUnderTest.Run(scope.TestContext, errors);
        // assert
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SequencesValidationComplexBusinessProfileRule_Run_TypeImmutable_NotValid_ExpectError()
    {
        // arrange
        var scope = new DefaultScope();
        var errors = new List<ValidationResult>();
        scope.TestContext.Target.Type = "Updated";
        scope.TestContext.Target.Prefix = "Test";
        scope.TestContext.Target.LastNumber = SequenceGenerationSeed + Offset;
        // act
        await scope.InstanceUnderTest.Run(scope.TestContext, errors);
        // assert
        errors.Cast<ValidationResult<TTErrorCodes>>()
              .Select(result => result.ErrorCode)
              .Should()
              .ContainSingle(nameof(TTErrorCodes.SequenceGeneration_TypeCheck));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SequencesValidationComplexBusinessProfileRule_Run_PrefixImmutable_NotValid_ExpectError()
    {
        // arrange
        var scope = new DefaultScope();
        var errors = new List<ValidationResult>();
        scope.TestContext.Target.Type = "Test";
        scope.TestContext.Target.Prefix = "Updated";
        scope.TestContext.Target.LastNumber = SequenceGenerationSeed + Offset;
        // act
        await scope.InstanceUnderTest.Run(scope.TestContext, errors);
        // assert
        errors.Cast<ValidationResult<TTErrorCodes>>()
              .Select(result => result.ErrorCode)
              .Should()
              .ContainSingle(nameof(TTErrorCodes.SequenceGeneration_PrefixCheck));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SequencesValidationComplexBusinessProfileRule_Run_BlockSize_NotValid_NegativeBlockSize_ExpectError()
    {
        // arrange
        var scope = new DefaultScope();
        var errors = new List<ValidationResult>();
        scope.TestContext.Target.Type = "Test";
        scope.TestContext.Target.Prefix = "Test";
        scope.TestContext.Target.LastNumber = SequenceGenerationSeed - Offset;
        // act
        await scope.InstanceUnderTest.Run(scope.TestContext, errors);
        // assert
        errors.Cast<ValidationResult<TTErrorCodes>>()
              .Select(result => result.ErrorCode)
              .Should()
              .ContainSingle(nameof(TTErrorCodes.SequenceGeneration_BlockSize));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SequencesValidationComplexBusinessProfileRule_Run_BlockSize_NotValid_GreaterThanMaxBlockSize_ExpectError()
    {
        // arrange
        var scope = new DefaultScope();
        var errors = new List<ValidationResult>();
        scope.TestContext.Target.Type = "Test";
        scope.TestContext.Target.Prefix = "Test";
        //Invalid LastNumber because less than SequenceType Seed
        scope.TestContext.Target.LastNumber = SequenceGenerationSeed + SequenceGenerationBlockSize + Offset;
        // act
        await scope.InstanceUnderTest.Run(scope.TestContext, errors);
        // assert
        errors.Cast<ValidationResult<TTErrorCodes>>()
              .Select(result => result.ErrorCode)
              .Should()
              .ContainSingle(nameof(TTErrorCodes.SequenceGeneration_BlockSize));
    }

    private class DefaultScope : TestScope<ValidationRuleBase<BusinessContext<SequenceEntity>>>
    {
        public DefaultScope(bool bypassConfigSectionMock = false)
        {
            TestContext = GetTestContext();
            TestContext.Operation = Operation.Update;
            TestContext.Original.Type = "Test";
            TestContext.Original.Prefix = "Test";
            TestContext.Original.LastNumber = SequenceGenerationSeed;

            if (!bypassConfigSectionMock)
            {
                AppSettingsMock.Setup(x => x.GetSection<SequenceConfiguration>(It.IsAny<string>())).Returns(new SequenceConfiguration
                {
                    Infix = "Test",
                    MaxRequestBlockSize = SequenceGenerationBlockSize,
                    Seed = SequenceGenerationSeed,
                    Suffix = "Test",
                });
            }

            InstanceUnderTest = new SequenceValidationComplexRule(AppSettingsMock.Object);
        }

        public Mock<IAppSettings> AppSettingsMock { get; } = new();

        public BusinessContext<SequenceEntity> TestContext { get; }

        private BusinessContext<SequenceEntity> GetTestContext()
        {
            var target = new SequenceEntity();
            var original = new SequenceEntity();
            return new(target, original);
        }
    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
}
