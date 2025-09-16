using FluentValidation;

using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;

namespace SE.Shared.Domain.Entities.VolumeChange.Rules;

public class VolumeChangeValidationRules : FluentValidationRule<VolumeChangeEntity, TTErrorCodes>
{
    public override int RunOrder => 100;

    protected override void ConfigureRules(BusinessContext<VolumeChangeEntity> context, InlineValidator<VolumeChangeEntity> validator)
    {
        validator.RuleFor(volumeChange => volumeChange.TicketDate)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_TicketDate);

        validator.RuleFor(volumeChange => volumeChange.TicketNumber)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_TicketNumber);

        validator.RuleFor(volumeChange => volumeChange.ProcessOriginal)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_ProcessOriginal);

        validator.RuleFor(volumeChange => volumeChange.OilVolumeOriginal)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_OilVolumeOriginal);

        validator.RuleFor(volumeChange => volumeChange.WaterVolumeOriginal)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_WaterVolumeOriginal);

        validator.RuleFor(volumeChange => volumeChange.SolidVolumeOriginal)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_SolidVolumeOriginal);

        validator.RuleFor(volumeChange => volumeChange.TotalVolumeOriginal)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_TotalVolumeOriginal);

        validator.RuleFor(volumeChange => volumeChange.ProcessAdjusted)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_ProcessAdjusted);

        validator.RuleFor(volumeChange => volumeChange.OilVolumeAdjusted)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_OilVolumeAdjusted);

        validator.RuleFor(volumeChange => volumeChange.WaterVolumeAdjusted)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_WaterVolumeAdjusted);

        validator.RuleFor(volumeChange => volumeChange.SolidVolumeAdjusted)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_SolidVolumeAdjusted);

        validator.RuleFor(volumeChange => volumeChange.TotalVolumeAdjusted)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_TotalVolumeAdjusted);

        validator.RuleFor(volumeChange => volumeChange.VolumeChangeReason)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_VolumeChangeReason);

        validator.RuleFor(volumeChange => volumeChange.FacilityId)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_FacilityId);

        validator.RuleFor(volumeChange => volumeChange.FacilityName)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_FacilityName);

        validator.RuleFor(volumeChange => volumeChange.TruckTicketStatus)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.VolumeChange_TruckTicketStatus);
    }
}
