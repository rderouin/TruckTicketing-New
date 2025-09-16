using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using SE.Shared.Domain.Entities.UserProfile;
using SE.Shared.Domain.Entities.UserProfile.Rules;

using Trident.Business;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.Shared.Domain.Tests.Entities.UserProfile;

[TestClass]
public class UserProfileComplexValidationRuleTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void UserProfileComplexValidationRule_CanBeInstantiated()
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
    public async Task UserProfileComplexValidationRule_ShouldPass_ForValidUserProfile()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidUserProfileEntity();
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task UserProfileComplexValidationRule_ShouldFail_WhenExternalAuthIdHasBeenChanged()
    {
        // arrange
        var scope = new DefaultScope();
        var original = scope.CreateContextWithValidUserProfileEntity().Original;
        var context = scope.CreateContextWithValidUserProfileEntity(scope.CreateContextWithValidUserProfileEntity().Target);
        context.Target.ExternalAuthId += "ChangingExternalAuthId";

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<ErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(ErrorCodes.UserProfile_ExternalAuthIdImmutable));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task UserProfileComplexValidationRule_ShouldFail_WhenExternalAuthIdIsNotUnique()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidUserProfileEntity();
        context.ContextBag.Add(UserProfileBusinessContextBagKeys.UserProfileExternalAuthIdIsUnique, false);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<ErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(ErrorCodes.UserProfile_ExternalAuthIdUnique));
    }

    public class DefaultScope : TestScope<UserProfileComplexValidationRules>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public BusinessContext<UserProfileEntity> CreateContextWithValidUserProfileEntity(UserProfileEntity original = null)
        {
            return new(new()
            {
                Email = "jjackson@secure-energy.com",
                GivenName = "Jack",
                Surname = "Jackson",
                DisplayName = "Jack Jackson",
                ExternalAuthId = "12345",
                LocalAuthId = "",
            }, original, new Dictionary<string, object>());
        }
    }
}
