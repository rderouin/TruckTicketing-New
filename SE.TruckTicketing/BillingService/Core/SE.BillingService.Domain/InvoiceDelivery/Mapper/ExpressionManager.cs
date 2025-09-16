using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SE.BillingService.Domain.InvoiceDelivery.Mapper;

public class ExpressionManager : IExpressionManager
{
    private const string ContextName = "Truck Ticketing - Mapper Expressions";

    private const string ClassName = "MapperExpressions";

    private readonly IDotNetCompiler _compiler;

    public ExpressionManager(IDotNetCompiler compiler)
    {
        _compiler = compiler;
    }

    public Assembly TryGetExisting(string moduleName)
    {
        return _compiler.TryGetAssembly(ContextName, GetAssemblyName(moduleName));
    }

    public Assembly CompileExpressions(string moduleName, IDictionary<Guid, string> expressions)
    {
        // check if it is already compiled
        var assembly = TryGetExisting(moduleName);
        if (assembly != null)
        {
            return assembly;
        }

        // basic validation for expressions
        if (expressions == null || expressions.Any(e => string.IsNullOrWhiteSpace(e.Value)))
        {
            throw new ArgumentException("Expressions must contain C# code.", nameof(expressions));
        }

        // source code
        var propertiesText = string.Join(Environment.NewLine,
                                         expressions.Select(arg =>
                                                                $@"public static Func<Dictionary<string, object>, Dictionary<string, object>, object> {GetExpressionName(arg.Key)} {{ get; }} = {arg.Value};"));

        var expressionsText = $@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class {ClassName}
{{

{propertiesText}

}}
";

        return _compiler.Compile(ContextName, GetAssemblyName(moduleName), expressionsText);
    }

    public Func<Dictionary<string, object>, Dictionary<string, object>, object> GetExpression(Assembly assembly, Guid expressionId)
    {
        // get the class name
        var type = assembly.GetType(ClassName);
        if (type == null)
        {
            return null;
        }

        // get the method
        var property = type.GetProperty(GetExpressionName(expressionId), BindingFlags.Public | BindingFlags.Static);
        if (property == null)
        {
            return null;
        }

        // get the expression
        var expression = property.GetValue(null);
        return (Func<Dictionary<string, object>, Dictionary<string, object>, object>)expression;
    }

    private string GetAssemblyName(string moduleName)
    {
        return $"MapperExpressions_{moduleName}";
    }

    private string GetExpressionName(Guid expressionId)
    {
        return $"exp_{expressionId:N}";
    }
}
