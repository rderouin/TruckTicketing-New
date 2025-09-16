using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Newtonsoft.Json;

using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.InvoiceConfiguration.Tasks;

using Trident.Business;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.Shared.Domain.Tests.Tasks;

[TestClass]
public class GenerateInvoiceConfigurationHashTaskTest : TestScope<GenerateInvoiceConfigurationHashTask>
{
    [TestMethod]
    public async Task Task_ShouldNotRun_WhenTargetIsNull()
    {
        ////arrange
        var scope = new DefaultScope();
        var context = new BusinessContext<InvoiceConfigurationEntity>(null);

        //act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        //assert
        result.Should().BeFalse();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationIdFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = Guid.NewGuid();
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = null;
        invoiceConfiguration.Permutations = null;
        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.Permutations = null;
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedById = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationBusinessUnitIdFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = "AA-100050";
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = null;
        invoiceConfiguration.Permutations = null;
        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.Permutations = null;
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedById = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationNameFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = "TT Petro Canada";
        invoiceConfiguration.Description = null;
        invoiceConfiguration.Permutations = null;
        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.Permutations = null;
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedById = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationDescriptionFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = "This is test invoice configuration";
        invoiceConfiguration.Permutations = null;
        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.Permutations = null;
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedById = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationPermutationsFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = null;
        invoiceConfiguration.Permutations = new()
        {
            new()
            {
                Name = "TT Petro Canada",
                SourceLocation = "Test SourceLocation A",
                ServiceType = "Test ServiceType A",
                WellClassification = "All",
                Substance = "All",
                Facility = "Facility A",
            },
        };

        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedById = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationCustomerNameFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = null;
        invoiceConfiguration.Permutations = null;
        invoiceConfiguration.CustomerName = "QQ Generator/Customer 01";
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedById = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationServiceTypeNameFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = new()
        {
            Key = Guid.NewGuid(),
            List = new()
            {
                "Service01",
                "Service02",
            },
        };

        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = null;
        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.Permutations = null;
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedById = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationSubstanceNameFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = new()
        {
            Key = Guid.NewGuid(),
            List = new()
            {
                "Substance01",
                "Substance02",
            },
        };

        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = null;
        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.Permutations = null;
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedById = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationSourceLocationIdentifierFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = new()
        {
            Key = Guid.NewGuid(),
            List = new()
            {
                "Id-001",
                "Id-002",
            },
        };

        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = null;
        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.Permutations = null;
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedById = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationFacilityCodeFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = new()
        {
            Key = Guid.NewGuid(),
            List = new()
            {
                "Id-001",
                "Id-002",
            },
        };

        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = null;
        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.Permutations = null;
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedById = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationPermutationHashFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = null;
        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.PermutationsHash = "5537093D2EA52B432228E072824E0CE0DE63986C44638429A85B29411AF43810";
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedById = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationCreatedAtFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = null;
        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedById = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationUpdatedAtFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = null;
        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedById = null;
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationCreatedByFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = null;
        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.CreatedById = null;
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationUpdatedByFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = null;
        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedById = null;
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationCreatedByIdFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = null;
        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.UpdatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ExcludeInvoiceConfigurationUpdatedByIdFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfiguration = context.Target.Clone();
        invoiceConfiguration.Id = default;
        invoiceConfiguration.BusinessUnitId = null;
        invoiceConfiguration.ServiceTypesName = null;
        invoiceConfiguration.SubstancesName = null;
        invoiceConfiguration.SourceLocationIdentifier = null;
        invoiceConfiguration.FacilityCode = null;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.Name = null;
        invoiceConfiguration.Description = null;
        invoiceConfiguration.CustomerName = null;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.IncludeExternalDocumentAttachment = false;
        invoiceConfiguration.PermutationsHash = null;
        invoiceConfiguration.UpdatedAt = default;
        invoiceConfiguration.CreatedBy = null;
        invoiceConfiguration.CreatedAt = default;
        invoiceConfiguration.UpdatedBy = null;
        invoiceConfiguration.CreatedById = null;

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForPermutations_ValidHashGenerated()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateInvoiceConfigurationContext();
        var invoiceConfigurationClone = context.Target.Clone();
        var invoiceConfiguration = new InvoiceConfigurationEntity
        {
            WellClassifications = invoiceConfigurationClone.AllWellClassifications ? null : invoiceConfigurationClone.WellClassifications,
            SourceLocations = invoiceConfigurationClone.AllSourceLocations ? null : invoiceConfigurationClone.SourceLocations,
            Facilities = invoiceConfigurationClone.AllFacilities ? null : invoiceConfigurationClone.Facilities,
            Substances = invoiceConfigurationClone.AllSubstances ? null : invoiceConfigurationClone.Substances,
            ServiceTypes = invoiceConfigurationClone.AllServiceTypes ? null : invoiceConfigurationClone.ServiceTypes,
            SplitEdiFieldDefinitions = invoiceConfigurationClone.SplitEdiFieldDefinitions,
            SplittingCategories = invoiceConfigurationClone.SplittingCategories,
            AllSourceLocations = invoiceConfigurationClone.AllSourceLocations,
            AllFacilities = invoiceConfigurationClone.AllFacilities,
            AllServiceTypes = invoiceConfigurationClone.AllServiceTypes,
            AllSubstances = invoiceConfigurationClone.AllSubstances,
            AllWellClassifications = invoiceConfigurationClone.AllWellClassifications,
            IsSplitByFacility = invoiceConfigurationClone.IsSplitByFacility,
            IsSplitByServiceType = invoiceConfigurationClone.IsSplitByServiceType,
            IsSplitBySourceLocation = invoiceConfigurationClone.IsSplitBySourceLocation,
            IsSplitBySubstance = invoiceConfigurationClone.IsSplitBySubstance,
            IsSplitByWellClassification = invoiceConfigurationClone.IsSplitByWellClassification,
            CatchAll = invoiceConfigurationClone.CatchAll,
            CustomerId = invoiceConfigurationClone.CustomerId,
        };

        var hash = scope.GenerateHash(invoiceConfiguration);
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsTrue(string.Equals(context.Target.PermutationsHash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    private class DefaultScope : TestScope<GenerateInvoiceConfigurationHashTask>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public List<InvoiceConfigurationEntity> GenerateInvoiceConfiguration()
        {
            return new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    BusinessUnitId = "AA-100050",
                    AllFacilities = false,
                    AllServiceTypes = false,
                    AllSourceLocations = false,
                    AllSubstances = false,
                    AllWellClassifications = false,
                    CatchAll = false,
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "QQ Generator/Customer 01",
                    Description = "This is test invoice configuration",
                    IncludeInternalDocumentAttachment = true,
                    IncludeExternalDocumentAttachment = true,
                    IsSplitByFacility = false,
                    IsSplitByServiceType = false,
                    IsSplitBySourceLocation = false,
                    IsSplitBySubstance = false,
                    IsSplitByWellClassification = false,
                    Name = "TT Petro Canada",
                    Facilities = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new()
                        {
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                        },
                    },
                    ServiceTypes = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new()
                        {
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                        },
                    },
                    SourceLocationIdentifier = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new()
                        {
                            "Id-001",
                            "Id-002",
                        },
                    },
                    SourceLocations = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new()
                        {
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                        },
                    },
                    SplitEdiFieldDefinitions = null,
                    SplittingCategories = null,
                    WellClassifications = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new() { "Drilling" },
                    },
                    PermutationsHash = "5537093D2EA52B432228E072824E0CE0DE63986C44638429A85B29411AF43810",
                    Permutations = new()
                    {
                        new()
                        {
                            Name = "TT Petro Canada",
                            SourceLocation = "Test SourceLocation A",
                            ServiceType = "Test ServiceType A",
                            WellClassification = "All",
                            Substance = "All",
                            Facility = "Facility A",
                        },
                    },
                    SubstancesName = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new()
                        {
                            "Substance01",
                            "Substance02",
                        },
                    },
                    ServiceTypesName = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new()
                        {
                            "Service01",
                            "Service02",
                        },
                    },
                    Substances = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new()
                        {
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                        },
                    },
                    CreatedAt = new DateTime(2025, 1, 17, 18, 11, 30),
                    UpdatedAt = new DateTime(2025, 1, 17, 18, 11, 30),
                    CreatedBy = "Panth Shah",
                    UpdatedBy = "Panth Shah",
                    CreatedById = Guid.NewGuid().ToString(),
                    UpdatedById = Guid.NewGuid().ToString(),
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    AllFacilities = true,
                    AllServiceTypes = true,
                    AllSourceLocations = true,
                    AllSubstances = true,
                    AllWellClassifications = false,
                    CatchAll = false,
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "QQ Customer 555",
                    Description = "New Invoice Configuration added with new customer",
                    IncludeInternalDocumentAttachment = false,
                    IncludeExternalDocumentAttachment = false,
                    IsSplitByFacility = false,
                    IsSplitByServiceType = false,
                    IsSplitBySourceLocation = false,
                    IsSplitBySubstance = false,
                    IsSplitByWellClassification = false,
                    Name = "TT Shell Corp",
                    Facilities = null,
                    ServiceTypes = null,
                    SourceLocationIdentifier = null,
                    SourceLocations = null,
                    SplitEdiFieldDefinitions = null,
                    SplittingCategories = null,
                    WellClassifications = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new() { "Drilling" },
                    },
                    SubstancesName = null,
                    ServiceTypesName = null,
                    Substances = null,
                },
            };
        }

        public string GenerateHash(InvoiceConfigurationEntity invoiceConfigurationClone)
        {
            using var sHa256 = SHA256.Create();
            return Convert.ToHexString(sHa256.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(invoiceConfigurationClone))));
        }

        public BusinessContext<InvoiceConfigurationEntity> CreateInvoiceConfigurationContext()
        {
            return new(GenerateInvoiceConfiguration().First());
        }
    }
}
