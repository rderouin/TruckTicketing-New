using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.BillingConfiguration.Tasks;
using SE.Shared.Domain.Entities.EDIFieldDefinition;
using SE.Shared.Domain.Entities.EDIFieldValue;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.BillingConfiguration;

[TestClass]
public class BillingConfigurationEDIFieldValueValidationCheckerTaskTest : TestScope<BillingConfigurationEDIFieldValueValidationCheckerTask>
{
    private class DefaultScope : TestScope<BillingConfigurationEDIFieldValueValidationCheckerTask>
    {
        private readonly BillingConfigurationEntity _defaultBillingConfiguration =
            new()
            {
                Id = Guid.NewGuid(),
                EDIValueData = new()
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
            };

        private readonly EDIFieldDefinitionEntity[] _defaultEdIFieldDefinitions =
        {
            new()
            {
                Id = Guid.Parse("4ea8a700-be06-46ef-a8ac-0247cb5bb9d5"),
                CustomerId = new("a2af9a2b-0367-4380-b7d7-19040bc8e433"),
                EDIFieldLookupId = "6ed5bd2a-02c6-4485-a906-8694cd462fc8",
                EDIFieldName = "Invoice Number",
                DefaultValue = "123456",
                IsRequired = true,
                IsPrinted = true,
                ValidationRequired = true,
                ValidationPatternId = Guid.Parse("970c2f87-1390-4e35-83ec-b9ba93255c74"),
                ValidationErrorMessage = "only numbers allowed",
                ValidationPattern = "[0-9]+",
                CreatedAt = DateTimeOffset.Parse("0001-01-01T00:00:00+00:00"),
                CreatedBy = null,
                CreatedById = null,
                UpdatedAt = DateTimeOffset.Parse("0001-01-01T00:00:00+00:00"),
                UpdatedBy = null,
                UpdatedById = null,
            },
            new()
            {
                Id = Guid.Parse("8c564913-7cf3-40c8-8404-2d6d46fd0b5b"),
                CustomerId = new("a2af9a2b-0367-4380-b7d7-19040bc8e433"),
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
            },
        };

        private readonly Mock<IProvider<Guid, EDIFieldDefinitionEntity>> EDIFieldDefinitionProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(EDIFieldDefinitionProviderMock.Object);
            SetupExistingEDIFieldConfiguration(_defaultEdIFieldDefinitions);
        }

        public void SetupExistingEDIFieldConfiguration(params EDIFieldDefinitionEntity[] entities)
        {
            EDIFieldDefinitionProviderMock.SetupEntities(entities);
        }

        public BusinessContext<BillingConfigurationEntity> CreateBillingConfigurationContext()
        {
            return new(_defaultBillingConfiguration);
        }

        public Dictionary<EDIFieldValueEntity, TTErrorCodes> EDIFieldValueValidation(BusinessContext<BillingConfigurationEntity> context)
        {
            return context.GetContextBagItemOrDefault<Dictionary<EDIFieldValueEntity, TTErrorCodes>>(BillingConfigurationEDIFieldValueValidationCheckerTask.ResultKey, new());
        }
    }
}
