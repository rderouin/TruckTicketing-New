using System.Linq;
using System.Runtime.CompilerServices;

using Autofac;

using SE.Shared.Domain;
using SE.Shared.Domain.EmailTemplates;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Tasks;
using SE.TruckTicketing.Domain.Entities.Invoices.InvoiceReversal;
using SE.TruckTicketing.Domain.Entities.Invoices.Tasks;
using SE.TruckTicketing.Domain.Entities.SalesLine;
using SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;
using SE.TruckTicketing.Domain.Infrastructure;
using SE.TruckTicketing.Domain.LocalReporting;

using Trident.Configuration;
using Trident.Contracts.Configuration;
using Trident.IoC;

[assembly: InternalsVisibleTo("SE.TruckTicketing.Domain.Tests")]

namespace SE.TruckTicketing.Domain;

public class PackageModule : IoCModule
{
    public override void Configure(IIoCProvider builder)
    {
        RegisterDefaultAssemblyScans(builder);
        builder.RegisterAll<ICoreConfiguration>(TargetAssemblies);
        builder.UsingTridentSearch(TargetAssemblies);
        builder.UsingTridentWorkflowManagers(TargetAssemblies);
        builder.UsingTridentValidationManagers(TargetAssemblies);
        builder.UsingTridentValidationRules(TargetAssemblies);
        builder.UsingTridentWorkflowTasks(TargetAssemblies);
        builder.Register<TruckTicketPdfRenderer, ITruckTicketPdfRenderer>();
        builder.Register<TruckTicketXlsxRenderer, ITruckTicketXlsxRenderer>();
        builder.Register<DefaultReportDefinitionResolver, IReportDefinitionResolver>();
        builder.Register<LoadConfirmationPdfRenderer, ILoadConfirmationPdfRenderer>();
        builder.Register<EmailTemplateAttachmentManager, IEmailTemplateAttachmentManager>();
        builder.Register<LoadConfirmationAttachmentManager, ILoadConfirmationAttachmentManager>();
        builder.Register<InvoiceReversalWorkflow, IInvoiceReversalWorkflow>();
        builder.Register<InvoiceWorkflowOrchestrator, IInvoiceWorkflowOrchestrator>();
        builder.Register<TruckTicketEffectiveDateSetterTask, ITruckTicketEffectiveDateService>();
        builder.Register<TruckTicketInvoiceService, ITruckTicketInvoiceService>();
        builder.Register<TruckTicketLoadConfirmationService, ITruckTicketLoadConfirmationService>();
        builder.Register<LoadConfirmationApprovalWorkflow, ILoadConfirmationApprovalWorkflow>();
        builder.Register<SalesLinesPublisher, ISalesLinesPublisher>();
        builder.Register<SalesOrderPublisher, ISalesOrderPublisher>();
        builder.Register<TruckTicketSalesManager, ITruckTicketSalesManager>();

        RegisterEntityPublishMessageTask(builder);

        builder.Register<EntityPublisher, IEntityPublisher>();

        RegisterBlobStorages(builder);
    }

    private void RegisterEntityPublishMessageTask(IIoCProvider builder)
    {
        var autofacBuilder = (AutofacIoCProvider)builder;

        autofacBuilder.Builder!
                      .RegisterAssemblyTypes(TargetAssemblies)
                      .Where(t => t.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEntityPublishMessageTask<>)))
                      .AsClosedTypesOf(typeof(IEntityPublishMessageTask<>))
                      .AsImplementedInterfaces()
                      .InstancePerLifetimeScope()
                      .AsSelf();
    }

    private void RegisterBlobStorages(IIoCProvider builder)
    {
        builder.RegisterBehavior(ConstructTradeAgreementBlobStorage);
        builder.RegisterBehavior(ConstructEmailTemplateAttachmentBlobStorage);

        ITradeAgreementUploadBlobStorage ConstructTradeAgreementBlobStorage(IIoCServiceLocator context)
        {
            var appSettings = context.Get<IAppSettings>();
            var connectionString = appSettings.ConnectionStrings[ConnectionStrings.DocumentStorageAccount].ConnectionString;
            var containerName = appSettings.GetKeyOrDefault(nameof(TradeAgreementUploadBlobStorage) + "Container", "trade-agreements");
            return new TradeAgreementUploadBlobStorage(connectionString, containerName);
        }

        IEmailTemplateAttachmentBlobStorage ConstructEmailTemplateAttachmentBlobStorage(IIoCServiceLocator context)
        {
            var appSettings = context.Get<IAppSettings>();
            var connectionString = appSettings.ConnectionStrings[ConnectionStrings.DocumentStorageAccount].ConnectionString;
            var containerName = appSettings.GetKeyOrDefault(nameof(EmailTemplateAttachmentBlobStorage) + "Container", "email-template-attachments");
            return new EmailTemplateAttachmentBlobStorage(connectionString, containerName);
        }
    }
}
