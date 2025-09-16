using System;
using System.Net;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.NewAccount;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.TruckTicketing.Api.Configuration;
using SE.TruckTicketing.Api.Functions;
using SE.TruckTicketing.Contracts.Api.Models;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;

using Trident.Logging;
using Trident.Mapper;
using Trident.Testing.TestScopes;
using Trident.Validation;

using AccountTypes = SE.Shared.Common.Lookups.AccountTypes;

namespace SE.TruckTicketing.Api.Tests.Functions;

[TestClass]
public class NewAccountFunctionsTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task NewAccountFunctions_CreateNewAccount_ShouldReturnSuccess_ForValidRequest()
    {
        // arrange
        var scope = new DefaultScope();
        var createNewAccountRequest = scope.NewAccountCreationRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createNewAccountRequest));
        scope.NewAccountManagerMock.Setup(manager => manager.CreateNewAccount(It.IsAny<NewAccountModel>()));
        // act
        var httpResponseData = await scope.InstanceUnderTest.CreateNewAccount(functionsRequest);

        // assert
        scope.NewAccountManagerMock.Verify(manager => manager.CreateNewAccount(It.IsAny<NewAccountModel>()));
        Assert.AreEqual(HttpStatusCode.OK, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NewAccountFunctions_CreateNewAccount_ShouldReturnBadRequest_ValidationRollupException()
    {
        // arrange
        var scope = new DefaultScope();
        var createNewAccountRequest = scope.NewAccountCreationRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createNewAccountRequest));
        scope.NewAccountManagerMock.Setup(manager => manager.CreateNewAccount(It.IsAny<NewAccountModel>()))
             .ThrowsAsync(new ValidationRollupException());

        // act
        var httpResponseData = await scope.InstanceUnderTest.CreateNewAccount(functionsRequest);
        // assert
        scope.LogMock.Verify(log => log.Error(null, null, It.IsAny<string>()));
        Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NewAccountFunctions_CreateNewAccount_ShouldReturnBadRequest_ArgumentException()
    {
        // arrange
        var scope = new DefaultScope();
        var createNewAccountRequest = scope.NewAccountCreationRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createNewAccountRequest));
        scope.NewAccountManagerMock.Setup(manager => manager.CreateNewAccount(It.IsAny<NewAccountModel>()))
             .ThrowsAsync(new ArgumentException());

        // act
        var httpResponseData = await scope.InstanceUnderTest.CreateNewAccount(functionsRequest);
        // assert
        scope.LogMock.Verify(log => log.Error(null, It.IsAny<ArgumentException>(), It.IsAny<string>()));
        Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NewAccountFunctions_CreateNewAccount_ShouldReturnInternalServerError_UnhandledException()
    {
        // arrange
        var scope = new DefaultScope();
        var createNewAccountRequest = scope.NewAccountCreationRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createNewAccountRequest));
        scope.NewAccountManagerMock.Setup(manager => manager.CreateNewAccount(It.IsAny<NewAccountModel>()))
             .ThrowsAsync(new());

        // act
        var httpResponseData = await scope.InstanceUnderTest.CreateNewAccount(functionsRequest);
        // assert
        scope.LogMock.Verify(log => log.Error(null, It.IsAny<Exception>(), It.IsAny<string>()));
        Assert.AreEqual(HttpStatusCode.InternalServerError, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NewAccountFunctions_NewAccountWorkflowValidation_ShouldReturnSuccess_ForValidRequest()
    {
        // arrange
        var scope = new DefaultScope();
        var createAccountRequest = scope.NewAccountRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createAccountRequest));
        scope.NewAccountWorkflowManagerMock.Setup(manager => manager.RunAccountWorkflowValidation(It.IsAny<AccountEntity>()));

        // act
        var httpResponseData = await scope.InstanceUnderTest.NewAccountWorkflowValidation(functionsRequest);

        // assert
        scope.NewAccountWorkflowManagerMock.Verify(manager => manager.RunAccountWorkflowValidation(It.IsAny<AccountEntity>()));
        Assert.AreEqual(HttpStatusCode.OK, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NewAccountFunctions_NewAccountWorkflowValidation_ShouldReturnBadRequest_ValidationRollupException()
    {
        // arrange
        var scope = new DefaultScope();
        var createAccountRequest = scope.NewAccountRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createAccountRequest));
        scope.NewAccountWorkflowManagerMock.Setup(manager => manager.RunAccountWorkflowValidation(It.IsAny<AccountEntity>()))
             .ThrowsAsync(new ValidationRollupException());

        // act
        var httpResponseData = await scope.InstanceUnderTest.NewAccountWorkflowValidation(functionsRequest);
        // assert
        scope.LogMock.Verify(log => log.Error(null, null, It.IsAny<string>()));
        Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NewAccountFunctions_NewAccountWorkflowValidation_ShouldReturnBadRequest_ArgumentException()
    {
        // arrange
        var scope = new DefaultScope();
        var createAccountRequest = scope.NewAccountRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createAccountRequest));
        scope.NewAccountWorkflowManagerMock.Setup(manager => manager.RunAccountWorkflowValidation(It.IsAny<AccountEntity>()))
             .ThrowsAsync(new ArgumentException());

        // act
        var httpResponseData = await scope.InstanceUnderTest.NewAccountWorkflowValidation(functionsRequest);
        // assert
        scope.LogMock.Verify(log => log.Error(null, It.IsAny<ArgumentException>(), It.IsAny<string>()));
        Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NewAccountFunctions_NewAccountWorkflowValidation_ShouldReturnInternalServerError_UnhandledException()
    {
        // arrange
        var scope = new DefaultScope();
        var createAccountRequest = scope.NewAccountRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createAccountRequest));
        scope.NewAccountWorkflowManagerMock.Setup(manager => manager.RunAccountWorkflowValidation(It.IsAny<AccountEntity>()))
             .ThrowsAsync(new());

        // act
        var httpResponseData = await scope.InstanceUnderTest.NewAccountWorkflowValidation(functionsRequest);
        // assert
        scope.LogMock.Verify(log => log.Error(null, It.IsAny<Exception>(), It.IsAny<string>()));
        Assert.AreEqual(HttpStatusCode.InternalServerError, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NewAccountFunctions_NewAccountSourceLocationWorkflowValidation_ShouldReturnSuccess_ForValidRequest()
    {
        // arrange
        var scope = new DefaultScope();
        var createSourceLocationRequest = scope.NewSourceLocationRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createSourceLocationRequest));
        scope.NewAccountSourceLocationWorkflowManagerMock.Setup(manager => manager.RunSourceLocationWorkflowValidation(It.IsAny<SourceLocationEntity>()));

        // act
        var httpResponseData = await scope.InstanceUnderTest.NewAccountSourceLocationWorkflowValidation(functionsRequest);

        // assert
        scope.NewAccountSourceLocationWorkflowManagerMock.Verify(manager => manager.RunSourceLocationWorkflowValidation(It.IsAny<SourceLocationEntity>()));
        Assert.AreEqual(HttpStatusCode.OK, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NewAccountFunctions_NewAccountSourceLocationWorkflowValidation_ShouldReturnBadRequest_ValidationRollupException()
    {
        // arrange
        var scope = new DefaultScope();
        var createSourceLocationRequest = scope.NewSourceLocationRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createSourceLocationRequest));
        scope.NewAccountSourceLocationWorkflowManagerMock.Setup(manager => manager.RunSourceLocationWorkflowValidation(It.IsAny<SourceLocationEntity>()))
             .ThrowsAsync(new ValidationRollupException());

        // act
        var httpResponseData = await scope.InstanceUnderTest.NewAccountSourceLocationWorkflowValidation(functionsRequest);
        // assert
        scope.LogMock.Verify(log => log.Error(null, null, It.IsAny<string>()));
        Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NewAccountFunctions_NewAccountSourceLocationWorkflowValidation_ShouldReturnBadRequest_ArgumentException()
    {
        // arrange
        var scope = new DefaultScope();
        var createSourceLocationRequest = scope.NewSourceLocationRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createSourceLocationRequest));
        scope.NewAccountSourceLocationWorkflowManagerMock.Setup(manager => manager.RunSourceLocationWorkflowValidation(It.IsAny<SourceLocationEntity>()))
             .ThrowsAsync(new ArgumentException());

        // act
        var httpResponseData = await scope.InstanceUnderTest.NewAccountSourceLocationWorkflowValidation(functionsRequest);
        // assert
        scope.LogMock.Verify(log => log.Error(null, It.IsAny<ArgumentException>(), It.IsAny<string>()));
        Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NewAccountFunctions_NewAccountSourceLocationWorkflowValidation_ShouldReturnInternalServerError_UnhandledException()
    {
        // arrange
        var scope = new DefaultScope();
        var createSourceLocationRequest = scope.NewSourceLocationRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createSourceLocationRequest));
        scope.NewAccountSourceLocationWorkflowManagerMock.Setup(manager => manager.RunSourceLocationWorkflowValidation(It.IsAny<SourceLocationEntity>()))
             .ThrowsAsync(new());

        // act
        var httpResponseData = await scope.InstanceUnderTest.NewAccountSourceLocationWorkflowValidation(functionsRequest);
        // assert
        scope.LogMock.Verify(log => log.Error(null, It.IsAny<Exception>(), It.IsAny<string>()));
        Assert.AreEqual(HttpStatusCode.InternalServerError, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task NewAccountFunctions_GetAccountAttachmentDownloadUri_ShouldReturnExpectedUri()
    {
        // arrange
        var scope = new DefaultScope();
        var requestData = scope.CreateHttpRequest(default, default);

        // act
        var responseData = await scope.InstanceUnderTest.GetAccountAttachmentDownloadUri(requestData, Guid.NewGuid(), Guid.NewGuid());

        // assert
        var response = responseData.ReadJsonToObject<UriDto>();
        response.Result.Uri.Should().Be("https://example.org/");
    }

    private class DefaultScope : HttpTestScope<NewAccountFunctions>
    {
        public DefaultScope()
        {
            MapperRegistry = new ServiceMapperRegistry(new[] { new ApiMapperProfile() });
            InstanceUnderTest = new(LogMock.Object,
                                    MapperRegistry,
                                    NewAccountManagerMock.Object,
                                    NewAccountWorkflowManagerMock.Object,
                                    NewAccountSourceLocationWorkflowManagerMock.Object);

            NewAccountManagerMock.Setup(m => m.GetAttachmentDownloadUri(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync("https://example.org/");
        }

        public Mock<ILog> LogMock { get; } = new();

        public IMapperRegistry MapperRegistry { get; }

        public Mock<INewAccountManager> NewAccountManagerMock { get; } = new();

        public Mock<INewAccountWorkflowManager> NewAccountWorkflowManagerMock { get; } = new();

        public Mock<INewAccountSourceLocationWorkflowManager> NewAccountSourceLocationWorkflowManagerMock { get; } = new();

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

        public Account NewAccountRequest()
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

        public SourceLocation NewSourceLocationRequest()
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
    }
}
