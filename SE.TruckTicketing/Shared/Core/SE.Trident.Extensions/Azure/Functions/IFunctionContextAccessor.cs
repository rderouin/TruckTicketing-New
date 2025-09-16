using Microsoft.Azure.Functions.Worker;

namespace SE.TridentContrib.Extensions.Azure.Functions;

public interface IFunctionContextAccessor
{
    FunctionContext FunctionContext { get; set; }
}
