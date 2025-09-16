using System;

using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents.DensityConversion;

public abstract class DensityConversionViewModelBase<TCutParameters> where TCutParameters : CutParameters, new()
{
    private double? _grossWeight;

    private double? _tareWeight;

    protected DensityConversionViewModelBase(TruckTicketDensityConversionParams parameters, PreSetDensityConversionParams defaultDensityFactors, TruckTicket truckTicket)
    {
        TruckTicket = truckTicket;

        if (parameters is null)
        {
            if (defaultDensityFactors is null)
            {
                return;
            }

            DefaultDensityFactors = defaultDensityFactors;
            Oil.DensityConversionFactor = DefaultDensityFactors.OilConversionFactor;
            Water.DensityConversionFactor = DefaultDensityFactors.WaterConversionFactor;
            Solids.DensityConversionFactor = DefaultDensityFactors.SolidsConversionFactor;
            return;
        }

        DefaultDensityFactors = defaultDensityFactors;

        _grossWeight = parameters.GrossWeight;
        _tareWeight = parameters.TareWeight;

        Oil.CutPercentage = parameters.OilCutPercentage;
        Water.CutPercentage = parameters.WaterCutPercentage;
        Solids.CutPercentage = parameters.SolidsCutPercentage;

        Oil.Weight = parameters.OilWeight;
        Water.Weight = parameters.WaterWeight;
        Solids.Weight = parameters.SolidsWeight;

        CutEntryMethod = parameters.CutEntryMethod;

        Oil.DensityConversionFactor = parameters.OilConversionFactor ?? DefaultDensityFactors.OilConversionFactor;
        Water.DensityConversionFactor = parameters.WaterConversionFactor ?? DefaultDensityFactors.WaterConversionFactor;
        Solids.DensityConversionFactor = parameters.SolidsConversionFactor ?? DefaultDensityFactors.SolidsConversionFactor;
    }

    public TruckTicket TruckTicket { get; set; }

    public PreSetDensityConversionParams DefaultDensityFactors { get; }

    public bool IsDensityFactorsOverridden =>
        (DefaultDensityFactors != null && Math.Abs(Oil.DensityConversionFactor - DefaultDensityFactors.OilConversionFactor) > 0.001)
     || (DefaultDensityFactors != null && Math.Abs(Water.DensityConversionFactor - DefaultDensityFactors.WaterConversionFactor) > 0.001)
     || (DefaultDensityFactors != null && Math.Abs(Solids.DensityConversionFactor - DefaultDensityFactors.SolidsConversionFactor) > 0.001);

    public CutEntryMethod CutEntryMethod { get; set; }

    public double? GrossWeight
    {
        get => _grossWeight;
        set
        {
            _grossWeight = value;
            OnInputWeightChange();
        }
    }

    public double? NetWeight => GrossWeight.HasValue && TareWeight.HasValue ? GrossWeight - TareWeight : null;

    public double? TareWeight
    {
        get => _tareWeight;
        set
        {
            _tareWeight = value;
            OnInputWeightChange();
        }
    }

    public TCutParameters Oil { get; set; } = new();

    public TCutParameters Water { get; set; } = new();

    public TCutParameters Solids { get; set; } = new();

    public TCutParameters Total =>
        new()
        {
            CutPercentage = SumCutParameterValues(cut => cut.CutPercentage),
            ComputedWeight = SumCutParameterValues(cut => cut.Weight),
            ComputedAdjustedVolume = SumCutParameterValues(cut => cut.AdjustedVolume),
        };

    public bool IsTotalCutPercentageInvalid => Math.Abs(Total.CutPercentage.GetValueOrDefault(0) - 100) > 0.11;

    public abstract bool IsTotalWeightInvalid { get; }

    public abstract string InvalidTotalWeightMessage { get; }

    protected abstract void OnInputWeightChange();

    public void UpdateCutParameters()
    {
        if (CutEntryMethod == CutEntryMethod.FixedValue)
        {
            Oil.UpdateCutPercentage();
            Water.UpdateCutPercentage();
            Solids.UpdateCutPercentage();
        }
        else
        {
            Oil.UpdateWeight();
            Water.UpdateWeight();
            Solids.UpdateWeight();
        }
    }

    private double? SumCutParameterValues(Func<TCutParameters, double?> cutParameter)
    {
        return cutParameter(Oil).GetValueOrDefault(0) + cutParameter(Water).GetValueOrDefault(0) + cutParameter(Solids).GetValueOrDefault(0);
    }

    public virtual void UpdateTruckTicket(TruckTicket truckTicket)
    {
        truckTicket.GrossWeight = GrossWeight.GetValueOrDefault();
        truckTicket.TareWeight = TareWeight.GetValueOrDefault();
        truckTicket.NetWeight = NetWeight.GetValueOrDefault();

        truckTicket.OilVolume = Math.Round(Oil.AdjustedVolume.GetValueOrDefault(), 1);
        truckTicket.WaterVolume = Math.Round(Water.AdjustedVolume.GetValueOrDefault(), 1);
        truckTicket.SolidVolume = Math.Round(Solids.AdjustedVolume.GetValueOrDefault(), 1);
        truckTicket.TotalVolume = Math.Round(Total.ComputedAdjustedVolume.GetValueOrDefault(), 1);
        truckTicket.LoadVolume = Math.Round(Total.ComputedAdjustedVolume.GetValueOrDefault(), 1);

        truckTicket.OilVolumePercent = truckTicket.TotalVolume > 0 ? Math.Round(truckTicket.OilVolume * 100.0 / truckTicket.TotalVolume, 1) : 0;
        truckTicket.WaterVolumePercent = truckTicket.TotalVolume > 0 ? Math.Round(truckTicket.WaterVolume * 100.0 / truckTicket.TotalVolume, 1) : 0;
        truckTicket.SolidVolumePercent = truckTicket.TotalVolume > 0 ? Math.Round(truckTicket.SolidVolume * 100.0 / truckTicket.TotalVolume, 1) : 0;
        truckTicket.TotalVolumePercent = truckTicket.OilVolumePercent + truckTicket.WaterVolumePercent + truckTicket.SolidVolumePercent;

        truckTicket.ConversionParameters ??= new();
        truckTicket.ConversionParameters.GrossWeight = GrossWeight;
        truckTicket.ConversionParameters.TareWeight = TareWeight;
        truckTicket.ConversionParameters.OilCutPercentage = Oil.CutPercentage;
        truckTicket.ConversionParameters.WaterCutPercentage = Water.CutPercentage;
        truckTicket.ConversionParameters.SolidsCutPercentage = Solids.CutPercentage;

        truckTicket.ConversionParameters.OilWeight = Oil.Weight;
        truckTicket.ConversionParameters.WaterWeight = Water.Weight;
        truckTicket.ConversionParameters.SolidsWeight = Solids.Weight;

        truckTicket.ConversionParameters.OilConversionFactor = Oil.DensityConversionFactor;
        truckTicket.ConversionParameters.WaterConversionFactor = Water.DensityConversionFactor;
        truckTicket.ConversionParameters.SolidsConversionFactor = Solids.DensityConversionFactor;

        truckTicket.ConversionParameters.CutEntryMethod = CutEntryMethod;
    }
}

public abstract class CutParameters
{
    public double? CutPercentage { get; set; }

    public double? Weight { get; set; }

    public double DensityConversionFactor { get; set; } = 1.0;

    public abstract double? AdjustedVolume { get; }

    public double? ComputedWeight { get; init; }

    public double? ComputedAdjustedVolume { get; init; }

    public abstract void UpdateWeight();

    public abstract void UpdateCutPercentage();
}
