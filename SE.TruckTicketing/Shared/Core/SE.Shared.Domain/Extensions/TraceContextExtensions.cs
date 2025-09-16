using Microsoft.Azure.Functions.Worker;

namespace SE.Shared.Domain.Extensions;

public static class TraceContextExtensions
{
    public static string GetOperationId(this TraceContext context)
    {
        var traceId = context?.TraceParent;
        if (string.IsNullOrWhiteSpace(traceId))
        {
            return null;
        }

        var elements = traceId.Split('-');
        return elements.Length >= 2 ? elements[1] : null;
    }
}
