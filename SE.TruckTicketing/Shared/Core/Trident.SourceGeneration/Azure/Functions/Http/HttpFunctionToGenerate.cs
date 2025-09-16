using System;
using System.Net.Http;

namespace Trident.SourceGeneration.Azure.Functions.Http;

public struct HttpFunctionToGenerate
{
    public readonly HttpFunctionApiMethod HttpFunctionApiMethod;

    public readonly string FunctionName;

    public readonly string Route;

    public readonly string[] HttpMethods;

    public readonly string ClaimsAuthorizeResource;

    public readonly string ClaimsAuthorizeOperation;

    public readonly string AuthorizeFacilityAccessWith;
    
    public readonly string EntityIdType;

    public HttpFunctionToGenerate(HttpFunctionApiMethod httpFunction,
                                  string functionName,
                                  string route,
                                  string claimsAuthorizeResource,
                                  string claimsAuthorizeOperation,
                                  string authorizeFacilityAccessWith,
                                  string entityIdType)
    {
        HttpFunctionApiMethod = httpFunction;
        FunctionName = functionName;
        Route = route;
        HttpMethods = httpFunction switch
                      {
                          HttpFunctionApiMethod.GetById => new[] { nameof(HttpMethod.Get) },
                          HttpFunctionApiMethod.Search => new[] { nameof(HttpMethod.Get), nameof(HttpMethod.Post) },
                          HttpFunctionApiMethod.Create => new[] { nameof(HttpMethod.Post) },
                          HttpFunctionApiMethod.Update => new[] { nameof(HttpMethod.Put) },
                          HttpFunctionApiMethod.Delete => new[] { nameof(HttpMethod.Delete) },
                          HttpFunctionApiMethod.Patch => new[] { "Patch" },
                          _ => throw new ArgumentException("Invalid HttpFunctionApiMethod"),
                      };

        ClaimsAuthorizeResource = claimsAuthorizeResource;
        ClaimsAuthorizeOperation = claimsAuthorizeOperation;
        AuthorizeFacilityAccessWith = authorizeFacilityAccessWith;
        EntityIdType = entityIdType;
    }
}
