using System.Collections.Generic;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Models;

using Trident.Contracts;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

public interface IMatchPredicateRankManager : IManager
{
    (int matches, int weight) Evaluate(TruckTicketEntity ticket, MatchPredicateEntity predicate);

    (int matches, int weight) Evaluate(TruckTicketEntity ticket, MatchPredicateEntity predicate, bool useLooseMatch);

    bool IsBillingConfigurationMatch(TruckTicketEntity ticket, MatchPredicateEntity predicate);

    List<RankConfiguration> EvaluatePredicateRank(List<RankConfiguration> configs,
                                                  string[] propertyValues,
                                                  Dictionary<string, int> propertyWeights,
                                                  string wildcard = "*",
                                                  bool includeNonMatchingConfigs = false,
                                                  bool includeOnlyExactMatchingConfigs = false);
}
