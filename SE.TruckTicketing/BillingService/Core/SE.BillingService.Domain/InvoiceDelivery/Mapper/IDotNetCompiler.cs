using System.Reflection;

namespace SE.BillingService.Domain.InvoiceDelivery.Mapper;

public interface IDotNetCompiler
{
    Assembly TryGetAssembly(string contextName, string assemblyName);

    Assembly Compile(string contextName, string assemblyName, string code);
}
