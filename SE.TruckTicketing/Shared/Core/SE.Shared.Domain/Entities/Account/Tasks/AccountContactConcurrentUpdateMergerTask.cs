using System.Linq;
using System.Threading.Tasks;

using Trident.Business;
using Trident.Extensions;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.Account.Tasks;

public class AccountContactConcurrentUpdateMergerTask : WorkflowTaskBase<BusinessContext<AccountEntity>>
{
    public override int RunOrder => -1;

    public override OperationStage Stage => OperationStage.BeforeUpdate;

    public override Task<bool> Run(BusinessContext<AccountEntity> context)
    {
        // In the event that a concurrent update to account contacts are happening,
        // account contact deletions are forbidden, we also do not want to trigger an
        // optimistic concurrency update violation. As such we merge contacts from the original
        // and target, leaving validations to deal with any potential overlaps.

        var original = context.Original;
        var target = context.Target;

        target.Contacts = original.Contacts.MergeBy(target.Contacts, contact => contact.Id).ToList();

        return Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<AccountEntity> context)
    {
        var shouldRun = context.Original.VersionTag != context.Target.VersionTag &&
                        !context.Original.Contacts.Select(contact => contact.Id)
                                .SequenceEqual(context.Target.Contacts.Select(contact => contact.Id));

        return Task.FromResult(shouldRun);
    }
}
