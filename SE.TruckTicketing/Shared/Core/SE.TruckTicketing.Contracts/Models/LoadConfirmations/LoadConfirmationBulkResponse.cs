using System;
using System.Collections.Generic;
using System.Linq;

using Trident.Contracts.Api;

namespace SE.TruckTicketing.Contracts.Models.LoadConfirmations;

public class LoadConfirmationBulkResponse
{
    public bool IsSuccessful { get; set; }

    public List<(CompositeKey<Guid>, bool)> PendingActions { get; set; }

    public Dictionary<CompositeKey<Guid>, bool> GetPendingActionsLookup()
    {
        return PendingActions.ToDictionary(pa => pa.Item1, pa => pa.Item2);
    }
}
