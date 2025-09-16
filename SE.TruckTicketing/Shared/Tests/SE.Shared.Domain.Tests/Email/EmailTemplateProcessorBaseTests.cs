using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.EmailTemplates;

using Trident.Data.Contracts;
using Trident.Testing.TestScopes;

namespace SE.Shared.Domain.Tests.Email;

[TestClass]
public class EmailTemplateProcessorBaseTests
{
    [TestMethod]
    public async Task BeforeSend_ShouldSetBody_FromTemplate()
    {
        // arrange
        var scope = new DefaultScope();
        var request = new EmailTemplateDeliveryRequest { Recipients = "test@email.com" };
        var template = new EmailTemplateEntity { Body = "Hello World" };
        var context = new EmailTemplateProcessingContext(request) { EmailTemplate = template };

        // act
        await scope.InstanceUnderTest.BeforeSend(context);

        // assert
        context.MailMessage.Body.Should().Be(template.Body);
    }

    [DataTestMethod]
    [DataRow("Hello world")]
    [DataRow("Hello world, Please send an email to me@yahoo")]
    [DataRow("Test the @@")]
    [DataRow("Test the entered token direct @(Model.Test)")]
    public async Task BeforeSend_ShouldEscapeSpecialRazorAtCharacters_BeforeRenderingTemplate(string body)
    {
        // arrange
        var scope = new DefaultScope();
        var request = new EmailTemplateDeliveryRequest { Recipients = "test@email.com" };
        var template = new EmailTemplateEntity { Body = body };
        var context = new EmailTemplateProcessingContext(request) { EmailTemplate = template };

        // act
        await scope.InstanceUnderTest.BeforeSend(context);

        // assert
        context.MailMessage.Body.Should().Be(template.Body);
    }

    [DataTestMethod]
    [DataRow("Hello world", "Hello world")]
    [DataRow("Hello world, Please send an email to me@yahoo [#Test#]", "Hello world, Please send an email to me@yahoo hello")]
    [DataRow("Test the @@ [#Test#]", "Test the @@ hello")]
    [DataRow("Test the entered token direct @(Model.Test)", "Test the entered token direct @(Model.Test)")]
    public async Task BeforeSend_ShouldReplaceUiTokensWithRazorTokens_BeforeRenderingTemplate(string body, string expected)
    {
        // arrange
        var scope = new DefaultScope();
        var request = new EmailTemplateDeliveryRequest { Recipients = "test@email.com" };
        var template = new EmailTemplateEntity
        {
            Body = body,
            EmailTemplateEvent = new()
            {
                Fields = new()
                {
                    new()
                    {
                        UiToken = "[#Test#]",
                        RazorToken = "@(Model.Test)",
                    },
                },
            },
        };

        var context = new EmailTemplateProcessingContext(request) { EmailTemplate = template };

        // act
        await scope.InstanceUnderTest.BeforeSend(context);

        // assert
        context.MailMessage.Body.Should().Be(expected);
    }

    [TestMethod]
    public async Task BeforeSend_ShouldSetSubject_FromTemplate()
    {
        // arrange
        var scope = new DefaultScope();
        var request = new EmailTemplateDeliveryRequest { Recipients = "test@email.com" };
        var template = new EmailTemplateEntity { Subject = "Hello World" };
        var context = new EmailTemplateProcessingContext(request) { EmailTemplate = template };

        // act
        await scope.InstanceUnderTest.BeforeSend(context);

        // assert
        context.MailMessage.Subject.Should().Be(template.Subject);
    }

    [TestMethod]
    public async Task BeforeSend_ShouldSetToRecipients_FromContext()
    {
        // arrange
        var scope = new DefaultScope();
        var request = new EmailTemplateDeliveryRequest { Recipients = "test@email.com" };
        var template = new EmailTemplateEntity();
        var context = new EmailTemplateProcessingContext(request) { EmailTemplate = template };

        // act
        await scope.InstanceUnderTest.BeforeSend(context);

        // assert
        context.MailMessage.To[0].ToString().Should().Be(request.Recipients);
    }

    [TestMethod]
    public async Task BeforeSend_ShouldSetCcRecipients_FromContext()
    {
        // arrange
        var scope = new DefaultScope();
        var request = new EmailTemplateDeliveryRequest { CcRecipients = "test@email.com" };
        var template = new EmailTemplateEntity();
        var context = new EmailTemplateProcessingContext(request) { EmailTemplate = template };

        // act
        await scope.InstanceUnderTest.BeforeSend(context);

        // assert
        context.MailMessage.CC[0].ToString().Should().Be(request.CcRecipients);
    }

    [TestMethod]
    public async Task BeforeSend_ShouldSetBccRecipients_FromContext()
    {
        // arrange
        var scope = new DefaultScope();
        var request = new EmailTemplateDeliveryRequest { BccRecipients = "test@email.com" };
        var template = new EmailTemplateEntity();
        var context = new EmailTemplateProcessingContext(request) { EmailTemplate = template };

        // act
        await scope.InstanceUnderTest.BeforeSend(context);

        // assert
        context.MailMessage.Bcc[0].ToString().Should().Be(request.BccRecipients);
    }

    [TestMethod]
    public async Task BeforeSend_ShouldSetReplyToRecipients_FromTemplateForAdhocReplyType()
    {
        // arrange
        var scope = new DefaultScope();
        var request = new EmailTemplateDeliveryRequest();
        var template = new EmailTemplateEntity { CustomReplyEmail = "test@email.com" };
        var context = new EmailTemplateProcessingContext(request) { EmailTemplate = template };

        // act
        await scope.InstanceUnderTest.BeforeSend(context);

        // assert
        context.MailMessage.ReplyToList[0].ToString().Should().Be(template.CustomReplyEmail);
    }

    [DataTestMethod]
    [DataRow(null, null, "f3ff730d-aca7-4ae5-8caa-da45a1827228", DisplayName = "ResolveEmailTemplate can resolve a global template.")]
    [DataRow("FCFST", null, "a0bd454c-d084-4671-bd4c-551f7d5471a8", DisplayName = "ResolveEmailTemplate can resolve a facility template.")]
    [DataRow(null, "e70afe22-2e46-4474-b275-3579cb0d7b78", "470835f1-8004-42c6-af37-09630dc3fd46", DisplayName = "ResolveEmailTemplate can resolve an account template.")]
    [DataRow("FCFST", "e70afe22-2e46-4474-b275-3579cb0d7b78", "edf30dc5-c2c6-43ee-b655-7c1c6415fc2c", DisplayName = "ResolveEmailTemplate can resolve a facility and an account template combined.")]
    public async Task ResolveEmailTemplate_Internal(string facilityId, string accountIdString, string expectedTemplateIdString)
    {
        // arrange
        var accountId = accountIdString.HasText() ? (Guid?)Guid.Parse(accountIdString) : null;
        var expectedTemplateId = Guid.Parse(expectedTemplateIdString);
        var scope = new DefaultScope();
        var dataset = CreateDataSet();
        var evt = new EmailTemplateEventEntity();
        scope.EmailTemplateProviderMock.Setup(e => e.Get(It.IsAny<Expression<Func<EmailTemplateEntity, bool>>>(),
                                                         It.IsAny<Func<IQueryable<EmailTemplateEntity>, IOrderedQueryable<EmailTemplateEntity>>>(),
                                                         It.IsAny<IEnumerable<string>>(),
                                                         It.IsAny<bool>(),
                                                         It.IsAny<bool>(),
                                                         It.IsAny<bool>())).ReturnsAsync(dataset);

        scope.EmailTemplateEventProviderMock.Setup(e => e.GetById(It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(evt);

        // act
        var emailTemplateEntity = await scope.InstanceUnderTest.ResolveEmailTemplateInternal("Random Template", facilityId, accountId);

        // assert
        emailTemplateEntity.Id.Should().Be(expectedTemplateId);

        List<EmailTemplateEntity> CreateDataSet()
        {
            return new()
            {
                new()
                {
                    Id = new("f3ff730d-aca7-4ae5-8caa-da45a1827228"),
                    Name = "Random Template",
                    FacilitySiteIds = null,
                    AccountIds = null,
                },
                new()
                {
                    Id = new("a0bd454c-d084-4671-bd4c-551f7d5471a8"),
                    Name = "Random Template",
                    FacilitySiteIds = new() { List = { "FCFST" } },
                    AccountIds = null,
                },
                new()
                {
                    Id = new("470835f1-8004-42c6-af37-09630dc3fd46"),
                    Name = "Random Template",
                    FacilitySiteIds = null,
                    AccountIds = new() { List = { new Guid("e70afe22-2e46-4474-b275-3579cb0d7b78") } },
                },
                new()
                {
                    Id = new("edf30dc5-c2c6-43ee-b655-7c1c6415fc2c"),
                    Name = "Random Template",
                    FacilitySiteIds = new() { List = { "FCFST" } },
                    AccountIds = new() { List = { new Guid("e70afe22-2e46-4474-b275-3579cb0d7b78") } },
                },
            };
        }
    }

    protected class DefaultScope : TestScope<SampleProcessor>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(new RazorEngineTemplateRenderer(),
                                    EmailTemplateProviderMock.Object,
                                    EmailTemplateEventProviderMock.Object);

            EmailTemplateRendererMock.Setup(renderer => renderer.RenderTemplate(It.IsAny<string>(),
                                                                                It.IsAny<string>(),
                                                                                It.IsAny<object>()))
                                     .Returns((string _,
                                               string source,
                                               object _) => ValueTask.FromResult(source));
        }

        public Mock<IEmailTemplateRenderer> EmailTemplateRendererMock { get; } = new();

        public Mock<IProvider<Guid, EmailTemplateEntity>> EmailTemplateProviderMock { get; } = new();

        public Mock<IProvider<Guid, EmailTemplateEventEntity>> EmailTemplateEventProviderMock { get; } = new();
    }

    public class SampleProcessor : EmailTemplateProcessorBase
    {
        public SampleProcessor(IEmailTemplateRenderer emailTemplateRenderer,
                               IProvider<Guid, EmailTemplateEntity> emailTemplateProvider,
                               IProvider<Guid, EmailTemplateEventEntity> emailTemplateEventProvider)
            : base(emailTemplateRenderer, emailTemplateProvider, emailTemplateEventProvider)
        {
        }

        public override ValueTask<EmailTemplateEntity> ResolveEmailTemplate(EmailTemplateProcessingContext context)
        {
            return ValueTask.FromResult<EmailTemplateEntity>(default);
        }

        public async ValueTask<EmailTemplateEntity> ResolveEmailTemplateInternal(string emailTemplateKey, string facilitySiteId = null, Guid? accountId = null)
        {
            return await base.ResolveEmailTemplate(emailTemplateKey, facilitySiteId, accountId);
        }

        protected override ValueTask<object> GetTemplateDataObject(EmailTemplateProcessingContext context)
        {
            return ValueTask.FromResult<object>(new SampleDataObject { Test = "hello" });
        }

        public class SampleDataObject
        {
            public string Test { get; set; }
        }
    }
}
