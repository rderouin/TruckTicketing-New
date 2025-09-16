using Autofac;
using Autofac.Core;

using SE.TridentContrib.Extensions.Azure.Functions;

using Trident.Azure.Logging;
using Trident.IoC;
using Trident.Logging;

namespace SE.TridentContrib.Extensions.Extensions;

public static class AppInsightsLoggerExtensions
{
    public static void RegisterAppInsightsLogger(this IIoCProvider provider)
    {
        if (provider is AutofacIoCProvider { Builder: { } builder })
        {
            builder.RegisterType<AppInsightsLogger>()
                   .As<ILog>()
                   .InstancePerLifetimeScope()
                   .OnActivating(ActivatingHandler);

            static void ActivatingHandler(IActivatingEventArgs<AppInsightsLogger> eventArgs)
            {
                var logger = (ILog)eventArgs.Instance;
                var functionContextAccessor = new FunctionContextAccessor();
                logger.SetCallContext(functionContextAccessor.FunctionContext);
            }
        }
    }
}
