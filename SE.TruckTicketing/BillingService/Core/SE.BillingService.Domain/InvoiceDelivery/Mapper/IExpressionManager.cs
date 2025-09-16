using System;
using System.Collections.Generic;
using System.Reflection;

namespace SE.BillingService.Domain.InvoiceDelivery.Mapper;

public interface IExpressionManager
{
    Assembly TryGetExisting(string moduleName);

    Assembly CompileExpressions(string moduleName, IDictionary<Guid, string> expressions);

    Func<Dictionary<string, object>, Dictionary<string, object>, object> GetExpression(Assembly assembly, Guid expressionId);
}
