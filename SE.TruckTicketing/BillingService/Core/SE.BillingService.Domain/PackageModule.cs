using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Autofac;

using SE.BillingService.Domain.Integrations.OpenInvoice;
using SE.BillingService.Domain.InvoiceDelivery;
using SE.BillingService.Domain.InvoiceDelivery.Encoders;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx;
using SE.BillingService.Domain.InvoiceDelivery.Enrichment;
using SE.BillingService.Domain.InvoiceDelivery.Mapper;
using SE.BillingService.Domain.InvoiceDelivery.Transport;
using SE.BillingService.Domain.InvoiceDelivery.Validation;
using SE.Shared.Domain;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Azure.KeyVault;
using SE.TridentContrib.Extensions.Compression;

using Trident.Configuration;
using Trident.Contracts.Configuration;
using Trident.IoC;
using Trident.Logging;

namespace SE.BillingService.Domain;

public class PackageModule : IoCModule
{
    public override Assembly[] TargetAssemblies => new[] { GetType().Assembly, typeof(IFileCompressor).Assembly };

    public override void Configure(IIoCProvider builder)
    {
        var targetAssemblies = TargetAssemblies!;

        RegisterDefaultAssemblyScans(builder);
        builder.RegisterAll<ICoreConfiguration>(targetAssemblies);
        builder.UsingTridentSearch(targetAssemblies);
        builder.UsingTridentWorkflowManagers(targetAssemblies);
        builder.UsingTridentValidationManagers(targetAssemblies);
        builder.UsingTridentValidationRules(targetAssemblies);
        builder.UsingTridentWorkflowTasks(targetAssemblies);
        builder.RegisterBehavior(ConstructInvoiceAttachmentsBlobStorage);
        RegisterInvoiceDeliveryTypes(builder, targetAssemblies);
        RegisterFileCompressors(builder, targetAssemblies);
    }

    private IInvoiceAttachmentsBlobStorage ConstructInvoiceAttachmentsBlobStorage(IIoCServiceLocator context)
    {
        var appSettings = context.Get<IAppSettings>();
        var connectionString = appSettings.ConnectionStrings[ConnectionStrings.DocumentStorageAccount].ConnectionString;
        var containerName = appSettings[nameof(InvoiceAttachmentsBlobStorage) + "Container"] ?? "invoice-delivery";
        return new InvoiceAttachmentsBlobStorage(connectionString, containerName);
    }

    private void RegisterFileCompressors(IIoCProvider builder, Assembly[] targetAssemblies)
    {
        // the file compressor resolver will find the most appropriate conversion when requested by the content type
        builder.Register<FileCompressorResolver, IFileCompressorResolver>();

        // a single implementation might handle different variations of the same underlying type
        var autofacBuilder = (AutofacIoCProvider)builder;
        foreach (var type in targetAssemblies.SelectMany(a => a.GetTypes().Where(t => t.IsAssignableTo<IFileCompressor>() && !t.IsAbstract)))
        {
            // multiple attributes are allowed on the type to define different targets
            foreach (var contentType in type.GetCustomAttributes<FileCompressorAttribute>().Select(a => a.ContentType).Distinct())
            {
                autofacBuilder.Builder
                              .RegisterType(type)
                              .InstancePerLifetimeScope()
                              .Named<IFileCompressor>(contentType)
                              .AsImplementedInterfaces()
                              .AsSelf();
            }
        }
    }

    private void RegisterInvoiceDeliveryTypes(IIoCProvider builder, Assembly[] assemblies)
    {
        builder.Register<InvoiceDeliveryOrchestrator, IInvoiceDeliveryOrchestrator>();
        builder.Register<InvoiceDeliveryRequestValidator, IInvoiceDeliveryRequestValidator>();
        builder.Register<InvoiceDeliveryEnricher, IInvoiceDeliveryEnricher>();
        builder.Register<InvoiceDeliveryMapper, IInvoiceDeliveryMapper>();
        builder.Register<JsonFiddler, IJsonFiddler>();
        builder.Register<ExpressionManager, IExpressionManager>();
        builder.Register<DotNetCompiler, IDotNetCompiler>();
        builder.Register<InvoiceDeliveryMessageEncoderSelector, IInvoiceDeliveryMessageEncoderSelector>();
        builder.RegisterAll<IInvoiceDeliveryMessageEncoder>(assemblies);
        builder.RegisterAll<IPidxAdapter>(assemblies);
        builder.RegisterBehavior(CreateInvoiceDeliveryTransportStrategy);
        builder.RegisterAll<IInvoiceDeliveryTransport>(assemblies);
        builder.Register<OpenInvoiceService, IOpenInvoiceService>();
    }

    private IInvoiceDeliveryTransportStrategy CreateInvoiceDeliveryTransportStrategy(IIoCServiceLocator context)
    {
        const string keyVaultSettingName = "InvoiceDeliveryKeyVaultName";
        var transports = context.Get<IEnumerable<IInvoiceDeliveryTransport>>();
        var log = context.Get<ILog>();
        var keyVault = context.Get<IKeyVault>();
        var blobStorage = context.Get<IInvoiceDeliveryTransportBlobStorage>();
        var appSettings = context.Get<IAppSettings>();
        var keyVaultName = appSettings[keyVaultSettingName] ?? throw new ArgumentException($"Key Vault is not configured ({keyVaultSettingName}).");
        return new InvoiceDeliveryTransportStrategy(transports, log, keyVault, new($"https://{keyVaultName}.vault.azure.net"), blobStorage);
    }
}
