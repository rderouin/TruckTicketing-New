using System;

using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface IMatchPredicateService : IServiceBase<MatchPredicate, Guid>
{
}
