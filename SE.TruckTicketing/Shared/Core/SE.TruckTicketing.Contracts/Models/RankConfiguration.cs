using System;

namespace SE.TruckTicketing.Contracts.Models;

public class RankConfiguration
{
    public Guid EntityId { get; set; }

    public string Name { get; set; }

    public string[] Predicates { get; set; }
}
