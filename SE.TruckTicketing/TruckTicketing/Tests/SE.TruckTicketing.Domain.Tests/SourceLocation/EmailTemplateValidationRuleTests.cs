using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain;
using SE.Shared.Domain.EmailTemplates;
using SE.Shared.Domain.EmailTemplates.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Tests.SourceLocation;

[TestClass]
public class EmailTemplateValidationRuleTests
{
    [TestMethod]
    public async Task Rule_ShouldPass_ForValidEmailTemplate()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        validationResults.Should().BeEmpty();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(null)]
    public async Task Rule_ShouldFail_WhenNameIsEmpty(string name)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        context.Target.Name = name;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EmailTemplate_Name_Required));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenNameIsTooLong()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        context.Target.Name = new(Enumerable.Range(0, 101).Select(_ => 'a').ToArray());
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EmailTemplate_Name_Length));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenNameIsNotUnique()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        context.Target.HasUniqueName = false;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EmailTemplate_Name_Unique));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenTemplateIsNotGloballyUnique()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        context.Target.Siblings = new EmailTemplateEntity[]
        {
            new()
            {
                FacilitySiteIds = new()
                {
                    List = new()
                    {
                        "DCFST",
                        "LGFST",
                    },
                },
            },
            new()
            {
                FacilitySiteIds = null,
            },
        };

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EmailTemplate_FacilitySiteIds_GloballyUnique));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenTemplateIsNotGloballyUnique_NonNullCheck()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        context.Target.Siblings = new EmailTemplateEntity[]
        {
            new()
            {
                FacilitySiteIds = new()
                {
                    List = new()
                    {
                        "DCFST",
                        "LGFST",
                    },
                },
            },
            new()
            {
                FacilitySiteIds = new(),
            },
        };

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EmailTemplate_FacilitySiteIds_GloballyUnique));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenTemplateHasFacilityOverlap()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        context.Target.FacilitySiteIds = new()
        {
            List = new()
            {
                "DCFST",
                "BGSWD",
            },
        };

        context.Target.Siblings = new EmailTemplateEntity[]
        {
            new()
            {
                FacilitySiteIds = new()
                {
                    List = new()
                    {
                        "DCFST",
                        "LGFST",
                    },
                },
            },
            new()
            {
                FacilitySiteIds = null,
            },
        };

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EmailTemplate_FacilitySiteIds_NoFacilityOverlap));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_WhenTemplateHasNoFacilityOverlap()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        context.Target.FacilitySiteIds = new()
        {
            List = new()
            {
                "BNYLF",
                "BGSWD",
            },
        };

        context.Target.Siblings = new EmailTemplateEntity[]
        {
            new()
            {
                FacilitySiteIds = new()
                {
                    List = new()
                    {
                        "DCFST",
                        "LGFST",
                    },
                },
            },
            new()
            {
                FacilitySiteIds = null,
            },
        };

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenEventIdIsDefault()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        context.Target.EventId = Guid.Empty;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EmailTemplate_EventId_Required));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenSubjectIsTooLong()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        context.Target.Subject = new(Enumerable.Range(0, 401).Select(_ => 'a').ToArray());
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EmailTemplate_Subject_Length));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenBodyIsTooLong()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        context.Target.Body = new(Enumerable.Range(0, 4001).Select(_ => 'a').ToArray());
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EmailTemplate_Body_Length));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenReplyEmailIsInvalid()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        context.Target.CustomReplyEmail = "hello world";
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EmailTemplate_CustomReplyEmail_Invalid));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenNoSenderEmailProvided_UserCustomSenderEmailEnabled()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        context.Target.UseCustomSenderEmail = true;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EmailTemplate_OverrideSender_EmailRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow("Hello World")]
    [DataRow("Hello World@")]
    [DataRow("Hello World@test")]
    public async Task Rule_ShouldFail_InvalidSenderEmailFormat(string senderEmail)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        context.Target.UseCustomSenderEmail = true;
        context.Target.SenderEmail = senderEmail;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EmailTemplate_SenderEmail_ValidFormat));
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow("hello@test.com")]
    [DataRow("hello@secure-energy.com")]
    [DataRow("hello@secure-energy.co.ca")]
    public async Task Rule_ShouldPass_ValidSenderEmailFormat(string senderEmail)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        context.Target.UseCustomSenderEmail = true;
        context.Target.SenderEmail = senderEmail;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .NotContain(TTErrorCodes.EmailTemplate_SenderEmail_ValidFormat);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("Hello World")]
    [DataRow("Hello World@")]
    [DataRow("Hello World@test")]
    [DataRow("Hello World@test; hello@test.com")]
    public async Task Rule_ShouldFail_WhenBccEmailsIsInvalid(string emails)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidContext();
        context.Target.CustomReplyEmail = emails;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.EmailTemplate_CustomBccEmails_Invalid));
    }

    [DataTestMethod]
    [DataRow("global", "global", false)]
    [DataRow("global", "one-facility", true)]
    [DataRow("global", "one-customer", true)]
    [DataRow("global", "unique", true)]
    [DataRow("one-facility", "global", true)]
    [DataRow("one-facility", "one-facility", false)]
    [DataRow("one-facility", "one-customer", true)]
    [DataRow("one-facility", "unique", true)]
    [DataRow("one-customer", "global", true)]
    [DataRow("one-customer", "one-facility", true)]
    [DataRow("one-customer", "one-customer", false)]
    [DataRow("one-customer", "unique", true)]
    [DataRow("unique", "global", true)]
    [DataRow("unique", "one-facility", true)]
    [DataRow("unique", "one-customer", true)]
    [DataRow("unique", "unique", false)]
    [DataRow("one-facility", "other-facility", true)]
    [DataRow("one-customer", "other-customer", true)]
    [DataRow("global", "many-overlapping", true)]
    [DataRow("unique", "many-overlapping", false)]
    [DataRow("one-facility", "many-overlapping", true)]
    [DataRow("one-customer", "many-overlapping", true)]
    public void Rule_GloballyUnique(string selfCase, string siblingCase, bool expectedToBeUnique)
    {
        // arrange
        var (sites, customers) = GetTemplate(selfCase);
        var (otherSite, otherCustomers) = GetTemplate(siblingCase);
        var siblings = new[]
        {
            new EmailTemplateEntity
            {
                FacilitySiteIds = new() { List = otherSite },
                AccountIds = new() { List = otherCustomers },
            },
        };

        var self = new EmailTemplateEntity
        {
            FacilitySiteIds = new() { List = sites },
            AccountIds = new() { List = customers },
            Siblings = siblings,
        };

        // act
        var isUnique = EmailTemplateValidationRules.BeUnique(self);

        // assert
        isUnique.Should().Be(expectedToBeUnique);

        (List<string>, List<Guid>) GetTemplate(string usecase)
        {
            var noFacility = new List<string>();
            var noCustomer = new List<Guid>();
            var singleFacility = new List<string> { "FCFST" };
            var singleCustomer = new List<Guid> { new("cb9e60a5-09eb-4750-84c0-3c33b19750d7") };
            var otherFacility = new List<string> { "BCFST" };
            var otherCustomer = new List<Guid> { new("80dbe577-a9ac-43ac-bc64-2174f08cae8a") };
            var manyFacilities = new List<string>
            {
                "FCFST",
                "BCFST",
            };

            var manyCustomers = new List<Guid>
            {
                new("cb9e60a5-09eb-4750-84c0-3c33b19750d7"),
                new("80dbe577-a9ac-43ac-bc64-2174f08cae8a"),
            };

            return usecase switch
                   {
                       "global" => (noFacility, noCustomer),
                       "one-facility" => (singleFacility, noCustomer),
                       "one-customer" => (noFacility, singleCustomer),
                       "unique" => (singleFacility, singleCustomer),
                       "other-facility" => (otherFacility, noCustomer),
                       "other-customer" => (noFacility, otherCustomer),
                       "many-overlapping" => (manyFacilities, manyCustomers),
                       _ => throw new ArgumentOutOfRangeException(nameof(usecase), usecase, null),
                   };
        }
    }

    private class DefaultScope : TestScope<EmailTemplateValidationRules>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public EmailTemplateEntity ValidTemplate =>
            new()
            {
                Name = "Test Email Template",
                EventId = Guid.NewGuid(),
                CustomReplyEmail = "est@email.com",
                CustomBccEmails = "est@email.com; esquire@email.com",
                SenderEmail = string.Empty,
                UseCustomSenderEmail = false,
            };

        public BusinessContext<EmailTemplateEntity> CreateValidContext(EmailTemplateEntity original = null)
        {
            return new(ValidTemplate, original);
        }
    }
}
