using System;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace SE.TridentContrib.Extensions.Azure.Functions;

public class FunctionContextAccessorMiddleware : IFunctionsWorkerMiddleware
{
    public FunctionContextAccessorMiddleware(IFunctionContextAccessor accessor)
    {
        FunctionContextAccessor = accessor;
    }

    private IFunctionContextAccessor FunctionContextAccessor { get; }

    public Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (FunctionContextAccessor.FunctionContext != null)
        {
            // This should never happen because the context should be localized to the current Task chain.
            // But if it does happen (perhaps the implementation is bugged), then we need to know immediately so it can be fixed.
            throw new InvalidOperationException($"Unable to initialize {nameof(IFunctionContextAccessor)}: context has already been initialized.");
        }

        FunctionContextAccessor.FunctionContext = context;

        return next(context);
    }
}
