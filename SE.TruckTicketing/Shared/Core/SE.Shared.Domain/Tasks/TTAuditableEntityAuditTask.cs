using System;
using System.Threading.Tasks;

using SE.TridentContrib.Extensions.Security;

using Trident.Business;
using Trident.Workflow;

namespace SE.Shared.Domain.Tasks;

public class TTAuditableEntityAuditTask<TEntity> : WorkflowTaskBase<BusinessContext<TEntity>> where TEntity : TTAuditableEntityBase
{
    private readonly IUserContextAccessor _userContextAccessor;

    public TTAuditableEntityAuditTask(IUserContextAccessor userContextAccessor = null)
    {
        _userContextAccessor = userContextAccessor;
    }

    // Ensure this is one of the first work-flow tasks to run.
    public override int RunOrder => -2;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate | OperationStage.Custom;

    public override Task<bool> Run(BusinessContext<TEntity> context)
    {
        var userContext = _userContextAccessor?.UserContext;
        var currentTime = DateTimeOffset.UtcNow;

        context.Target.UpdatedBy = userContext?.DisplayName ?? "Integrations";
        context.Target.UpdatedById = userContext?.ObjectId;
        context.Target.UpdatedAt = currentTime;

        if (context.Original is null)
        {
            context.Target.CreatedBy = userContext?.DisplayName ?? "Integrations";
            context.Target.CreatedById = userContext?.ObjectId;
            context.Target.CreatedAt = currentTime;
        }
        else
        {
            context.Target.CreatedBy = context.Original.CreatedBy;
            context.Target.CreatedById = context.Original.CreatedById;
            context.Target.CreatedAt = context.Original.CreatedAt;
        }

        return Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<TEntity> context)
    {
        return Task.FromResult(true);
    }
}
