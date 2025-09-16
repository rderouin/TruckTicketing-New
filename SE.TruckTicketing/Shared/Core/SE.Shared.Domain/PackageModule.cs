using System.Linq;
using System.Runtime.CompilerServices;

using Autofac;

using SE.Shared.Domain.EmailTemplates;
using SE.Shared.Domain.Entities.Changes;
using SE.Shared.Domain.Entities.InvoiceDelivery;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Infrastructure;
using SE.Shared.Domain.Tasks;
using SE.TridentContrib.Extensions.Azure.Functions;
using SE.TridentContrib.Extensions.Azure.KeyVault;
using SE.TridentContrib.Extensions.Azure.ServiceBus.ReEnqueue;
using SE.TridentContrib.Extensions.Pdf;
using SE.TridentContrib.Extensions.Threading;

using Trident.Configuration;
using Trident.Contracts.Changes;
using Trident.Contracts.Configuration;
using Trident.EFCore.Changes;
using Trident.IoC;
using Trident.Mapper;
using Trident.Workflow;

[assembly: InternalsVisibleTo("SE.TruckTicketing.Domain.Tests")]
[assembly: InternalsVisibleTo("SE.Shared.Domain.Tests")]

namespace SE.Shared.Domain;

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
        builder.UsingTridentMapperProfiles(TargetAssemblies);
        RegisterAuditWorkflowTask(builder);
        RegisterEntityUpdatePublisherWorkflowTask(builder);
        RegisterPopulateSearchableIdWorkflowTask(builder);
        RegisterOptimisticConcurrencyUpdateViolationWorkflowTask(builder);
        RegisterMonthlyDocumentPartitionerWorkflowTask(builder);
        RegisterInfra(builder);
        RegisterChangeInfra(builder);
        builder.Register<SalesLinesPublisher, ISalesLinesPublisher>();
        builder.Register<SalesOrderPublisher, ISalesOrderPublisher>();
        builder.Register<FunctionContextAccessor, IFunctionContextAccessor>();
        builder.Register<EntityPublisher, IEntityPublisher>();
        builder.Register<DistributedRateLimiter, IDistributedRateLimiter>();
        builder.Register<PdfMerger, IPdfMerger>();
        builder.Register<ServiceBusReEnqueueStrategyFactory, IServiceBusReEnqueueStrategyFactory>();
        builder.Register<ServiceBusMessageEnqueuer, IServiceBusMessageEnqueuer>();
        RegisterEntityPublishMessageTask(builder);
    }

    private void RegisterChangeInfra(IIoCProvider builder)
    {
        // async outbound
        builder.Register<ChangeObserver, IChangeObserver>();
        builder.Register<ChangePublisher, IChangePublisher>();
        builder.RegisterBehavior(CreateChangeServiceBus);

        // async inbound
        builder.Register<ChangeOrchestrator, IChangeOrchestrator>();
        builder.Register<ChangeService, IChangeService>();
        builder.Register<ChangeComparer, IChangeComparer>();
        builder.RegisterBehavior(ConstructChangeBlobStorage);

        IChangeServiceBus CreateChangeServiceBus(IIoCServiceLocator context)
        {
            var appSettings = context.Get<IAppSettings>();
            var connectionString = appSettings.ConnectionStrings[ConnectionStrings.PrivateServiceBusNamespace].ConnectionString;
            return new ChangeServiceBus(connectionString, appSettings);
        }

        IChangeBlobStorage ConstructChangeBlobStorage(IIoCServiceLocator context)
        {
            var appSettings = context.Get<IAppSettings>();
            var connectionString = appSettings.ConnectionStrings[ConnectionStrings.DocumentStorageAccount].ConnectionString;
            var containerName = appSettings[$"{nameof(ChangeBlobStorage)}Container"] ?? "changes";
            return new ChangeBlobStorage(connectionString, containerName);
        }
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

    private void RegisterAuditWorkflowTask(IIoCProvider builder)
    {
        var autofacBuilder = (AutofacIoCProvider)builder;

        autofacBuilder.Builder!
                      .RegisterGeneric(typeof(TTAuditableEntityAuditTask<>))
                      .As(typeof(IWorkflowTask<>));
    }

    private void RegisterEntityUpdatePublisherWorkflowTask(IIoCProvider builder)
    {
        var autofacBuilder = (AutofacIoCProvider)builder;

        autofacBuilder.Builder!
                      .RegisterGeneric(typeof(EntityUpdatePublisherTask<>))
                      .As(typeof(IWorkflowTask<>));
    }

    private void RegisterOptimisticConcurrencyUpdateViolationWorkflowTask(IIoCProvider builder)
    {
        var autofacBuilder = (AutofacIoCProvider)builder;

        autofacBuilder.Builder!
                      .RegisterGeneric(typeof(OptimisticConcurrencyViolationCheckerTask<>))
                      .As(typeof(IWorkflowTask<>));
    }

    private void RegisterPopulateSearchableIdWorkflowTask(IIoCProvider builder)
    {
        var autofacBuilder = (AutofacIoCProvider)builder;

        autofacBuilder.Builder!
                      .RegisterGeneric(typeof(PopulateSearchableIdTask<>))
                      .As(typeof(IWorkflowTask<>));
    }

    private void RegisterMonthlyDocumentPartitionerWorkflowTask(IIoCProvider builder)
    {
        var autofacBuilder = (AutofacIoCProvider)builder;

        autofacBuilder.Builder!
                      .RegisterGeneric(typeof(GenericDocumentPartitionerTask<>))
                      .As(typeof(IWorkflowTask<>));
    }

    private void RegisterInfra(IIoCProvider builder)
    {
        builder.RegisterBehavior(ConstructSignatureUploadBlobStorage);
        builder.RegisterBehavior(ConstructTtScannedAttachmentBlobStorage);
        builder.RegisterBehavior(ConstructAccountAttachmentBlobStorage);
        builder.RegisterBehavior(ConstructInvoiceAttachmentsBlobStorage);
        builder.RegisterBehavior(ConstructInvoiceDeliveryTransportBlobStorage);
        builder.RegisterBehavior(CreateIntegrationsServiceBus);
        builder.RegisterBehavior(CreateInvoiceDeliveryServiceBus);
        builder.Register<RazorEngineTemplateRenderer, IEmailTemplateRenderer>();
        builder.Register<EmailTemplateSender, IEmailTemplateSender>();
        builder.Register<SmtpClientProvider, ISmtpClientProvider>();
        builder.RegisterBehavior(ConstructTruckTicketAttachmentsBlobStorage);
        builder.Register<KeyVault, IKeyVault>();
        builder.RegisterBehavior(ConstructBlobLeaseStorage);

        ITruckTicketUploadBlobStorage ConstructTruckTicketAttachmentsBlobStorage(IIoCServiceLocator context)
        {
            var appSettings = context.Get<IAppSettings>();
            var connectionString = appSettings.ConnectionStrings[ConnectionStrings.DocumentStorageAccount].ConnectionString;
            var containerName = appSettings.GetKeyOrDefault(nameof(TruckTicketBlobStorage) + "Container", "tt-attachments");
            return new TruckTicketBlobStorage(connectionString, containerName);
        }

        ISignatureUploadBlobStorage ConstructSignatureUploadBlobStorage(IIoCServiceLocator context)
        {
            var appSettings = context.Get<IAppSettings>();
            var connectionString = appSettings.ConnectionStrings[ConnectionStrings.DocumentStorageAccount].ConnectionString;
            var containerName = appSettings.GetKeyOrDefault(nameof(SignatureUploadBlobStorage) + "Container", "user-signatures");
            return new SignatureUploadBlobStorage(connectionString, containerName);
        }

        ITtScannedAttachmentBlobStorage ConstructTtScannedAttachmentBlobStorage(IIoCServiceLocator context)
        {
            var appSettings = context.Get<IAppSettings>();
            var connectionString = appSettings.ConnectionStrings[ConnectionStrings.DocumentStorageAccount].ConnectionString;
            var containerName = appSettings.GetKeyOrDefault(nameof(TtScannedAttachmentBlobStorage) + "Container", "tt-attachments");
            return new TtScannedAttachmentBlobStorage(connectionString, containerName);
        }

        IAccountAttachmentsBlobStorage ConstructAccountAttachmentBlobStorage(IIoCServiceLocator context)
        {
            var appSettings = context.Get<IAppSettings>();
            var connectionString = appSettings.ConnectionStrings[ConnectionStrings.DocumentStorageAccount].ConnectionString;
            var containerName = appSettings.GetKeyOrDefault(nameof(AccountAttachmentsBlobStorage) + "Container", "tt-attachments");
            return new AccountAttachmentsBlobStorage(connectionString, containerName);
        }

        IInvoiceAttachmentsBlobStorage ConstructInvoiceAttachmentsBlobStorage(IIoCServiceLocator context)
        {
            var appSettings = context.Get<IAppSettings>();
            var connectionString = appSettings.ConnectionStrings[ConnectionStrings.DocumentStorageAccount].ConnectionString;
            var containerName = appSettings[nameof(InvoiceAttachmentsBlobStorage) + "Container"] ?? "invoice-delivery";
            return new InvoiceAttachmentsBlobStorage(connectionString, containerName);
        }

        IInvoiceDeliveryTransportBlobStorage ConstructInvoiceDeliveryTransportBlobStorage(IIoCServiceLocator context)
        {
            var appSettings = context.Get<IAppSettings>();
            var connectionString = appSettings.ConnectionStrings[ConnectionStrings.DocumentStorageAccount].ConnectionString;
            var containerName = appSettings[nameof(InvoiceDeliveryTransportBlobStorage) + "Container"] ?? "invoice-delivery";
            return new InvoiceDeliveryTransportBlobStorage(connectionString, containerName);
        }

        IIntegrationsServiceBus CreateIntegrationsServiceBus(IIoCServiceLocator context)
        {
            var appSettings = context.Get<IAppSettings>();
            var connectionString = appSettings.ConnectionStrings[ConnectionStrings.PrivateServiceBusNamespace].ConnectionString;
            return new IntegrationsServiceBus(connectionString);
        }

        IInvoiceDeliveryServiceBus CreateInvoiceDeliveryServiceBus(IIoCServiceLocator context)
        {
            var appSettings = context.Get<IAppSettings>();
            var connectionString = appSettings.ConnectionStrings[ConnectionStrings.PrivateServiceBusNamespace].ConnectionString;
            return new InvoiceDeliveryServiceBus(connectionString, appSettings);
        }

        ILeaseObjectBlobStorage ConstructBlobLeaseStorage(IIoCServiceLocator context)
        {
            var appSettings = context.Get<IAppSettings>();
            var connectionString = appSettings.ConnectionStrings[ConnectionStrings.PrivateStorageAccount].ConnectionString;
            var containerName = appSettings.GetKeyOrDefault(nameof(LeaseObjectBlobStorage) + "Container", "lease-objects");
            return new LeaseObjectBlobStorage(connectionString, containerName);
        }
    }
}
