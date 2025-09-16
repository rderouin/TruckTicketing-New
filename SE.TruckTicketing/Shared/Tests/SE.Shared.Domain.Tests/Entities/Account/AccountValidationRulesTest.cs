using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Account.Rules;
using SE.Shared.Domain.LegalEntity;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Contracts.Configuration;
using Trident.Data.Contracts;
using Trident.Testing.TestScopes;
using Trident.Validation;

using AccountTypes = SE.Shared.Common.Lookups.AccountTypes;

namespace SE.Shared.Domain.Tests.Entities.Account;

[TestClass]
public class AccountValidationRulesTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_ForValidAccount()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
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
        var context = scope.CreateValidAccountContext();
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
           .ContainSingle(nameof(TTErrorCodes.Account_NameRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_WhenAccountTypesIsEmpty()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountTypes = new()
        {
            List = new(),
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
           .Contain(TTErrorCodes.Account_TypeRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_DuplicateAccountCheck_MatchingNameCustomerNumber_SameLegalEntity()
    {
        // arrange
        var scope = new DefaultScope();
        var account = GenFu.GenFu.New<AccountEntity>();
        var accountEntities = scope.GenericAccountProviderSetup();
        //Duplicate Setup
        account.LegalEntityId = accountEntities.First().LegalEntityId;
        account.Name = accountEntities.First().Name;
        account.CustomerNumber = accountEntities.First().CustomerNumber;
        account.AccountStatus = AccountStatus.Open;
        account.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new()
            {
                AccountTypes.Customer.ToString(),
                AccountTypes.Generator.ToString(),
            },
        };

        account.AccountAddresses = GenFu.GenFu.ListOf<AccountAddressEntity>(2);
        account.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(2);
        account.Contacts.ForEach(x =>
                                 {
                                     x.AccountContactAddress = GenFu.GenFu.New<ContactAddressEntity>();
                                 });

        var context = new BusinessContext<AccountEntity>(account);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_NameMustBeUniqueForOpenAccounts);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_DuplicateAccountCheck_MatchingName_NoCustomerNumber_SameLegalEntity()
    {
        // arrange
        var scope = new DefaultScope();
        var account = GenFu.GenFu.New<AccountEntity>();
        var accountEntities = scope.GenericAccountProviderSetup();
        //Duplicate Setup
        account.LegalEntityId = accountEntities.First().LegalEntityId;
        account.Name = accountEntities.First().Name;
        account.CustomerNumber = string.Empty;
        account.AccountStatus = AccountStatus.Open;
        account.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new()
            {
                AccountTypes.Customer.ToString(),
                AccountTypes.Generator.ToString(),
            },
        };

        account.AccountAddresses = GenFu.GenFu.ListOf<AccountAddressEntity>(2);
        account.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(2);
        account.Contacts.ForEach(x =>
                                 {
                                     x.AccountContactAddress = GenFu.GenFu.New<ContactAddressEntity>();
                                 });

        var context = new BusinessContext<AccountEntity>(account);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_NameMustBeUniqueForOpenAccounts);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_NoDuplicateAccount_WithSameNameCustomerNumber_SameLegalEntity()
    {
        // arrange
        var scope = new DefaultScope();
        var account = GenFu.GenFu.New<AccountEntity>();
        var accountEntities = scope.GenericAccountProviderSetup();
        //Duplicate Setup
        account.LegalEntityId = accountEntities.First().LegalEntityId;
        account.Name = "Generation Diamond Ltd.";
        account.CustomerNumber = "78907898";
        account.AccountStatus = AccountStatus.Open;
        account.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new()
            {
                AccountTypes.Customer.ToString(),
                AccountTypes.Generator.ToString(),
            },
        };

        account.AccountAddresses = GenFu.GenFu.ListOf<AccountAddressEntity>(2);
        account.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(2);
        account.Contacts.ForEach(x =>
                                 {
                                     x.AccountContactAddress = GenFu.GenFu.New<ContactAddressEntity>();
                                 });

        var context = new BusinessContext<AccountEntity>(account);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .NotContain(TTErrorCodes.Account_NameMustBeUniqueForOpenAccounts);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_DuplicateAccountCheck()
    {
        // arrange
        var scope = new DefaultScope();
        var account = GenFu.GenFu.New<AccountEntity>();
        var accountEntities = scope.GenericAccountProviderSetup();
        //Duplicate Setup
        account.LegalEntityId = accountEntities.First().LegalEntityId;
        account.Name = accountEntities.First().Name;
        account.CustomerNumber = accountEntities.First().CustomerNumber;
        account.AccountStatus = AccountStatus.Open;
        account.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new()
            {
                AccountTypes.Customer.ToString(),
                AccountTypes.Generator.ToString(),
            },
        };

        account.AccountAddresses = GenFu.GenFu.ListOf<AccountAddressEntity>(2);
        account.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(2);
        account.Contacts.ForEach(x =>
                                 {
                                     x.AccountContactAddress = GenFu.GenFu.New<ContactAddressEntity>();
                                 });

        var context = new BusinessContext<AccountEntity>(account);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_NameMustBeUniqueForOpenAccounts);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_NoPrimaryAddressAddedWithAccount()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountAddresses.First().IsPrimaryAddress = false;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.Account_Address_MinimumOnePrimaryAddressForAccount));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_NoPrimaryAccountContactNeeded_ForAccountWithTypeGenerator()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.ForEach(contact => contact.IsPrimaryAccountContact = false);
        context.Target.AccountTypes.List = new()
        {
            AccountTypes.Generator.ToString(),
        };

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_NoPrimaryAccountContactAddedForAccountWithTypeThirdPartyAnalyticalOrTruckingCompany()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.ForEach(contact => contact.IsPrimaryAccountContact = false);
        context.Target.AccountTypes.List = new()
        {
            AccountTypes.ThirdPartyAnalytical.ToString(),
        };

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_NoMinimumRequiredPrimaryContactsForAccountWithTypeThirdPartyAnalyticalOrTruckingCompany()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.ForEach(contact => contact.IsPrimaryAccountContact = false);
        context.Target.AccountTypes.List = new()
        {
            AccountTypes.ThirdPartyAnalytical.ToString(),
        };

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    //Account Primary Contacts
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_PrimaryContactNameRequired()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.First(x => x.IsPrimaryAccountContact).Name = string.Empty;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.Account_Contact_FirstNameRequiredForPrimaryContact));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_PrimaryContactLastNameRequired()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.First(x => x.IsPrimaryAccountContact).LastName = string.Empty;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.Account_Contact_LastNameRequiredForPrimaryContact));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_PrimaryContactEmailRequired()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.First(x => x.IsPrimaryAccountContact).Email = string.Empty;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_ContactEmailRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_PrimaryContactEmailInValidFormatRequired()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.First(x => x.IsPrimaryAccountContact).Email = "abc";
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.Account_Contact_ContactEmailInvalidFormat));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_PrimaryContactPhoneNumberRequired()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.First(x => x.IsPrimaryAccountContact).PhoneNumber = string.Empty;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_ContactPhoneNumberRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_PrimaryContactPhoneNumberInvalidFormat()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.First(x => x.IsPrimaryAccountContact).PhoneNumber = "abc";
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .NotContain(TTErrorCodes.Account_Contact_ContactPhoneNumberInvalidFormat);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ContactFunctionsRequired()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.FirstOrDefault(new AccountContactEntity()).ContactFunctions = new()
        {
            List = new(),
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
           .Contain(TTErrorCodes.Account_Contact_FunctionsRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_CustomerPrimaryContact_EnforceCustomerPrimaryContactConstraint_NoContactsAdded()
    {
        // arrange
        var scope = new DefaultScope();

        var validationResults = new List<ValidationResult>();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts = null;
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_AtleastOnePrimaryContact);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_CustomerPrimaryContact_No_EnforceCustomerPrimaryContactConstraint_NoContactsAdded()
    {
        // arrange
        var scope = new DefaultScope();

        var validationResults = new List<ValidationResult>();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts = null;
        context.ContextBag.TryAdd("IsLegalEntityEnforcePrimaryContactConstraint", false);
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .NotContain(TTErrorCodes.Account_Contact_AtleastOnePrimaryContact);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_CustomerPrimaryContact_EnforceCustomerPrimaryContactConstraint()
    {
        // arrange
        var scope = new DefaultScope();

        var validationResults = new List<ValidationResult>();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.ForEach(x => x.IsPrimaryAccountContact = false);
        context.ContextBag.TryAdd("IsLegalEntityEnforcePrimaryContactConstraint", true);
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_AtleastOnePrimaryContact);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_CustomerDeletedPrimaryContact_EnforceCustomerPrimaryContactConstraint()
    {
        // arrange
        var scope = new DefaultScope();

        var validationResults = new List<ValidationResult>();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.ForEach(x =>
                                        {
                                            x.IsPrimaryAccountContact = true;
                                            x.IsDeleted = true;
                                        });

        context.ContextBag.TryAdd("IsLegalEntityEnforcePrimaryContactConstraint", true);
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_AtleastOnePrimaryContact);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_CustomerPrimaryContact_EnforceCustomerPrimaryContactConstraint()
    {
        // arrange
        var scope = new DefaultScope();

        var validationResults = new List<ValidationResult>();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.ForEach(x =>
                                        {
                                            x.IsPrimaryAccountContact = true;
                                            x.IsDeleted = false;
                                        });

        context.ContextBag.TryAdd("IsLegalEntityEnforcePrimaryContactConstraint", true);
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .NotContain(TTErrorCodes.Account_Contact_AtleastOnePrimaryContact);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_CustomerDeletedPrimaryContact_Not_EnforceCustomerPrimaryContactConstraint()
    {
        // arrange
        var scope = new DefaultScope();

        var validationResults = new List<ValidationResult>();
        var context = scope.CreateValidAccountContext();
        context.ContextBag.TryAdd("IsLegalEntityEnforcePrimaryContactConstraint", false);

        context.Target.Contacts.ForEach(x =>
                                        {
                                            x.IsPrimaryAccountContact = true;
                                            x.IsDeleted = true;
                                        });

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .NotContain(TTErrorCodes.Account_Contact_AtleastOnePrimaryContact);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldPass_CustomerNoPrimaryContact_Not_EnforceCustomerPrimaryContactConstraint()
    {
        // arrange
        var scope = new DefaultScope();

        var validationResults = new List<ValidationResult>();
        var context = scope.CreateValidAccountContext();
        context.ContextBag.TryAdd("IsLegalEntityEnforcePrimaryContactConstraint", false);

        context.Target.Contacts.ForEach(x =>
                                        {
                                            x.IsPrimaryAccountContact = false;
                                            x.IsDeleted = false;
                                        });

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .NotContain(TTErrorCodes.Account_Contact_AtleastOnePrimaryContact);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(AccountTypes.TruckingCompany)]
    [DataRow(AccountTypes.ThirdPartyAnalytical)]
    [DataRow(AccountTypes.Generator)]
    public async Task Rule_ShouldPass_PrimaryContactConstraint_NonCustomerAccounts(AccountTypes accountType)
    {
        // arrange
        var scope = new DefaultScope();

        var validationResults = new List<ValidationResult>();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new() { accountType.ToString() },
        };

        context.Target.Contacts.ForEach(x => x.IsPrimaryAccountContact = false);
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .NotContain(TTErrorCodes.Account_Contact_AtleastOnePrimaryContact);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(AccountTypes.TruckingCompany)]
    [DataRow(AccountTypes.ThirdPartyAnalytical)]
    [DataRow(AccountTypes.Generator)]
    public async Task Rule_ShouldPass_PrimaryContactConstraint_NonCustomerAccounts_AllContactsDeleted(AccountTypes accountType)
    {
        // arrange
        var scope = new DefaultScope();

        var validationResults = new List<ValidationResult>();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new() { accountType.ToString() },
        };

        context.Target.Contacts.ForEach(x => x.IsDeleted = true);
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .NotContain(TTErrorCodes.Account_Contact_AtleastOnePrimaryContact);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ContactAddressUSPostalCodeValidFormat()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.FirstOrDefault(new AccountContactEntity()).AccountContactAddress = new()
        {
            Street = "111 Main St",
            City = "Moon",
            ZipCode = "L6R2P2",
            Country = CountryCode.US,
            Province = StateProvince.RI,
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
           .Contain(TTErrorCodes.Account_Contact_Address_PostalCodeValidFormat);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ContactAddressCanadaPostalCodeRequired()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.FirstOrDefault(new AccountContactEntity()).AccountContactAddress = new()
        {
            Street = "111 Main St",
            City = "Brampton",
            ZipCode = "15108",
            Country = CountryCode.CA,
            Province = StateProvince.ON,
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
           .Contain(TTErrorCodes.Account_Contact_Address_PostalCodeValidFormat);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(AccountTypes.TruckingCompany)]
    [DataRow(AccountTypes.ThirdPartyAnalytical)]
    [DataRow(AccountTypes.Generator)]
    [DataRow(AccountTypes.Customer)]
    public async Task Rule_ShouldFail_AddressPostalCodeInvalidFormat_ForCountryCodeUS(AccountTypes accountType)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new() { accountType.ToString() },
        };

        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).ZipCode = "L6R";
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).Country = CountryCode.US;

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Address_PostalCodeInvalidFormat);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(AccountTypes.TruckingCompany)]
    [DataRow(AccountTypes.ThirdPartyAnalytical)]
    [DataRow(AccountTypes.Generator)]
    [DataRow(AccountTypes.Customer)]
    public async Task Rule_ShouldFail_AddressPostalCodeInvalidFormat_ForCanada(AccountTypes accountType)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new() { accountType.ToString() },
        };

        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).ZipCode = "15105";
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).Country = CountryCode.CA;

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Address_PostalCodeInvalidFormat);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldBypassPhoneNumberConstraint_PrimaryContact()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.First().PhoneNumber = null;
        scope.ConfigureSettings(new()
        {
            BypassPrimaryContactPhoneNumberConstraint = true,
        });

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .NotContain(TTErrorCodes.Account_Contact_ContactPhoneNumberRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldEnforcePhoneNumberConstraint_PrimaryContact()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.First().PhoneNumber = null;
        scope.ConfigureSettings(new());

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_ContactPhoneNumberRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_CustomerPrimaryAddressConstraint_NoAddressesAdded_WithCustomer()
    {
        // arrange
        var scope = new DefaultScope();
        var account = GenFu.GenFu.New<AccountEntity>();
        account.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new()
            {
                AccountTypes.Customer.ToString(),
                AccountTypes.Generator.ToString(),
            },
        };

        account.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(2);

        var context = new BusinessContext<AccountEntity>(account);
        var validationResults = new List<ValidationResult>();
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Address_MinimumOnePrimaryAddressForAccount);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_CustomerPrimaryAddressConstraint_NoPrimaryAddress_WithCustomer()
    {
        // arrange
        var scope = new DefaultScope();
        var account = GenFu.GenFu.New<AccountEntity>();
        account.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new()
            {
                AccountTypes.Customer.ToString(),
                AccountTypes.Generator.ToString(),
            },
        };

        account.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(2);
        account.AccountAddresses = GenFu.GenFu.ListOf<AccountAddressEntity>(2);
        account.AccountAddresses.ForEach(x => x.IsPrimaryAddress = false);

        var context = new BusinessContext<AccountEntity>(account);
        var validationResults = new List<ValidationResult>();
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Address_AtleastOnePrimaryAddressRequiredForAccounts);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_CustomerPrimaryAddressConstraint_DeletedAddressMarkedPrimary_WithCustomer()
    {
        // arrange
        var scope = new DefaultScope();
        var account = GenFu.GenFu.New<AccountEntity>();
        account.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new()
            {
                AccountTypes.Customer.ToString(),
                AccountTypes.Generator.ToString(),
            },
        };

        account.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(2);
        account.AccountAddresses = GenFu.GenFu.ListOf<AccountAddressEntity>(2);
        account.AccountAddresses.ForEach(x =>
                                         {
                                             x.IsDeleted = false;
                                             x.IsPrimaryAddress = false;
                                         });

        account.AccountAddresses.First().IsDeleted = true;
        account.AccountAddresses.First().IsPrimaryAddress = true;
        var context = new BusinessContext<AccountEntity>(account);
        var validationResults = new List<ValidationResult>();
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Address_AtleastOnePrimaryAddressRequiredForAccounts);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_CustomerPrimaryAddressConstraint_AllAddressesDeleted_WithCustomer()
    {
        // arrange
        var scope = new DefaultScope();
        var account = GenFu.GenFu.New<AccountEntity>();
        account.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new()
            {
                AccountTypes.Customer.ToString(),
                AccountTypes.Generator.ToString(),
            },
        };

        account.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(2);
        account.AccountAddresses = GenFu.GenFu.ListOf<AccountAddressEntity>(2);
        account.AccountAddresses.ForEach(x =>
                                         {
                                             x.IsDeleted = true;
                                         });

        var context = new BusinessContext<AccountEntity>(account);
        var validationResults = new List<ValidationResult>();
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Address_MinimumOnePrimaryAddressForAccount);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(AccountTypes.Generator)]
    [DataRow(AccountTypes.TruckingCompany)]
    [DataRow(AccountTypes.ThirdPartyAnalytical)]
    public async Task Rule_ShouldPass_CustomerPrimaryAddressConstraint_AllAddressesDeleted_WithNoCustomer(AccountTypes accountType)
    {
        // arrange
        var scope = new DefaultScope();
        var account = GenFu.GenFu.New<AccountEntity>();
        account.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new()
            {
                accountType.ToString(),
            },
        };

        account.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(2);
        account.AccountAddresses = GenFu.GenFu.ListOf<AccountAddressEntity>(2);
        account.AccountAddresses.ForEach(x =>
                                         {
                                             x.IsDeleted = true;
                                         });

        var context = new BusinessContext<AccountEntity>(account);
        var validationResults = new List<ValidationResult>();
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .NotContain(TTErrorCodes.Account_Address_MinimumOnePrimaryAddressForAccount);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(AccountTypes.Generator)]
    [DataRow(AccountTypes.TruckingCompany)]
    [DataRow(AccountTypes.ThirdPartyAnalytical)]
    public async Task Rule_ShouldPass_CustomerPrimaryAddressConstraint_NoPrimaryAddress_WithNoCustomer(AccountTypes accountType)
    {
        // arrange
        var scope = new DefaultScope();
        var account = GenFu.GenFu.New<AccountEntity>();
        account.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new()
            {
                accountType.ToString(),
            },
        };

        account.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(2);
        account.AccountAddresses = GenFu.GenFu.ListOf<AccountAddressEntity>(2);
        account.AccountAddresses.ForEach(x =>
                                         {
                                             x.IsDeleted = false;
                                             x.IsPrimaryAddress = false;
                                         });

        var context = new BusinessContext<AccountEntity>(account);
        var validationResults = new List<ValidationResult>();
        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .NotContain(TTErrorCodes.Account_Address_AtleastOnePrimaryAddressRequiredForAccounts);
    }

    private class DefaultScope : TestScope<AccountValidationRules>
    {
        public readonly Mock<IProvider<Guid, AccountContactReferenceIndexEntity>> AccountContactReferenceIndexProviderMock = new();

        public readonly Mock<IProvider<Guid, AccountEntity>> AccountProviderMock = new();

        public readonly Mock<IAppSettings> AppSettingsMock = new();

        public readonly Mock<IProvider<Guid, LegalEntityEntity>> LegalEntityProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(AccountProviderMock.Object, AccountContactReferenceIndexProviderMock.Object, LegalEntityProviderMock.Object, AppSettingsMock.Object);
        }

        public AccountEntity ValidAccountEntity =>
            new()
            {
                Id = Guid.NewGuid(),
                AccountNumber = "59260670",
                LegalEntityId = Guid.NewGuid(),
                LegalEntity = "Canada",
                AccountStatus = AccountStatus.Open,
                BillingTransferRecipientId = null,
                BillingTransferRecipientName = null,
                BillingType = BillingType.CreditCard,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Panth Shah",
                CreatedById = Guid.NewGuid().ToString(),
                CreditLimit = 5000,
                CreditStatus = CreditStatus.Approved,
                CustomerNumber = "33279271",
                EnableNewThirdPartyAnalytical = true,
                EnableNewTruckingCompany = true,
                Name = "Hayes - Koss",
                NickName = "Kuhlman Inc",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "Panth Shah",
                UpdatedById = Guid.NewGuid().ToString(),
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
                                AccountContactFunctions.BillingContact.ToString(),
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
                },
            };

        public BusinessContext<AccountEntity> CreateValidAccountContext(AccountEntity original = null)
        {
            return new(ValidAccountEntity, original);
        }

        public List<AccountEntity> GenericAccountProviderSetup()
        {
            var accountEntities = GenFu.GenFu.ListOf<AccountEntity>(2);
            accountEntities.ForEach(x =>
                                    {
                                        x.AccountStatus = AccountStatus.Open;
                                        x.AccountTypes = new()
                                        {
                                            Key = Guid.NewGuid(),
                                            List = new()
                                            {
                                                AccountTypes.Customer.ToString(),
                                                AccountTypes.Generator.ToString(),
                                            },
                                        };

                                        x.AccountAddresses = GenFu.GenFu.ListOf<AccountAddressEntity>(2);
                                        x.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(2);
                                    });

            SetupAccountProvider(accountEntities);
            return accountEntities;
        }

        public void SetupAccountProvider(List<AccountEntity> accountEntities)
        {
            AccountProviderMock.Setup(x => x.Get(It.IsAny<Expression<Func<AccountEntity, bool>>>(),
                                                 It.IsAny<Func<IQueryable<AccountEntity>,
                                                     IOrderedQueryable<AccountEntity>>>(),
                                                 It.IsAny<List<string>>(),
                                                 It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                               .ReturnsAsync((Expression<Func<AccountEntity, bool>> filter,
                                              Func<IQueryable<AccountEntity>, IOrderedQueryable<AccountEntity>> _,
                                              List<string> __,
                                              bool ___,
                                              bool ____,
                                              bool _____) => accountEntities.Where(filter.Compile()));
        }

        public void ConfigureSettings(AccountEntityConfiguration mockupSettings = null)
        {
            AppSettingsMock.Setup(settings => settings.GetSection<AccountEntityConfiguration>($"{AccountEntityConfiguration.Section}"))
                           .Returns(mockupSettings);
        }
    }

    #region Primary Contact Address

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ContactAddressPostalCodeRequired_ForPrimaryContact()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.FirstOrDefault(new AccountContactEntity()).AccountContactAddress = new();
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_Account_PostalCodeRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ContactAddressCountryCodeRequired_ForPrimaryContact()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.FirstOrDefault(new AccountContactEntity()).AccountContactAddress = new();
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_Account_CountryCodeRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ContactAddressProvinceStateRequired_ForPrimaryContact()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.FirstOrDefault(new AccountContactEntity()).AccountContactAddress = new();
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_Account_StateProvinceRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ContactAddressStreetRequired_ForPrimaryContact()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.FirstOrDefault(new AccountContactEntity()).AccountContactAddress = new();
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_Account_StreetRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ContactAddressRequired_ForPrimaryContact()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.FirstOrDefault(new AccountContactEntity()).AccountContactAddress = null;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_Address_Required);
    }

    #endregion

    #region AccountContactAddress Validation if ContactFunction is BillingContact

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ContactAddressPostalCodeRequired_ForBillingContact()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(1);
        context.Target.Contacts.ForEach(x =>
                                        {
                                            x.ContactFunctions = new()
                                            {
                                                Key = Guid.NewGuid(),
                                                List = new() { AccountContactFunctions.BillingContact.ToString() },
                                            };

                                            x.AccountContactAddress = new();
                                        });

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_Account_PostalCodeRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ContactAddressUSPostalCodeValidFormat_ForBillingContact()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts.ForEach(x =>
                                        {
                                            x.ContactFunctions = new()
                                            {
                                                Key = Guid.NewGuid(),
                                                List = new() { AccountContactFunctions.BillingContact.ToString() },
                                            };

                                            x.AccountContactAddress = new()
                                            {
                                                Street = "111 Main St",
                                                City = "Moon",
                                                ZipCode = "L6R2P2",
                                                Country = CountryCode.US,
                                                Province = StateProvince.RI,
                                            };
                                        });

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_Address_PostalCodeValidFormat);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ContactAddressCanadaPostalCodeRequired_ForBillingContact()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(1);
        context.Target.Contacts.ForEach(x =>
                                        {
                                            x.ContactFunctions = new()
                                            {
                                                Key = Guid.NewGuid(),
                                                List = new() { AccountContactFunctions.BillingContact.ToString() },
                                            };

                                            x.AccountContactAddress = new()
                                            {
                                                Street = "112 Main St",
                                                City = "Oakville",
                                                ZipCode = "15108",
                                                Country = CountryCode.CA,
                                                Province = StateProvince.ON,
                                            };
                                        });

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_Address_PostalCodeValidFormat);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ContactAddressCountryCodeRequired_ForBillingContact()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(1);
        context.Target.Contacts.ForEach(x =>
                                        {
                                            x.ContactFunctions = new()
                                            {
                                                Key = Guid.NewGuid(),
                                                List = new() { AccountContactFunctions.BillingContact.ToString() },
                                            };

                                            x.AccountContactAddress = new();
                                        });

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_Account_CountryCodeRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ContactAddressProvinceStateRequired_ForBillingContact()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(1);
        context.Target.Contacts.ForEach(x =>
                                        {
                                            x.ContactFunctions = new()
                                            {
                                                Key = Guid.NewGuid(),
                                                List = new() { AccountContactFunctions.BillingContact.ToString() },
                                            };

                                            x.AccountContactAddress = new();
                                        });

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_Account_StateProvinceRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ContactAddressStreetRequired_ForBillingContact()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(1);
        context.Target.Contacts.ForEach(x =>
                                        {
                                            x.ContactFunctions = new()
                                            {
                                                Key = Guid.NewGuid(),
                                                List = new() { AccountContactFunctions.BillingContact.ToString() },
                                            };

                                            x.AccountContactAddress = new();
                                        });

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_Account_StreetRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_ContactAddressRequired_ForBillingContact()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(1);
        context.Target.Contacts.ForEach(x =>
                                        {
                                            x.ContactFunctions = new()
                                            {
                                                Key = Guid.NewGuid(),
                                                List = new() { AccountContactFunctions.BillingContact.ToString() },
                                            };

                                            x.AccountContactAddress = null;
                                        });

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Contact_Address_Required);
    }

    #endregion

    #region Customer Address

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_CustomerAddress_CityRequired()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).City = string.Empty;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Address_CityRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_CustomerAddress_StreetRequired()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).Street = string.Empty;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Address_StreetRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_CustomerAddress_CountryCodeRequired()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).Country = CountryCode.Undefined;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Address_CountryCodeRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_CustomerAddress_StateProvinceRequired()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).Province = StateProvince.Unspecified;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Address_StateProvinceRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Rule_ShouldFail_CustomerAddress_PostalCodeRequired()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).ZipCode = string.Empty;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_Address_PostalCodeRequired);
    }

    #endregion

    #region Mailing Address validation

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(AccountTypes.TruckingCompany)]
    [DataRow(AccountTypes.ThirdPartyAnalytical)]
    [DataRow(AccountTypes.Generator)]
    [DataRow(AccountTypes.Customer)]
    public async Task Rule_ShouldFail_AllAccountTypes_MailingAddress_CityRequired(AccountTypes accountType)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).City = string.Empty;
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).AddressType = AddressType.Mail;
        context.Target.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new() { accountType.ToString() },
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
           .Contain(TTErrorCodes.Account_MailingAddress_CityRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(AccountTypes.TruckingCompany)]
    [DataRow(AccountTypes.ThirdPartyAnalytical)]
    [DataRow(AccountTypes.Generator)]
    [DataRow(AccountTypes.Customer)]
    public async Task Rule_ShouldFail_AllAccountTypes_MailingAddress_StreetRequired(AccountTypes accountType)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).Street = string.Empty;
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).AddressType = AddressType.Mail;

        var validationResults = new List<ValidationResult>();
        context.Target.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new() { accountType.ToString() },
        };

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_MailingAddress_StreetRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(AccountTypes.TruckingCompany)]
    [DataRow(AccountTypes.ThirdPartyAnalytical)]
    [DataRow(AccountTypes.Generator)]
    [DataRow(AccountTypes.Customer)]
    public async Task Rule_ShouldFail_AllAccountTypes_MailingAddress_CountryCodeRequired(AccountTypes accountType)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).Country = CountryCode.Undefined;
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).AddressType = AddressType.Mail;
        context.Target.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new() { accountType.ToString() },
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
           .Contain(TTErrorCodes.Account_MailingAddress_CountryCodeRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(AccountTypes.TruckingCompany)]
    [DataRow(AccountTypes.ThirdPartyAnalytical)]
    [DataRow(AccountTypes.Generator)]
    [DataRow(AccountTypes.Customer)]
    public async Task Rule_ShouldFail_AllAccountTypes_MailingAddress_StateProvinceRequired(AccountTypes accountType)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).Province = StateProvince.Unspecified;
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).AddressType = AddressType.Mail;
        context.Target.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new() { accountType.ToString() },
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
           .Contain(TTErrorCodes.Account_MailingAddress_StateProvinceRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(AccountTypes.TruckingCompany)]
    [DataRow(AccountTypes.ThirdPartyAnalytical)]
    [DataRow(AccountTypes.Generator)]
    [DataRow(AccountTypes.Customer)]
    public async Task Rule_ShouldFail_AllAccountTypes_MailingAddress_PostalCodeRequired(AccountTypes accountType)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new() { accountType.ToString() },
        };

        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).ZipCode = string.Empty;
        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).AddressType = AddressType.Mail;

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .Contain(TTErrorCodes.Account_MailingAddress_PostalCodeRequired);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(AccountTypes.TruckingCompany)]
    [DataRow(AccountTypes.ThirdPartyAnalytical)]
    [DataRow(AccountTypes.Generator)]
    [DataRow(AccountTypes.Customer)]
    public async Task Rule_ShouldPass_AllAccountTypes_MailingAddress_PostalCodeRequired(AccountTypes accountType)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext();
        context.Target.AccountTypes = new()
        {
            Key = Guid.NewGuid(),
            List = new() { accountType.ToString() },
        };

        context.Target.AccountAddresses.First(x => x.IsPrimaryAddress).AddressType = AddressType.Mail;

        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        Assert.IsTrue(validationResults.Count == 0);
    }

    #endregion
}
