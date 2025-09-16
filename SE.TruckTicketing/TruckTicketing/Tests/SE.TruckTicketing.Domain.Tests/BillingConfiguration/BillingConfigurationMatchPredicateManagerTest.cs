using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.BillingConfiguration;

[TestClass]
public class MatchPredicateManagerTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_ExistingBillingConfigurationWithSameGenerator_WithIncludeForAutomation_FacilityOverlap()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        // act
        var data = await scope.InstanceUnderTest.GetOverlappingBillingConfigurations(scope.DefaultBillingConfiguration);

        //// assert
        Assert.IsTrue(data.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_ExistingBillingConfigurationWithSameGenerator_WithIncludeForAutomation_NoFacilityOverlap()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.Facilities.List = new() { Guid.NewGuid() };
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        // act
        var data = await scope.InstanceUnderTest.GetOverlappingBillingConfigurations(scope.DefaultBillingConfiguration);

        //// assert
        Assert.IsTrue(data.Count == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_ExistingBillingConfigurationWithSameGenerator_WithIncludeForAutomation_WithNoFacilityAssociated_NoFacilityOverlap()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.Facilities.List = new();
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        // act
        var data = await scope.InstanceUnderTest.GetOverlappingBillingConfigurations(scope.DefaultBillingConfiguration);

        //// assert
        Assert.IsTrue(data.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_ExistingBillingConfigurationWithSameGenerator_WithIncludeForAutomation_StartDateInFuture()
    {
        // arrange
        var scope = new DefaultScope { DefaultBillingConfiguration = { EndDate = new DateTime(2023, 01, 01, 22, 02, 02, 0) } };
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.StartDate = new DateTime(2025, 01, 01, 22, 02, 02, 0);
        billingConfigurationWithSameGenerator.EndDate = null;

        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();

        // act
        var data = await scope.InstanceUnderTest.GetOverlappingBillingConfigurations(scope.DefaultBillingConfiguration);

        //// assert
        Assert.IsTrue(data.Count == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_ExistingBillingConfigurationWithSameGenerator_WithIncludeForAutomation_EndDateInPast()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.StartDate = null;
        billingConfigurationWithSameGenerator.EndDate = new DateTime(2021, 01, 01, 22, 02, 02, 0);

        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();

        // act
        var data = await scope.InstanceUnderTest.GetOverlappingBillingConfigurations(scope.DefaultBillingConfiguration);

        //// assert
        Assert.IsTrue(data.Count == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_ExistingBillingConfigurationWithSameGenerator_WithIncludeForAutomation_EndDateNull()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.EndDate = null;

        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();

        // act
        var data = await scope.InstanceUnderTest.GetOverlappingBillingConfigurations(scope.DefaultBillingConfiguration);

        //// assert
        Assert.IsTrue(data.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_ExistingBillingConfigurationWithSameGenerator_WithIncludeForAutomation_StartAndEndDateInValidDateRange()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.StartDate = new DateTime(2021, 01, 01, 22, 02, 02, 0);
        billingConfigurationWithSameGenerator.EndDate = new DateTime(2025, 01, 01, 22, 02, 02, 0);

        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();

        // act
        var data = await scope.InstanceUnderTest.GetOverlappingBillingConfigurations(scope.DefaultBillingConfiguration);

        //// assert
        Assert.IsTrue(data.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_ExistingBillingConfigurationWithSameGenerator_WithIncludeForAutomation_WithNoFacilityAssociatedOnEntity()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.Facilities.List = new();
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        scope.DefaultBillingConfiguration.Facilities.List = new();

        // act
        var data = await scope.InstanceUnderTest.GetOverlappingBillingConfigurations(scope.DefaultBillingConfiguration);

        //// assert
        Assert.IsTrue(data.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_WithIncludeForAutomation()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.LoadDate = billingConfigurationWithSameGenerator.StartDate.HasValue ? billingConfigurationWithSameGenerator.StartDate.Value.AddDays(1) : DateTimeOffset.UtcNow;
        truckTicketEntity.EffectiveDate = truckTicketEntity.LoadDate.Value.Date;
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();

        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_NotIncludeForAutomation()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = false;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.LoadDate = billingConfigurationWithSameGenerator.StartDate.HasValue ? billingConfigurationWithSameGenerator.StartDate.Value.AddDays(1) : DateTimeOffset.UtcNow;
        truckTicketEntity.EffectiveDate = truckTicketEntity.LoadDate.Value.Date;
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();

        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, false);

        //// assert
        Assert.IsTrue(data.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_NoRecordWithIncludedForAutomation()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = false;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();

        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_NoRecordWithNotIncludedForAutomation()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();

        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, false);

        //// assert
        Assert.IsTrue(data.Count == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_WithIncludeForAutomation_NoFacilityOverlap()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.Facilities.List = new() { Guid.NewGuid() };
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;

        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_WithIncludeForAutomation_WithNoFacilityAssociated_NoFacilityOverlap()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.Facilities.List = new();
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.LoadDate = billingConfigurationWithSameGenerator.StartDate.HasValue ? billingConfigurationWithSameGenerator.StartDate.Value.AddDays(1) : DateTimeOffset.UtcNow;
        truckTicketEntity.EffectiveDate = truckTicketEntity.LoadDate.Value.Date;
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;

        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_WithIncludeForAutomation_StartDateInTheFuture()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.StartDate = new DateTime(2025, 01, 01, 22, 02, 02, 0);
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();

        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_WithIncludeForAutomation_StartDateNull()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.StartDate = null;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();

        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_WithIncludeForAutomation_EndDatePastDue()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.StartDate = null;
        billingConfigurationWithSameGenerator.EndDate = new DateTime(2021, 01, 01, 22, 02, 02, 0);
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.LoadDate = billingConfigurationWithSameGenerator.StartDate.HasValue ? billingConfigurationWithSameGenerator.StartDate.Value.AddDays(1) : DateTimeOffset.UtcNow;
        truckTicketEntity.EffectiveDate = truckTicketEntity.LoadDate.Value.Date;
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();

        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_WithIncludeForAutomation_EndDateNull()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.StartDate = new DateTime(2022, 01, 01, 22, 02, 02, 0);
        billingConfigurationWithSameGenerator.EndDate = null;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.LoadDate = billingConfigurationWithSameGenerator.StartDate.HasValue ? billingConfigurationWithSameGenerator.StartDate.Value.AddDays(1) : DateTimeOffset.UtcNow;
        truckTicketEntity.EffectiveDate = truckTicketEntity.LoadDate.Value.Date;
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();

        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_WithIncludeForAutomation_StartAndEndDateNull()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.StartDate = null;
        billingConfigurationWithSameGenerator.EndDate = null;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();

        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_WithIncludeForAutomation_StartAndEndDateInValidDateRange()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.StartDate = new DateTime(2021, 01, 01, 22, 02, 02, 0);
        billingConfigurationWithSameGenerator.EndDate = new DateTime(2025, 01, 01, 22, 02, 02, 0);

        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.LoadDate = billingConfigurationWithSameGenerator.StartDate.HasValue ? billingConfigurationWithSameGenerator.StartDate.Value.AddDays(1) : DateTimeOffset.UtcNow;
        truckTicketEntity.EffectiveDate = truckTicketEntity.LoadDate.Value.Date;
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();

        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_WithIncludeForAutomation_NoFacilityMatchForInvoiceConfiguration()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();

        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();

        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        invoiceConfigurationEntity.AllFacilities = false;
        invoiceConfigurationEntity.Facilities = new()
        {
            Key = Guid.NewGuid(),
            List = new() { Guid.NewGuid() },
        };

        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_WithIncludeForAutomation_NoSourceLocationMatchForInvoiceConfiguration()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();

        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();

        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        invoiceConfigurationEntity.AllSourceLocations = false;
        invoiceConfigurationEntity.SourceLocations = new()
        {
            Key = Guid.NewGuid(),
            List = new() { Guid.NewGuid() },
        };

        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_WithIncludeForAutomation_NoWellClassificationMatchForInvoiceConfiguration()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();

        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();
        truckTicketEntity.WellClassification = WellClassifications.Drilling;
        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        invoiceConfigurationEntity.AllWellClassifications = false;
        invoiceConfigurationEntity.WellClassifications = new()
        {
            Key = Guid.NewGuid(),
            List = new() { WellClassifications.Completions.ToString() },
        };

        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_WithIncludeForAutomation_NoSubstanceMatchForInvoiceConfiguration()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();

        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();
        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        invoiceConfigurationEntity.AllSubstances = false;
        invoiceConfigurationEntity.Substances = new()
        {
            Key = Guid.NewGuid(),
            List = new() { Guid.NewGuid() },
        };

        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_WithIncludeForAutomation_NoServiceTypeMatchForInvoiceConfiguration()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();

        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();
        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        invoiceConfigurationEntity.AllServiceTypes = false;
        invoiceConfigurationEntity.ServiceTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new() { Guid.NewGuid() },
        };

        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetBillingConfigurations_WithIncludeForAutomation_ValidMatchForInvoiceConfiguration()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();

        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.LoadDate = billingConfigurationWithSameGenerator.StartDate.HasValue ? billingConfigurationWithSameGenerator.StartDate.Value.AddDays(1) : DateTimeOffset.UtcNow;
        truckTicketEntity.EffectiveDate = truckTicketEntity.LoadDate.Value.Date;
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();

        var invoiceConfigurationEntity = scope.ConfigureCatchAllInvoiceConfiguration();
        invoiceConfigurationEntity = scope.SetUpInvoiceConfigurationWithTruckTicketData(invoiceConfigurationEntity, truckTicketEntity);
        scope.SetupExistingInvoiceConfiguration(invoiceConfigurationEntity);

        billingConfigurationWithSameGenerator.InvoiceConfigurationId = invoiceConfigurationEntity.Id;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = invoiceConfigurationEntity.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetBillingConfigurations(truckTicketEntity, true);

        //// assert
        Assert.IsTrue(data.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetMatchingBillingConfigurations_MatchPredicateForBillingConfiguration_StartDateInTheFuture()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        var startDate = new DateTime(2025, 01, 01, 22, 02, 02, 0);
        billingConfigurationWithSameGenerator.StartDate = startDate;
        billingConfigurationWithSameGenerator.MatchCriteria.ForEach(x => x.StartDate = startDate);
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();

        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();
        truckTicketEntity.LoadDate = DateTimeOffset.UtcNow;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = billingConfigurationWithSameGenerator.Id;

        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetMatchingBillingConfiguration(new() { billingConfigurationWithSameGenerator }, truckTicketEntity);
        var automatedBillingConfigurationEntity = scope.InstanceUnderTest.SelectAutomatedBillingConfiguration(new() { billingConfigurationWithSameGenerator }, truckTicketEntity);

        //// assert
        scope.MatchPredicateManagerMock.Verify(x => x.EvaluatePredicateRank(It.IsAny<List<RankConfiguration>>(),
                                                                            It.IsAny<string[]>(),
                                                                            It.IsAny<Dictionary<string, int>>(),
                                                                            It.IsAny<string>(),
                                                                            It.IsAny<bool>(),
                                                                            It.IsAny<bool>()), Times.Never);

        Assert.IsTrue(data.Id == Guid.Empty);
        Assert.IsTrue(automatedBillingConfigurationEntity.Id == Guid.Empty);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetMatchingBillingConfigurations_MatchPredicateForBillingConfiguration_StartDateNull()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.StartDate = null;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();

        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();
        truckTicketEntity.LoadDate = DateTimeOffset.UtcNow;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = billingConfigurationWithSameGenerator.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetMatchingBillingConfiguration(new() { billingConfigurationWithSameGenerator }, truckTicketEntity);
        var automatedBillingConfigurationEntity = scope.InstanceUnderTest.SelectAutomatedBillingConfiguration(new() { billingConfigurationWithSameGenerator }, truckTicketEntity);

        //// assert
        scope.MatchPredicateManagerMock.Verify(x => x.EvaluatePredicateRank(It.IsAny<List<RankConfiguration>>(),
                                                                            It.IsAny<string[]>(),
                                                                            It.IsAny<Dictionary<string, int>>(),
                                                                            It.IsAny<string>(),
                                                                            It.IsAny<bool>(),
                                                                            It.IsAny<bool>()), Times.AtLeastOnce);

        Assert.IsTrue(data.Id != Guid.Empty);
        Assert.IsTrue(automatedBillingConfigurationEntity.Id != Guid.Empty);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetMatchingBillingConfigurations_MatchPredicateForBillingConfiguration_EndDatePastDue()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        var endDate = new DateTime(2021, 01, 01, 22, 02, 02, 0);
        billingConfigurationWithSameGenerator.MatchCriteria.ForEach(x => x.EndDate = endDate);
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.StartDate = null;
        billingConfigurationWithSameGenerator.EndDate = endDate;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.LoadDate = billingConfigurationWithSameGenerator.StartDate.HasValue ? billingConfigurationWithSameGenerator.StartDate.Value.AddDays(1) : DateTimeOffset.UtcNow;
        truckTicketEntity.EffectiveDate = truckTicketEntity.LoadDate.Value.Date;
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();
        truckTicketEntity.LoadDate = DateTimeOffset.UtcNow;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = billingConfigurationWithSameGenerator.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetMatchingBillingConfiguration(new() { billingConfigurationWithSameGenerator }, truckTicketEntity);
        var automatedBillingConfigurationEntity = scope.InstanceUnderTest.SelectAutomatedBillingConfiguration(new() { billingConfigurationWithSameGenerator }, truckTicketEntity);

        //// assert
        scope.MatchPredicateManagerMock.Verify(x => x.EvaluatePredicateRank(It.IsAny<List<RankConfiguration>>(),
                                                                            It.IsAny<string[]>(),
                                                                            It.IsAny<Dictionary<string, int>>(),
                                                                            It.IsAny<string>(),
                                                                            It.IsAny<bool>(),
                                                                            It.IsAny<bool>()), Times.Never);

        Assert.IsTrue(data.Id == Guid.Empty);
        Assert.IsTrue(automatedBillingConfigurationEntity.Id == Guid.Empty);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetMatchingBillingConfigurations_MatchPredicateForBillingConfiguration_EndDateNull()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.StartDate = new DateTime(2022, 01, 01, 22, 02, 02, 0);
        billingConfigurationWithSameGenerator.EndDate = null;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();
        truckTicketEntity.LoadDate = DateTimeOffset.UtcNow;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = billingConfigurationWithSameGenerator.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetMatchingBillingConfiguration(new() { billingConfigurationWithSameGenerator }, truckTicketEntity);
        var automatedBillingConfigurationEntity = scope.InstanceUnderTest.SelectAutomatedBillingConfiguration(new() { billingConfigurationWithSameGenerator }, truckTicketEntity);

        //// assert
        scope.MatchPredicateManagerMock.Verify(x => x.EvaluatePredicateRank(It.IsAny<List<RankConfiguration>>(),
                                                                            It.IsAny<string[]>(),
                                                                            It.IsAny<Dictionary<string, int>>(),
                                                                            It.IsAny<string>(),
                                                                            It.IsAny<bool>(),
                                                                            It.IsAny<bool>()), Times.AtLeastOnce);

        Assert.IsTrue(data.Id != Guid.Empty);
        Assert.IsTrue(automatedBillingConfigurationEntity.Id != Guid.Empty);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetMatchingBillingConfigurations_MatchPredicateForBillingConfiguration_StartAndEndDateNull()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.StartDate = null;
        billingConfigurationWithSameGenerator.EndDate = null;
        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();
        truckTicketEntity.LoadDate = DateTimeOffset.UtcNow;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = billingConfigurationWithSameGenerator.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetMatchingBillingConfiguration(new() { billingConfigurationWithSameGenerator }, truckTicketEntity);
        var automatedBillingConfigurationEntity = scope.InstanceUnderTest.SelectAutomatedBillingConfiguration(new() { billingConfigurationWithSameGenerator }, truckTicketEntity);

        //// assert
        scope.MatchPredicateManagerMock.Verify(x => x.EvaluatePredicateRank(It.IsAny<List<RankConfiguration>>(),
                                                                            It.IsAny<string[]>(),
                                                                            It.IsAny<Dictionary<string, int>>(),
                                                                            It.IsAny<string>(),
                                                                            It.IsAny<bool>(),
                                                                            It.IsAny<bool>()), Times.AtLeastOnce);

        Assert.IsTrue(data.Id != Guid.Empty);
        Assert.IsTrue(automatedBillingConfigurationEntity.Id != Guid.Empty);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task MatchPredicateManager_GetMatchingBillingConfigurations_MatchPredicateForBillingConfiguration_StartAndEndDateInValidDateRange()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfigurationWithSameGenerator = scope.DefaultBillingConfiguration.Clone();
        var startDate = new DateTime(2021, 01, 01, 22, 02, 02, 0);
        var endDate = new DateTime(2025, 01, 01, 22, 02, 02, 0);

        billingConfigurationWithSameGenerator.IncludeForAutomation = true;
        billingConfigurationWithSameGenerator.StartDate = startDate;
        billingConfigurationWithSameGenerator.EndDate = endDate;
        billingConfigurationWithSameGenerator.MatchCriteria.ForEach(x => x.StartDate = startDate);
        billingConfigurationWithSameGenerator.MatchCriteria.ForEach(x => x.EndDate = endDate);

        scope.SetupExistingBillingConfiguration(billingConfigurationWithSameGenerator);
        scope.DefaultBillingConfiguration.Id = Guid.NewGuid();
        var truckTicketEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckTicketEntity.LoadDate = billingConfigurationWithSameGenerator.StartDate.HasValue ? billingConfigurationWithSameGenerator.StartDate.Value.AddDays(1) : DateTimeOffset.UtcNow;
        truckTicketEntity.EffectiveDate = truckTicketEntity.LoadDate.Value.Date;
        truckTicketEntity.GeneratorId = billingConfigurationWithSameGenerator.CustomerGeneratorId;
        truckTicketEntity.FacilityId = billingConfigurationWithSameGenerator.Facilities.List.FirstOrDefault();
        truckTicketEntity.LoadDate = DateTimeOffset.UtcNow;
        truckTicketEntity.LoadDate = DateTimeOffset.UtcNow;

        var rankConfiguration = GenFu.GenFu.ListOf<RankConfiguration>(1);
        rankConfiguration.First().EntityId = billingConfigurationWithSameGenerator.Id;
        scope.SetupExistingEvaluatePredicateRankConfiguration(rankConfiguration.ToArray());

        // act
        var data = await scope.InstanceUnderTest.GetMatchingBillingConfiguration(new() { billingConfigurationWithSameGenerator }, truckTicketEntity);
        var automatedBillingConfigurationEntity = scope.InstanceUnderTest.SelectAutomatedBillingConfiguration(new() { billingConfigurationWithSameGenerator }, truckTicketEntity);

        //// assert
        scope.MatchPredicateManagerMock.Verify(x => x.EvaluatePredicateRank(It.IsAny<List<RankConfiguration>>(),
                                                                            It.IsAny<string[]>(),
                                                                            It.IsAny<Dictionary<string, int>>(),
                                                                            It.IsAny<string>(),
                                                                            It.IsAny<bool>(),
                                                                            It.IsAny<bool>()), Times.AtLeastOnce);

        Assert.IsTrue(data.Id != Guid.Empty);
        Assert.IsTrue(automatedBillingConfigurationEntity.Id != Guid.Empty);
    }

    private class DefaultScope : TestScope<MatchPredicateManager>
    {
        public readonly BillingConfigurationEntity DefaultBillingConfiguration =
            new()
            {
                Id = Guid.NewGuid(),
                BillingConfigurationEnabled = true,
                BillingContactAddress = "599 Harry Square",
                BillingContactId = Guid.NewGuid(),
                BillingContactName = "Dr. Eduardo Lesch",
                BillingCustomerAccountId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33"),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = string.Empty,
                CreatedById = Guid.NewGuid().ToString(),
                CustomerGeneratorId = Guid.NewGuid(),
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
                InvoiceConfigurationId = Guid.NewGuid(),
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

        public DefaultScope()
        {
            InstanceUnderTest = new(BillingConfigurationProviderMock.Object,
                                    InvoiceConfigurationProviderMock.Object,
                                    MatchPredicateManagerMock.Object,
                                    _truckTicketEffectiveDateService.Object);
        }

        private Mock<IProvider<Guid, BillingConfigurationEntity>> BillingConfigurationProviderMock { get; } = new();

        private Mock<ITruckTicketEffectiveDateService> _truckTicketEffectiveDateService { get; } = new();

        private Mock<IProvider<Guid, InvoiceConfigurationEntity>> InvoiceConfigurationProviderMock { get; } = new();

        public Mock<IMatchPredicateRankManager> MatchPredicateManagerMock { get; } = new();

        public void SetupExistingBillingConfiguration(params BillingConfigurationEntity[] entities)
        {
            BillingConfigurationProviderMock.SetupEntities(entities);
        }

        public void SetupExistingInvoiceConfiguration(params InvoiceConfigurationEntity[] entities)
        {
            InvoiceConfigurationProviderMock.SetupEntities(entities);
        }

        public void SetupExistingEvaluatePredicateRankConfiguration(params RankConfiguration[] rankConfigs)
        {
            MatchPredicateManagerMock.Setup(x => x.EvaluatePredicateRank(It.IsAny<List<RankConfiguration>>(),
                                                                         It.IsAny<string[]>(),
                                                                         It.IsAny<Dictionary<string, int>>(),
                                                                         It.IsAny<string>(),
                                                                         It.IsAny<bool>(), It.IsAny<bool>())).Returns(rankConfigs.ToList());
        }

        public InvoiceConfigurationEntity ConfigureCatchAllInvoiceConfiguration()
        {
            var invoiceConfigurationEntity = GenFu.GenFu.New<InvoiceConfigurationEntity>();
            invoiceConfigurationEntity.AllServiceTypes = true;
            invoiceConfigurationEntity.ServiceTypes = null;
            invoiceConfigurationEntity.ServiceTypesName = null;
            invoiceConfigurationEntity.AllWellClassifications = true;
            invoiceConfigurationEntity.WellClassifications = null;
            invoiceConfigurationEntity.AllFacilities = true;
            invoiceConfigurationEntity.Facilities = null;
            invoiceConfigurationEntity.FacilityCode = null;
            invoiceConfigurationEntity.AllSourceLocations = true;
            invoiceConfigurationEntity.SourceLocationIdentifier = null;
            invoiceConfigurationEntity.SourceLocations = null;
            invoiceConfigurationEntity.AllSubstances = true;
            invoiceConfigurationEntity.Substances = null;
            invoiceConfigurationEntity.SubstancesName = null;
            invoiceConfigurationEntity.CatchAll = true;
            return invoiceConfigurationEntity;
        }

        public InvoiceConfigurationEntity SetUpInvoiceConfigurationWithTruckTicketData(InvoiceConfigurationEntity invoiceConfigurationEntity, TruckTicketEntity truckTicketEntity)
        {
            invoiceConfigurationEntity.AllWellClassifications = false;
            invoiceConfigurationEntity.WellClassifications = new()
            {
                Key = Guid.NewGuid(),
                List = new() { truckTicketEntity.WellClassification.ToString() },
            };

            invoiceConfigurationEntity.AllSourceLocations = false;
            invoiceConfigurationEntity.SourceLocations = new()
            {
                Key = Guid.NewGuid(),
                List = new() { truckTicketEntity.SourceLocationId },
            };

            invoiceConfigurationEntity.AllServiceTypes = false;
            invoiceConfigurationEntity.ServiceTypes = new()
            {
                Key = Guid.NewGuid(),
                List = new() { truckTicketEntity.ServiceTypeId.GetValueOrDefault() },
            };

            invoiceConfigurationEntity.AllSubstances = false;
            invoiceConfigurationEntity.Substances = new()
            {
                Key = Guid.NewGuid(),
                List = new() { truckTicketEntity.SubstanceId },
            };

            invoiceConfigurationEntity.AllFacilities = false;
            invoiceConfigurationEntity.Facilities = new()
            {
                Key = Guid.NewGuid(),
                List = new() { truckTicketEntity.FacilityId },
            };

            return invoiceConfigurationEntity;
        }
    }
}
