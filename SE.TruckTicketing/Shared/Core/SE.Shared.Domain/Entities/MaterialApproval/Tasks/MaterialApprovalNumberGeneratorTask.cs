using System;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.Sequences;
using SE.TridentContrib.Extensions.Security;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.MaterialApproval.Tasks;

public class MaterialApprovalNumberGeneratorTask : WorkflowTaskBase<BusinessContext<MaterialApprovalEntity>>
{
    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IUserContextAccessor _userContextAccessor;

    private readonly ISequenceNumberGenerator _sequenceNumberGenerator;

    public MaterialApprovalNumberGeneratorTask(ISequenceNumberGenerator sequenceNumberGenerator,
                                               IProvider<Guid, FacilityEntity> facilityProvider,
                                               IUserContextAccessor userContextAccessor)
    {
        _sequenceNumberGenerator = sequenceNumberGenerator;
        _facilityProvider = facilityProvider;
        _userContextAccessor = userContextAccessor;
    }

    public override int RunOrder => 40;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<MaterialApprovalEntity> context)
    {
        var materialApprovalFacility = await _facilityProvider.GetById(context.Target.FacilityId) ?? new();

        if (materialApprovalFacility.Id == default)
        {
            return await Task.FromResult(false);
        }

        await foreach (var generatedSequence in _sequenceNumberGenerator.GenerateSequenceNumbers(SequenceTypes.MaterialApproval, materialApprovalFacility.SiteId, 1))
        {
            context.Target.MaterialApprovalNumber = generatedSequence;
        }
        
        context.Target.SecureRepresentative = _userContextAccessor?.UserContext?.DisplayName;
        context.Target.SecureRepresentativeId = _userContextAccessor?.UserContext?.ObjectId;

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<MaterialApprovalEntity> context)
    {
        return Task.FromResult(context.Operation == Operation.Insert && context.Target.FacilityId != default);
    }
}
