using System.Threading.Tasks;

using SE.TridentContrib.Extensions.Security;

using Trident.Business;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TradeAgreementUploads.Tasks;

public class TradeAgreementUploadEmailAddressTask : WorkflowTaskBase<BusinessContext<TradeAgreementUploadEntity>>
{
    private readonly IUserContextAccessor _userContextAccessor;

    public TradeAgreementUploadEmailAddressTask(IUserContextAccessor userContextAccessor)
    {
        _userContextAccessor = userContextAccessor;
    }

    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.BeforeInsert;

    public override async Task<bool> Run(BusinessContext<TradeAgreementUploadEntity> context)
    {
        var userContext = _userContextAccessor.UserContext;

        if (context.Target is null)
        {
            return await Task.FromResult(false);
        }

        context.Target.EmailAddress = userContext?.EmailAddress;

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<TradeAgreementUploadEntity> context)
    {
        return Task.FromResult(context.Operation == Operation.Insert);
    }
}
