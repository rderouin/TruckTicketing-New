using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using AutoMapper;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain;
using SE.Shared.Domain.EmailTemplates;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.EDIFieldDefinition;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.NewAccount;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Accounts;

using Trident.Contracts;
using Trident.Extensions;
using Trident.Mapper;
using Trident.Testing.TestScopes;

using AccountTypes = SE.Shared.Common.Lookups.AccountTypes;

namespace SE.TruckTicketing.Domain.Tests.Account;

[TestClass]
public class NewAccountManagerTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task AccountManager_CreateNewAccount_AccountType_ThirdPartyAnalytical_AccountSaveWithoutDeferCommit()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetAccountManager();
        var thirdPartyAnalyticalAccountRequest = scope.NewAccountCreationRequest().Clone();
        thirdPartyAnalyticalAccountRequest.Account = scope.NewAccountRequest();
        thirdPartyAnalyticalAccountRequest.Account.Id = Guid.NewGuid();
        thirdPartyAnalyticalAccountRequest.Account.AccountTypes = new() { AccountTypes.ThirdPartyAnalytical.ToString() };
        // act
        await scope.InstanceUnderTest.CreateNewAccount(thirdPartyAnalyticalAccountRequest);

        //// assert
        scope.AccountManagerMock.Verify(num => num.Save(It.IsAny<AccountEntity>(), false));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task AccountManager_CreateNewAccount_AccountType_TruckingCompany_AccountSaveWithoutDeferCommit()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetAccountManager();
        var truckingCompanyAccountRequest = scope.NewAccountCreationRequest().Clone();
        truckingCompanyAccountRequest.Account = scope.NewAccountRequest();
        truckingCompanyAccountRequest.Account.Id = Guid.NewGuid();
        truckingCompanyAccountRequest.Account.AccountTypes = new() { AccountTypes.TruckingCompany.ToString() };
        // act
        await scope.InstanceUnderTest.CreateNewAccount(truckingCompanyAccountRequest);

        //// assert
        scope.AccountManagerMock.Verify(num => num.Save(It.IsAny<AccountEntity>(), false));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task AccountManager_CreateNewAccount_AccountType_Customer_AccountSaveWithDeferCommit()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetAccountManager();
        var customerAccountRequest = scope.NewAccountCreationRequest().Clone();
        customerAccountRequest.Account = scope.NewAccountRequest();
        customerAccountRequest.Account.Id = Guid.NewGuid();
        customerAccountRequest.Account.AccountTypes = new() { AccountTypes.Customer.ToString() };
        // act
        await scope.InstanceUnderTest.CreateNewAccount(customerAccountRequest);

        //// assert
        scope.AccountManagerMock.Verify(num => num.Save(It.IsAny<AccountEntity>(), true));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task AccountManager_CreateNewAccount_AccountType_Generator_AccountSaveWithDeferCommit()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetAccountManager();
        var generatorAccountRequest = scope.NewAccountCreationRequest().Clone();
        generatorAccountRequest.Account = scope.NewAccountRequest();
        generatorAccountRequest.Account.Id = Guid.NewGuid();
        generatorAccountRequest.Account.AccountTypes = new() { AccountTypes.Generator.ToString() };
        // act
        await scope.InstanceUnderTest.CreateNewAccount(generatorAccountRequest);

        //// assert
        scope.AccountManagerMock.Verify(num => num.Save(It.IsAny<AccountEntity>(), true));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task AccountManager_CreateNewAccount_BillingCustomerExists_SaveWithDeferCommit()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetAccountManager();
        var accountRequestWithBillingCustomer = scope.NewAccountCreationRequest().Clone();
        accountRequestWithBillingCustomer.Account = scope.NewAccountRequest();
        accountRequestWithBillingCustomer.BillingCustomer = scope.NewBillingCustomerRequest();

        accountRequestWithBillingCustomer.Account.Id = Guid.NewGuid();
        accountRequestWithBillingCustomer.Account.AccountTypes = new() { AccountTypes.Generator.ToString() };

        accountRequestWithBillingCustomer.BillingCustomer.Id = Guid.NewGuid();

        // act
        await scope.InstanceUnderTest.CreateNewAccount(accountRequestWithBillingCustomer);

        //// assert
        scope.AccountManagerMock.Verify(num => num.Save(It.IsAny<AccountEntity>(), true));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task AccountManager_CreateNewAccount_SaveEDIFieldDefinition_SaveWithDeferCommit()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetAccountManager();
        var accountRequestWithEDIFieldDefinition = scope.NewAccountCreationRequest().Clone();
        accountRequestWithEDIFieldDefinition.Account = scope.NewAccountRequest();
        accountRequestWithEDIFieldDefinition.BillingCustomer = scope.NewBillingCustomerRequest();

        accountRequestWithEDIFieldDefinition.Account.Id = Guid.NewGuid();
        accountRequestWithEDIFieldDefinition.Account.AccountTypes = new() { AccountTypes.Generator.ToString() };

        accountRequestWithEDIFieldDefinition.BillingCustomer.Id = Guid.NewGuid();

        accountRequestWithEDIFieldDefinition.BillingConfiguration = scope.NewBillingConfigurationRequest().Clone();
        accountRequestWithEDIFieldDefinition.BillingConfiguration.Id = Guid.NewGuid();

        accountRequestWithEDIFieldDefinition.GeneratorSourceLocations = new() { scope.NewSourceLocationRequest().Clone() };

        accountRequestWithEDIFieldDefinition.GeneratorSourceLocations[0].GeneratorId = accountRequestWithEDIFieldDefinition.Account.Id;
        accountRequestWithEDIFieldDefinition.GeneratorSourceLocations[0].Id = Guid.NewGuid();

        accountRequestWithEDIFieldDefinition.EDIFieldDefinitions = scope.NewEdiFieldDefinitionRequest().Clone();
        // act
        await scope.InstanceUnderTest.CreateNewAccount(accountRequestWithEDIFieldDefinition);

        //// assert
        scope.EDIFieldDefinitionManagerMock.Verify(num =>
                                                       num.Save(It.IsAny<EDIFieldDefinitionEntity>(), true));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task AccountManager_CreateNewAccount_SaveInvoiceConfiguration_DeferCommitTrue()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetAccountManager();
        var accountRequestWithBillingConfiguration = scope.NewAccountCreationRequest().Clone();
        accountRequestWithBillingConfiguration.Account = scope.NewAccountRequest();
        accountRequestWithBillingConfiguration.BillingCustomer = scope.NewBillingCustomerRequest();

        accountRequestWithBillingConfiguration.Account.Id = Guid.NewGuid();
        accountRequestWithBillingConfiguration.Account.AccountTypes = new() { AccountTypes.Generator.ToString() };

        accountRequestWithBillingConfiguration.BillingCustomer.Id = Guid.NewGuid();

        accountRequestWithBillingConfiguration.BillingConfiguration = scope.NewBillingConfigurationRequest().Clone();
        accountRequestWithBillingConfiguration.BillingConfiguration.Id = Guid.NewGuid();

        // act
        await scope.InstanceUnderTest.CreateNewAccount(accountRequestWithBillingConfiguration);

        //// assert
        scope.InvoiceConfigurationManagerMock.Verify(num => num.Save(It.IsAny<InvoiceConfigurationEntity>(), true));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task AccountManager_CreateNewAccount_SaveBillingConfiguration_DeferCommitFalse()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetAccountManager();
        var accountRequestWithBillingConfiguration = scope.NewAccountCreationRequest().Clone();
        accountRequestWithBillingConfiguration.Account = scope.NewAccountRequest();
        accountRequestWithBillingConfiguration.BillingCustomer = scope.NewBillingCustomerRequest();

        accountRequestWithBillingConfiguration.Account.Id = Guid.NewGuid();
        accountRequestWithBillingConfiguration.Account.AccountTypes = new() { AccountTypes.Generator.ToString() };

        accountRequestWithBillingConfiguration.BillingCustomer.Id = Guid.NewGuid();

        accountRequestWithBillingConfiguration.BillingConfiguration = scope.NewBillingConfigurationRequest().Clone();
        accountRequestWithBillingConfiguration.BillingConfiguration.Id = Guid.NewGuid();

        // act
        await scope.InstanceUnderTest.CreateNewAccount(accountRequestWithBillingConfiguration);

        //// assert
        scope.BillingConfigurationManagerMock.Verify(num => num.Save(It.IsAny<BillingConfigurationEntity>(), false));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task AccountManager_GetAttachmentDownloadUri()
    {
        // arrange
        var scope = new DefaultScope();
        var accountId = scope.TestAccountId;
        var attachmentId = scope.TestAttachmentId;

        // act
        var uri = await scope.InstanceUnderTest.GetAttachmentDownloadUri(accountId, attachmentId);

        // assert
        uri.Should().Be(scope.BlobUri);
        scope.AccountAttachmentsBlobStorageMock.Verify(s => s.Exists(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private class DefaultScope : TestScope<NewAccountManager>
    {
        public DefaultScope()
        {
            MapperRegistry = new ServiceMapperRegistry(new List<Profile>
            {
                new ApiMapperProfile(),
                new Api.Configuration.ApiMapperProfile(),
            });

            InstanceUnderTest = new(AccountManagerMock.Object,
                                    BillingConfigurationManagerMock.Object,
                                    EDIFieldDefinitionManagerMock.Object,
                                    InvoiceConfigurationManagerMock.Object,
                                    MapperRegistry,
                                    AccountAttachmentsBlobStorageMock.Object,
                                    UserContextAccessorMock.Object,
                                    EmailTemplateSenderMock.Object);

            AccountManagerMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(TestAccount);
            AccountAttachmentsBlobStorageMock.Setup(x => x.GetDownloadUri(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(new Uri(BlobUri));
            AccountAttachmentsBlobStorageMock.Setup(x => x.Exists(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        }

        public IMapperRegistry MapperRegistry { get; }

        public Mock<IManager<Guid, AccountEntity>> AccountManagerMock { get; } = new();

        public Mock<IUserContextAccessor> UserContextAccessorMock { get; } = new();

        public Mock<IManager<Guid, BillingConfigurationEntity>> BillingConfigurationManagerMock { get; } = new();

        public Mock<IManager<Guid, EDIFieldDefinitionEntity>> EDIFieldDefinitionManagerMock { get; } = new();

        public Mock<IManager<Guid, SourceLocationEntity>> SourceLocationManagerMock { get; } = new();

        public Mock<IManager<Guid, InvoiceConfigurationEntity>> InvoiceConfigurationManagerMock { get; } = new();

        public Mock<IAccountAttachmentsBlobStorage> AccountAttachmentsBlobStorageMock { get; } = new();

        public Mock<IEmailTemplateSender> EmailTemplateSenderMock { get; } = new();

        public string BlobUri => "https://storage/container/folder/test.pdf";

        public Guid TestAccountId => Guid.Parse("a0667c19-bcf1-4e49-834c-917e6b9cd71b");

        public Guid TestAttachmentId => Guid.Parse("eaf35de1-8a45-41ba-b3f4-92c95431b84c");

        public AccountEntity TestAccount =>
            new()
            {
                Id = TestAccountId,
                Attachments = new()
                {
                    new()
                    {
                        Id = TestAttachmentId,
                        ContainerName = "attachment",
                        FileName = "test.pdf",
                        Blob = "5e4d2936-9003-4c9e-b446-baa89edb171a/test.pdf",
                    },
                },
            };

        public NewAccountModel NewAccountCreationRequest()
        {
            return new()
            {
                Account = new(),
                BillingCustomer = new(),
                BillingConfiguration = new(),
                GeneratorSourceLocations = new(),
                EDIFieldDefinitions = new(),
            };
        }

        public Contracts.Models.Operations.Account NewAccountRequest()
        {
            return new()
            {
                Id = Guid.NewGuid(),
                AccountNumber = "59260670",
                LegalEntityId = Guid.NewGuid(),
                LegalEntity = "Canada",
                AccountStatus = AccountStatus.Open,
                BillingTransferRecipientId = null,
                BillingTransferRecipientName = null,
                BillingType = BillingType.CreditCard,
                CreditLimit = 5000,
                CreditStatus = CreditStatus.Approved,
                CustomerNumber = "33279271",
                EnableNewThirdPartyAnalytical = true,
                EnableNewTruckingCompany = true,
                Name = "Hayes - Koss",
                NickName = "Kuhlman Inc",
                WatchListStatus = WatchListStatus.Red,
                HasPriceBook = true,
                IsEdiFieldsEnabled = true,
                IsElectronicBillingEnabled = true,
                MailingRecipientName = "Adam Stain",
                AccountAddresses = new()
                {
                    new()
                    {
                        IsPrimaryAddress = true,
                        City = "Moon",
                        Street = "1210 Landing Ln.",
                        Country = CountryCode.US,
                        Province = StateProvince.PA,
                        ZipCode = "15108",
                    },
                },
                AccountTypes = new()
                {
                    AccountTypes.Generator.ToString(),
                },
                Contacts = new()
                {
                    new()
                    {
                        IsPrimaryAccountContact = true,
                        Email = "Salvador.Fahey0@yahoo.com",
                        PhoneNumber = "271-668-1312",
                        Name = "Jonathan",
                        LastName = "Collins",
                    },
                    new()
                    {
                        IsPrimaryAccountContact = false,
                        Email = "Nelle.Legros@hotmail.com",
                        PhoneNumber = "213-632-4251",
                        Name = "Ted Abshire II",
                        LastName = "Collins",
                    },
                },
            };
        }

        public Contracts.Models.Operations.Account NewBillingCustomerRequest()
        {
            return new()
            {
                Id = Guid.NewGuid(),
                AccountNumber = "59260670",
                LegalEntityId = Guid.NewGuid(),
                LegalEntity = "Canada",
                AccountStatus = AccountStatus.Open,
                BillingTransferRecipientId = null,
                BillingTransferRecipientName = null,
                BillingType = BillingType.CreditCard,
                CreditLimit = 5000,
                CreditStatus = CreditStatus.Approved,
                CustomerNumber = "33279271",
                EnableNewThirdPartyAnalytical = true,
                EnableNewTruckingCompany = true,
                Name = "Hayes - Koss",
                NickName = "Kuhlman Inc",
                WatchListStatus = WatchListStatus.Red,
                HasPriceBook = true,
                IsEdiFieldsEnabled = true,
                IsElectronicBillingEnabled = true,
                MailingRecipientName = "Adam Stain",
                AccountAddresses = new()
                {
                    new()
                    {
                        IsPrimaryAddress = true,
                        City = "Moon",
                        Street = "1210 Landing Ln.",
                        Country = CountryCode.US,
                        Province = StateProvince.PA,
                        ZipCode = "15108",
                    },
                },
                AccountTypes = new()
                {
                    AccountTypes.Customer.ToString(),
                },
                Contacts = new()
                {
                    new()
                    {
                        IsPrimaryAccountContact = true,
                        Email = "Salvador.Fahey0@yahoo.com",
                        PhoneNumber = "271-668-1312",
                        Name = "Jonathan",
                        LastName = "Collins",
                        IsActive = true,
                        ContactFunctions = new()
                        {
                            AccountContactFunctions.General.ToString(),
                        },
                        AccountContactAddress = new()
                        {
                            City = "Moon",
                            Street = "1210 Landing Ln.",
                            Country = CountryCode.US,
                            Province = StateProvince.PA,
                            ZipCode = "15108",
                        },
                    },
                    new()
                    {
                        IsPrimaryAccountContact = false,
                        Email = "Nelle.Legros@hotmail.com",
                        PhoneNumber = "213-632-4251",
                        Name = "Ted Abshire II",
                        LastName = "Collins",
                        IsActive = true,
                        ContactFunctions = new()
                        {
                            AccountContactFunctions.GeneratorRepresentative.ToString(),
                        },
                        AccountContactAddress = new()
                        {
                            City = "Moon",
                            Street = "1210 Landing Ln.",
                            Country = CountryCode.US,
                            Province = StateProvince.PA,
                            ZipCode = "15108",
                        },
                    },
                },
            };
        }

        public Contracts.Models.SourceLocations.SourceLocation NewSourceLocationRequest()
        {
            return new()
            {
                SourceLocationName = "UWI NTS",
                SourceLocationTypeCategory = SourceLocationTypeCategory.Well,
                CountryCode = CountryCode.CA,
                FormattedIdentifier = "###/@-###-@/###-@-##/##",
                DownHoleType = DownHoleType.Pit,
                DeliveryMethod = DeliveryMethod.Pipeline,
                GeneratorId = Guid.NewGuid(),
                GeneratorName = "Andrew Atkin",
                WellFileNumber = "5589777",
            };
        }

        public List<Contracts.Models.Operations.EDIFieldDefinition> NewEdiFieldDefinitionRequest()
        {
            return new()
            {
                new()
                {
                    Id = Guid.Parse("b6532cb5-6693-4e1a-b651-a6c19b1f426e"),
                    DefaultValue = $"{nameof(DefaultValue)}",
                    EDIFieldLookupId = Guid.NewGuid(),
                    EDIFieldName = "Policy Name",
                    IsPrinted = true,
                    IsRequired = true,
                    ValidationRequired = true,
                    ValidationErrorMessage = "only alphabets allowed",
                    ValidationPattern = "[a-zA-Z]+",
                },
                new()
                {
                    Id = Guid.Parse("a5a02b0b-6dfd-45cc-99c5-4af8f2ea0a25"),
                    DefaultValue = $"{nameof(DefaultValue)}",
                    EDIFieldLookupId = Guid.NewGuid(),
                    EDIFieldName = "Invoice Number",
                    IsPrinted = true,
                    IsRequired = true,
                    ValidationRequired = true,
                    ValidationErrorMessage = "only numbers allowed",
                    ValidationPattern = "[0-9]+",
                },
                new()
                {
                    Id = Guid.Parse("6f9bcf08-fcc8-41ac-ae4a-4095f9fb50b7"),
                    DefaultValue = $"{nameof(DefaultValue)}",
                    EDIFieldLookupId = Guid.NewGuid(),
                    EDIFieldName = "Invoice Number",
                    IsPrinted = true,
                    IsRequired = true,
                    ValidationRequired = true,
                    ValidationErrorMessage = "only numbers allowed",
                    ValidationPattern = "[0-9]+",
                },
                new()
                {
                    Id = Guid.Parse("e6253340-d62e-485e-980d-fa2429ce1edb"),
                    DefaultValue = $"{nameof(DefaultValue)}",
                    EDIFieldLookupId = Guid.NewGuid(),
                    EDIFieldName = "Port Number",
                    IsPrinted = true,
                    IsRequired = false,
                    ValidationRequired = true,
                    ValidationErrorMessage = "Pattern not valid for Port Number",
                    ValidationPattern = @"^[A-Z0-9]\d{2}[A-Z0-9](-\d{3}){2}[A-Z0-9]$",
                },
            };
        }

        public Contracts.Models.Operations.BillingConfiguration NewBillingConfigurationRequest()
        {
            return new()
            {
                BillingConfigurationEnabled = true,
                BillingContactAddress = "123 Maple Ridge",
                BillingContactId = Guid.NewGuid(),
                BillingContactName = "Er. Steve Martin",
                BillingCustomerAccountId = Guid.Parse("d4368508-d884-4aa3-9083-ab020f569a1e"),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = string.Empty,
                CreatedById = Guid.NewGuid().ToString(),
                CustomerGeneratorId = Guid.NewGuid(),
                CustomerGeneratorName = "Kuvalis, Herman and Langworth",
                Description = null,
                EmailDeliveryEnabled = true,
                FieldTicketsUploadEnabled = false,
                StartDate = DateTimeOffset.Now,
                GeneratorRepresentativeId = Guid.NewGuid(),
                IncludeExternalAttachmentInLC = true,
                IncludeInternalAttachmentInLC = true,
                IsDefaultConfiguration = true,
                LastComment = "This is a sample comment",
                LoadConfirmationsEnabled = true,
                LoadConfirmationFrequency = null,
                RigNumber = null,
                ThirdPartyBillingContactAddress = "345 Altosa Drive",
                ThirdPartyBillingContactId = Guid.NewGuid(),
                ThirdPartyBillingContactName = "Third Party Contact",
                ThirdPartyCompanyId = Guid.NewGuid(),
                ThirdPartyCompanyName = null,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "Manish M",
                UpdatedById = Guid.NewGuid().ToString(),
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
                        EmailAddress = "SimpleMail@gmail.com",
                        IsAuthorized = true,
                        SignatoryContact = "Singatory Person Name",
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
                        SourceLocationValueState = MatchPredicateValueState.NotSet,
                        SubstanceId = null,
                        SubstanceName = null,
                        SubstanceValueState = MatchPredicateValueState.NotSet,
                        WellClassification = WellClassifications.Drilling,
                        WellClassificationState = MatchPredicateValueState.NotSet,
                    },
                },
            };
        }

        public AccountEntity CreatedAccountEntity()
        {
            return new()
            {
                Id = Guid.NewGuid(),
                AccountNumber = "59260670",
                LegalEntityId = Guid.NewGuid(),
                LegalEntity = "Canada",
                AccountPrimaryContactEmail = "Teresa_Daugherty63@yahoo.com",
                AccountPrimaryContactName = "Bradford Lang",
                AccountPrimaryContactPhoneNumber = "534-732-2170",
                AccountStatus = AccountStatus.Open,
                BillingTransferRecipientId = null,
                BillingTransferRecipientName = null,
                BillingType = BillingType.CreditCard,
                CreditLimit = 5000,
                CreditStatus = CreditStatus.Approved,
                CustomerNumber = "33279271",
                EnableNewThirdPartyAnalytical = true,
                EnableNewTruckingCompany = true,
                Name = "Hayes - Koss",
                NickName = "Kuhlman Inc",
                WatchListStatus = WatchListStatus.Red,
                HasPriceBook = true,
                IsEdiFieldsEnabled = true,
                IsElectronicBillingEnabled = true,
                MailingRecipientName = "Adam Stain",
                AccountAddresses = new()
                {
                    new()
                    {
                        IsPrimaryAddress = true,
                        City = "Moon",
                        Street = "1210 Landing Ln.",
                        Country = CountryCode.US,
                        Province = StateProvince.PA,
                        ZipCode = "15108",
                    },
                },
                AccountTypes = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        AccountTypes.Customer.ToString(),
                        AccountTypes.Generator.ToString(),
                    },
                },
                Contacts = new()
                {
                    new()
                    {
                        IsPrimaryAccountContact = true,
                        Email = "Salvador.Fahey0@yahoo.com",
                        PhoneNumber = "271-668-1312",
                        Name = "Jonathan",
                        LastName = "Collins",
                        IsActive = true,
                        ContactFunctions = new()
                        {
                            Key = Guid.NewGuid(),
                            List = new()
                            {
                                AccountContactFunctions.General.ToString(),
                            },
                        },
                        AccountContactAddress = new()
                        {
                            City = "Moon",
                            Street = "1210 Landing Ln.",
                            Country = CountryCode.US,
                            Province = StateProvince.PA,
                            ZipCode = "15108",
                        },
                    },
                    new()
                    {
                        IsPrimaryAccountContact = false,
                        Email = "Nelle.Legros@hotmail.com",
                        PhoneNumber = "213-632-4251",
                        Name = "Ted Abshire II",
                        LastName = "Collins",
                        IsActive = true,
                        ContactFunctions = new()
                        {
                            Key = Guid.NewGuid(),
                            List = new()
                            {
                                AccountContactFunctions.GeneratorRepresentative.ToString(),
                            },
                        },
                        AccountContactAddress = new()
                        {
                            City = "Moon",
                            Street = "1210 Landing Ln.",
                            Country = CountryCode.US,
                            Province = StateProvince.PA,
                            ZipCode = "15108",
                        },
                    },
                },
            };
        }

        public BillingConfigurationEntity CreateBillingConfiurationEntity()
        {
            return new()
            {
                BillingConfigurationEnabled = true,
                BillingContactAddress = "123 Maple Ridge",
                BillingContactId = Guid.NewGuid(),
                BillingContactName = "Er. Steve Martin",
                BillingCustomerAccountId = Guid.Parse("d4368508-d884-4aa3-9083-ab020f569a1e"),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = string.Empty,
                CreatedById = Guid.NewGuid().ToString(),
                CustomerGeneratorId = Guid.NewGuid(),
                CustomerGeneratorName = "Kuvalis, Herman and Langworth",
                Description = null,
                EmailDeliveryEnabled = true,
                FieldTicketsUploadEnabled = false,
                StartDate = DateTimeOffset.Now,
                GeneratorRepresentativeId = Guid.NewGuid(),
                IncludeExternalAttachmentInLC = true,
                IncludeInternalAttachmentInLC = true,
                IsDefaultConfiguration = true,
                LastComment = "This is a sample comment",
                LoadConfirmationsEnabled = true,
                LoadConfirmationFrequency = null,
                RigNumber = null,
                ThirdPartyBillingContactAddress = "345 Altosa Drive",
                ThirdPartyBillingContactId = Guid.NewGuid(),
                ThirdPartyBillingContactName = "Third Party Contact",
                ThirdPartyCompanyId = Guid.NewGuid(),
                ThirdPartyCompanyName = null,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "Manish M",
                UpdatedById = Guid.NewGuid().ToString(),
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
                        EmailAddress = "SimpleMail@gmail.com",
                        IsAuthorized = true,
                        SignatoryContact = "Singatory Person Name",
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
                        SourceLocationValueState = MatchPredicateValueState.NotSet,
                        SubstanceId = null,
                        SubstanceName = null,
                        SubstanceValueState = MatchPredicateValueState.NotSet,
                        WellClassification = WellClassifications.Drilling,
                        WellClassificationState = MatchPredicateValueState.NotSet,
                    },
                },
            };
        }

        public InvoiceConfigurationEntity CreateInvoiceConfigurationEntity()
        {
            return new()
            {
                Id = Guid.NewGuid(),
                BusinessUnitId = "AA-100050",
                AllFacilities = true,
                AllServiceTypes = true,
                AllSourceLocations = true,
                AllSubstances = true,
                AllWellClassifications = true,
                CatchAll = true,
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
                Facilities = null,
                ServiceTypes = null,
                SourceLocationIdentifier = null,
                SourceLocations = null,
                SplitEdiFieldDefinitions = null,
                SplittingCategories = null,
                WellClassifications = null,
                PermutationsHash = "5537093D2EA52B432228E072824E0CE0DE63986C44638429A85B29411AF43810",
                Permutations = new(),
                SubstancesName = null,
                ServiceTypesName = null,
                Substances = null,
                CreatedAt = new DateTime(2025, 1, 17, 18, 11, 30),
                UpdatedAt = new DateTime(2025, 1, 17, 18, 11, 30),
                CreatedBy = "Panth Shah",
                UpdatedBy = "Panth Shah",
                CreatedById = Guid.NewGuid().ToString(),
                UpdatedById = Guid.NewGuid().ToString(),
            };
        }

        public SourceLocationEntity CreateSourceLocationEntity()
        {
            return new()
            {
                SourceLocationName = "UWI NTS",
                SourceLocationTypeCategory = SourceLocationTypeCategory.Well,
                CountryCode = CountryCode.CA,
                FormattedIdentifier = "###/@-###-@/###-@-##/##",
                DownHoleType = DownHoleType.Pit,
                DeliveryMethod = DeliveryMethod.Pipeline,
                GeneratorId = Guid.NewGuid(),
                GeneratorName = "Andrew Atkin",
                WellFileNumber = "5589777",
            };
        }

        public EDIFieldDefinitionEntity CreateEDIFieldDefinitionEntity()
        {
            return new()
            {
                Id = Guid.Parse("b6532cb5-6693-4e1a-b651-a6c19b1f426e"),
                DefaultValue = $"{nameof(DefaultValue)}",
                EDIFieldLookupId = Guid.NewGuid().ToString(),
                EDIFieldName = "Policy Name",
                IsPrinted = true,
                IsRequired = true,
                ValidationRequired = true,
                ValidationErrorMessage = "only alphabets allowed",
                ValidationPattern = "[a-zA-Z]+",
            };
        }

        public void SetAccountManager()
        {
            ConfigureAccountManagerMock(AccountManagerMock);
            ConfigureBillingConfigurationManagerMock(BillingConfigurationManagerMock);
            ConfigureSourceLocationManagerMock(SourceLocationManagerMock);
            ConfigureEDIFieldDefinitionManagerMock(EDIFieldDefinitionManagerMock);
            ConfigureInvoiceConfigurationManagerMock(InvoiceConfigurationManagerMock);
        }

        private void ConfigureAccountManagerMock(Mock<IManager<Guid, AccountEntity>> mock)
        {
            mock.Setup(sequence => sequence.Save(It.IsAny<AccountEntity>(),
                                                 It.IsAny<bool>())).ReturnsAsync(CreatedAccountEntity());
        }

        private void ConfigureBillingConfigurationManagerMock(Mock<IManager<Guid, BillingConfigurationEntity>> mock)
        {
            mock.Setup(sequence => sequence.Save(It.IsAny<BillingConfigurationEntity>(),
                                                 It.IsAny<bool>())).ReturnsAsync(CreateBillingConfiurationEntity());
        }

        private void ConfigureSourceLocationManagerMock(Mock<IManager<Guid, SourceLocationEntity>> mock)
        {
            mock.Setup(sequence => sequence.Save(It.IsAny<SourceLocationEntity>(),
                                                 It.IsAny<bool>())).ReturnsAsync(CreateSourceLocationEntity());
        }

        private void ConfigureEDIFieldDefinitionManagerMock(Mock<IManager<Guid, EDIFieldDefinitionEntity>> mock)
        {
            mock.Setup(sequence => sequence.Save(It.IsAny<EDIFieldDefinitionEntity>(),
                                                 It.IsAny<bool>())).ReturnsAsync(CreateEDIFieldDefinitionEntity());
        }

        private void ConfigureInvoiceConfigurationManagerMock(Mock<IManager<Guid, InvoiceConfigurationEntity>> mock)
        {
            mock.Setup(sequence => sequence.Save(It.IsAny<InvoiceConfigurationEntity>(),
                                                 It.IsAny<bool>())).ReturnsAsync(CreateInvoiceConfigurationEntity());
        }
    }
}
