using System.Collections.Generic;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;

namespace SE.TruckTicketing.Domain.Entities.SalesLine;
public class AdditionalServicesConfigurationHelper
{
    public static string[] BuildMatchPredicates(AdditionalServicesConfigurationMatchPredicateEntity matchPredicate)
    {
        var predicates = new List<string>();

        AddWellClassificationToPredicates(matchPredicate, predicates);

        AddSourceLocationToPredicates(matchPredicate, predicates);

        AddFacilityServiceSubstanceToPredicates(matchPredicate, predicates);

        return predicates.ToArray();
    }

    public static void AddWellClassificationToPredicates(AdditionalServicesConfigurationMatchPredicateEntity matchPredicate, ICollection<string> predicates)
    {
        if (matchPredicate.WellClassificationState is MatchPredicateValueState.Value or MatchPredicateValueState.Any)
        {
            var value = matchPredicate.WellClassificationState is MatchPredicateValueState.Value ? matchPredicate.WellClassification.ToString() : "*";

            predicates.Add($"WellClassification:{value}");
        }
    }

    public static void AddSourceLocationToPredicates(AdditionalServicesConfigurationMatchPredicateEntity matchPredicate, List<string> predicates)
    {
        if (matchPredicate.SourceIdentifierValueState is MatchPredicateValueState.Value or MatchPredicateValueState.Any)
        {
            var value = matchPredicate.SourceIdentifierValueState is MatchPredicateValueState.Value ? matchPredicate.SourceLocationId?.ToString() : "*";

            predicates.Add($"SourceLocation:{value}");
        }
    }

    public static void AddFacilityServiceSubstanceToPredicates(AdditionalServicesConfigurationMatchPredicateEntity matchPredicate, List<string> predicates)
    {
        if (matchPredicate.SubstanceValueState is MatchPredicateValueState.Value or MatchPredicateValueState.Any)
        {
            var value = matchPredicate.SubstanceValueState is MatchPredicateValueState.Value ? matchPredicate.FacilityServiceSubstanceId?.ToString() : "*";

            predicates.Add($"FacilityServiceSubstance:{value}");
        }
    }
}
