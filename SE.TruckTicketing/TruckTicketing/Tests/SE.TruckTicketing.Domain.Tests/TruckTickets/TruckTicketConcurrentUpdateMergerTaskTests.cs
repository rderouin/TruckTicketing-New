using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

using Trident.Business;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.TruckTickets;

[TestClass]
public class TruckTicketConcurrentUpdateMergerTaskTests
{
    [TestMethod]
    public async Task Task_Should_Run_When_There_Is_Concurrent_Update_Violation_And_Attachments_Are_Different()
    {
        // arrange
        var scope = new DefaultScope();

        var target = GenFu.GenFu.New<TruckTicketEntity>();
        target.VersionTag = Guid.NewGuid().ToString();
        target.Attachments = GenFu.GenFu.ListOf<TruckTicketAttachmentEntity>();

        var original = target.Clone();
        original.VersionTag = Guid.NewGuid().ToString();
        original.Attachments = GenFu.GenFu.ListOf<TruckTicketAttachmentEntity>();

        var context = new BusinessContext<TruckTicketEntity>(target, original);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeTrue();
        scope.InstanceUnderTest.RunOrder.Should().BeNegative();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.BeforeUpdate);
    }

    [TestMethod]
    public async Task Task_Should_Not_Run_When_There_Is_Concurrent_Update_Violation_And_Attachments_Are_The_Same()
    {
        // arrange
        var scope = new DefaultScope();

        var target = GenFu.GenFu.New<TruckTicketEntity>();
        target.VersionTag = Guid.NewGuid().ToString();
        target.Attachments = GenFu.GenFu.ListOf<TruckTicketAttachmentEntity>();

        var original = target.Clone();
        original.VersionTag = Guid.NewGuid().ToString();

        var context = new BusinessContext<TruckTicketEntity>(target, original);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_Should_Not_Run_When_There_Is_No_Concurrent_Update_Violation()
    {
        // arrange
        var scope = new DefaultScope();

        var target = GenFu.GenFu.New<TruckTicketEntity>();
        target.VersionTag = Guid.NewGuid().ToString();
        target.Attachments = GenFu.GenFu.ListOf<TruckTicketAttachmentEntity>();

        var original = target.Clone();

        var context = new BusinessContext<TruckTicketEntity>(target, original);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_Should_Merge_Attachments_With_No_Deletes()
    {
        // arrange
        var scope = new DefaultScope();

        var target = GenFu.GenFu.New<TruckTicketEntity>();
        target.VersionTag = Guid.NewGuid().ToString();
        target.Attachments = new();

        var original = target.Clone();
        original.VersionTag = Guid.NewGuid().ToString();
        original.Attachments = GenFu.GenFu.ListOf<TruckTicketAttachmentEntity>(3);

        var context = new BusinessContext<TruckTicketEntity>(target, original);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        target.Attachments.Select(attachment => attachment.Id).Should().BeEquivalentTo(original.Attachments.Select(attachment => attachment.Id));
    }

    [TestMethod]
    public async Task Task_Should_Update_Proper_Attachment_Types_If_Mismatch_Found_in_Attachment_Type()
    {
        // arrange
        var scope = new DefaultScope();

        string ticketNumber = "WEFST10001004-WT";
        TruckTicketAttachmentEntity invalidAtachment = new TruckTicketAttachmentEntity
        {
            AttachmentType = Contracts.Lookups.AttachmentType.Internal,
            File = $"{ticketNumber}-EXT.pdf"
        };

        var target = GenFu.GenFu.New<TruckTicketEntity>();
        target.VersionTag = Guid.NewGuid().ToString();
        target.Attachments = new();
        target.Attachments.Add(invalidAtachment);
       
        target.CountryCode = Contracts.Lookups.CountryCode.US;
        target.TruckTicketType = Contracts.Lookups.TruckTicketType.WT;
        

        var original = target.Clone();
        original.VersionTag = Guid.NewGuid().ToString();
        original.Attachments = GenFu.GenFu.ListOf<TruckTicketAttachmentEntity>(3);
        original.TicketNumber = ticketNumber;

        var context = new BusinessContext<TruckTicketEntity>(target, original);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        target.Attachments.Where(f=>f.File.Contains("-EXT") && f.AttachmentType == Contracts.Lookups.AttachmentType.Internal).Any().Should().BeFalse();
    }

    private class DefaultScope : TestScope<TruckTicketConcurrentUpdateMergerTask>
    {
        public Mock<ILogger<TruckTicketConcurrentUpdateMergerTask>> LoggerMock { get; } = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(LoggerMock.Object);
        }
    }
}
