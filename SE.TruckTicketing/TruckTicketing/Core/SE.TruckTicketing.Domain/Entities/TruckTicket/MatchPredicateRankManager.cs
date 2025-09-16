using System;
using System.Collections.Generic;
using System.Linq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Models;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

public class MatchPredicateRankManager : IMatchPredicateRankManager
{
    private readonly PredicateParameters[] _parameters =
    {
        new(t => t.SourceLocationId, p => p.SourceLocationId, p => p.SourceLocationValueState, 21),
        new(t => t.Stream, p => p.Stream, p => p.StreamValueState, 13),
        new(t => t.WellClassification, p => p.WellClassification, p => p.WellClassificationState, 8),
        new(t => t.ServiceTypeId, p => p.ServiceTypeId, p => p.ServiceTypeValueState, 5),
        new(t => t.SubstanceId, p => p.SubstanceId, p => p.SubstanceValueState, 3),
    };

    public (int matches, int weight) Evaluate(TruckTicketEntity ticket, MatchPredicateEntity predicate)
    {
        return Evaluate(ticket, predicate, true);
    }

    public bool IsBillingConfigurationMatch(TruckTicketEntity ticket, MatchPredicateEntity predicate)
    {
        var isMatch = true;
        foreach (var parameter in _parameters)
        {
            if (parameter.StateSelector(predicate) != MatchPredicateValueState.Any &&
                (parameter.StateSelector(predicate) != MatchPredicateValueState.Value || !parameter.PredicateValueSelector(predicate).Equals(parameter.TicketValueSelector(ticket))))
            {
                isMatch = false;
            }
        }

        return isMatch;
    }

    public (int matches, int weight) Evaluate(TruckTicketEntity ticket, MatchPredicateEntity predicate, bool useLooseMatch)
    {
        var result = (matches: 0, weight: 0);
        foreach (var parameter in _parameters)
        {
            if ((useLooseMatch && parameter.StateSelector(predicate) == MatchPredicateValueState.Any) ||
                (parameter.StateSelector(predicate) == MatchPredicateValueState.Value && parameter.PredicateValueSelector(predicate).Equals(parameter.TicketValueSelector(ticket))))
            {
                result.matches += 1;
                result.weight += parameter.Weight;
            }
        }

        return result;
    }

    private class PredicateParameters
    {
        public PredicateParameters(Func<TruckTicketEntity, object> ticketValueSelector,
                                   Func<MatchPredicateEntity, object> predicateValueSelector,
                                   Func<MatchPredicateEntity, MatchPredicateValueState> stateSelector,
                                   int weight)
        {
            TicketValueSelector = ticketValueSelector;
            PredicateValueSelector = predicateValueSelector;
            StateSelector = stateSelector;
            Weight = weight;
        }

        public Func<TruckTicketEntity, object> TicketValueSelector { get; }

        public Func<MatchPredicateEntity, object> PredicateValueSelector { get; }

        public Func<MatchPredicateEntity, MatchPredicateValueState> StateSelector { get; }

        public int Weight { get; }
    }

    #region New Ranking Algorithm

    public List<RankConfiguration> EvaluatePredicateRank(List<RankConfiguration> configs,
                                                         string[] propertyValues,
                                                         Dictionary<string, int> propertyWeights,
                                                         string wildcard,
                                                         bool includeNonMatchingConfigs,
                                                         bool includeOnlyExactMatchingConfigs)
    {
        var rankedConfigs =
            configs.OrderByDescending(config => ComputeRank(config, propertyValues, propertyWeights, wildcard),
                                      new RankComparer()).ToList();

        if (includeOnlyExactMatchingConfigs)
        {
            return rankedConfigs.Where(config =>
                                       {
                                           var computedRank = ComputeRank(config, propertyValues, propertyWeights, wildcard);
                                           return computedRank.numMatches == config.Predicates?.Length && computedRank.numMatches > 0;
                                       }).ToList();
        }

        return includeNonMatchingConfigs
                   ? rankedConfigs.ToList()
                   : rankedConfigs.Where(config =>
                                             ComputeRank(config, propertyValues, propertyWeights, wildcard).numMatches > 0).ToList();
    }

    private (int numMatches, int weight, int numExactMatches) ComputeRank(RankConfiguration config,
                                                                          string[] propertyValues,
                                                                          IReadOnlyDictionary<string, int> propertyWeights,
                                                                          string wildcard = "*")
    {
        var wildcardMatches = config.Predicates
                                    .Where(predicate => predicate.EndsWith(wildcard))
                                    .Where(predicate => propertyValues.Any(value => value.StartsWith(PropertyOf(predicate))))
                                    .Select(predicate =>
                                            {
                                                propertyWeights.TryGetValue(PropertyOf(predicate), out var weight);
                                                return weight;
                                            })
                                    .ToArray();

        var exactMatches = config.Predicates
                                 .Where(predicate => !predicate.EndsWith(wildcard))
                                 .Intersect(propertyValues)
                                 .Select(predicate =>
                                         {
                                             propertyWeights.TryGetValue(PropertyOf(predicate), out var weight);
                                             return weight;
                                         })
                                 .ToArray();

        return (wildcardMatches.Length + exactMatches.Length, wildcardMatches.Sum() + exactMatches.Sum(),
                exactMatches.Length);
    }

    public IEnumerable<RankConfiguration> GetMatchingConfigs(List<RankConfiguration> configs,
                                                             string[] propertyValues,
                                                             Dictionary<string, int> propertyWeights,
                                                             string wildcard = "*")
    {
        return configs.Where(config => ComputeRank(config, propertyValues, propertyWeights, wildcard).numMatches > 0);
    }

    private static string PropertyOf(string predicate)
    {
        return predicate[..predicate.IndexOf(':')];
    }

    private sealed class RankComparer : IComparer<(int numMatches, int weight, int numExactMatches)>
    {
        public int Compare((int numMatches, int weight, int numExactMatches) x,
                           (int numMatches, int weight, int numExactMatches) y)
        {
            var intComparer = Comparer<int>.Default;
            var item1Comparison = intComparer.Compare(x.numMatches, y.numMatches);
            if (item1Comparison != 0)
            {
                return item1Comparison;
            }

            var item2Comparison = intComparer.Compare(x.weight, y.weight);
            if (item2Comparison != 0)
            {
                return item2Comparison;
            }

            return intComparer.Compare(x.numExactMatches, y.numExactMatches);
        }
    }

    #endregion
}
