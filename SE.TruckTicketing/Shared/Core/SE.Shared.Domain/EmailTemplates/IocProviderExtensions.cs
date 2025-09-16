using System;
using System.Reflection;

using Autofac;

using Trident.IoC;

namespace SE.Shared.Domain.EmailTemplates;

public static class IocProviderExtensions
{
    public static void RegisterEmailProcessors(this IIoCProvider builder, Assembly[] assemblies)
    {
        var autofacBuilder = (AutofacIoCProvider)builder;
        var targetAssemblies = assemblies;

        autofacBuilder.Builder!
                      .RegisterAssemblyTypes(targetAssemblies)
                      .Where(t => t.IsAssignableTo<IEmailTemplateProcessor>())
                      .Named<IEmailTemplateProcessor>(GetEmailTemplateProcessorKey)
                      .AsImplementedInterfaces()
                      .InstancePerLifetimeScope()
                      .AsSelf();

        static string GetEmailTemplateProcessorKey(Type type)
        {
            const string message = "EmailTemplateProcessor is not marked for a target event type.";
            return type.GetCustomAttribute<EmailTemplateProcessorForAttribute>()?.TemplateKey ?? throw new InvalidOperationException(message);
        }
    }
}
