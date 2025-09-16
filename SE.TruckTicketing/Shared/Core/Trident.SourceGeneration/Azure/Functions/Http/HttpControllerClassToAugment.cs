using System.Collections.Generic;

namespace Trident.SourceGeneration.Azure.Functions.Http;

public readonly struct HttpControllerClassToAugment
{
    public readonly string ClassName;

    public readonly string ClassNamespace;

    public readonly List<HttpFunctionToGenerate> FunctionsToGenerate;

    public HttpControllerClassToAugment(string className, string classNamespace, List<HttpFunctionToGenerate> functions)
    {
        ClassName = className;
        ClassNamespace = classNamespace;
        FunctionsToGenerate = functions;
    }
}
