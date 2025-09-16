using System.Collections.Generic;
using System.Threading.Tasks;

using SE.Shared.Domain.Extensions;
using SE.TruckTicketing.Contracts;

using Trident.Business;
using Trident.Contracts.Configuration;
using Trident.Validation;

namespace SE.Shared.Domain.Entities.Sequences.Rules;

public class SequenceValidationComplexRule : ValidationRuleBase<BusinessContext<SequenceEntity>>
{
    private readonly IAppSettings _appSettings;

    public SequenceValidationComplexRule(IAppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    public override int RunOrder => 200;

    public override Task Run(BusinessContext<SequenceEntity> context, List<ValidationResult> errors)
    {
        var sequenceConfig = _appSettings.GetSequenceConfiguration(context.Target.Type);

        if (context.Operation == Operation.Update)
        {
            //Type is immutable
            if (context.Original.Type != context.Target.Type)
            {
                errors.Add(new ValidationResult<TTErrorCodes>("SequenceType is immutable", TTErrorCodes.SequenceGeneration_TypeCheck, nameof(context.Target.Type)));
            }

            //Prefix is immutable
            if (context.Original.Prefix != context.Target.Prefix)
            {
                errors.Add(new ValidationResult<TTErrorCodes>("SequencePrefix is immutable", TTErrorCodes.SequenceGeneration_PrefixCheck, nameof(context.Target.Prefix)));
            }

            var requestedBlockSize = context.Target.LastNumber - context.Original.LastNumber;
            if (requestedBlockSize <= 0 || requestedBlockSize > sequenceConfig.MaxRequestBlockSize)
            {
                errors.Add(new ValidationResult<TTErrorCodes>($"Requested Blocksize should be greater than 0 and no greater than {sequenceConfig.MaxRequestBlockSize} for given SequenceType",
                                                              TTErrorCodes.SequenceGeneration_BlockSize, nameof(context.Target.LastNumber)));
            }
        }

        return Task.CompletedTask;
    }
}
