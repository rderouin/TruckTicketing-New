using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Extensions;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.ManualTruckTicket;

[TestClass]
public class MatchPredicateRankManagerTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateMatches_PredicateWithSourceLocationValue_MatchWithTruckTicketSourceLocation()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithSourceLocationMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithSourceLocationMatch.SourceLocationValueState = MatchPredicateValueState.Value;
        matchPredicateWithSourceLocationMatch.SourceLocationId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");

        scope.DefaultTruckTicket.SourceLocationId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithSourceLocationMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.matches == 1);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateMatches_PredicateWithSourceLocationAny_NoMatchingTruckTicketSourceLocation()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithSourceLocationMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithSourceLocationMatch.SourceLocationValueState = MatchPredicateValueState.Any;

        scope.DefaultTruckTicket.SourceLocationId = Guid.NewGuid();
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithSourceLocationMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.matches == 1);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateRank_PredicateWithSourceLocationValue_MatchWithTruckTicketSourceLocation()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithSourceLocationMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithSourceLocationMatch.SourceLocationValueState = MatchPredicateValueState.Value;
        matchPredicateWithSourceLocationMatch.SourceLocationId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");

        scope.DefaultTruckTicket.SourceLocationId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithSourceLocationMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.weight == 21);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateRank_PredicateWithSourceLocationAny_NoMatchingTruckTicketSourceLocation()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithSourceLocationMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithSourceLocationMatch.SourceLocationValueState = MatchPredicateValueState.Any;

        scope.DefaultTruckTicket.SourceLocationId = Guid.NewGuid();
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithSourceLocationMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.weight == 21);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateMatches_PredicateWithStreamValue_MatchWithTruckTicketStream()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithStreamMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithStreamMatch.StreamValueState = MatchPredicateValueState.Value;
        matchPredicateWithStreamMatch.Stream = Stream.Pipeline;

        scope.DefaultTruckTicket.Stream = Stream.Pipeline;
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithStreamMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.matches == 1);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(Stream.Pipeline)]
    [DataRow(Stream.Terminalling)]
    [DataRow(Stream.Landfill)]
    [DataRow(Stream.Treating)]
    [DataRow(Stream.Waste)]
    [DataRow(Stream.Water)]
    public void MatchPredicateRankManager_EvaluateMatches_PredicateWithStreamAny_NoMatchingTruckTicketStream(Stream stream)
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithStreamMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithStreamMatch.StreamValueState = MatchPredicateValueState.Any;

        scope.DefaultTruckTicket.Stream = stream;
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithStreamMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.matches == 1);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateRank_PredicateWithStreamValue_MatchWithTruckTicketStream()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithStreamMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithStreamMatch.StreamValueState = MatchPredicateValueState.Value;
        matchPredicateWithStreamMatch.Stream = Stream.Pipeline;

        scope.DefaultTruckTicket.Stream = Stream.Pipeline;
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithStreamMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.weight == 13);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(Stream.Pipeline)]
    [DataRow(Stream.Terminalling)]
    [DataRow(Stream.Landfill)]
    [DataRow(Stream.Treating)]
    [DataRow(Stream.Waste)]
    [DataRow(Stream.Water)]
    public void MatchPredicateRankManager_EvaluateRank_PredicateWithStreamAny_NoMatchingTruckTicketStream(Stream stream)
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithStreamMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithStreamMatch.StreamValueState = MatchPredicateValueState.Any;

        scope.DefaultTruckTicket.Stream = stream;
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithStreamMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.weight == 13);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateMatches_PredicateWithWellClassificationValue_MatchWithTruckTicketWellClassification()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithWllClassificationMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithWllClassificationMatch.WellClassificationState = MatchPredicateValueState.Value;
        matchPredicateWithWllClassificationMatch.WellClassification = WellClassifications.Drilling;

        scope.DefaultTruckTicket.WellClassification = WellClassifications.Drilling;
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithWllClassificationMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.matches == 1);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(WellClassifications.Completions)]
    [DataRow(WellClassifications.Drilling)]
    [DataRow(WellClassifications.Production)]
    public void MatchPredicateRankManager_EvaluateMatches_PredicateWithWellClassificationAny_NoMatchingTruckTicketWellClassification(WellClassifications wellClassifications)
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithWllClassificationMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithWllClassificationMatch.WellClassificationState = MatchPredicateValueState.Any;

        scope.DefaultTruckTicket.WellClassification = wellClassifications;
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithWllClassificationMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.matches == 1);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateRank_PredicateWithWellClassificationValue_MatchWithTruckTicketWellClassification()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithWllClassificationMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithWllClassificationMatch.WellClassificationState = MatchPredicateValueState.Value;
        matchPredicateWithWllClassificationMatch.WellClassification = WellClassifications.Drilling;

        scope.DefaultTruckTicket.WellClassification = WellClassifications.Drilling;
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithWllClassificationMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.weight == 8);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(WellClassifications.Completions)]
    [DataRow(WellClassifications.Drilling)]
    [DataRow(WellClassifications.Production)]
    public void MatchPredicateRankManager_EvaluateRank_PredicateWithWellClassificationAny_NoMatchingTruckTicketWellClassification(WellClassifications wellClassifications)
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithWllClassificationMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithWllClassificationMatch.WellClassificationState = MatchPredicateValueState.Any;

        scope.DefaultTruckTicket.WellClassification = wellClassifications;
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithWllClassificationMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.weight == 8);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateMatches_PredicateWithServiceTypeValue_MatchWithTruckTicketServiceType()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithServiceTypeMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithServiceTypeMatch.ServiceTypeValueState = MatchPredicateValueState.Value;
        matchPredicateWithServiceTypeMatch.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");

        scope.DefaultTruckTicket.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithServiceTypeMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.matches == 1);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateMatches_PredicateWithServiceTypeAny_NoMatchingTruckTicketServiceType()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithServiceTypeMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithServiceTypeMatch.ServiceTypeValueState = MatchPredicateValueState.Any;

        scope.DefaultTruckTicket.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithServiceTypeMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.matches == 1);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateRank_PredicateWithServiceTypeValue_MatchWithTruckTicketServiceType()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithServiceTypeMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithServiceTypeMatch.ServiceTypeValueState = MatchPredicateValueState.Value;
        matchPredicateWithServiceTypeMatch.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");

        scope.DefaultTruckTicket.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithServiceTypeMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.weight == 5);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateRank_PredicateWithServiceTypeAny_NoMatchingTruckTicketServiceType()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithServiceTypeMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithServiceTypeMatch.ServiceTypeValueState = MatchPredicateValueState.Any;

        scope.DefaultTruckTicket.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithServiceTypeMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.weight == 5);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateMatches_PredicateWithSubstanceValue_MatchWithTruckTicketSubstance()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithSubstanceMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithSubstanceMatch.SubstanceValueState = MatchPredicateValueState.Value;
        matchPredicateWithSubstanceMatch.SubstanceId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");

        scope.DefaultTruckTicket.SubstanceId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithSubstanceMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.matches == 1);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateMatches_PredicateWithSubstanceAny_NoMatchingTruckTicketSubstance()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithSubstanceMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithSubstanceMatch.SubstanceValueState = MatchPredicateValueState.Any;

        scope.DefaultTruckTicket.SubstanceId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithSubstanceMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.matches == 1);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateRank_PredicateWithSubstanceValue_MatchWithTruckTicketSubstance()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithSubstanceMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithSubstanceMatch.SubstanceValueState = MatchPredicateValueState.Value;
        matchPredicateWithSubstanceMatch.SubstanceId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");

        scope.DefaultTruckTicket.SubstanceId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithSubstanceMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.weight == 3);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateRank_PredicateWithSubstanceAny_NoMatchingTruckTicketSubstance()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithSubstanceMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithSubstanceMatch.SubstanceValueState = MatchPredicateValueState.Any;

        scope.DefaultTruckTicket.SubstanceId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithSubstanceMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.weight == 3);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(WellClassifications.Completions, Stream.Pipeline)]
    [DataRow(WellClassifications.Completions, Stream.Terminalling)]
    [DataRow(WellClassifications.Completions, Stream.Landfill)]
    [DataRow(WellClassifications.Completions, Stream.Treating)]
    [DataRow(WellClassifications.Completions, Stream.Waste)]
    [DataRow(WellClassifications.Completions, Stream.Water)]
    [DataRow(WellClassifications.Drilling, Stream.Pipeline)]
    [DataRow(WellClassifications.Drilling, Stream.Terminalling)]
    [DataRow(WellClassifications.Drilling, Stream.Landfill)]
    [DataRow(WellClassifications.Drilling, Stream.Treating)]
    [DataRow(WellClassifications.Drilling, Stream.Waste)]
    [DataRow(WellClassifications.Drilling, Stream.Water)]
    [DataRow(WellClassifications.Production, Stream.Pipeline)]
    [DataRow(WellClassifications.Production, Stream.Terminalling)]
    [DataRow(WellClassifications.Production, Stream.Landfill)]
    [DataRow(WellClassifications.Production, Stream.Treating)]
    [DataRow(WellClassifications.Production, Stream.Waste)]
    [DataRow(WellClassifications.Production, Stream.Water)]
    public void MatchPredicateRankManager_EvaluateMatches_PredicateWithAllCriteriaSetAny_MatchWithAllCriteriaInTruckTicket(WellClassifications wellClassifications, Stream stream)
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithCriteriaAnyMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithCriteriaAnyMatch.SubstanceValueState = MatchPredicateValueState.Any;
        matchPredicateWithCriteriaAnyMatch.WellClassificationState = MatchPredicateValueState.Any;
        matchPredicateWithCriteriaAnyMatch.SourceLocationValueState = MatchPredicateValueState.Any;
        matchPredicateWithCriteriaAnyMatch.StreamValueState = MatchPredicateValueState.Any;
        matchPredicateWithCriteriaAnyMatch.ServiceTypeValueState = MatchPredicateValueState.Any;

        scope.DefaultTruckTicket.SubstanceId = Guid.Parse("e7efb981-9442-4366-90e0-3d4e22ede60c");
        scope.DefaultTruckTicket.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        scope.DefaultTruckTicket.WellClassification = wellClassifications;
        scope.DefaultTruckTicket.Stream = stream;
        scope.DefaultTruckTicket.SourceLocationId = Guid.Parse("a7e5ce4e-d7fe-40af-af63-c70ef50b7c39");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithCriteriaAnyMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.matches == 5);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(WellClassifications.Production, WellClassifications.Production)]
    [DataRow(WellClassifications.Completions, WellClassifications.Completions)]
    [DataRow(WellClassifications.Drilling, WellClassifications.Drilling)]
    public void MatchPredicateRankManager_EvaluateMatches_PredicateWithValueExactMatch_WellClassificationCombination_MatchingWithAllCriteriaInTruckTicket(
        WellClassifications matchPredicateWellClassification,
        WellClassifications truckTicketWellClassification)
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithCriteriaExactValue = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithCriteriaExactValue.SubstanceValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.SubstanceId = Guid.Parse("e7efb981-9442-4366-90e0-3d4e22ede60c");

        matchPredicateWithCriteriaExactValue.WellClassificationState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.WellClassification = matchPredicateWellClassification;

        matchPredicateWithCriteriaExactValue.SourceLocationValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.SourceLocationId = Guid.Parse("a7e5ce4e-d7fe-40af-af63-c70ef50b7c39");

        matchPredicateWithCriteriaExactValue.StreamValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.Stream = Stream.Pipeline;

        matchPredicateWithCriteriaExactValue.ServiceTypeValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");

        scope.DefaultTruckTicket.SubstanceId = Guid.Parse("e7efb981-9442-4366-90e0-3d4e22ede60c");
        scope.DefaultTruckTicket.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        scope.DefaultTruckTicket.WellClassification = truckTicketWellClassification;
        scope.DefaultTruckTicket.Stream = Stream.Pipeline;
        scope.DefaultTruckTicket.SourceLocationId = Guid.Parse("a7e5ce4e-d7fe-40af-af63-c70ef50b7c39");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithCriteriaExactValue);

        //// assert
        Assert.IsTrue(matchEvaluate.matches == 5);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(Stream.Landfill, Stream.Landfill)]
    [DataRow(Stream.Pipeline, Stream.Pipeline)]
    [DataRow(Stream.Terminalling, Stream.Terminalling)]
    [DataRow(Stream.Waste, Stream.Waste)]
    [DataRow(Stream.Water, Stream.Water)]
    [DataRow(Stream.Treating, Stream.Treating)]
    public void MatchPredicateRankManager_EvaluateMatches_PredicateWithValueExactMatch_StreamCombination_MatchingWithAllCriteriaInTruckTicket(Stream matchPredicateStream,
        Stream truckTicketStream)
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithCriteriaExactValue = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithCriteriaExactValue.SubstanceValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.SubstanceId = Guid.Parse("e7efb981-9442-4366-90e0-3d4e22ede60c");

        matchPredicateWithCriteriaExactValue.WellClassificationState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.WellClassification = WellClassifications.Completions;

        matchPredicateWithCriteriaExactValue.SourceLocationValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.SourceLocationId = Guid.Parse("a7e5ce4e-d7fe-40af-af63-c70ef50b7c39");

        matchPredicateWithCriteriaExactValue.StreamValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.Stream = matchPredicateStream;

        matchPredicateWithCriteriaExactValue.ServiceTypeValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");

        scope.DefaultTruckTicket.SubstanceId = Guid.Parse("e7efb981-9442-4366-90e0-3d4e22ede60c");
        scope.DefaultTruckTicket.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        scope.DefaultTruckTicket.WellClassification = WellClassifications.Completions;
        scope.DefaultTruckTicket.Stream = truckTicketStream;
        scope.DefaultTruckTicket.SourceLocationId = Guid.Parse("a7e5ce4e-d7fe-40af-af63-c70ef50b7c39");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithCriteriaExactValue);

        //// assert
        Assert.IsTrue(matchEvaluate.matches == 5);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(WellClassifications.Completions, Stream.Pipeline)]
    [DataRow(WellClassifications.Completions, Stream.Terminalling)]
    [DataRow(WellClassifications.Completions, Stream.Landfill)]
    [DataRow(WellClassifications.Completions, Stream.Treating)]
    [DataRow(WellClassifications.Completions, Stream.Waste)]
    [DataRow(WellClassifications.Completions, Stream.Water)]
    [DataRow(WellClassifications.Drilling, Stream.Pipeline)]
    [DataRow(WellClassifications.Drilling, Stream.Terminalling)]
    [DataRow(WellClassifications.Drilling, Stream.Landfill)]
    [DataRow(WellClassifications.Drilling, Stream.Treating)]
    [DataRow(WellClassifications.Drilling, Stream.Waste)]
    [DataRow(WellClassifications.Drilling, Stream.Water)]
    [DataRow(WellClassifications.Production, Stream.Pipeline)]
    [DataRow(WellClassifications.Production, Stream.Terminalling)]
    [DataRow(WellClassifications.Production, Stream.Landfill)]
    [DataRow(WellClassifications.Production, Stream.Treating)]
    [DataRow(WellClassifications.Production, Stream.Waste)]
    [DataRow(WellClassifications.Production, Stream.Water)]
    public void MatchPredicateRankManager_EvaluateRank_PredicateWithAllCriteriaSetAny_MatchWithAllCriteriaInTruckTicket(WellClassifications wellClassifications, Stream stream)
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithCriteriaAnyMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithCriteriaAnyMatch.SubstanceValueState = MatchPredicateValueState.Any;
        matchPredicateWithCriteriaAnyMatch.WellClassificationState = MatchPredicateValueState.Any;
        matchPredicateWithCriteriaAnyMatch.SourceLocationValueState = MatchPredicateValueState.Any;
        matchPredicateWithCriteriaAnyMatch.StreamValueState = MatchPredicateValueState.Any;
        matchPredicateWithCriteriaAnyMatch.ServiceTypeValueState = MatchPredicateValueState.Any;

        scope.DefaultTruckTicket.SubstanceId = Guid.Parse("e7efb981-9442-4366-90e0-3d4e22ede60c");
        scope.DefaultTruckTicket.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        scope.DefaultTruckTicket.WellClassification = wellClassifications;
        scope.DefaultTruckTicket.Stream = stream;
        scope.DefaultTruckTicket.SourceLocationId = Guid.Parse("a7e5ce4e-d7fe-40af-af63-c70ef50b7c39");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithCriteriaAnyMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.weight == 50);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(WellClassifications.Production, WellClassifications.Production)]
    [DataRow(WellClassifications.Completions, WellClassifications.Completions)]
    [DataRow(WellClassifications.Drilling, WellClassifications.Drilling)]
    public void MatchPredicateRankManager_EvaluateRank_PredicateWithValueExactMatch_WellClassificationCombination_MatchingWithAllCriteriaInTruckTicket(
        WellClassifications matchPredicateWellClassification,
        WellClassifications truckTicketWellClassification)
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithCriteriaExactValue = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithCriteriaExactValue.SubstanceValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.SubstanceId = Guid.Parse("e7efb981-9442-4366-90e0-3d4e22ede60c");

        matchPredicateWithCriteriaExactValue.WellClassificationState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.WellClassification = matchPredicateWellClassification;

        matchPredicateWithCriteriaExactValue.SourceLocationValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.SourceLocationId = Guid.Parse("a7e5ce4e-d7fe-40af-af63-c70ef50b7c39");

        matchPredicateWithCriteriaExactValue.StreamValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.Stream = Stream.Pipeline;

        matchPredicateWithCriteriaExactValue.ServiceTypeValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");

        scope.DefaultTruckTicket.SubstanceId = Guid.Parse("e7efb981-9442-4366-90e0-3d4e22ede60c");
        scope.DefaultTruckTicket.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        scope.DefaultTruckTicket.WellClassification = truckTicketWellClassification;
        scope.DefaultTruckTicket.Stream = Stream.Pipeline;
        scope.DefaultTruckTicket.SourceLocationId = Guid.Parse("a7e5ce4e-d7fe-40af-af63-c70ef50b7c39");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithCriteriaExactValue);

        //// assert
        Assert.IsTrue(matchEvaluate.weight == 50);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(Stream.Landfill, Stream.Landfill)]
    [DataRow(Stream.Pipeline, Stream.Pipeline)]
    [DataRow(Stream.Terminalling, Stream.Terminalling)]
    [DataRow(Stream.Waste, Stream.Waste)]
    [DataRow(Stream.Water, Stream.Water)]
    [DataRow(Stream.Treating, Stream.Treating)]
    public void MatchPredicateRankManager_EvaluateRank_PredicateWithValueExactMatch_StreamCombination_MatchingWithAllCriteriaInTruckTicket(Stream matchPredicateStream,
        Stream truckTicketStream)
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithCriteriaExactValue = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithCriteriaExactValue.SubstanceValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.SubstanceId = Guid.Parse("e7efb981-9442-4366-90e0-3d4e22ede60c");

        matchPredicateWithCriteriaExactValue.WellClassificationState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.WellClassification = WellClassifications.Drilling;

        matchPredicateWithCriteriaExactValue.SourceLocationValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.SourceLocationId = Guid.Parse("a7e5ce4e-d7fe-40af-af63-c70ef50b7c39");

        matchPredicateWithCriteriaExactValue.StreamValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.Stream = matchPredicateStream;

        matchPredicateWithCriteriaExactValue.ServiceTypeValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");

        scope.DefaultTruckTicket.SubstanceId = Guid.Parse("e7efb981-9442-4366-90e0-3d4e22ede60c");
        scope.DefaultTruckTicket.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        scope.DefaultTruckTicket.WellClassification = WellClassifications.Drilling;
        scope.DefaultTruckTicket.Stream = matchPredicateStream;
        scope.DefaultTruckTicket.SourceLocationId = Guid.Parse("a7e5ce4e-d7fe-40af-af63-c70ef50b7c39");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithCriteriaExactValue);

        //// assert
        Assert.IsTrue(matchEvaluate.weight == 50);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(WellClassifications.Production, WellClassifications.Completions)]
    [DataRow(WellClassifications.Production, WellClassifications.Drilling)]
    [DataRow(WellClassifications.Completions, WellClassifications.Production)]
    [DataRow(WellClassifications.Completions, WellClassifications.Drilling)]
    [DataRow(WellClassifications.Drilling, WellClassifications.Production)]
    [DataRow(WellClassifications.Drilling, WellClassifications.Completions)]
    public void MatchPredicateRankManager_EvaluateMatches_PredicateWithWellClassificationValueNotMatch_MatchPredicateWithTruckTicket(WellClassifications matchPredicateWellClassification,
        WellClassifications truckTicketWellClassification)
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithCriteriaAnyMatch = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithCriteriaAnyMatch.SubstanceValueState = MatchPredicateValueState.Any;
        matchPredicateWithCriteriaAnyMatch.WellClassificationState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaAnyMatch.WellClassification = matchPredicateWellClassification;
        matchPredicateWithCriteriaAnyMatch.SourceLocationValueState = MatchPredicateValueState.Any;
        matchPredicateWithCriteriaAnyMatch.StreamValueState = MatchPredicateValueState.Any;
        matchPredicateWithCriteriaAnyMatch.ServiceTypeValueState = MatchPredicateValueState.Any;

        scope.DefaultTruckTicket.SubstanceId = Guid.Parse("e7efb981-9442-4366-90e0-3d4e22ede60c");
        scope.DefaultTruckTicket.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        scope.DefaultTruckTicket.WellClassification = truckTicketWellClassification;
        scope.DefaultTruckTicket.Stream = Stream.Pipeline;
        scope.DefaultTruckTicket.SourceLocationId = Guid.Parse("a7e5ce4e-d7fe-40af-af63-c70ef50b7c39");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithCriteriaAnyMatch);

        //// assert
        Assert.IsTrue(matchEvaluate.matches == 4);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(Stream.Terminalling, Stream.Landfill)]
    [DataRow(Stream.Terminalling, Stream.Pipeline)]
    [DataRow(Stream.Terminalling, Stream.Treating)]
    [DataRow(Stream.Terminalling, Stream.Waste)]
    [DataRow(Stream.Terminalling, Stream.Water)]
    [DataRow(Stream.Landfill, Stream.Pipeline)]
    [DataRow(Stream.Landfill, Stream.Treating)]
    [DataRow(Stream.Landfill, Stream.Waste)]
    [DataRow(Stream.Landfill, Stream.Water)]
    [DataRow(Stream.Landfill, Stream.Terminalling)]
    [DataRow(Stream.Pipeline, Stream.Landfill)]
    [DataRow(Stream.Pipeline, Stream.Treating)]
    [DataRow(Stream.Pipeline, Stream.Waste)]
    [DataRow(Stream.Pipeline, Stream.Water)]
    [DataRow(Stream.Pipeline, Stream.Terminalling)]
    [DataRow(Stream.Treating, Stream.Landfill)]
    [DataRow(Stream.Treating, Stream.Pipeline)]
    [DataRow(Stream.Treating, Stream.Waste)]
    [DataRow(Stream.Treating, Stream.Water)]
    [DataRow(Stream.Treating, Stream.Terminalling)]
    [DataRow(Stream.Waste, Stream.Landfill)]
    [DataRow(Stream.Waste, Stream.Pipeline)]
    [DataRow(Stream.Waste, Stream.Treating)]
    [DataRow(Stream.Waste, Stream.Water)]
    [DataRow(Stream.Waste, Stream.Terminalling)]
    [DataRow(Stream.Water, Stream.Landfill)]
    [DataRow(Stream.Water, Stream.Pipeline)]
    [DataRow(Stream.Water, Stream.Treating)]
    [DataRow(Stream.Water, Stream.Waste)]
    [DataRow(Stream.Water, Stream.Terminalling)]
    public void MatchPredicateRankManager_EvaluateMatches_PredicateWithStreamValueNotMatch_MatchPredicateWithTruckTicket(Stream matchPredicateStream,
                                                                                                                         Stream truckTicketStream)
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithCriteriaExactValue = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithCriteriaExactValue.SubstanceValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.SubstanceId = Guid.Parse("e7efb981-9442-4366-90e0-3d4e22ede60c");

        matchPredicateWithCriteriaExactValue.WellClassificationState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.WellClassification = WellClassifications.Drilling;

        matchPredicateWithCriteriaExactValue.SourceLocationValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.SourceLocationId = Guid.Parse("a7e5ce4e-d7fe-40af-af63-c70ef50b7c39");

        matchPredicateWithCriteriaExactValue.StreamValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.Stream = matchPredicateStream;

        matchPredicateWithCriteriaExactValue.ServiceTypeValueState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");

        scope.DefaultTruckTicket.SubstanceId = Guid.Parse("e7efb981-9442-4366-90e0-3d4e22ede60c");
        scope.DefaultTruckTicket.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        scope.DefaultTruckTicket.WellClassification = WellClassifications.Drilling;
        scope.DefaultTruckTicket.Stream = truckTicketStream;
        scope.DefaultTruckTicket.SourceLocationId = Guid.Parse("a7e5ce4e-d7fe-40af-af63-c70ef50b7c39");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithCriteriaExactValue);

        //// assert
        Assert.IsTrue(matchEvaluate.matches == 4);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluateMatches_NoMatchBetweenPredicateAndTruckTicket()
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithCriteriaExactValue = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithCriteriaExactValue.SubstanceValueState = MatchPredicateValueState.NotSet;

        matchPredicateWithCriteriaExactValue.WellClassificationState = MatchPredicateValueState.NotSet;

        matchPredicateWithCriteriaExactValue.SourceLocationValueState = MatchPredicateValueState.NotSet;

        matchPredicateWithCriteriaExactValue.StreamValueState = MatchPredicateValueState.NotSet;

        matchPredicateWithCriteriaExactValue.ServiceTypeValueState = MatchPredicateValueState.NotSet;

        scope.DefaultTruckTicket.SubstanceId = Guid.Parse("e7efb981-9442-4366-90e0-3d4e22ede60c");
        scope.DefaultTruckTicket.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        scope.DefaultTruckTicket.WellClassification = WellClassifications.Drilling;
        scope.DefaultTruckTicket.Stream = Stream.Landfill;
        scope.DefaultTruckTicket.SourceLocationId = Guid.Parse("a7e5ce4e-d7fe-40af-af63-c70ef50b7c39");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithCriteriaExactValue);

        //// assert
        Assert.IsTrue(matchEvaluate.weight == 0);
        Assert.IsTrue(matchEvaluate.matches == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(WellClassifications.Drilling)]
    [DataRow(WellClassifications.Completions)]
    public void MatchPredicateRankManager_EvaluateMatches_NoMatchBetweenPredicateAndTruckTicket_WithWellClassificationDiffer(WellClassifications matchPredicateWellClassification)
    {
        // arrange
        var scope = new DefaultScope();
        var matchPredicateWithCriteriaExactValue = scope.DefaultMatchPredicateEntity.Clone();
        matchPredicateWithCriteriaExactValue.SubstanceValueState = MatchPredicateValueState.NotSet;

        matchPredicateWithCriteriaExactValue.WellClassificationState = MatchPredicateValueState.Value;
        matchPredicateWithCriteriaExactValue.WellClassification = matchPredicateWellClassification;

        matchPredicateWithCriteriaExactValue.SourceLocationValueState = MatchPredicateValueState.NotSet;

        matchPredicateWithCriteriaExactValue.StreamValueState = MatchPredicateValueState.NotSet;

        matchPredicateWithCriteriaExactValue.ServiceTypeValueState = MatchPredicateValueState.NotSet;

        scope.DefaultTruckTicket.SubstanceId = Guid.Parse("e7efb981-9442-4366-90e0-3d4e22ede60c");
        scope.DefaultTruckTicket.ServiceTypeId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33");
        scope.DefaultTruckTicket.WellClassification = WellClassifications.Production;
        scope.DefaultTruckTicket.Stream = Stream.Landfill;
        scope.DefaultTruckTicket.SourceLocationId = Guid.Parse("a7e5ce4e-d7fe-40af-af63-c70ef50b7c39");
        // act
        var matchEvaluate = scope.InstanceUnderTest.Evaluate(scope.DefaultTruckTicket, matchPredicateWithCriteriaExactValue);

        //// assert
        Assert.IsTrue(matchEvaluate.weight == 0);
        Assert.IsTrue(matchEvaluate.matches == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(WellClassifications.Drilling)]
    [DataRow(WellClassifications.Completions)]
    [DataRow(WellClassifications.Industrial)]
    [DataRow(WellClassifications.Oilfield)]
    [DataRow(WellClassifications.Remediation)]
    public void MatchPredicateRankManager_EvaluatePredicateRank_MatchBasedOnExactMatch_WithWellClassificationDiffer(WellClassifications matchPredicateWellClassification)
    {
        // arrange
        var scope = new DefaultScope();
        var sourceLocationId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var substance = Guid.NewGuid();
        var matchPredicateValue = GenFu.GenFu.ListOf<MatchPredicateEntity>(4);
        var billingConfiguration = GenFu.GenFu.New<BillingConfigurationEntity>();
        matchPredicateValue.ForEach(x =>
                                    {
                                        x.IsEnabled = true;
                                        x.StartDate = null;
                                        x.EndDate = null;
                                        x.SourceLocationValueState = MatchPredicateValueState.Value;
                                        x.SourceLocationId = sourceLocationId;
                                        x.ServiceTypeValueState = MatchPredicateValueState.Any;
                                        x.SubstanceValueState = MatchPredicateValueState.Any;
                                        x.WellClassification = matchPredicateWellClassification;
                                        x.WellClassificationState = MatchPredicateValueState.Value;
                                        x.StreamValueState = MatchPredicateValueState.NotSet;
                                    });

        billingConfiguration.MatchCriteria = matchPredicateValue;
        var overlappingMatchPredicates = billingConfiguration.MatchCriteria.Select(predicate => (predicate, billingConfig: billingConfiguration)).ToList();
        var matchPredicates = scope.GenerateRankConfigurationInput(overlappingMatchPredicates);
        scope.DefaultTruckTicket.SubstanceId = substance;
        scope.DefaultTruckTicket.ServiceTypeId = serviceTypeId;
        scope.DefaultTruckTicket.WellClassification = WellClassifications.Production;
        scope.DefaultTruckTicket.Stream = Stream.Landfill;
        scope.DefaultTruckTicket.SourceLocationId = sourceLocationId;

        var truckTicketProperties = scope.GetTruckTicketProperties(scope.DefaultTruckTicket);
        // act
        var matchEvaluate = scope.InstanceUnderTest.EvaluatePredicateRank(matchPredicates, truckTicketProperties, scope._billingConfigurationWeights, "*", false, true);

        //// assert
        Assert.IsTrue(!matchEvaluate.Any());
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(Stream.Water)]
    [DataRow(Stream.Pipeline)]
    [DataRow(Stream.Terminalling)]
    [DataRow(Stream.Treating)]
    [DataRow(Stream.Waste)]
    public void MatchPredicateRankManager_EvaluatePredicateRank_MatchBasedOnExactMatch_WithStreamDiffer(Stream stream)
    {
        // arrange
        var scope = new DefaultScope();
        var sourceLocationId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var substance = Guid.NewGuid();
        var matchPredicateValue = GenFu.GenFu.ListOf<MatchPredicateEntity>(4);
        var billingConfiguration = GenFu.GenFu.New<BillingConfigurationEntity>();
        matchPredicateValue.ForEach(x =>
                                    {
                                        x.IsEnabled = true;
                                        x.StartDate = null;
                                        x.EndDate = null;
                                        x.SourceLocationValueState = MatchPredicateValueState.Value;
                                        x.SourceLocationId = sourceLocationId;
                                        x.ServiceTypeValueState = MatchPredicateValueState.Any;
                                        x.SubstanceValueState = MatchPredicateValueState.Any;
                                        x.WellClassification = WellClassifications.Production;
                                        x.WellClassificationState = MatchPredicateValueState.Value;
                                        x.Stream = stream;
                                        x.StreamValueState = MatchPredicateValueState.Value;
                                    });

        billingConfiguration.MatchCriteria = matchPredicateValue;
        var overlappingMatchPredicates = billingConfiguration.MatchCriteria.Select(predicate => (predicate, billingConfig: billingConfiguration)).ToList();
        var matchPredicates = scope.GenerateRankConfigurationInput(overlappingMatchPredicates);
        scope.DefaultTruckTicket.SubstanceId = substance;
        scope.DefaultTruckTicket.ServiceTypeId = serviceTypeId;
        scope.DefaultTruckTicket.WellClassification = WellClassifications.Production;
        scope.DefaultTruckTicket.Stream = Stream.Landfill;
        scope.DefaultTruckTicket.SourceLocationId = sourceLocationId;

        var truckTicketProperties = scope.GetTruckTicketProperties(scope.DefaultTruckTicket);
        // act
        var matchEvaluate = scope.InstanceUnderTest.EvaluatePredicateRank(matchPredicates, truckTicketProperties, scope._billingConfigurationWeights, "*", false, true);

        //// assert
        Assert.IsTrue(!matchEvaluate.Any());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluatePredicateRank_MatchBasedOnExactMatch_WithSubstanceDiffer()
    {
        // arrange
        var scope = new DefaultScope();
        var sourceLocationId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var substance = Guid.NewGuid();
        var matchPredicateValue = GenFu.GenFu.ListOf<MatchPredicateEntity>(4);
        var billingConfiguration = GenFu.GenFu.New<BillingConfigurationEntity>();
        matchPredicateValue.ForEach(x =>
                                    {
                                        x.IsEnabled = true;
                                        x.StartDate = null;
                                        x.EndDate = null;
                                        x.SourceLocationValueState = MatchPredicateValueState.Value;
                                        x.SourceLocationId = sourceLocationId;
                                        x.ServiceTypeValueState = MatchPredicateValueState.Any;
                                        x.SubstanceId = Guid.NewGuid();
                                        x.SubstanceValueState = MatchPredicateValueState.Value;
                                        x.WellClassification = WellClassifications.Production;
                                        x.WellClassificationState = MatchPredicateValueState.Value;
                                        x.Stream = Stream.Landfill;
                                        x.StreamValueState = MatchPredicateValueState.Value;
                                    });

        billingConfiguration.MatchCriteria = matchPredicateValue;
        var overlappingMatchPredicates = billingConfiguration.MatchCriteria.Select(predicate => (predicate, billingConfig: billingConfiguration)).ToList();
        var matchPredicates = scope.GenerateRankConfigurationInput(overlappingMatchPredicates);
        scope.DefaultTruckTicket.SubstanceId = substance;
        scope.DefaultTruckTicket.ServiceTypeId = serviceTypeId;
        scope.DefaultTruckTicket.WellClassification = WellClassifications.Production;
        scope.DefaultTruckTicket.Stream = Stream.Landfill;
        scope.DefaultTruckTicket.SourceLocationId = sourceLocationId;

        var truckTicketProperties = scope.GetTruckTicketProperties(scope.DefaultTruckTicket);
        // act
        var matchEvaluate = scope.InstanceUnderTest.EvaluatePredicateRank(matchPredicates, truckTicketProperties, scope._billingConfigurationWeights, "*", false, true);

        //// assert
        Assert.IsTrue(!matchEvaluate.Any());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluatePredicateRank_MatchBasedOnExactMatch_WithSourceLocationDiffer()
    {
        // arrange
        var scope = new DefaultScope();
        var sourceLocationId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var substance = Guid.NewGuid();
        var matchPredicateValue = GenFu.GenFu.ListOf<MatchPredicateEntity>(4);
        var billingConfiguration = GenFu.GenFu.New<BillingConfigurationEntity>();
        matchPredicateValue.ForEach(x =>
                                    {
                                        x.IsEnabled = true;
                                        x.StartDate = null;
                                        x.EndDate = null;
                                        x.SourceLocationValueState = MatchPredicateValueState.Value;
                                        x.SourceLocationId = Guid.NewGuid();
                                        x.ServiceTypeValueState = MatchPredicateValueState.Any;
                                        x.SubstanceId = substance;
                                        x.SubstanceValueState = MatchPredicateValueState.Value;
                                        x.WellClassification = WellClassifications.Production;
                                        x.WellClassificationState = MatchPredicateValueState.Value;
                                        x.Stream = Stream.Landfill;
                                        x.StreamValueState = MatchPredicateValueState.Value;
                                    });

        billingConfiguration.MatchCriteria = matchPredicateValue;
        var overlappingMatchPredicates = billingConfiguration.MatchCriteria.Select(predicate => (predicate, billingConfig: billingConfiguration)).ToList();
        var matchPredicates = scope.GenerateRankConfigurationInput(overlappingMatchPredicates);
        scope.DefaultTruckTicket.SubstanceId = substance;
        scope.DefaultTruckTicket.ServiceTypeId = serviceTypeId;
        scope.DefaultTruckTicket.WellClassification = WellClassifications.Production;
        scope.DefaultTruckTicket.Stream = Stream.Landfill;
        scope.DefaultTruckTicket.SourceLocationId = sourceLocationId;

        var truckTicketProperties = scope.GetTruckTicketProperties(scope.DefaultTruckTicket);
        // act
        var matchEvaluate = scope.InstanceUnderTest.EvaluatePredicateRank(matchPredicates, truckTicketProperties, scope._billingConfigurationWeights, "*", false, true);

        //// assert
        Assert.IsTrue(!matchEvaluate.Any());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluatePredicateRank_MatchBasedOnExactMatch_WithServiceTypeDiffer()
    {
        // arrange
        var scope = new DefaultScope();
        var sourceLocationId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var substance = Guid.NewGuid();
        var matchPredicateValue = GenFu.GenFu.ListOf<MatchPredicateEntity>(4);
        var billingConfiguration = GenFu.GenFu.New<BillingConfigurationEntity>();
        matchPredicateValue.ForEach(x =>
                                    {
                                        x.IsEnabled = true;
                                        x.StartDate = null;
                                        x.EndDate = null;
                                        x.SourceLocationValueState = MatchPredicateValueState.Value;
                                        x.SourceLocationId = sourceLocationId;
                                        x.ServiceTypeValueState = MatchPredicateValueState.Value;
                                        x.ServiceTypeId = Guid.NewGuid();
                                        x.SubstanceId = substance;
                                        x.SubstanceValueState = MatchPredicateValueState.Value;
                                        x.WellClassification = WellClassifications.Production;
                                        x.WellClassificationState = MatchPredicateValueState.Value;
                                        x.Stream = Stream.Landfill;
                                        x.StreamValueState = MatchPredicateValueState.Value;
                                    });

        billingConfiguration.MatchCriteria = matchPredicateValue;
        var overlappingMatchPredicates = billingConfiguration.MatchCriteria.Select(predicate => (predicate, billingConfig: billingConfiguration)).ToList();
        var matchPredicates = scope.GenerateRankConfigurationInput(overlappingMatchPredicates);
        scope.DefaultTruckTicket.SubstanceId = substance;
        scope.DefaultTruckTicket.ServiceTypeId = serviceTypeId;
        scope.DefaultTruckTicket.WellClassification = WellClassifications.Production;
        scope.DefaultTruckTicket.Stream = Stream.Landfill;
        scope.DefaultTruckTicket.SourceLocationId = sourceLocationId;

        var truckTicketProperties = scope.GetTruckTicketProperties(scope.DefaultTruckTicket);
        // act
        var matchEvaluate = scope.InstanceUnderTest.EvaluatePredicateRank(matchPredicates, truckTicketProperties, scope._billingConfigurationWeights, "*", false, true);

        //// assert
        Assert.IsTrue(!matchEvaluate.Any());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void MatchPredicateRankManager_EvaluatePredicateRank_AllIgnore()
    {
        // arrange
        var scope = new DefaultScope();
        var sourceLocationId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var substance = Guid.NewGuid();
        var matchPredicateValue = GenFu.GenFu.ListOf<MatchPredicateEntity>(4);
        var billingConfiguration = GenFu.GenFu.New<BillingConfigurationEntity>();
        matchPredicateValue.ForEach(x =>
                                    {
                                        x.IsEnabled = true;
                                        x.StartDate = null;
                                        x.EndDate = null;
                                        x.SourceLocationValueState = MatchPredicateValueState.NotSet;
                                        x.ServiceTypeValueState = MatchPredicateValueState.NotSet;
                                        x.SubstanceValueState = MatchPredicateValueState.NotSet;
                                        x.WellClassificationState = MatchPredicateValueState.NotSet;
                                        x.StreamValueState = MatchPredicateValueState.NotSet;
                                    });

        billingConfiguration.MatchCriteria = matchPredicateValue;
        var overlappingMatchPredicates = billingConfiguration.MatchCriteria.Select(predicate => (predicate, billingConfig: billingConfiguration)).ToList();
        var matchPredicates = scope.GenerateRankConfigurationInput(overlappingMatchPredicates);
        scope.DefaultTruckTicket.SubstanceId = substance;
        scope.DefaultTruckTicket.ServiceTypeId = serviceTypeId;
        scope.DefaultTruckTicket.WellClassification = WellClassifications.Production;
        scope.DefaultTruckTicket.Stream = Stream.Landfill;
        scope.DefaultTruckTicket.SourceLocationId = sourceLocationId;

        var truckTicketProperties = scope.GetTruckTicketProperties(scope.DefaultTruckTicket);
        // act
        var matchEvaluate = scope.InstanceUnderTest.EvaluatePredicateRank(matchPredicates, truckTicketProperties, scope._billingConfigurationWeights, "*", false, true);

        //// assert
        Assert.IsTrue(!matchEvaluate.Any());
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(WellClassifications.Completions, Stream.Pipeline)]
    [DataRow(WellClassifications.Completions, Stream.Terminalling)]
    [DataRow(WellClassifications.Completions, Stream.Landfill)]
    [DataRow(WellClassifications.Completions, Stream.Treating)]
    [DataRow(WellClassifications.Completions, Stream.Waste)]
    [DataRow(WellClassifications.Completions, Stream.Water)]
    [DataRow(WellClassifications.Drilling, Stream.Pipeline)]
    [DataRow(WellClassifications.Drilling, Stream.Terminalling)]
    [DataRow(WellClassifications.Drilling, Stream.Landfill)]
    [DataRow(WellClassifications.Drilling, Stream.Treating)]
    [DataRow(WellClassifications.Drilling, Stream.Waste)]
    [DataRow(WellClassifications.Drilling, Stream.Water)]
    [DataRow(WellClassifications.Production, Stream.Pipeline)]
    [DataRow(WellClassifications.Production, Stream.Terminalling)]
    [DataRow(WellClassifications.Production, Stream.Landfill)]
    [DataRow(WellClassifications.Production, Stream.Treating)]
    [DataRow(WellClassifications.Production, Stream.Waste)]
    [DataRow(WellClassifications.Production, Stream.Water)]
    public void MatchPredicateRankManager_EvaluatePredicateRank_OnlyOnePredicateWithValue(WellClassifications wellClassification, Stream stream)
    {
        // arrange
        var scope = new DefaultScope();
        var sourceLocationId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var substance = Guid.NewGuid();
        var matchPredicateValue = GenFu.GenFu.ListOf<MatchPredicateEntity>(4);
        var billingConfiguration = GenFu.GenFu.New<BillingConfigurationEntity>();
        matchPredicateValue.ForEach(x =>
                                    {
                                        x.IsEnabled = true;
                                        x.StartDate = null;
                                        x.EndDate = null;
                                        x.SourceLocationValueState = MatchPredicateValueState.NotSet;
                                        x.ServiceTypeValueState = MatchPredicateValueState.NotSet;
                                        x.SubstanceValueState = MatchPredicateValueState.NotSet;
                                        x.WellClassificationState = MatchPredicateValueState.NotSet;
                                        x.StreamValueState = MatchPredicateValueState.NotSet;
                                    });

        matchPredicateValue.First().WellClassificationState = MatchPredicateValueState.Value;
        matchPredicateValue.First().WellClassification = wellClassification;
        matchPredicateValue.First().StreamValueState = MatchPredicateValueState.Value;
        matchPredicateValue.First().Stream = stream;
        billingConfiguration.MatchCriteria = matchPredicateValue;
        var overlappingMatchPredicates = billingConfiguration.MatchCriteria.Select(predicate => (predicate, billingConfig: billingConfiguration)).ToList();
        var matchPredicates = scope.GenerateRankConfigurationInput(overlappingMatchPredicates);
        scope.DefaultTruckTicket.SubstanceId = substance;
        scope.DefaultTruckTicket.ServiceTypeId = serviceTypeId;
        scope.DefaultTruckTicket.WellClassification = wellClassification;
        scope.DefaultTruckTicket.Stream = stream;
        scope.DefaultTruckTicket.SourceLocationId = sourceLocationId;

        var truckTicketProperties = scope.GetTruckTicketProperties(scope.DefaultTruckTicket);
        // act
        var matchEvaluate = scope.InstanceUnderTest.EvaluatePredicateRank(matchPredicates, truckTicketProperties, scope._billingConfigurationWeights, "*", false, true);

        //// assert
        Assert.IsTrue(matchEvaluate.Count() == 1);
    }

    private class DefaultScope : TestScope<MatchPredicateRankManager>
    {
        public readonly Dictionary<string, int> _billingConfigurationWeights = new()
        {
            { MatchPredicateProperties.SourceLocation, 21 },
            { MatchPredicateProperties.Stream, 13 },
            { MatchPredicateProperties.WellClassification, 8 },
            { MatchPredicateProperties.ServiceType, 5 },
            { MatchPredicateProperties.Substance, 3 },
        };

        public readonly BillingConfigurationEntity DefaultBillingConfiguration =
            new()
            {
                Id = Guid.NewGuid(),
                BillingConfigurationEnabled = true,
                BillingContactAddress = "599 Harry Square",
                BillingContactId = Guid.NewGuid(),
                BillingContactName = "Dr. Eduardo Lesch",
                BillingCustomerAccountId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = string.Empty,
                CreatedById = Guid.NewGuid().ToString(),
                CustomerGeneratorId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33"),
                CustomerGeneratorName = "Kemmer, Maggio and Reynolds",
                Description = null,
                EmailDeliveryEnabled = true,
                FieldTicketsUploadEnabled = false,
                StartDate = new DateTime(2022, 01, 01, 22, 02, 02, 0),
                EndDate = null,
                GeneratorRepresentativeId = Guid.NewGuid(),
                IncludeExternalAttachmentInLC = true,
                IncludeInternalAttachmentInLC = true,
                IsDefaultConfiguration = false,
                IncludeForAutomation = true,
                LastComment = "new comment added",
                LoadConfirmationsEnabled = true,
                LoadConfirmationFrequency = null,
                RigNumber = null,
                ThirdPartyBillingContactAddress = "07958 Althea Ford",
                ThirdPartyBillingContactId = Guid.NewGuid(),
                ThirdPartyBillingContactName = "Barbara McClure II",
                ThirdPartyCompanyId = Guid.NewGuid(),
                ThirdPartyCompanyName = null,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "Panth Shah",
                UpdatedById = Guid.NewGuid().ToString(),
                Facilities = new()
                {
                    Key = Guid.NewGuid(),
                    List = new() { Guid.NewGuid() },
                },
                EDIValueData = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        EDIFieldDefinitionId = Guid.NewGuid(),
                        EDIFieldName = "Invoice Number",
                        EDIFieldValueContent = null,
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        EDIFieldDefinitionId = Guid.NewGuid(),
                        EDIFieldName = "Policy Name",
                        EDIFieldValueContent = null,
                    },
                },
                EmailDeliveryContacts = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AccountContactId = Guid.NewGuid(),
                        EmailAddress = "Noble60@gmail.com",
                        IsAuthorized = true,
                        SignatoryContact = "Jenna Schroeder",
                    },
                },
                Signatories = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AccountContactId = Guid.NewGuid(),
                        IsAuthorized = true,
                        Address = "6557 Cortez Field",
                        Email = "Janae_Corkery95@gmail.com",
                        FirstName = "Johnnie Kunde Sr.",
                        LastName = null,
                        PhoneNumber = "510-297-3998",
                    },
                },
                MatchCriteria = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Stream = Stream.Landfill,
                        StreamValueState = MatchPredicateValueState.Value,
                        IsEnabled = true,
                        ServiceType = null,
                        ServiceTypeId = null,
                        ServiceTypeValueState = MatchPredicateValueState.Any,
                        SourceIdentifier = null,
                        SourceLocationId = null,
                        SourceLocationValueState = MatchPredicateValueState.Any,
                        SubstanceId = null,
                        SubstanceName = null,
                        SubstanceValueState = MatchPredicateValueState.Any,
                        WellClassification = WellClassifications.Drilling,
                        WellClassificationState = MatchPredicateValueState.Value,
                        StartDate = null,
                        EndDate = null,
                        Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Stream = Stream.Pipeline,
                        StreamValueState = MatchPredicateValueState.Value,
                        IsEnabled = true,
                        ServiceType = null,
                        ServiceTypeId = null,
                        ServiceTypeValueState = MatchPredicateValueState.Any,
                        SourceIdentifier = null,
                        SourceLocationId = null,
                        SourceLocationValueState = MatchPredicateValueState.Any,
                        SubstanceId = null,
                        SubstanceName = null,
                        SubstanceValueState = MatchPredicateValueState.Any,
                        WellClassification = WellClassifications.Completions,
                        WellClassificationState = MatchPredicateValueState.Value,
                        StartDate = null,
                        EndDate = null,
                        Hash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08",
                    },
                },
            };

        public readonly MatchPredicateEntity DefaultMatchPredicateEntity = new()
        {
            Id = Guid.NewGuid(),
            Stream = Stream.Undefined,
            StreamValueState = MatchPredicateValueState.NotSet,
            IsEnabled = true,
            ServiceType = null,
            ServiceTypeId = null,
            ServiceTypeValueState = MatchPredicateValueState.NotSet,
            SourceIdentifier = null,
            SourceLocationId = null,
            SourceLocationValueState = MatchPredicateValueState.NotSet,
            SubstanceId = null,
            SubstanceName = null,
            SubstanceValueState = MatchPredicateValueState.NotSet,
            WellClassification = WellClassifications.Undefined,
            WellClassificationState = MatchPredicateValueState.NotSet,
            StartDate = null,
            EndDate = null,
            Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
        };

        public readonly TruckTicketEntity DefaultTruckTicket = new()
        {
            Id = Guid.NewGuid(),
            Acknowledgement = "",
            AdditionalServicesEnabled = true,
            BillOfLading = "",
            BillingCustomerId = Guid.NewGuid(),
            BillingCustomerName = "",
            ClassNumber = "",
            CountryCode = CountryCode.CA,
            CustomerId = Guid.NewGuid(),
            CustomerName = "",
            Date = DateTimeOffset.UtcNow,
            Destination = "",
            FacilityId = Guid.NewGuid(),
            FacilityName = "",
            FacilityServiceSubstanceId = Guid.NewGuid(),
            ServiceTypeId = Guid.NewGuid(),
            GeneratorId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33"),
            GeneratorName = "",
            GrossWeight = 1,
            IsDeleted = true,
            IsDow = true,
            Level = "",
            LoadDate = DateTimeOffset.UtcNow,
            LocationOperatingStatus = LocationOperatingStatus.Completions,
            ManifestNumber = "",
            MaterialApprovalId = Guid.NewGuid(),
            MaterialApprovalNumber = "",
            NetWeight = 1,
            OilVolume = 1,
            OilVolumePercent = 1,
            Quadrant = "",
            SaleOrderId = Guid.NewGuid(),
            SaleOrderNumber = "",
            ServiceType = "",
            SolidVolume = 1,
            SolidVolumePercent = 1,
            Source = TruckTicketSource.Manual,
            SourceLocationFormatted = "",
            SourceLocationId = Guid.NewGuid(),
            SourceLocationName = "",
            SpartanProductParameterDisplay = "",
            SpartanProductParameterId = Guid.NewGuid(),
            Status = TruckTicketStatus.Hold,
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "",
            TareWeight = 1,
            TicketNumber = "",
            TimeIn = DateTimeOffset.UtcNow,
            TimeOut = DateTimeOffset.UtcNow,
            Tnorms = "",
            TotalVolume = 1,
            TotalVolumePercent = 1,
            TrackingNumber = "",
            TrailerNumber = "",
            TruckNumber = "",
            TruckingCompanyId = Guid.NewGuid(),
            TruckingCompanyName = "",
            UnNumber = "",
            UnloadOilDensity = 1,
            UpdatedAt = DateTimeOffset.UtcNow,
            UploadFieldTicket = true,
            ValidationStatus = TruckTicketValidationStatus.Valid,
            WaterVolume = 1,
            WaterVolumePercent = 1,
            WellClassification = WellClassifications.Completions,
            AdditionalServices = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    AdditionalServiceName = "Service 1",
                    AdditionalServiceNumber = "123",
                    AdditionalServiceQuantity = 1,
                    IsPrimarySalesLine = true,
                    ProductId = Guid.NewGuid(),
                },
            },
            Attachments = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Container = "",
                    File = "Sample-Work-Ticket-US-Stub.pdf",
                    Path = "66e6ba1e-afc2-4a87-923e-41335a92b98f/Sample-Work-Ticket-US-Stub.pdf",
                },
            },
            BillingContact = new()
            {
                AccountContactId = Guid.NewGuid(),
                Address = "",
                Email = "",
                Name = "",
                PhoneNumber = "",
            },
            EdiFieldValues = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    EDIFieldDefinitionId = Guid.NewGuid(),
                    EDIFieldName = "Invoice Number",
                    EDIFieldValueContent = "123",
                },
            },
            Signatories = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    AccountContactId = Guid.NewGuid(),
                    ContactAddress = "512 Jamarcus Islands",
                    ContactEmail = "Waldo_Harvey32@yahoo.com",
                    ContactName = "Kent Purdy",
                    ContactPhoneNumber = "742-548-1249",
                    IsAuthorized = true,
                },
            },
        };

        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public string[] GetTruckTicketProperties(TruckTicketEntity truckTicketEntity)
        {
            return new List<string>
            {
                $"{MatchPredicateProperties.SourceLocation}:{truckTicketEntity.SourceLocationId}",
                $"{MatchPredicateProperties.Facility}:{truckTicketEntity.FacilityId}",
                $"{MatchPredicateProperties.ServiceType}:{truckTicketEntity.ServiceTypeId.GetValueOrDefault()}",
                $"{MatchPredicateProperties.Substance}:{truckTicketEntity.SubstanceId}",
                $"{MatchPredicateProperties.WellClassification}:{truckTicketEntity.WellClassification}",
                $"{MatchPredicateProperties.Stream}:{truckTicketEntity.Stream}",
            }.ToArray();
        }

        public List<RankConfiguration> GenerateRankConfigurationInput(List<(MatchPredicateEntity predicate, BillingConfigurationEntity billingConfiguration)> billingConfigurationPredicates,
                                                                      string wildCard = "*")
        {
            var rankConfigurationInput = new List<RankConfiguration>();
            if (billingConfigurationPredicates == null || !billingConfigurationPredicates.Any())
            {
                return rankConfigurationInput;
            }
            //If All -> Wildcard *
            //If Values defined -> Split them into single records

            foreach (var billingConfigurationPredicate in billingConfigurationPredicates)
            {
                var predicate = billingConfigurationPredicate.predicate;
                var predicateMap = new List<string>();
                switch (predicate.SourceLocationValueState)
                {
                    case MatchPredicateValueState.Any:
                        predicateMap.Add($"{MatchPredicateProperties.SourceLocation}:{wildCard}");
                        break;
                    case MatchPredicateValueState.Value:
                        predicateMap.Add($"{MatchPredicateProperties.SourceLocation}:{predicate.SourceLocationId.GetValueOrDefault()}");
                        break;
                }

                switch (predicate.WellClassificationState)
                {
                    case MatchPredicateValueState.Any:
                        predicateMap.Add($"{MatchPredicateProperties.WellClassification}:{wildCard}");
                        break;
                    case MatchPredicateValueState.Value:
                        predicateMap.Add($"{MatchPredicateProperties.WellClassification}:{predicate.WellClassification}");
                        break;
                }

                switch (predicate.ServiceTypeValueState)
                {
                    case MatchPredicateValueState.Any:
                        predicateMap.Add($"{MatchPredicateProperties.ServiceType}:{wildCard}");
                        break;
                    case MatchPredicateValueState.Value:
                        predicateMap.Add($"{MatchPredicateProperties.ServiceType}:{predicate.ServiceTypeId.GetValueOrDefault()}");
                        break;
                }

                switch (predicate.SubstanceValueState)
                {
                    case MatchPredicateValueState.Any:
                        predicateMap.Add($"{MatchPredicateProperties.Substance}:{wildCard}");
                        break;
                    case MatchPredicateValueState.Value:
                        predicateMap.Add($"{MatchPredicateProperties.Substance}:{predicate.SubstanceId.GetValueOrDefault()}");
                        break;
                }

                switch (predicate.StreamValueState)
                {
                    case MatchPredicateValueState.Any:
                        predicateMap.Add($"{MatchPredicateProperties.Stream}:{wildCard}");
                        break;
                    case MatchPredicateValueState.Value:
                        predicateMap.Add($"{MatchPredicateProperties.Stream}:{predicate.Stream}");
                        break;
                }

                rankConfigurationInput.Add(new()
                {
                    EntityId = billingConfigurationPredicate.billingConfiguration.Id,
                    Name = billingConfigurationPredicate.billingConfiguration.Name,
                    Predicates = predicateMap.ToArray(),
                });
            }

            return rankConfigurationInput;
        }
    }
}
