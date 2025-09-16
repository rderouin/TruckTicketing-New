using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain;
using SE.Shared.Domain.EmailTemplates;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.Note;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Pdf;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.Testing.TestScopes;
using Trident.Validation;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.Invoices;

[TestClass]
public class InvoiceManagerTests
{
    [TestMethod]
    public async Task InvoiceManager_ForSortedAttachments_AllCases()
    {
        // arrange
        var scope = new DefaultScope();
        var salesLines = new List<SalesLineEntity>
        {
            new()
            {
                Substance = "basic use case",
                Attachments = new()
                {
                    new()
                    {
                        AttachmentType = null,
                        File = "-ext",
                    },
                    new()
                    {
                        AttachmentType = null,
                        File = "-int",
                    },
                    new()
                    {
                        AttachmentType = AttachmentType.External,
                        File = "-ext",
                    },
                    new()
                    {
                        AttachmentType = AttachmentType.External,
                        File = "-int",
                    },
                    new()
                    {
                        AttachmentType = AttachmentType.Internal,
                        File = "-ext",
                    },
                    new()
                    {
                        AttachmentType = AttachmentType.Internal,
                        File = "-int",
                    },
                },
            },
        };

        var sortedAttachments = new List<SalesLineAttachmentEntity>();

        // act
        await scope.InstanceUnderTest.ForSortedAttachments(salesLines, sl =>
                                                                       {
                                                                           sortedAttachments.Add(sl);
                                                                           return Task.CompletedTask;
                                                                       });

        // assert
        sortedAttachments.Count.Should().Be(6);
    }

    public class DefaultScope : TestScope<InvoiceManager>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(Logger.Object, Provider.Object, SalesLineProvider.Object,
                                    BlobStorage.Object, NoteManager.Object, UserContextAccessor.Object,
                                    PdfMerger.Object, EmailTemplateSender.Object, MapperRegistry.Object,
                                    InvoiceConfigProvider.Object, AccountContactProvider.Object, AccountProvider.Object,
                                    LoadConfirmationProvider.Object, ValidationManager.Object, WorkflowManager.Object);
        }

        public Mock<ILog> Logger { get; } = new();

        public Mock<IProvider<Guid, InvoiceEntity>> Provider { get; } = new();

        public Mock<IProvider<Guid, SalesLineEntity>> SalesLineProvider { get; } = new();

        public Mock<IInvoiceAttachmentsBlobStorage> BlobStorage { get; } = new();

        public Mock<IManager<Guid, NoteEntity>> NoteManager { get; } = new();

        public Mock<IUserContextAccessor> UserContextAccessor { get; } = new();

        public Mock<IEntityPublisher> EntityPublisher { get; } = new();

        public Mock<IPdfMerger> PdfMerger { get; } = new();

        public Mock<IEmailTemplateSender> EmailTemplateSender { get; } = new();

        public Mock<IMapperRegistry> MapperRegistry { get; } = new();

        public Mock<IProvider<Guid, BillingConfigurationEntity>> BillingConfigProvider { get; } = new();

        public Mock<IProvider<Guid, InvoiceConfigurationEntity>> InvoiceConfigProvider { get; } = new();

        public Mock<IProvider<Guid, AccountContactIndexEntity>> AccountContactProvider { get; } = new();

        public Mock<IProvider<Guid, AccountEntity>> AccountProvider { get; } = new();

        public Mock<IProvider<Guid, LoadConfirmationEntity>> LoadConfirmationProvider { get; } = new();

        public Mock<IValidationManager<InvoiceEntity>> ValidationManager { get; } = new();

        public Mock<IWorkflowManager<InvoiceEntity>> WorkflowManager { get; } = new();
    }
}
