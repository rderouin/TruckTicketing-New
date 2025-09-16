using System;
using System.Collections.Generic;

using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts.Api;

namespace SE.TruckTicketing.Contracts.Models.LoadConfirmations;

public class LoadConfirmationAdhocModel
{
    public List<CompositeKey<Guid>> SalesLineKeys { get; set; }

    public AttachmentIndicatorType AttachmentType { get; set; }
}
