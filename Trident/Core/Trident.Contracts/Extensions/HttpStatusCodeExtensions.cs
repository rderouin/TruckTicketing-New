using System.Net;

namespace Trident.Contracts.Extensions;

public static class HttpStatusCodeExtensions
{
    public static bool IsSuccessStatusCode(this HttpStatusCode statusCode)
    {
        var statusCodeInt = (int)statusCode;
        return statusCodeInt is >= 200 and <= 299;
    }
}
