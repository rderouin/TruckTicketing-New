using System.Threading;

using Microsoft.Azure.Functions.Worker;

namespace SE.TridentContrib.Extensions.Azure.Functions;

public class FunctionContextAccessor : IFunctionContextAccessor
{
    private static readonly AsyncLocal<FunctionContextRedirect> _currentContext = new();

    public virtual FunctionContext FunctionContext
    {
        get => _currentContext.Value?.HeldContext;
        set
        {
            var holder = _currentContext.Value;
            if (holder != null)
            {
                // Clear current context trapped in the AsyncLocals, as its done.
                holder.HeldContext = null;
            }

            if (value != null)
            {
                // Use an object indirection to hold the context in the AsyncLocal,
                // so it can be cleared in all ExecutionContexts when its cleared.
                _currentContext.Value = new() { HeldContext = value };
            }
        }
    }

    private class FunctionContextRedirect
    {
        public FunctionContext HeldContext;
    }
}
