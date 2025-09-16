using System;

using FluentValidation;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Rules;

public class TruckTicketBasicValidationRule : FluentValidationRule<TruckTicketEntity, TTErrorCodes>
{
    public override int RunOrder => 100;

    protected override void ConfigureRules(BusinessContext<TruckTicketEntity> context, InlineValidator<TruckTicketEntity> validator)
    {
        if (context.Operation == Operation.Insert && context.Target.TruckTicketType == TruckTicketType.SP && context.Target.Source == TruckTicketSource.Manual)
        {
            validator.RuleFor(tt => tt.FacilityId)
                     .NotEmpty()
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_FacilityId);

            validator.RuleFor(tt => tt.TicketNumber)
                     .NotEmpty()
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_TicketNumber_Required);
        }

        validator.RuleFor(tt => tt.TicketNumber)
                 .Matches(ticket => ticket.SiteId + @"\d{5,6}-SP")
                 .When(ticket => ticket.TruckTicketType is TruckTicketType.SP && ticket.Source is TruckTicketSource.Manual && ticket.SiteId.HasText())
                 .WithTridentErrorCode(TTErrorCodes.TruckTicket_Spartan_TicketNumber_Invalid)
                 .WithMessage(ticket => $"Spartan ticket number is invalid. Must be {ticket.SiteId}######-SP {ticket.TicketNumber}");

        validator.RuleFor(tt => tt.LoadVolume)
                 .Must((ticket, volume) => Math.Abs(volume.Value! - ticket.TotalVolume) < 0.005)
                 .When(ticket => ticket.LoadVolume.HasValue && ticket.TruckTicketType != TruckTicketType.LF)
                 .WithMessage("'Load Volume' must be equal to 'Total Volume'. Verify load cuts.");

        if (context.Target.Status == TruckTicketStatus.Approved)
        {
            validator.RuleFor(tt => tt.FacilityId)
                     .NotEmpty()
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_FacilityId);

            validator.RuleFor(tt => tt.TicketNumber)
                     .NotEmpty()
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_TicketNumber_Required);

            validator.RuleFor(tt => tt.LoadDate)
                     .NotEmpty()
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_LoadDate_Required);

            validator.RuleFor(tt => tt.SourceLocationId)
                     .NotEmpty()
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_SourceLocationId_Required);

            validator.RuleFor(tt => tt.GeneratorId)
                     .NotEmpty()
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_GeneratorId);

            validator.RuleFor(tt => tt.WellClassification)
                     .NotEmpty()
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_WellClassification);

            validator.RuleFor(tt => tt.FacilityServiceId)
                     .NotEmpty()
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_FacilityServiceId);

            validator.RuleFor(tt => tt.TruckingCompanyId)
                     .NotEmpty()
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_TruckingCompanyId);

            validator.RuleFor(tt => tt.BillOfLading)
                     .NotEmpty()
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_BillOfLading_Required);

            validator.RuleFor(tt => tt.TimeIn)
                     .NotEmpty()
                     .When(tt => !(tt.CountryCode == CountryCode.US && tt.FacilityType is FacilityType.Fst or FacilityType.Swd))
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_TimeIn);

            validator.RuleFor(tt => tt.TimeOut)
                     .NotEmpty()
                     .When(tt => !(tt.CountryCode == CountryCode.US && tt.FacilityType is FacilityType.Fst or FacilityType.Swd))
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_TimeOut);

            validator.RuleFor(tt => tt.BillingConfigurationId)
                     .NotEmpty()
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_BillingConfiguration_Required);

            // LANDFILL tickets only
            validator.RuleFor(tt => tt.MaterialApprovalId)
                     .NotEmpty()
                     .When(tt => tt.TruckTicketType is TruckTicketType.LF)
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_MaterialApprovalId);

                // LF ticket hazard state is recorded as DowNonDow
            validator.RuleFor(tt => tt.DowNonDow)
                    .NotEmpty()
                    .When(tt => tt.TruckTicketType == TruckTicketType.LF)
                    .WithTridentErrorCode(TTErrorCodes.TruckTicket_HazNonHaz);

            validator.RuleFor(tt => tt.GrossWeight)
                     .NotEmpty()
                     .When(tt => tt.TruckTicketType is TruckTicketType.LF)
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_GrossWeight);

            validator.RuleFor(tt => tt.TareWeight)
                     .NotEmpty()
                     .When(tt => tt.TruckTicketType is TruckTicketType.LF)
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_TareWeight);

            validator.RuleFor(tt => tt.NetWeight)
                     .NotEmpty()
                     .When(tt => tt.TruckTicketType is TruckTicketType.LF)
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_NetWeight);

            // NON-LANDFILL tickets
            validator.RuleFor(tt => tt.UnitOfMeasure)
                     .NotEmpty()
                     .When(ticket => ticket.TruckTicketType != TruckTicketType.LF && ticket.IsServiceOnlyTicket != true)
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_UnitOfMeasure);

            validator.RuleFor(tt => tt.DowNonDow)
                     .NotEmpty()
                     .When(tt => tt.TruckTicketType is TruckTicketType.SP or TruckTicketType.WT && !(tt.CountryCode == CountryCode.US && tt.FacilityType is FacilityType.Fst or FacilityType.Swd))
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_Dow);

            validator.RuleFor(tt => tt.LoadVolume)
                     .NotEmpty()
                     .When(ticket => ticket.TruckTicketType != TruckTicketType.LF && ticket.IsServiceOnlyTicket != true);
        }

        validator.RuleFor(tt => tt.VolumeChangeReasonText)
                 .NotEmpty()
                 .When(tt => tt.VolumeChangeReason == VolumeChangeReason.Other)
                 .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeChangeReasonComments);
    }
}
