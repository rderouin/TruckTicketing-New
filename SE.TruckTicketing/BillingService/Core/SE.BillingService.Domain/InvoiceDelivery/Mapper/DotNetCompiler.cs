using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SE.BillingService.Domain.InvoiceDelivery.Mapper;

public class DotNetCompiler : IDotNetCompiler
{
    private static readonly object GlobalLock = new();

    public Assembly TryGetAssembly(string contextName, string assemblyName)
    {
        return GetOrCreateAssemblyLoadContext(contextName).Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName);
    }

    public Assembly Compile(string contextName, string assemblyName, string code)
    {
        // get the dependencies
        var references = AssemblyLoadContext.Default.Assemblies
                                            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                                            .Select(a => MetadataReference.CreateFromFile(a.Location))
                                            .ToList();

        // compilation prep
        var compilation = CSharpCompilation.Create(assemblyName,
                                                   new[] { CSharpSyntaxTree.ParseText(code) },
                                                   references,
                                                   new(OutputKind.DynamicallyLinkedLibrary));

        // compile
        using var stream = new MemoryStream();
        var outcome = compilation.Emit(stream);
        if (!outcome.Success)
        {
            var failures = string.Join(Environment.NewLine,
                                       outcome.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                                              .Select(d => $"{d.Id}: {d.GetMessage()}"));

            throw new InvalidOperationException("Compilation failed with the following errors:" + Environment.NewLine + failures);
        }

        // reset to the start
        stream.Flush();
        stream.Position = 0;

        // load the assembly into the specific context
        var assemblyLoadContext = GetOrCreateAssemblyLoadContext(contextName);
        var assembly = assemblyLoadContext.LoadFromStream(stream);
        if (assembly == null)
        {
            throw new InvalidOperationException($"Unable to load an assembly from the custom context: {assemblyName}");
        }

        return assembly;
    }

    private AssemblyLoadContext GetOrCreateAssemblyLoadContext(string contextName)
    {
        var assemblyLoadContext = AssemblyLoadContext.All.FirstOrDefault(c => c.Name == contextName);
        if (assemblyLoadContext == null)
        {
            lock (GlobalLock)
            {
                assemblyLoadContext = AssemblyLoadContext.All.FirstOrDefault(c => c.Name == contextName);
                if (assemblyLoadContext == null)
                {
                    assemblyLoadContext = new(contextName, true);
                    var registeredContext = AssemblyLoadContext.All.FirstOrDefault(c => c.Name == contextName);
                    if (assemblyLoadContext != registeredContext)
                    {
                        throw new InvalidOperationException("AssemblyLoadContext is not registered.");
                    }
                }
            }
        }

        return assemblyLoadContext;
    }
}
