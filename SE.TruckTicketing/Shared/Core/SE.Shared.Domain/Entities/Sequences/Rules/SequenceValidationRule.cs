using FluentValidation;

using SE.Shared.Domain.Extensions;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;
using Trident.Contracts.Configuration;

namespace SE.Shared.Domain.Entities.Sequences.Rules;

public class SequenceValidationRule : FluentValidationRule<SequenceEntity, TTErrorCodes>
{
    private readonly IAppSettings _appSettings;

    public SequenceValidationRule(IAppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    public override int RunOrder => 100;

    protected override void ConfigureRules(BusinessContext<SequenceEntity> context, InlineValidator<SequenceEntity> validator)
    {
        var sequenceConfig = _appSettings.GetSequenceConfiguration(context.Target.Type);

        validator.RuleFor(sequence => sequence.Type)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.SequenceGeneration_Prefix);

        validator.RuleFor(sequence => sequence.LastNumber)
                 .GreaterThan(sequenceConfig.Seed)
                 .WithTridentErrorCode(TTErrorCodes.SequenceGeneration_LastNumber);
    }
}
