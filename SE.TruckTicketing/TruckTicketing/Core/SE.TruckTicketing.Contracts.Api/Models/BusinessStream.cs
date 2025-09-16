using System;

using SE.TruckTicketing.Contracts.Models;

using Trident.Contracts.Api;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Contracts.Api.Models;

public class BusinessStream : GuidApiModelBase
{
    public string Name { get; set; }
}
