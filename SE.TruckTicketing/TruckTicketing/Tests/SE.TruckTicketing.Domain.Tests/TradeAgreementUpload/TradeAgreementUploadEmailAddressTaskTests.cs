using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Domain.Entities.TradeAgreementUploads;
using SE.TruckTicketing.Domain.Entities.TradeAgreementUploads.Tasks;

using Trident.Business;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.TradeAgreementUpload;

[TestClass]
public class TradeAgreementUploadEmailAddressTaskTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_OnlyWhenTradeAgreementUploadEmailContext_Insert()
    {
        // arrange
        var scope = new DefaultScope();
        var validEntity = GenFu.GenFu.New<TradeAgreementUploadEntity>();
        var context = scope.CreateValidContext(validEntity, new());
        context.Operation = Operation.Insert;
        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.BeforeInsert);
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_IfTradeAgreementUploadEmailContext_NotInsert()
    {
        // arrange
        var scope = new DefaultScope();
        var validEntity = GenFu.GenFu.New<TradeAgreementUploadEntity>();
        var context = scope.CreateValidContext(validEntity, new()); 
        context.Operation = Operation.Update;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_ValidUserContext_EmailAddressPopulated()
    {
        // arrange
        var scope = new DefaultScope();
        var validUserContext = GenFu.GenFu.New<UserContext>();
        scope.SetUpUserContextAccessor(validUserContext);
        var validEntity = GenFu.GenFu.New<TradeAgreementUploadEntity>();
        var context = scope.CreateValidContext(validEntity, new());
        // act
        var result = await scope.InstanceUnderTest.Run(context);
        // assert
        result.Should().BeTrue();
        context.Target.EmailAddress.Should().NotBeNullOrEmpty();
        Assert.IsTrue(string.Equals(context.Target.EmailAddress, validUserContext.EmailAddress));
    }
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_NoUserContextExists_EmailAddressNotPopulated()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetUpUserContextAccessor(new());
        var validEntity = GenFu.GenFu.New<TradeAgreementUploadEntity>();
        var context = scope.CreateValidContext(validEntity, new());
        // act
        var result = await scope.InstanceUnderTest.Run(context);
        // assert
        result.Should().BeTrue();
        context.Target.EmailAddress.Should().BeNullOrEmpty();
    }
    private class DefaultScope : TestScope<TradeAgreementUploadEmailAddressTask>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(UserContextAccessorMock.Object);
        }

        private Mock<IUserContextAccessor> UserContextAccessorMock { get; } = new();

        public BusinessContext<TradeAgreementUploadEntity> CreateValidContext(TradeAgreementUploadEntity target, TradeAgreementUploadEntity original)
        {
            return new(target, original);
        }

        public void SetUpUserContextAccessor(UserContext userContext)
        {
            UserContextAccessorMock.Setup(c => c.UserContext).Returns(userContext);
        }
    }
}
