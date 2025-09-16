using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;
using SE.TruckTicketing.Domain.Entities.SalesLine;

namespace SE.TruckTicketing.Domain.Tests.SalesLine;

[TestClass]
public class AdditionalServicesConfigHelperTests
{
    [TestMethod]
    public void BuildMatchPredicates_ReturnsArrayOfThree_AllNotSetMatchStates_ReturnsZero()
    {
        //setup
        Guid? sourceLocationId = Guid.NewGuid();
        Guid? facilityServiceSubstanceId = Guid.NewGuid();

        AdditionalServicesConfigurationMatchPredicateEntity matchPredicate = new AdditionalServicesConfigurationMatchPredicateEntity { 
            WellClassificationState = MatchPredicateValueState.NotSet,
            WellClassification = WellClassifications.Completions,
            SourceIdentifierValueState = MatchPredicateValueState.NotSet,
            SourceLocationId = sourceLocationId,
            SubstanceValueState = MatchPredicateValueState.NotSet,
            FacilityServiceSubstanceId = facilityServiceSubstanceId,
        };

        //execute
        var results = AdditionalServicesConfigurationHelper.BuildMatchPredicates(matchPredicate);

        //assert
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Length);
    }

    [TestMethod]
    public void BuildMatchPredicates_ReturnsArrayOfThree_AllAnyMatchStates()
    {
        //setup
        Guid? sourceLocationId = Guid.NewGuid();
        Guid? facilityServiceSubstanceId = Guid.NewGuid();

        AdditionalServicesConfigurationMatchPredicateEntity matchPredicate = new AdditionalServicesConfigurationMatchPredicateEntity { 
            WellClassificationState = MatchPredicateValueState.Any,
            WellClassification = WellClassifications.Completions,
            SourceIdentifierValueState = MatchPredicateValueState.Any,
            SourceLocationId = sourceLocationId,
            SubstanceValueState = MatchPredicateValueState.Any,
            FacilityServiceSubstanceId = facilityServiceSubstanceId,
        };

        //execute
        var results = AdditionalServicesConfigurationHelper.BuildMatchPredicates(matchPredicate);

        //assert
        Assert.IsNotNull(results);
        Assert.AreEqual(3, results.Length);
        var first = results[0];
        Assert.IsNotNull(first);
        Assert.AreEqual("WellClassification:*", first);
        var second = results[1];
        Assert.AreEqual($"SourceLocation:*", second);
        var third = results[2];
        Assert.AreEqual($"FacilityServiceSubstance:*", third);
    }

    [TestMethod]
    public void BuildMatchPredicates_ReturnsArrayOfThree_AllValueMatchStates()
    {
        //setup
        Guid? sourceLocationId = Guid.NewGuid();
        Guid? facilityServiceSubstanceId = Guid.NewGuid();

        AdditionalServicesConfigurationMatchPredicateEntity matchPredicate = new AdditionalServicesConfigurationMatchPredicateEntity { 
            WellClassificationState = MatchPredicateValueState.Value,
            WellClassification = WellClassifications.Completions,
            SourceIdentifierValueState = MatchPredicateValueState.Value,
            SourceLocationId = sourceLocationId,
            SubstanceValueState = MatchPredicateValueState.Value,
            FacilityServiceSubstanceId = facilityServiceSubstanceId,
        };

        //execute
        var results = AdditionalServicesConfigurationHelper.BuildMatchPredicates(matchPredicate);

        //assert
        Assert.IsNotNull(results);
        Assert.AreEqual(3, results.Length);
        var first = results[0];
        Assert.IsNotNull(first);
        Assert.AreEqual("WellClassification:Completions", first);
        var second = results[1];
        Assert.AreEqual($"SourceLocation:{sourceLocationId}", second);
        var third = results[2];
        Assert.AreEqual($"FacilityServiceSubstance:{facilityServiceSubstanceId}", third);
    }

    [TestMethod]
    public void AddFacilityServiceSubstanceToPredicates_AnyState_NoClassification_ReturnsStar()
    {
        //setup
        var predicates = new List<string>();
        AdditionalServicesConfigurationMatchPredicateEntity matchPredicate = new AdditionalServicesConfigurationMatchPredicateEntity
        {
            SubstanceValueState = MatchPredicateValueState.Any,
        };

        //execute
        AdditionalServicesConfigurationHelper.AddFacilityServiceSubstanceToPredicates(matchPredicate, predicates);

        //assert
        Assert.AreEqual(1, predicates.Count);
        Assert.AreEqual($"FacilityServiceSubstance:*", predicates[0]);
    }
    
    [TestMethod]
    public void AddFacilityServiceSubstanceToPredicates_ValueState_AddsId()
    {
        //setup
        var predicates = new List<string>();
        Guid? facilityServiceSubstanceId = Guid.NewGuid();

        AdditionalServicesConfigurationMatchPredicateEntity matchPredicate = new AdditionalServicesConfigurationMatchPredicateEntity
        {
            SubstanceValueState = MatchPredicateValueState.Value,
            FacilityServiceSubstanceId = facilityServiceSubstanceId
        };

        //execute
        AdditionalServicesConfigurationHelper.AddFacilityServiceSubstanceToPredicates(matchPredicate, predicates);

        //assert
        Assert.AreEqual(1, predicates.Count);
        Assert.AreEqual($"FacilityServiceSubstance:{facilityServiceSubstanceId}", predicates[0]);
    }
    
    [TestMethod]
    public void AddFacilityServiceSubstanceToPredicates_AnyState_AddsStar()
    {
        //setup
        var predicates = new List<string>();
        Guid? facilityServiceSubstanceId = Guid.NewGuid();

        AdditionalServicesConfigurationMatchPredicateEntity matchPredicate = new AdditionalServicesConfigurationMatchPredicateEntity
        {
            SubstanceValueState = MatchPredicateValueState.Any,
            FacilityServiceSubstanceId = facilityServiceSubstanceId
        };

        //execute
        AdditionalServicesConfigurationHelper.AddFacilityServiceSubstanceToPredicates(matchPredicate, predicates);

        //assert
        Assert.AreEqual(1, predicates.Count);
        Assert.AreEqual($"FacilityServiceSubstance:*", predicates[0]);
    }
    
    [TestMethod]
    public void AddSourceLocationToPredicates_AnyState_NoClassification_ReturnsStar()
    {
        //setup
        var predicates = new List<string>();
        AdditionalServicesConfigurationMatchPredicateEntity matchPredicate = new AdditionalServicesConfigurationMatchPredicateEntity
        {
            SourceIdentifierValueState = MatchPredicateValueState.Any,
        };

        //execute
        AdditionalServicesConfigurationHelper.AddSourceLocationToPredicates(matchPredicate, predicates);

        //assert
        Assert.AreEqual(1, predicates.Count);
        Assert.AreEqual($"SourceLocation:*", predicates[0]);
    }
    
    [TestMethod]
    public void AddSourceLocationToPredicates_ValueState_AddsId()
    {
        //setup
        var predicates = new List<string>();
        Guid? sourceLocationId = Guid.NewGuid();

        AdditionalServicesConfigurationMatchPredicateEntity matchPredicate = new AdditionalServicesConfigurationMatchPredicateEntity
        {
            SourceIdentifierValueState = MatchPredicateValueState.Value,
            SourceLocationId = sourceLocationId
        };

        //execute
        AdditionalServicesConfigurationHelper.AddSourceLocationToPredicates(matchPredicate, predicates);

        //assert
        Assert.AreEqual(1, predicates.Count);
        Assert.AreEqual($"SourceLocation:{sourceLocationId}", predicates[0]);
    }
    
    [TestMethod]
    public void AddSourceLocationToPredicates_AnyState_AddsStar()
    {
        //setup
        var predicates = new List<string>();
        Guid? sourceLocationId = Guid.NewGuid();

        AdditionalServicesConfigurationMatchPredicateEntity matchPredicate = new AdditionalServicesConfigurationMatchPredicateEntity
        {
            SourceIdentifierValueState = MatchPredicateValueState.Any,
            SourceLocationId = sourceLocationId
        };

        //execute
        AdditionalServicesConfigurationHelper.AddSourceLocationToPredicates(matchPredicate, predicates);

        //assert
        Assert.AreEqual(1, predicates.Count);
        Assert.AreEqual($"SourceLocation:*", predicates[0]);
    }
    
    [TestMethod]
    public void AddWellClassificationToPredicates_DoesNotAddToListForUnspecifiedState()
    {
        //setup
        var predicates = new List<string>();
        AdditionalServicesConfigurationMatchPredicateEntity matchPredicate = new AdditionalServicesConfigurationMatchPredicateEntity
        {
            WellClassificationState = MatchPredicateValueState.Unspecified,
            WellClassification = WellClassifications.Completions
        };

        //execute
        AdditionalServicesConfigurationHelper.AddWellClassificationToPredicates(matchPredicate, predicates);

        //assert
        Assert.AreEqual(0, predicates.Count);
    }
    
    [TestMethod]
    public void AddWellClassificationToPredicates_AnyState_NoClassification_ReturnsStar()
    {
        //setup
        var predicates = new List<string>();
        AdditionalServicesConfigurationMatchPredicateEntity matchPredicate = new AdditionalServicesConfigurationMatchPredicateEntity
        {
            WellClassificationState = MatchPredicateValueState.Any,
        };

        //execute
        AdditionalServicesConfigurationHelper.AddWellClassificationToPredicates(matchPredicate, predicates);

        //assert
        Assert.AreEqual(1, predicates.Count);
        Assert.AreEqual("WellClassification:*", predicates[0]);
    }
    
    [TestMethod]
    public void AddWellClassificationToPredicates_AddsValueStateForCompletions()
    {
        //setup
        var predicates = new List<string>();
        AdditionalServicesConfigurationMatchPredicateEntity matchPredicate = new AdditionalServicesConfigurationMatchPredicateEntity
        {
            WellClassificationState = MatchPredicateValueState.Value,
            WellClassification = WellClassifications.Completions
        };

        //execute
        AdditionalServicesConfigurationHelper.AddWellClassificationToPredicates(matchPredicate, predicates);

        //assert
        Assert.AreEqual(1, predicates.Count);
        Assert.AreEqual("WellClassification:Completions", predicates[0]);
    }
    
    [TestMethod]
    public void AddWellClassificationToPredicates_AddsAnyStateForConstruction()
    {
        //setup
        var predicates = new List<string>();
        AdditionalServicesConfigurationMatchPredicateEntity matchPredicate = new AdditionalServicesConfigurationMatchPredicateEntity
        {
            WellClassificationState = MatchPredicateValueState.Value,
            WellClassification = WellClassifications.Construction
        };

        //execute
        AdditionalServicesConfigurationHelper.AddWellClassificationToPredicates(matchPredicate, predicates);

        //assert
        Assert.AreEqual(1, predicates.Count);
        Assert.AreEqual("WellClassification:Construction", predicates[0]);
    }
}
