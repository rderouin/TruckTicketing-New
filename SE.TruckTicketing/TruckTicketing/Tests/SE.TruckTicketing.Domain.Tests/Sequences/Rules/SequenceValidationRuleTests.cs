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
public class SequenceValidationRuleTests
{
    private const int SequenceGenerationSeed = 10000;

    private const int SequenceGenerationBlockSize = 50;

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
    public async Task SequenceValidationBusinessProfileRule_Run_Exist_ExpectPass()
    {
        // arrange
        var scope = new DefaultScope(true);
        var errors = new List<ValidationResult>();
        scope.TestContext.Target.Type = "Test";
        scope.TestContext.Target.Prefix = "Test";
        scope.TestContext.Target.LastNumber = SequenceGenerationSeed + SequenceGenerationBlockSize;
        // act
        await scope.InstanceUnderTest.Run(scope.TestContext, errors);
        // assert
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SequencesValidationBusinessProfileRule_Run_LastNumber_NotValid_LessThanSeed_ExpectError()
    {
        // arrange
        var scope = new DefaultScope(true);
        var errors = new List<ValidationResult>();
        scope.TestContext.Target.Type = "Test";
        scope.TestContext.Target.Prefix = "Test";
        //Invalid LastNumber because less than SequenceType Seed
        scope.TestContext.Target.LastNumber = SequenceGenerationSeed - SequenceGenerationBlockSize;
        // act
        await scope.InstanceUnderTest.Run(scope.TestContext, errors);
        // assert
        errors.Cast<ValidationResult<TTErrorCodes>>()
              .Select(result => result.ErrorCode)
              .Should()
              .ContainSingle(nameof(TTErrorCodes.SequenceGeneration_LastNumber));
    }

    private class DefaultScope : TestScope<ValidationRuleBase<BusinessContext<SequenceEntity>>>
    {
        public DefaultScope(bool isAddOperation = false, bool bypassConfigSectionMock = false)
        {
            TestContext = GetTestContext();
            TestContext.Operation = isAddOperation ? Operation.Insert : Operation.Update;
            if (!isAddOperation)
            {
                TestContext.Original.Type = "Test";
                TestContext.Original.Prefix = "Test";
                TestContext.Original.LastNumber = SequenceGenerationSeed;
            }

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

            InstanceUnderTest = new SequenceValidationRule(AppSettingsMock.Object);
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
