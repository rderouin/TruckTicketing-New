using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;

namespace SE.TruckTicketing.UI.ViewModels;

public class EditMultipleTicketsViewModel
{
    public static readonly List<TruckTicketStatus> AllowedStatuses = new()
    {
        TruckTicketStatus.Open,
        TruckTicketStatus.Hold,
    };

    public EditMultipleTicketsViewModel(IList<TruckTicket> truckTickets)
    {
        // snapshot tickets to be edited
        TruckTickets = truckTickets.ToImmutableList();
        if (truckTickets.Count == 0)
        {
            return;
        }

        // populate list of properties
        var truckTicket = truckTickets.First();
        PropertyBag = new(truckTicket.FacilityId, truckTicket.FacilityType);
        PropertyBag.Init(truckTickets);
    }

    public ImmutableList<TruckTicket> TruckTickets { get; }

    public TruckTicketPropertyBag PropertyBag { get; }

    public bool IsValid()
    {
        // all tickets should be from the same facility and only from allowed statuses
        if (TruckTickets.Count < 2 ||
            TruckTickets.DistinctBy(tt => tt.FacilityId).Count() > 1 ||
            TruckTickets.Any(tt => !AllowedStatuses.Contains(tt.Status)))
        {
            return false;
        }

        // has the tickets & properties to edit
        return PropertyBag.CanEditAny();
    }

    public class TruckTicketPropertyBag
    {
        public TruckTicketPropertyBag(Guid facilityId, FacilityType? facilityType)
        {
            FacilityId = facilityId;
            FacilityTypeValue = facilityType;
        }

        public Guid FacilityId { get; }

        public FacilityType? FacilityTypeValue { get; }

        public bool CanEditSourceLocation { get; set; }

        public Guid SourceLocationId { get; set; }

        public SourceLocation SourceLocation { get; set; }

        public bool CanEditFacilityServiceSubstance { get; set; }

        public Guid? FacilityServiceSubstanceId { get; set; }

        public FacilityServiceSubstanceIndex FacilityServiceSubstance { get; set; }

        public bool CanEditTruckingCompany { get; set; }

        public Guid TruckingCompanyId { get; set; }

        public Account TruckingCompany { get; set; }

        public bool CanEditWellClassification { get; set; }

        public WellClassifications WellClassification { get; set; }

        public bool CanEditQuadrant { get; set; }

        public string Quadrant { get; set; }

        public bool CanEditLevel { get; set; }

        public string Level { get; set; }

        public bool CanEditAny()
        {
            return CanEditSourceLocation ||
                   CanEditFacilityServiceSubstance ||
                   CanEditTruckingCompany ||
                   CanEditWellClassification ||
                   CanEditQuadrant ||
                   CanEditLevel;
        }

        public void Init(IList<TruckTicket> truckTickets)
        {
            // source locations
            var sourceLocations = truckTickets.Select(tt => tt.SourceLocationId).ToHashSet();
            if (sourceLocations.Count == 1)
            {
                CanEditSourceLocation = true;
                SourceLocationId = sourceLocations.First();
            }

            // exclude LF from changing services
            if (FacilityTypeValue != FacilityType.Lf)
            {
                // facility service substance
                var substances = truckTickets.Select(tt => tt.FacilityServiceSubstanceId).ToHashSet();
                if (substances.Count == 1)
                {
                    CanEditFacilityServiceSubstance = true;
                    FacilityServiceSubstanceId = substances.First();
                }
            }

            // trucking companies
            var companies = truckTickets.Select(tt => tt.TruckingCompanyId).ToHashSet();
            if (companies.Count == 1)
            {
                CanEditTruckingCompany = true;
                TruckingCompanyId = companies.First();
            }

            // well classifications
            var wellClassifications = truckTickets.Select(tt => tt.WellClassification).ToHashSet();
            if (wellClassifications.Count == 1)
            {
                CanEditWellClassification = true;
                WellClassification = wellClassifications.First();
            }

            // LF-only properties
            if (FacilityTypeValue == FacilityType.Lf)
            {
                // quadrants
                var quadrants = truckTickets.Select(tt => tt.Quadrant).ToHashSet();
                if (quadrants.Count == 1)
                {
                    CanEditQuadrant = true;
                    Quadrant = quadrants.First();
                }

                // levels
                var levels = truckTickets.Select(tt => tt.Level).ToHashSet();
                if (levels.Count == 1)
                {
                    CanEditLevel = true;
                    Level = levels.First();
                }
            }
        }
    }
}
