using AutoMapper;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Api.Models.SpartanData;

namespace SE.TruckTicketing.Api.Configuration;

public class SpartanSummaryMapperProfile : BaseMapperProfile
{
    public SpartanSummaryMapperProfile()
    {
        CreateMap<SpartanSummaryModel, TruckTicketEntity>(MemberList.Source)
           .ForMember(s => s.LoadDate, opt => opt.MapFrom(s => s.TransferFinishTime))
           .ForMember(s => s.OilVolume, opt => opt.MapFrom(s => s.RoundedCorrectedOilVolume))
           .ForMember(s => s.TimeIn, opt => opt.MapFrom(s => s.TransferStartTime))
           .ForMember(s => s.TimeOut, opt => opt.MapFrom(s => s.TransferFinishTime))
           .ForMember(s => s.TotalVolume, opt => opt.MapFrom(s => s.RoundedCorrectedEmulsionVolume))
           .ForMember(s => s.LoadVolume, opt => opt.MapFrom(s => s.RoundedCorrectedEmulsionVolume))
           .ForMember(s => s.TruckingCompanyName, opt => opt.MapFrom(s => s.TransportCompanyName))
           .ForMember(s => s.TruckNumber, opt => opt.MapFrom(s => s.TransportVehicleUnitNumber))
           .ForMember(s => s.UnloadOilDensity, opt => opt.MapFrom(s => s.CorrectedOilDensity))
           .ForMember(s => s.WaterVolume, opt => opt.MapFrom(s => s.RoundedCorrectedWaterVolume))
           .ForMember(s => s.BillOfLading, opt => opt.MapFrom(s => s.WaybillNumber))
           .ForMember(s => s.WellClassification, opt => opt.MapFrom(s => s.LocationOperatingStatus))
           .ForMember(s => s.WaterVolumePercent, opt => opt.MapFrom(s => s.RoundedCorrectedEmulsionWaterCut))
           .ForMember(s => s.OilVolumePercent, opt => opt.MapFrom(s => s.RoundedCorrectedEmulsionOilCut))
           .ForMember(s => s.WiqNumber, opt => opt.MapFrom(s => s.ScheduleNumber))
           .ForSourceMember(s => s.FacilityOperatorName, opt => opt.DoNotValidate())
           .ForSourceMember(s => s.LocationUwi, opt => opt.DoNotValidate())
           .ForSourceMember(s => s.ProductName, opt => opt.DoNotValidate())
           .ForSourceMember(s => s.TicketId, opt => opt.DoNotValidate())
           .ForSourceMember(s => s.TransactionId, opt => opt.DoNotValidate())
           .ForSourceMember(s => s.CustomerTransactionId, opt => opt.DoNotValidate())
           .ForSourceMember(s => s.PlantIdentifier, opt => opt.DoNotValidate())
           .ForSourceMember(s => s.RoundedCorrectedEmulsionWaterCut, opt => opt.DoNotValidate())
           .ForSourceMember(s => s.CorrectedShrinkageEmulsionVolume, opt => opt.DoNotValidate());
    }
}
