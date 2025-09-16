using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.LoadConfirmation.Rules;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.Shared.Domain.Tests.Entities.LoadConfirmation;

[TestClass]
public class LoadConfirmationValidationRulesTest : TestScope<LoadConfirmationValidationRules>
{
    private const string ActiveSalesLinesOnLoadConfirmation = nameof(ActiveSalesLinesOnLoadConfirmation);

    private const string ResultKey = ActiveSalesLinesOnLoadConfirmation;

    [TestMethod]
    [TestCategory("Unit")]
    public void LoadConfirmationValidationRules_CanBeInstantiated()
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
    [DataRow(LoadConfirmationStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Open)]
    [DataRow(LoadConfirmationStatus.PendingSignature)]
    [DataRow(LoadConfirmationStatus.Rejected)]
    [DataRow(LoadConfirmationStatus.SignatureVerified)]
    [DataRow(LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(LoadConfirmationStatus.Unknown)]
    public async Task LoadConfirmationValidationRules_Status_NotVoid_ShouldPass(LoadConfirmationStatus status)
    {
        // arrange
        var scope = new DefaultScope();
        var target = GenFu.GenFu.New<LoadConfirmationEntity>();
        var origin = target.Clone();
        target.Status = status;
        var context = scope.CreateContextWithValidLoadConfirmationEntity(target, origin);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        Assert.IsFalse(context.ContextBag.ContainsKey(ResultKey));
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.PendingSignature)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Open)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Rejected)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Unknown)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingSignatureValidation)]
    public async Task LoadConfirmationValidationRules_VoidStatus_NoSalesLinesAssociated_ShouldPass(LoadConfirmationStatus targetStatus, LoadConfirmationStatus originStatus)
    {
        // arrange
        var scope = new DefaultScope();
        var target = GenFu.GenFu.New<LoadConfirmationEntity>();
        var origin = target.Clone();
        target.Status = targetStatus;
        origin.Status = originStatus;
        var context = scope.CreateContextWithValidLoadConfirmationEntity(target, origin);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        Assert.IsTrue(context.ContextBag.ContainsKey(ResultKey));
        context.ContextBag.TryGetValue(ResultKey, out var isActiveLoadConfirmationForInvoice);
        Assert.IsFalse(isActiveLoadConfirmationForInvoice != null && (bool)isActiveLoadConfirmationForInvoice);
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(LoadConfirmationStatus.Posted, LoadConfirmationStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Posted, LoadConfirmationStatus.PendingSignature)]
    [DataRow(LoadConfirmationStatus.Posted, LoadConfirmationStatus.Open)]
    [DataRow(LoadConfirmationStatus.Posted, LoadConfirmationStatus.Rejected)]
    [DataRow(LoadConfirmationStatus.Posted, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(LoadConfirmationStatus.Posted, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(LoadConfirmationStatus.Posted, LoadConfirmationStatus.Unknown)]
    [DataRow(LoadConfirmationStatus.Posted, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(LoadConfirmationStatus.Posted, LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(LoadConfirmationStatus.PendingSignature, LoadConfirmationStatus.Posted)]
    [DataRow(LoadConfirmationStatus.PendingSignature, LoadConfirmationStatus.PendingSignature)]
    [DataRow(LoadConfirmationStatus.PendingSignature, LoadConfirmationStatus.Open)]
    [DataRow(LoadConfirmationStatus.PendingSignature, LoadConfirmationStatus.Rejected)]
    [DataRow(LoadConfirmationStatus.PendingSignature, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(LoadConfirmationStatus.PendingSignature, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(LoadConfirmationStatus.PendingSignature, LoadConfirmationStatus.Unknown)]
    [DataRow(LoadConfirmationStatus.PendingSignature, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(LoadConfirmationStatus.PendingSignature, LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(LoadConfirmationStatus.Open, LoadConfirmationStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Open, LoadConfirmationStatus.PendingSignature)]
    [DataRow(LoadConfirmationStatus.Open, LoadConfirmationStatus.Open)]
    [DataRow(LoadConfirmationStatus.Open, LoadConfirmationStatus.Rejected)]
    [DataRow(LoadConfirmationStatus.Open, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(LoadConfirmationStatus.Open, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(LoadConfirmationStatus.Open, LoadConfirmationStatus.Unknown)]
    [DataRow(LoadConfirmationStatus.Open, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(LoadConfirmationStatus.Open, LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(LoadConfirmationStatus.Rejected, LoadConfirmationStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Rejected, LoadConfirmationStatus.PendingSignature)]
    [DataRow(LoadConfirmationStatus.Rejected, LoadConfirmationStatus.Open)]
    [DataRow(LoadConfirmationStatus.Rejected, LoadConfirmationStatus.Rejected)]
    [DataRow(LoadConfirmationStatus.Rejected, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(LoadConfirmationStatus.Rejected, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(LoadConfirmationStatus.Rejected, LoadConfirmationStatus.Unknown)]
    [DataRow(LoadConfirmationStatus.Rejected, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(LoadConfirmationStatus.Rejected, LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(LoadConfirmationStatus.SignatureVerified, LoadConfirmationStatus.Posted)]
    [DataRow(LoadConfirmationStatus.SignatureVerified, LoadConfirmationStatus.PendingSignature)]
    [DataRow(LoadConfirmationStatus.SignatureVerified, LoadConfirmationStatus.Open)]
    [DataRow(LoadConfirmationStatus.SignatureVerified, LoadConfirmationStatus.Rejected)]
    [DataRow(LoadConfirmationStatus.SignatureVerified, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(LoadConfirmationStatus.SignatureVerified, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(LoadConfirmationStatus.SignatureVerified, LoadConfirmationStatus.Unknown)]
    [DataRow(LoadConfirmationStatus.SignatureVerified, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(LoadConfirmationStatus.SignatureVerified, LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(LoadConfirmationStatus.SubmittedToGateway, LoadConfirmationStatus.Posted)]
    [DataRow(LoadConfirmationStatus.SubmittedToGateway, LoadConfirmationStatus.PendingSignature)]
    [DataRow(LoadConfirmationStatus.SubmittedToGateway, LoadConfirmationStatus.Open)]
    [DataRow(LoadConfirmationStatus.SubmittedToGateway, LoadConfirmationStatus.Rejected)]
    [DataRow(LoadConfirmationStatus.SubmittedToGateway, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(LoadConfirmationStatus.SubmittedToGateway, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(LoadConfirmationStatus.SubmittedToGateway, LoadConfirmationStatus.Unknown)]
    [DataRow(LoadConfirmationStatus.SubmittedToGateway, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(LoadConfirmationStatus.SubmittedToGateway, LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(LoadConfirmationStatus.Unknown, LoadConfirmationStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Unknown, LoadConfirmationStatus.PendingSignature)]
    [DataRow(LoadConfirmationStatus.Unknown, LoadConfirmationStatus.Open)]
    [DataRow(LoadConfirmationStatus.Unknown, LoadConfirmationStatus.Rejected)]
    [DataRow(LoadConfirmationStatus.Unknown, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(LoadConfirmationStatus.Unknown, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(LoadConfirmationStatus.Unknown, LoadConfirmationStatus.Unknown)]
    [DataRow(LoadConfirmationStatus.Unknown, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(LoadConfirmationStatus.Unknown, LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(LoadConfirmationStatus.WaitingForInvoice, LoadConfirmationStatus.Posted)]
    [DataRow(LoadConfirmationStatus.WaitingForInvoice, LoadConfirmationStatus.PendingSignature)]
    [DataRow(LoadConfirmationStatus.WaitingForInvoice, LoadConfirmationStatus.Open)]
    [DataRow(LoadConfirmationStatus.WaitingForInvoice, LoadConfirmationStatus.Rejected)]
    [DataRow(LoadConfirmationStatus.WaitingForInvoice, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(LoadConfirmationStatus.WaitingForInvoice, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(LoadConfirmationStatus.WaitingForInvoice, LoadConfirmationStatus.Unknown)]
    [DataRow(LoadConfirmationStatus.WaitingForInvoice, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(LoadConfirmationStatus.WaitingForInvoice, LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(LoadConfirmationStatus.WaitingSignatureValidation, LoadConfirmationStatus.Posted)]
    [DataRow(LoadConfirmationStatus.WaitingSignatureValidation, LoadConfirmationStatus.PendingSignature)]
    [DataRow(LoadConfirmationStatus.WaitingSignatureValidation, LoadConfirmationStatus.Open)]
    [DataRow(LoadConfirmationStatus.WaitingSignatureValidation, LoadConfirmationStatus.Rejected)]
    [DataRow(LoadConfirmationStatus.WaitingSignatureValidation, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(LoadConfirmationStatus.WaitingSignatureValidation, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(LoadConfirmationStatus.WaitingSignatureValidation, LoadConfirmationStatus.Unknown)]
    [DataRow(LoadConfirmationStatus.WaitingSignatureValidation, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(LoadConfirmationStatus.WaitingSignatureValidation, LoadConfirmationStatus.WaitingSignatureValidation)]
    public async Task LoadConfirmationValidationRules_ChangeStatus_NotVoid_ShouldPass(LoadConfirmationStatus targetStatus, LoadConfirmationStatus originStatus)
    {
        // arrange
        var scope = new DefaultScope();
        var target = GenFu.GenFu.New<LoadConfirmationEntity>();
        var origin = target.Clone();
        target.Status = targetStatus;
        origin.Status = originStatus;
        var context = scope.CreateContextWithValidLoadConfirmationEntity(target, origin);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        Assert.IsFalse(context.ContextBag.ContainsKey(ResultKey));
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.PendingSignature, SalesLineStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Open, SalesLineStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Rejected, SalesLineStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SignatureVerified, SalesLineStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SubmittedToGateway, SalesLineStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Unknown, SalesLineStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingForInvoice, SalesLineStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingSignatureValidation, SalesLineStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.PendingSignature, SalesLineStatus.Approved)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Open, SalesLineStatus.Approved)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Rejected, SalesLineStatus.Approved)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SignatureVerified, SalesLineStatus.Approved)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SubmittedToGateway, SalesLineStatus.Approved)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Unknown, SalesLineStatus.Approved)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingForInvoice, SalesLineStatus.Approved)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingSignatureValidation, SalesLineStatus.Approved)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.PendingSignature, SalesLineStatus.Exception)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Open, SalesLineStatus.Exception)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Rejected, SalesLineStatus.Exception)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SignatureVerified, SalesLineStatus.Exception)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SubmittedToGateway, SalesLineStatus.Exception)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Unknown, SalesLineStatus.Exception)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingForInvoice, SalesLineStatus.Exception)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingSignatureValidation, SalesLineStatus.Exception)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.PendingSignature, SalesLineStatus.Preview)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Open, SalesLineStatus.Preview)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Rejected, SalesLineStatus.Preview)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SignatureVerified, SalesLineStatus.Preview)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SubmittedToGateway, SalesLineStatus.Preview)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Unknown, SalesLineStatus.Preview)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingForInvoice, SalesLineStatus.Preview)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingSignatureValidation, SalesLineStatus.Preview)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.PendingSignature, SalesLineStatus.SentToFo)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Open, SalesLineStatus.SentToFo)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Rejected, SalesLineStatus.SentToFo)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SignatureVerified, SalesLineStatus.SentToFo)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SubmittedToGateway, SalesLineStatus.SentToFo)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Unknown, SalesLineStatus.SentToFo)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingForInvoice, SalesLineStatus.SentToFo)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingSignatureValidation, SalesLineStatus.SentToFo)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.PendingSignature, SalesLineStatus.Unspecified)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Open, SalesLineStatus.Unspecified)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Rejected, SalesLineStatus.Unspecified)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SignatureVerified, SalesLineStatus.Unspecified)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SubmittedToGateway, SalesLineStatus.Unspecified)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Unknown, SalesLineStatus.Unspecified)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingForInvoice, SalesLineStatus.Unspecified)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingSignatureValidation, SalesLineStatus.Unspecified)]
    public async Task LoadConfirmationValidationRules_VoidStatus_ActiveSalesLines_ShouldFail(LoadConfirmationStatus targetStatus, LoadConfirmationStatus originStatus, SalesLineStatus slStatus)
    {
        // arrange
        var scope = new DefaultScope();
        var target = GenFu.GenFu.New<LoadConfirmationEntity>();
        var origin = target.Clone();
        target.Status = targetStatus;
        origin.Status = originStatus;
        var context = scope.CreateContextWithValidLoadConfirmationEntity(target, origin);
        var validationResults = new List<ValidationResult>();
        var activeSalesLines = GenFu.GenFu.ListOf<SalesLineEntity>(5);
        activeSalesLines.ForEach(x =>
                                 {
                                     x.Status = slStatus;
                                     x.LoadConfirmationId = target.Id;
                                 });

        scope.SalesLineProviderMock.SetupEntities(activeSalesLines);
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        Assert.IsTrue(context.ContextBag.ContainsKey(ResultKey));
        context.ContextBag.TryGetValue(ResultKey, out var isActiveSalesLinesForLoadConfirmation);
        Assert.IsTrue(isActiveSalesLinesForLoadConfirmation != null && (bool)isActiveSalesLinesForLoadConfirmation);
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.LoadConfirmationVoid_ActiveSalesLines_Exist));
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.PendingSignature, SalesLineStatus.Void)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Open, SalesLineStatus.Void)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Rejected, SalesLineStatus.Void)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SignatureVerified, SalesLineStatus.Void)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.SubmittedToGateway, SalesLineStatus.Void)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.Unknown, SalesLineStatus.Void)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingForInvoice, SalesLineStatus.Void)]
    [DataRow(LoadConfirmationStatus.Void, LoadConfirmationStatus.WaitingSignatureValidation, SalesLineStatus.Void)]
    public async Task LoadConfirmationValidationRules_VoidStatus_InActiveSalesLines_ShouldPass(LoadConfirmationStatus targetStatus, LoadConfirmationStatus originStatus, SalesLineStatus slStatus)
    {
        // arrange
        var scope = new DefaultScope();
        var target = GenFu.GenFu.New<LoadConfirmationEntity>();
        var origin = target.Clone();
        target.Status = targetStatus;
        origin.Status = originStatus;
        var context = scope.CreateContextWithValidLoadConfirmationEntity(target, origin);
        var validationResults = new List<ValidationResult>();
        var activeSalesLines = GenFu.GenFu.ListOf<SalesLineEntity>(5);
        activeSalesLines.ForEach(x =>
                                 {
                                     x.Status = slStatus;
                                     x.LoadConfirmationId = target.Id;
                                 });

        scope.SalesLineProviderMock.SetupEntities(activeSalesLines);
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        Assert.IsTrue(context.ContextBag.ContainsKey(ResultKey));
        context.ContextBag.TryGetValue(ResultKey, out var isActiveSalesLineForInvoice);
        Assert.IsFalse(isActiveSalesLineForInvoice != null && (bool)isActiveSalesLineForInvoice);
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .NotContain(TTErrorCodes.LoadConfirmationVoid_ActiveSalesLines_Exist);
    }

    private class DefaultScope : TestScope<LoadConfirmationValidationRules>
    {
        public readonly Mock<IProvider<Guid, SalesLineEntity>> SalesLineProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(SalesLineProviderMock.Object);
        }

        public BusinessContext<LoadConfirmationEntity> CreateContextWithValidLoadConfirmationEntity(LoadConfirmationEntity target, LoadConfirmationEntity origin = null)
        {
            return new(target, origin);
        }
    }
}
