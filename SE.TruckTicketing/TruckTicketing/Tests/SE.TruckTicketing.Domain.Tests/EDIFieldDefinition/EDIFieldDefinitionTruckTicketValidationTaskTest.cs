using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Castle.Components.DictionaryAdapter;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.EDIFieldDefinition;
using SE.Shared.Domain.Entities.EDIFieldValue;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Domain.Entities.EDIFieldDefinition;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Search;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.EDIFieldDefinition;

[TestClass]
public class EDIFieldDefinitionTruckTicketValidationTaskTest : TestScope<EDIFieldDefinitionTruckTicketValidationTask>
{
    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_TruckTicketEDIFieldValueValidation_ShouldPass_EDIData_IsValid()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateEDIFieldDefinitionContext();
        context.ContextBag[nameof(EdiDefinitionDataLoaderTask.EdiDefinitionsKey)] = new List<EDIFieldDefinitionEntity>();
        var operationStage = scope.InstanceUnderTest.Stage;
        context.Target.CustomerId = new("a2af9a2b-0367-4380-b7d7-19040bc8e433");

        var searchResults = new List<TruckTicketEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EdiFieldValues = new EditableList<EDIFieldValueEntity>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        EDIFieldDefinitionId = Guid.Parse("4ea8a700-be06-46ef-a8ac-0247cb5bb9d5"),
                        EDIFieldName = "Invoice Number",
                        EDIFieldValueContent = "123456",
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        EDIFieldDefinitionId = Guid.Parse("8c564913-7cf3-40c8-8404-2d6d46fd0b5b"),
                        EDIFieldName = "Invoice Name",
                        EDIFieldValueContent = "ABC Invoice",
                    },
                },
            },
        };

        var results = new SearchResults<TruckTicketEntity, SearchCriteria> { Results = searchResults };

        scope.TruckTicketProviderMock.Setup(x => x.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
             .ReturnsAsync(results);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        operationStage.Should().Be(OperationStage.PostValidation);
    }

    private class DefaultScope : TestScope<EDIFieldDefinitionTruckTicketValidationTask>
    {
        private readonly EDIFieldDefinitionEntity _defaultEdiFieldDefinition =
            new()
            {
                Id = Guid.Parse("8c564913-7cf3-40c8-8404-2d6d46fd0b5b"),
                CustomerId = CustomerId,
                EDIFieldLookupId = "e8a6de2f-f63a-41b7-83e9-831c9cc4b4f5",
                EDIFieldName = "Invoice Name",
                DefaultValue = "ABC Invoice",
                IsRequired = true,
                IsPrinted = true,
                ValidationRequired = true,
                ValidationPatternId = Guid.Parse("3fe2abb0-aba6-4065-a8a6-b0c3690d580e"),
                ValidationErrorMessage = "only letters allowed",
                ValidationPattern = "^[a-zA-Z ]*$",
                CreatedAt = DateTimeOffset.Parse("0001-01-01T00:00:00+00:00"),
                CreatedBy = null,
                CreatedById = null,
                UpdatedAt = DateTimeOffset.Parse("0001-01-01T00:00:00+00:00"),
                UpdatedBy = null,
                UpdatedById = null,
            };

        public DefaultScope()
        {
            InstanceUnderTest = new(TruckTicketProviderMock.Object);
        }

        public Mock<IProvider<Guid, TruckTicketEntity>> TruckTicketProviderMock { get; } = new();

        public static Guid CustomerId => new("a2af9a2b-0367-4380-b7d7-19040bc8e433");

        public BusinessContext<EDIFieldDefinitionEntity> CreateEDIFieldDefinitionContext()
        {
            return new(_defaultEdiFieldDefinition);
        }
    }
}
