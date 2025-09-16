using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.Invoices.Rules;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.Shared.Domain.Tests.Entities.Invoice;

[TestClass]
public class InvoiceValidationRulesTest : TestScope<InvoiceValidationRules>
{
    private const string ActiveLoadConfirmationsOnInvoice = nameof(ActiveLoadConfirmationsOnInvoice);

    private const string ActiveSalesLinesOnInvoice = nameof(ActiveSalesLinesOnInvoice);

    private const string ResultKey = ActiveLoadConfirmationsOnInvoice;

    [TestMethod]
    [TestCategory("Unit")]
    public void InvoiceValidationRules_CanBeInstantiated()
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
    [DataRow(InvoiceStatus.AgingUnSent)]
    [DataRow(InvoiceStatus.PaidPartial)]
    [DataRow(InvoiceStatus.PaidSettled)]
    [DataRow(InvoiceStatus.PaidUnSettled)]
    [DataRow(InvoiceStatus.Posted)]
    [DataRow(InvoiceStatus.PostedRejected)]
    [DataRow(InvoiceStatus.UnPosted)]
    public async Task InvoiceValidationRules_Status_NotVoid_ShouldPass(InvoiceStatus status)
    {
        // arrange
        var scope = new DefaultScope();
        var target = GenFu.GenFu.New<InvoiceEntity>();
        var origin = target.Clone();
        target.Status = status;
        var context = scope.CreateContextWithValidInvoiceEntity(target, origin);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        Assert.IsFalse(context.ContextBag.ContainsKey(ResultKey));
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .NotContain(TTErrorCodes.InvoiceVoid_ActiveLoadConfirmations_Exist);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.AgingUnSent)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidPartial)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidSettled)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidUnSettled)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.Posted)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PostedRejected)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.UnPosted)]
    public async Task InvoiceValidationRules_VoidStatus_NoActiveLoadConfirmation_ShouldPass(InvoiceStatus targetStatus, InvoiceStatus originStatus)
    {
        // arrange
        var scope = new DefaultScope();
        var target = GenFu.GenFu.New<InvoiceEntity>();
        var origin = target.Clone();
        target.Status = targetStatus;
        origin.Status = originStatus;
        var context = scope.CreateContextWithValidInvoiceEntity(target, origin);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        Assert.IsTrue(context.ContextBag.ContainsKey(ResultKey));
        context.ContextBag.TryGetValue(ResultKey, out var isActiveLoadConfirmationForInvoice);
        Assert.IsFalse(isActiveLoadConfirmationForInvoice != null && (bool)isActiveLoadConfirmationForInvoice);
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .NotContain(TTErrorCodes.InvoiceVoid_ActiveLoadConfirmations_Exist);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(InvoiceStatus.AgingUnSent, InvoiceStatus.AgingUnSent)]
    [DataRow(InvoiceStatus.AgingUnSent, InvoiceStatus.PaidPartial)]
    [DataRow(InvoiceStatus.AgingUnSent, InvoiceStatus.PaidSettled)]
    [DataRow(InvoiceStatus.AgingUnSent, InvoiceStatus.PaidUnSettled)]
    [DataRow(InvoiceStatus.AgingUnSent, InvoiceStatus.Posted)]
    [DataRow(InvoiceStatus.AgingUnSent, InvoiceStatus.PostedRejected)]
    [DataRow(InvoiceStatus.AgingUnSent, InvoiceStatus.UnPosted)]
    [DataRow(InvoiceStatus.PaidPartial, InvoiceStatus.AgingUnSent)]
    [DataRow(InvoiceStatus.PaidPartial, InvoiceStatus.PaidPartial)]
    [DataRow(InvoiceStatus.PaidPartial, InvoiceStatus.PaidSettled)]
    [DataRow(InvoiceStatus.PaidPartial, InvoiceStatus.PaidUnSettled)]
    [DataRow(InvoiceStatus.PaidPartial, InvoiceStatus.Posted)]
    [DataRow(InvoiceStatus.PaidPartial, InvoiceStatus.PostedRejected)]
    [DataRow(InvoiceStatus.PaidPartial, InvoiceStatus.UnPosted)]
    [DataRow(InvoiceStatus.PaidSettled, InvoiceStatus.AgingUnSent)]
    [DataRow(InvoiceStatus.PaidSettled, InvoiceStatus.PaidPartial)]
    [DataRow(InvoiceStatus.PaidSettled, InvoiceStatus.PaidSettled)]
    [DataRow(InvoiceStatus.PaidSettled, InvoiceStatus.PaidUnSettled)]
    [DataRow(InvoiceStatus.PaidSettled, InvoiceStatus.Posted)]
    [DataRow(InvoiceStatus.PaidSettled, InvoiceStatus.PostedRejected)]
    [DataRow(InvoiceStatus.PaidSettled, InvoiceStatus.UnPosted)]
    [DataRow(InvoiceStatus.PaidUnSettled, InvoiceStatus.AgingUnSent)]
    [DataRow(InvoiceStatus.PaidUnSettled, InvoiceStatus.PaidPartial)]
    [DataRow(InvoiceStatus.PaidUnSettled, InvoiceStatus.PaidSettled)]
    [DataRow(InvoiceStatus.PaidUnSettled, InvoiceStatus.PaidUnSettled)]
    [DataRow(InvoiceStatus.PaidUnSettled, InvoiceStatus.Posted)]
    [DataRow(InvoiceStatus.PaidUnSettled, InvoiceStatus.PostedRejected)]
    [DataRow(InvoiceStatus.PaidUnSettled, InvoiceStatus.UnPosted)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.AgingUnSent)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.PaidPartial)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.PaidSettled)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.PaidUnSettled)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.Posted)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.PostedRejected)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.UnPosted)]
    [DataRow(InvoiceStatus.PostedRejected, InvoiceStatus.AgingUnSent)]
    [DataRow(InvoiceStatus.PostedRejected, InvoiceStatus.PaidPartial)]
    [DataRow(InvoiceStatus.PostedRejected, InvoiceStatus.PaidSettled)]
    [DataRow(InvoiceStatus.PostedRejected, InvoiceStatus.PaidUnSettled)]
    [DataRow(InvoiceStatus.PostedRejected, InvoiceStatus.Posted)]
    [DataRow(InvoiceStatus.PostedRejected, InvoiceStatus.PostedRejected)]
    [DataRow(InvoiceStatus.PostedRejected, InvoiceStatus.UnPosted)]
    [DataRow(InvoiceStatus.UnPosted, InvoiceStatus.AgingUnSent)]
    [DataRow(InvoiceStatus.UnPosted, InvoiceStatus.PaidPartial)]
    [DataRow(InvoiceStatus.UnPosted, InvoiceStatus.PaidSettled)]
    [DataRow(InvoiceStatus.UnPosted, InvoiceStatus.PaidUnSettled)]
    [DataRow(InvoiceStatus.UnPosted, InvoiceStatus.Posted)]
    [DataRow(InvoiceStatus.UnPosted, InvoiceStatus.PostedRejected)]
    [DataRow(InvoiceStatus.UnPosted, InvoiceStatus.UnPosted)]
    public async Task InvoiceValidationRules_ChangeStatus_NotVoid_ShouldPass(InvoiceStatus targetStatus, InvoiceStatus originStatus)
    {
        // arrange
        var scope = new DefaultScope();
        var target = GenFu.GenFu.New<InvoiceEntity>();
        var origin = target.Clone();
        target.Status = targetStatus;
        origin.Status = originStatus;
        var context = scope.CreateContextWithValidInvoiceEntity(target, origin);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        Assert.IsFalse(context.ContextBag.ContainsKey(ResultKey));
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .NotContain(TTErrorCodes.InvoiceVoid_ActiveLoadConfirmations_Exist);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.AgingUnSent, LoadConfirmationStatus.Open)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidPartial, LoadConfirmationStatus.Open)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidSettled, LoadConfirmationStatus.Open)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidUnSettled, LoadConfirmationStatus.Open)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.Posted, LoadConfirmationStatus.Open)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PostedRejected, LoadConfirmationStatus.Open)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.UnPosted, LoadConfirmationStatus.Open)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.AgingUnSent, LoadConfirmationStatus.PendingSignature)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidPartial, LoadConfirmationStatus.PendingSignature)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidSettled, LoadConfirmationStatus.PendingSignature)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidUnSettled, LoadConfirmationStatus.PendingSignature)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.Posted, LoadConfirmationStatus.PendingSignature)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PostedRejected, LoadConfirmationStatus.PendingSignature)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.UnPosted, LoadConfirmationStatus.PendingSignature)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.AgingUnSent, LoadConfirmationStatus.Rejected)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidPartial, LoadConfirmationStatus.Rejected)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidSettled, LoadConfirmationStatus.Rejected)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidUnSettled, LoadConfirmationStatus.Rejected)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.Posted, LoadConfirmationStatus.Rejected)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PostedRejected, LoadConfirmationStatus.Rejected)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.UnPosted, LoadConfirmationStatus.Rejected)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.AgingUnSent, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidPartial, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidSettled, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidUnSettled, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.Posted, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PostedRejected, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.UnPosted, LoadConfirmationStatus.SignatureVerified)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.AgingUnSent, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidPartial, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidSettled, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidUnSettled, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.Posted, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PostedRejected, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.UnPosted, LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.AgingUnSent, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidPartial, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidSettled, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidUnSettled, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.Posted, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PostedRejected, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.UnPosted, LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.AgingUnSent, LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidPartial, LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidSettled, LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidUnSettled, LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.Posted, LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PostedRejected, LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.UnPosted, LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.AgingUnSent, LoadConfirmationStatus.Posted)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidPartial, LoadConfirmationStatus.Posted)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidSettled, LoadConfirmationStatus.Posted)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidUnSettled, LoadConfirmationStatus.Posted)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.Posted, LoadConfirmationStatus.Posted)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PostedRejected, LoadConfirmationStatus.Posted)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.UnPosted, LoadConfirmationStatus.Posted)]
    public async Task InvoiceValidationRules_VoidStatus_ActiveLoadConfirmation_ShouldFail(InvoiceStatus targetStatus, InvoiceStatus originStatus, LoadConfirmationStatus lcStatus)
    {
        // arrange
        var scope = new DefaultScope();
        var target = GenFu.GenFu.New<InvoiceEntity>();
        var origin = target.Clone();
        target.Status = targetStatus;
        origin.Status = originStatus;
        var context = scope.CreateContextWithValidInvoiceEntity(target, origin);
        var validationResults = new List<ValidationResult>();
        var activeLoadConfirmations = GenFu.GenFu.ListOf<LoadConfirmationEntity>(5);
        activeLoadConfirmations.ForEach(x =>
                                        {
                                            x.Status = lcStatus;
                                            x.InvoiceId = target.Id;
                                        });

        scope.LoadConfirmationProviderMock.SetupEntities(activeLoadConfirmations);
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        Assert.IsTrue(context.ContextBag.ContainsKey(ResultKey));
        context.ContextBag.TryGetValue(ResultKey, out var isActiveLoadConfirmationForInvoice);
        Assert.IsTrue(isActiveLoadConfirmationForInvoice != null && (bool)isActiveLoadConfirmationForInvoice);
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.InvoiceVoid_ActiveLoadConfirmations_Exist));
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.AgingUnSent, LoadConfirmationStatus.Void)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidPartial, LoadConfirmationStatus.Void)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidSettled, LoadConfirmationStatus.Void)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PaidUnSettled, LoadConfirmationStatus.Void)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.Posted, LoadConfirmationStatus.Void)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.PostedRejected, LoadConfirmationStatus.Void)]
    [DataRow(InvoiceStatus.Void, InvoiceStatus.UnPosted, LoadConfirmationStatus.Void)]
    public async Task InvoiceValidationRules_VoidStatus_InActiveLoadConfirmation_ShouldPass(InvoiceStatus targetStatus, InvoiceStatus originStatus, LoadConfirmationStatus lcStatus)
    {
        // arrange
        var scope = new DefaultScope();
        var target = GenFu.GenFu.New<InvoiceEntity>();
        var origin = target.Clone();
        target.Status = targetStatus;
        origin.Status = originStatus;
        var context = scope.CreateContextWithValidInvoiceEntity(target, origin);
        var validationResults = new List<ValidationResult>();
        var activeLoadConfirmations = GenFu.GenFu.ListOf<LoadConfirmationEntity>(5);
        activeLoadConfirmations.ForEach(x =>
                                        {
                                            x.Status = lcStatus;
                                            x.InvoiceId = target.Id;
                                        });

        scope.LoadConfirmationProviderMock.SetupEntities(activeLoadConfirmations);
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        Assert.IsTrue(context.ContextBag.ContainsKey(ResultKey));
        context.ContextBag.TryGetValue(ResultKey, out var isActiveLoadConfirmationForInvoice);
        Assert.IsFalse(isActiveLoadConfirmationForInvoice != null && (bool)isActiveLoadConfirmationForInvoice);
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .NotContain(TTErrorCodes.InvoiceVoid_ActiveLoadConfirmations_Exist);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.UnPosted, SalesLineStatus.Approved)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.AgingUnSent, SalesLineStatus.Approved)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.PostedRejected, SalesLineStatus.Approved)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.PaidUnSettled, SalesLineStatus.Approved)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.PaidSettled, SalesLineStatus.Approved)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.PaidPartial, SalesLineStatus.Approved)]
    public async Task InvoiceValidationRules_PostedStatus_ActiveSalesLines_ShouldPass(InvoiceStatus targetStatus, InvoiceStatus originStatus, SalesLineStatus slStatus)
    {
        // arrange
        var scope = new DefaultScope();
        var target = GenFu.GenFu.New<InvoiceEntity>();
        var origin = target.Clone();
        target.Status = targetStatus;
        origin.Status = originStatus;
        var context = scope.CreateContextWithValidInvoiceEntity(target, origin);
        var validationResults = new List<ValidationResult>();
        var activeSalesLines = GenFu.GenFu.ListOf<SalesLineEntity>(5);
        activeSalesLines.ForEach(x =>
        {
            x.Status = slStatus;
            x.InvoiceId = target.Id;
        });

        scope.SalesLineProviderMock.SetupEntities(activeSalesLines);
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        Assert.IsTrue(context.ContextBag.ContainsKey(ActiveSalesLinesOnInvoice));
        context.ContextBag.TryGetValue(ActiveSalesLinesOnInvoice, out var isActiveSalesLineForInvoice);
        Assert.IsTrue(isActiveSalesLineForInvoice != null && (bool)isActiveSalesLineForInvoice);
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .NotContain(TTErrorCodes.InvoicePosted_NoActiveSalesLines_Exist);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.UnPosted, SalesLineStatus.Void)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.AgingUnSent, SalesLineStatus.Void)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.PostedRejected, SalesLineStatus.Void)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.PaidUnSettled, SalesLineStatus.Void)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.PaidSettled, SalesLineStatus.Void)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.PaidPartial, SalesLineStatus.Void)]
    public async Task InvoiceValidationRules_PostedStatus_NoActiveSalesLines_ShouldFail(InvoiceStatus targetStatus, InvoiceStatus originStatus, SalesLineStatus slStatus)
    {
        // arrange
        var scope = new DefaultScope();
        var target = GenFu.GenFu.New<InvoiceEntity>();
        var origin = target.Clone();
        target.Status = targetStatus;
        origin.Status = originStatus;
        var context = scope.CreateContextWithValidInvoiceEntity(target, origin);
        var validationResults = new List<ValidationResult>();
        var activeSalesLines = GenFu.GenFu.ListOf<SalesLineEntity>(5);
        activeSalesLines.ForEach(x =>
        {
            x.Status = slStatus;
            x.InvoiceId = target.Id;
        });

        scope.SalesLineProviderMock.SetupEntities(activeSalesLines);
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        Assert.IsTrue(context.ContextBag.ContainsKey(ActiveSalesLinesOnInvoice));
        context.ContextBag.TryGetValue(ActiveSalesLinesOnInvoice, out var isActiveSalesLineForInvoice);
        Assert.IsFalse(isActiveSalesLineForInvoice != null && (bool)isActiveSalesLineForInvoice);
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .Contain(TTErrorCodes.InvoicePosted_NoActiveSalesLines_Exist);
    }

    private class DefaultScope : TestScope<InvoiceValidationRules>
    {
        public readonly Mock<IProvider<Guid, LoadConfirmationEntity>> LoadConfirmationProviderMock = new();

        public readonly Mock<IProvider<Guid, SalesLineEntity>> SalesLineProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(LoadConfirmationProviderMock.Object, SalesLineProviderMock.Object);
        }

        public BusinessContext<InvoiceEntity> CreateContextWithValidInvoiceEntity(InvoiceEntity target, InvoiceEntity origin = null)
        {
            return new(target, origin);
        }
    }
}
