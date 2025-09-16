using System;

namespace SE.TruckTicketing.Contracts.Models;

public interface IFacilityRelatedModel
{
    Guid FacilityId { get; }
}
