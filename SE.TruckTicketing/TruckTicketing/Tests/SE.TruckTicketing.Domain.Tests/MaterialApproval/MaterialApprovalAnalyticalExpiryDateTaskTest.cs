using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.MaterialApproval.Tasks;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.MaterialApproval;

[TestClass]
public class MaterialApprovalAnalyticalExpiryDateTaskTest : TestScope<MaterialApprovalAnalyticalDateTask>
{
    [TestMethod]
    public void Task_ShouldRun_AfterValidation()
    {
        // arrange
        var scope = new DefaultScope();

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow(Operation.Custom)]
    [DataRow(Operation.Delete)]
    [DataRow(Operation.Insert)]
    [DataRow(Operation.Undefined)]
    public async Task Task_ShouldNotRun_IfOperationIsNotUpdate(Operation operation)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateMaterialApprovalContext();
        context.Operation = operation;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldUpdateAnalyticalExpiryPreviousDate()
    {
        // arrange
        var scope = new DefaultScope();
        var original = scope.MaterialApproval.Clone();

        var context = scope.CreateMaterialApprovalContext(original);
        context.Target.AnalyticalExpiryDate = context.Original.AnalyticalExpiryDate.AddMonths(2);
        context.Operation = Operation.Update;

        // act
        var shouldRun = await scope.InstanceUnderTest.Run(context);

        // assert
        shouldRun.Should().BeTrue();
        context.Target.AnalyticalExpiryDatePrevious.Should().Be(original.AnalyticalExpiryDate);
    }

    private class DefaultScope : TestScope<MaterialApprovalAnalyticalDateTask>
    {
        public readonly MaterialApprovalEntity MaterialApproval =
            new()
            {
                Facility = "Facility1",
                FacilityId = Guid.NewGuid(),
                FacilityServiceId = Guid.NewGuid(),
                FacilityServiceNumber = "number",
                AccumulatedTonnage = 13,
                FacilityServiceName = "Facility Service 1",
                SubstanceId = Guid.NewGuid(),
                GeneratorId = Guid.NewGuid(),
                GeneratorName = "Generator",
                BillingCustomerId = Guid.NewGuid(),
                BillingCustomerName = "Billing Customer",
                BillingCustomerContactId = Guid.NewGuid(),
                BillingCustomerContact = "Billing Contact",
                BillingCustomerContactAddress = "Billing Address",
                BillingCustomerContactReceiveLoadSummary = true,
                ThirdPartyAnalyticalCompanyId = Guid.NewGuid(),
                ThirdPartyAnalyticalCompanyName = "Third party company",
                ThirdPartyAnalyticalCompanyContactId = Guid.NewGuid(),
                ThirdPartyAnalyticalCompanyContact = " Third Party Contact",
                ThirdPartyAnalyticalContactReceiveLoadSummary = false,
                TruckingCompanyId = Guid.NewGuid(),
                TruckingCompanyName = "Trucking Company",
                TruckingCompanyContactId = Guid.NewGuid(),
                TruckingCompanyContact = "Trucking Contact",
                TruckingCompanyContactReceiveLoadSummary = true,
                HazardousNonhazardous = HazardousClassification.Nonhazardous,
                LoadSummaryReport = true,
                AnalyticalExpiryAlertActive = false,
                AnalyticalExpiryDate = DateTimeOffset.Now,
                SecureRepresentative = "CreatedBy",
                SourceRegion = SourceRegionEnum.InRegion,
                LegalEntity = "Christiansen - Rowe",
                LegalEntityId = Guid.NewGuid(),
            };

        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public BusinessContext<MaterialApprovalEntity> CreateMaterialApprovalContext(MaterialApprovalEntity original = null)
        {
            return new(MaterialApproval, original ?? MaterialApproval);
        }
    }
}
