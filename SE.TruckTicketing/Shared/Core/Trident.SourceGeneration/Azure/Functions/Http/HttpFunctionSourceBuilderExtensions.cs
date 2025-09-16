using System;
using System.Text;

namespace Trident.SourceGeneration.Azure.Functions.Http;

internal static class HttpFunctionSourceBuilderExtensions
{
    internal static string GenerateAugmentedHttpFunctionController(this HttpControllerClassToAugment classToAugment)
    {
        return string.Format(@"
            using System;
            using System.Net;
            using System.Net.Http;
            using System.Threading.Tasks;
            using Microsoft.Azure.Functions.Worker;
            using Microsoft.Azure.Functions.Worker.Http;
            using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
            using Trident.Azure.Functions;
            using Trident.Azure.Security;

            namespace {0}
            {{
                public partial class {1}
                {{
                    {2}
                }}
            }}
        ", classToAugment.ClassNamespace, classToAugment.ClassName, classToAugment.GenerateHttpFunctions());
    }

    internal static string GenerateBaseRouteFunction(this HttpFunctionToGenerate functionToGenerate)
    {
        return string.Format(@"
            [System.CodeDom.Compiler.GeneratedCode(""Trident.SourceGeneration"", ""1.0.0"")]
            [Function(""{0}"")]
            {4}
            {5}
            public async Task<HttpResponseData> {0}([HttpTrigger(AuthorizationLevel.Anonymous, {1} Route = ""{2}"")] HttpRequestData request)
            {{
                return await {3}(request, ""{0}"");
            }}
        ", functionToGenerate.FunctionName,
                             functionToGenerate.GenerateMethodParams(),
                             functionToGenerate.Route,
                             functionToGenerate.HttpFunctionApiMethod.ToString(),
                             GenerateClaimsAuthorizeAttribute(functionToGenerate),
                             GenerateAuthorizeFacilityAccessWithAttribute(functionToGenerate));
    }

    internal static string GenerateHttpFunctions(this HttpControllerClassToAugment classToAugment)
    {
        var stringBuilder = new StringBuilder();

        foreach (var function in classToAugment.FunctionsToGenerate)
        {
            stringBuilder.AppendLine(function.HttpFunctionApiMethod switch
                                     {
                                         HttpFunctionApiMethod.GetById => function.GenerateIdTemplateRouteFunction(),
                                         HttpFunctionApiMethod.Search => function.GenerateBaseRouteFunction(),
                                         HttpFunctionApiMethod.Create => function.GenerateBaseRouteFunction(),
                                         HttpFunctionApiMethod.Update => function.GenerateIdTemplateRouteFunction(),
                                         HttpFunctionApiMethod.Delete => function.GenerateIdTemplateRouteFunction(),
                                         HttpFunctionApiMethod.Patch => function.GenerateIdTemplateRouteFunction(),
                                         _ => throw new ArgumentException("Invalid HttpFunctionApiMethod"),
                                     });
        }

        return stringBuilder.ToString();
    }

    internal static string GenerateClaimsAuthorizeAttribute(this HttpFunctionToGenerate functionToGenerate)
    {
        if (string.IsNullOrWhiteSpace(functionToGenerate.ClaimsAuthorizeResource) ||
            string.IsNullOrWhiteSpace(functionToGenerate.ClaimsAuthorizeOperation))
        {
            return string.Empty;
        }

        return string.Format(@"[ClaimsAuthorize(""{0}"", ""{1}"")]", functionToGenerate.ClaimsAuthorizeResource, functionToGenerate.ClaimsAuthorizeOperation);
    }

    internal static string GenerateAuthorizeFacilityAccessWithAttribute(this HttpFunctionToGenerate functionToGenerate)
    {
        if (string.IsNullOrWhiteSpace(functionToGenerate.AuthorizeFacilityAccessWith))
        {
            return string.Empty;
        }

        return string.Format(@"[SE.Shared.Functions.AuthorizeFacilityAccessWith(typeof({0}))]", functionToGenerate.AuthorizeFacilityAccessWith);
    }

    internal static string GenerateIdTemplateRouteFunction(this HttpFunctionToGenerate functionToGenerate)
    {
        return string.Format(@"
            [System.CodeDom.Compiler.GeneratedCode(""Trident.SourceGeneration"", ""1.0.0"")]
            [Function(""{0}"")]
            {4}
            {5}
            public async Task<HttpResponseData> {0}([HttpTrigger(AuthorizationLevel.Anonymous, {1} Route = ""{2}"")] HttpRequestData request, {6} id)
            {{
                return await {3}(request, id, ""{0}"");
            }}
        ", functionToGenerate.FunctionName,
                             functionToGenerate.GenerateMethodParams(),
                             functionToGenerate.Route,
                             functionToGenerate.HttpFunctionApiMethod.ToString(),
                             GenerateClaimsAuthorizeAttribute(functionToGenerate),
                             GenerateAuthorizeFacilityAccessWithAttribute(functionToGenerate),
                             functionToGenerate.EntityIdType);
    }

    internal static string GenerateMethodParams(this HttpFunctionToGenerate functionToGenerate)
    {
        var mathods = "";

        foreach (var method in functionToGenerate.HttpMethods)
        {
            mathods += $@"""{method}"", ";
        }

        return mathods;
    }
}
