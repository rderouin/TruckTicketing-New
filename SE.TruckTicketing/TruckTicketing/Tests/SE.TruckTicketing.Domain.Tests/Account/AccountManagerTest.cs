using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.EmailTemplates;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.EDIFieldDefinition;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.NewAccount;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Security;

using Trident.Contracts;
using Trident.Mapper;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.Account;

[TestClass]
public class AccountManagerTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task AccountManager_InitiateNewAccountCreditReviewal()
    {
        // arrange
        var scope = new DefaultScope();
        Exception exception = null;
        try
        {
            // act
            await scope.InstanceUnderTest.InitiateNewAccountCreditReviewal(new());
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // assert
        Assert.IsTrue(exception is null);
    }

    private class DefaultScope : TestScope<NewAccountManager>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(AccountManagerMock.Object,
                                    BillingManagerMock.Object,
                                    EDIFIeldsManagerMock.Object,
                                    InvoiceConfigurationManagerMock.Object,
                                    MapperRegistryMock.Object,
                                    AccountAttachmentsBlobStorage.Object,
                                    UserContextAccessMock.Object,
                                    EmailTemplateSenderMock.Object);

            AccountManagerMock.Setup(x => x.Patch(It.IsAny<Guid>(), It.IsAny<Dictionary<string, object>>(), null, false)).ReturnsAsync(new AccountEntity());
        }

        public Mock<IMapperRegistry> MapperRegistryMock { get; } = new();

        public Mock<UserContextAccessor> UserContextAccessMock { get; } = new();

        public Mock<IManager<Guid, AccountEntity>> AccountManagerMock { get; } = new();

        public Mock<IManager<Guid, BillingConfigurationEntity>> BillingManagerMock { get; } = new();

        public Mock<IManager<Guid, EDIFieldDefinitionEntity>> EDIFIeldsManagerMock { get; } = new();

        public Mock<IManager<Guid, InvoiceConfigurationEntity>> InvoiceConfigurationManagerMock { get; } = new();

        public Mock<IAccountAttachmentsBlobStorage> AccountAttachmentsBlobStorage { get; } = new();

        public Mock<IEmailTemplateSender> EmailTemplateSenderMock { get; } = new();
    }
}
