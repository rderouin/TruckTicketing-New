using System;
using System.Linq;
using System.Threading.Tasks;

using Humanizer;

using SE.Shared.Common.Extensions;

using Trident.Business;
using Trident.Validation;
using Trident.Workflow;

namespace SE.Shared.Domain.Tasks;

public class OptimisticConcurrencyViolationCheckerTask<TEntity> : WorkflowTaskBase<BusinessContext<TEntity>> where TEntity : TTAuditableEntityBase, ISupportOptimisticConcurrentUpdates
{
    public override int RunOrder => 100;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override Task<bool> Run(BusinessContext<TEntity> context)
    {
        var original = context.Original;
        var target = context.Target;

        // If original version tag is not set, this is an existing entity skip check and set version going forward
        if (context.Operation is Operation.Insert || !original.VersionTag.HasText())
        {
            target.VersionTag = Guid.NewGuid().ToString();
            return Task.FromResult(true);
        }
        
        if (original.VersionTag != target.VersionTag &&
            !original.GetFieldsToCompare().Select(value => value?.ToString() ?? "")
                     .SequenceEqual(target.GetFieldsToCompare().Select(value => value?.ToString() ?? "")))
        {
            var errorMessage =
                $"The {typeof(TEntity).Name.Humanize()} you are trying to update has been modified by another user or process since you retrieved it. Please refresh the data and try again.";

            throw new ValidationRollupException(new[] { new ValidationResult(errorMessage) });
        }
        
        target.VersionTag = Guid.NewGuid().ToString();
        return Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<TEntity> context)
    {
        return Task.FromResult(true);
    }
}
