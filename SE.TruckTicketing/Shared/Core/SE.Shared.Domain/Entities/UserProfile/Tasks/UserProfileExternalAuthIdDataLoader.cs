using System;
using System.Linq;
using System.Threading.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.UserProfile.Tasks;

public class UserProfileExternalAuthIdDataLoader : WorkflowTaskBase<BusinessContext<UserProfileEntity>>
{
    private readonly IProvider<Guid, UserProfileEntity> _userProfileProvider;

    public UserProfileExternalAuthIdDataLoader(IProvider<Guid, UserProfileEntity> userProfileProvider)
    {
        _userProfileProvider = userProfileProvider;
    }

    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<UserProfileEntity> context)
    {
        var matches = await _userProfileProvider.Get(x => x.ExternalAuthId == context.Target.ExternalAuthId && x.Id != context.Target.Id);

        context.ContextBag.Add(UserProfileBusinessContextBagKeys.UserProfileExternalAuthIdIsUnique, !matches.Any());

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<UserProfileEntity> context)
    {
        return Task.FromResult(true);
    }
}
