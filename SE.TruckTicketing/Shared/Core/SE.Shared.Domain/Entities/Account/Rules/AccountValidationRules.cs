using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentValidation;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.LegalEntity;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Contracts.Configuration;
using Trident.Data.Contracts;
using Trident.Search;
using Trident.Validation;

using AccountTypes = SE.Shared.Common.Lookups.AccountTypes;

namespace SE.Shared.Domain.Entities.Account.Rules;

public class AccountValidationRules : FluentValidationRule<AccountEntity, TTErrorCodes>
{
    private const string AccountNameUniqueConstraintChecker = nameof(AccountNameUniqueConstraintChecker);

    private const string AccountContactReferenceIndexChecker = nameof(AccountContactReferenceIndexChecker);

    private const string AccountReferenceIndexChecker = nameof(AccountReferenceIndexChecker);

    private const string IsCustomerAccountTypeUpdated = nameof(IsCustomerAccountTypeUpdated);

    private const string IsLegalEntityEnforcePrimaryContactConstraint = nameof(IsLegalEntityEnforcePrimaryContactConstraint);

    private const string BypassPrimaryContactPhoneNumberConstraint = nameof(BypassPrimaryContactPhoneNumberConstraint);

    public const string ResultKey = AccountNameUniqueConstraintChecker;

    private const string USZipCodeRagexPattern = @"^\d{5}(?:[-\s]\d{4})?$";

    private const string CanadianZipCodeRagexPattern = @"^[ABCEGHJ-NPRSTVXY][0-9][ABCEGHJ-NPRSTV-Z]\s?[0-9][ABCEGHJ-NPRSTV-Z][0-9]";

    private readonly IProvider<Guid, AccountContactReferenceIndexEntity> _accountContactReferenceIndexProvider;

    private readonly IAppSettings _appSettings;

    private readonly IProvider<Guid, LegalEntityEntity> _legalEntityProvider;

    private readonly SearchResults<AccountContactReferenceIndexEntity, SearchCriteria> _loadAccountContactReferenceIndexData = new();

    private readonly SearchResults<AccountContactReferenceIndexEntity, SearchCriteria> _loadAccountReferenceIndexData = new();

    private readonly IProvider<Guid, AccountEntity> _provider;

    private readonly Dictionary<Guid, List<string>> AccountContactToReferenceErrorMap = new();

    private List<string> AccountContactReferenceErrors = new();

    private Dictionary<string, List<Guid>> AccountContactToReferenceEntityMap = new();

    private Dictionary<string, List<Guid>> AccountReferenceEntityMap = new();

    private List<string> AccountReferenceErrors = new();

    public AccountValidationRules(IProvider<Guid, AccountEntity> provider,
                                  IProvider<Guid, AccountContactReferenceIndexEntity> accountContactReferenceIndex,
                                  IProvider<Guid, LegalEntityEntity> legalEntityProvider,
                                  IAppSettings appSettings)
    {
        _provider = provider;
        _accountContactReferenceIndexProvider = accountContactReferenceIndex;
        _legalEntityProvider = legalEntityProvider;
        _appSettings = appSettings;
    }

    public override int RunOrder => 10;

    public override async Task Run(BusinessContext<AccountEntity> context, List<ValidationResult> errors)
    {
        if (context.Target.AccountStatus == AccountStatus.Open &&
            !string.IsNullOrEmpty(context.Target.Name) &&
            (context.Original?.Name == null || context.Original?.Name != context.Target.Name))
        {
            //Name, LegalEntity & CustomerNumber should be unique
            //Under same LegalEntity => Unique Name & CustomerNumber

            //When Creating new Account; we don't have CustomerNumber
            //If no CustomerNumber; don't check CustomerNumber
            var isDuplicate = await _provider.Get(type =>
                                                      type.Id != context.Target.Id &&
                                                      type.AccountStatus == AccountStatus.Open &&
                                                      type.Name == context.Target.Name &&
                                                      type.LegalEntityId == context.Target.LegalEntityId &&
                                                      (type.CustomerNumber == context.Target.CustomerNumber || !context.Target.CustomerNumber.HasText()));

            context.ContextBag.TryAdd(ResultKey, !isDuplicate.Any());
        }

        //Check based on LegalEntity
        await RunAccountContactReferenceIndexChecker(context);

        if (context.Original != null)
        {
            RunAccountReferenceIndexChecker(context);
        }

        if (context.Original != null && context.Original.AccountTypes.List.Contains(AccountTypes.Customer.ToString()) &&
            !context.Target.AccountTypes.List.Contains(AccountTypes.Customer.ToString()))
        {
            context.ContextBag.TryAdd(IsCustomerAccountTypeUpdated, true);
        }

        if (context.Target.AccountTypes.List.Contains(AccountTypes.Customer.ToString()))
        {
            var legalEntity = await _legalEntityProvider.GetById(context.Target.LegalEntityId);
            if (legalEntity != null && legalEntity.Id != default)
            {
                context.ContextBag.TryAdd(IsLegalEntityEnforcePrimaryContactConstraint, legalEntity.IsCustomerPrimaryContactRequired);
            }
        }

        var accountEntityConfiguration = _appSettings?.GetSection<AccountEntityConfiguration>($"{AccountEntityConfiguration.Section}");
        if (accountEntityConfiguration != null)
        {
            context.ContextBag.TryAdd(BypassPrimaryContactPhoneNumberConstraint, accountEntityConfiguration.BypassPrimaryContactPhoneNumberConstraint);
        }

        await base.Run(context, errors);
    }

    protected override void ConfigureRules(BusinessContext<AccountEntity> context, InlineValidator<AccountEntity> validator)
    {
        validator.RuleFor(account => account.Name)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.Account_NameRequired);

        validator.RuleFor(account => account.AccountTypes.List)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.Account_TypeRequired);

        validator.RuleFor(account => account.LegalEntityId)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.Account_LegalEntity_Required);

        validator.RuleFor(account => account)
                 .Must(_ => BeUniqueAccountName(context))
                 .WithMessage("An Open account with the Name you have entered already exists in the system. Enter a new Account Name that does not match for any existing Open accounts.")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.Account_NameMustBeUniqueForOpenAccounts, nameof(AccountEntity.Name)));

        validator.RuleFor(account => account)
                 .Must(_ => !CustomerAccountTypeUpdated(context))
                 .WithMessage("Customer account type flag can't be removed once set.")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.Account_AccountType_CustomerFlagUpdated, nameof(AccountEntity.AccountTypes)));

        validator.RuleFor(account => account.NickName)
                 .MaximumLength(50)
                 .WithTridentErrorCode(TTErrorCodes.Account_NickNameMax50);

        validator.RuleFor(account => account.BillingType)
                 .Must(type => type != BillingType.Undefined)
                 .When(account => account.IsElectronicBillingEnabled)
                 .WithMessage("Billing Type required if account configured for billing")
                 .WithTridentErrorCode(TTErrorCodes.Account_BillingType_Required);

        var accountReferences = GetAccountReferenceIndexes(context);
        validator.RuleFor(account => account)
                 .Must(x => !accountReferences.Any())
                 .WithMessage(x => "Account type flag can't be removed, active association to " + string.Join(", ", accountReferences))
                 .WithTridentErrorCode(TTErrorCodes.Account_ActiveAssociationFound);

        if (context.Target.AccountTypes != null && context.Target.AccountTypes.List.Count > 0 && context.Target.AccountTypes.List != null)
        {
            //Rule for AccountAddress
            validator.RuleFor(account => account)
                     .Must(account => account.AccountAddresses != null && account.AccountAddresses.Any(x => !x.IsDeleted))
                     .When(account => account.AccountTypes.List.Contains(AccountTypes.Customer.ToString()) &&
                                      account.AccountTypes.List.Count >= 1)
                     .WithMessage("Address/es required for customer.")
                     .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.Account_Address_MinimumOnePrimaryAddressForAccount, nameof(AccountEntity.AccountAddresses)));

            validator.RuleFor(account => account)
                     .Must(address => address.AccountAddresses.Any(x => !x.IsDeleted && x.IsPrimaryAddress))
                     .When(account => account.AccountAddresses != null && account.AccountAddresses.Any() && account.AccountTypes.List.Contains(AccountTypes.Customer.ToString()) &&
                                      account.AccountTypes.List.Count >= 1)
                     .WithMessage("At least one added address marked as primary for customer")
                     .WithTridentErrorCode(TTErrorCodes.Account_Address_AtleastOnePrimaryAddressRequiredForAccounts);

            validator.RuleForEach(account => account.AccountAddresses)
                     .SetValidator(new AccountAddressValidator())
                     .When(account => account.AccountAddresses != null && account.AccountAddresses.Any(x => !x.IsDeleted) && account.AccountTypes.List.Contains(AccountTypes.Customer.ToString()) &&
                                      account.AccountTypes.List.Count >= 1);

            validator.RuleForEach(account => account.AccountAddresses)
                     .SetValidator(new AccountAddressPostalCodeValidator())
                     .When(account => account.AccountAddresses != null && account.AccountAddresses.Any(x => !x.IsDeleted));

            validator.RuleForEach(account => account.AccountAddresses)
                     .SetValidator(new AccountMailingAddressValidator())
                     .When(account => account.AccountAddresses != null && account.AccountAddresses.Any(x => !x.IsDeleted));
        }

        //Account Contact Validation
        validator.RuleFor(account => account)
                 .Must(account => account.Contacts != null && account.Contacts.Any(x => x.IsPrimaryAccountContact && !x.IsDeleted))
                 .When(x => x.AccountTypes.List.Contains(AccountTypes.Customer.ToString()) &&
                            EnforceCustomerPrimaryContactConstraint(context))
                 .WithMessage("At least one primary contact required for the customer")
                 .WithTridentErrorCode(TTErrorCodes.Account_Contact_AtleastOnePrimaryContact);

        validator.RuleFor(account => account)
                 .Must(account => account.Contacts.Any(x => x.IsPrimaryAccountContact && !x.IsDeleted))
                 .When(x => x.Contacts != null && x.AccountTypes.List.Contains(AccountTypes.Customer.ToString()) && EnforceCustomerPrimaryContactConstraint(context))
                 .WithMessage("At least one added contact marked as primary contact for the customer")
                 .WithTridentErrorCode(TTErrorCodes.Account_Contact_AtleastOnePrimaryContact);

        if (context.Target.Contacts != null && context.Target.Contacts.Any())
        {
            validator.RuleFor(account => account)
                     .Must(account => account.Contacts != null && account.Contacts.Count(x => x.IsPrimaryAccountContact && !x.IsDeleted) == 1)
                     .When(x => x.Contacts.Count > 1 && x.Contacts.Count(contact => contact.IsPrimaryAccountContact && !contact.IsDeleted) > 1
                                                     && x.AccountTypes.List.Contains(AccountTypes.Customer.ToString()))
                     .WithMessage("Only one added contact marked as primary contact for the customer")
                     .WithTridentErrorCode(TTErrorCodes.Account_Contact_OnlySinglePrimaryContactAllowed);

            validator.RuleForEach(account => account.Contacts)
                     .SetValidator(new ContactValidator(context));

            validator.RuleForEach(account => account.Contacts)
                     .ChildRules(contact => contact
                                           .RuleFor(x => x.ContactFunctions)
                                           .Must(function => function.List.Any() && function.List.Contains(AccountContactFunctions.BillingContact.ToString()))
                                           .When(x => x.IsPrimaryAccountContact)
                                           .WithMessage("Contact function should have 'Billing Contact' for primary billing customer contact")
                                           .WithTridentErrorCode(TTErrorCodes.Account_Contact_InvalidContactFunction_PrimaryBillingContact))
                     .When(x => x.AccountTypes != null && x.AccountTypes.List.Any() && x.AccountTypes.List.Contains(AccountTypes.Customer.ToString()));

            var accountContactReferences = GetAccountContactReferenceIndexes(context);
            validator.RuleForEach(account => account.Contacts)
                     .ChildRules(contact => contact
                                           .RuleFor(x => x)
                                           .Must(x => !accountContactReferences.ContainsKey(x.Id))
                                           .When(x => !x.IsActive)
                                           .WithMessage(x => "Contact can't be deactivated, active reference to " + string.Join(", ", accountContactReferences[x.Id]))
                                           .WithTridentErrorCode(TTErrorCodes.Account_Contact_ActiveAssociationFound));
        }
    }

    private static bool BeUniqueAccountName(BusinessContext<AccountEntity> context)
    {
        return context.GetContextBagItemOrDefault(AccountNameUniqueConstraintChecker, true);
    }

    private static bool EnforceCustomerPrimaryContactConstraint(BusinessContext<AccountEntity> context)
    {
        return context.GetContextBagItemOrDefault(IsLegalEntityEnforcePrimaryContactConstraint, true);
    }

    private static bool BypassPhoneNumberForPrimaryContactConstraint(BusinessContext<AccountEntity> context)
    {
        return context.GetContextBagItemOrDefault(BypassPrimaryContactPhoneNumberConstraint, false);
    }

    private static bool CustomerAccountTypeUpdated(BusinessContext<AccountEntity> context)
    {
        return context.GetContextBagItemOrDefault(IsCustomerAccountTypeUpdated, false);
    }

    private static Dictionary<Guid, List<string>> GetAccountContactReferenceIndexes(BusinessContext<AccountEntity> context)
    {
        return context.GetContextBagItemOrDefault(AccountContactReferenceIndexChecker, new Dictionary<Guid, List<string>>());
    }

    private static List<string> GetAccountReferenceIndexes(BusinessContext<AccountEntity> context)
    {
        return context.GetContextBagItemOrDefault(AccountReferenceIndexChecker, new List<string>());
    }

    private void RunAccountReferenceIndexChecker(BusinessContext<AccountEntity> context)
    {
        List<AccountContactReferenceIndexEntity> accountReferenceIndexEntities = new();
        if (context.Original.AccountTypes.List.Contains(AccountTypes.Generator.ToString()) && !context.Target.AccountTypes.List.Contains(AccountTypes.Generator.ToString()))
        {
            accountReferenceIndexEntities.AddRange(_loadAccountContactReferenceIndexData.Results.Where(x => x.AccountContactId == null && x.ReferenceEntityName == "Source Location").ToList());
        }

        if (context.Original.AccountTypes.List.Contains(AccountTypes.ThirdPartyAnalytical.ToString()) && !context.Target.AccountTypes.List.Contains(AccountTypes.ThirdPartyAnalytical.ToString()))
        {
            accountReferenceIndexEntities.AddRange(_loadAccountContactReferenceIndexData.Results.Where(x => x.AccountContactId == null && x.ReferenceEntityName == "Material Approval").ToList());
        }

        if (context.Original.AccountTypes.List.Contains(AccountTypes.TruckingCompany.ToString()) && !context.Target.AccountTypes.List.Contains(AccountTypes.TruckingCompany.ToString()))
        {
            accountReferenceIndexEntities.AddRange(_loadAccountContactReferenceIndexData.Results
                                                                                        .Where(x => x.AccountContactId == null && x.ReferenceEntityName is "Material Approval" or "Truck Ticket")
                                                                                        .ToList());
        }

        if (accountReferenceIndexEntities.Any())
        {
            AccountReferenceEntityMap = new();
            foreach (var result in accountReferenceIndexEntities)
            {
                AccountReferenceEntityMap.TryAdd(result.ReferenceEntityName, new());
                if (!AccountReferenceEntityMap[result.ReferenceEntityName].Contains(result.ReferenceEntityId))
                {
                    AccountReferenceEntityMap[result.ReferenceEntityName].Add(result.ReferenceEntityId);
                }
            }

            if (AccountReferenceEntityMap.Count > 0)
            {
                AccountReferenceErrors = new();
                foreach (var association in AccountReferenceEntityMap)
                {
                    var entityReferenceName = association.Key;
                    var entityReferenceCount = association.Value.Count;
                    AccountReferenceErrors.Add($"{entityReferenceCount} {entityReferenceName}(s)");
                }
            }
        }

        context.ContextBag.TryAdd(AccountReferenceIndexChecker, AccountReferenceErrors);
    }

    private async Task RunAccountContactReferenceIndexChecker(BusinessContext<AccountEntity> context)
    {
        var searchCriteria = new SearchCriteria
        {
            CurrentPage = 0,
            Keywords = "",
            Filters = new()
            {
                [nameof(AccountContactReferenceIndexEntity.DocumentType)] = $"AccountContact|{context.Target.Id}",
                [nameof(AccountContactReferenceIndexEntity.IsDisabled)] = new Compare
                {
                    Value = true,
                    Operator = CompareOperators.ne,
                },
            },
        };

        var results = await _accountContactReferenceIndexProvider.Search(searchCriteria); // PK - OK
        _loadAccountContactReferenceIndexData.Results = results?.Results ?? new List<AccountContactReferenceIndexEntity>();

        var accountContactReferenceIndexEntities = _loadAccountContactReferenceIndexData.Results.ToList();
        if (accountContactReferenceIndexEntities.Any())
        {
            foreach (var contact in context.Target.Contacts)
            {
                AccountContactToReferenceEntityMap = new();
                foreach (var result in accountContactReferenceIndexEntities)
                {
                    if (result.AccountContactId != null && result.AccountContactId != default && result.AccountContactId == contact.Id)
                    {
                        AccountContactToReferenceEntityMap.TryAdd(result.ReferenceEntityName, new());
                        if (!AccountContactToReferenceEntityMap[result.ReferenceEntityName].Contains(result.ReferenceEntityId))
                        {
                            AccountContactToReferenceEntityMap[result.ReferenceEntityName].Add(result.ReferenceEntityId);
                        }
                    }
                }

                if (AccountContactToReferenceEntityMap.Count > 0)
                {
                    AccountContactReferenceErrors = new();
                    foreach (var association in AccountContactToReferenceEntityMap)
                    {
                        var entityReferenceName = association.Key;
                        var entityReferenceCount = association.Value.Count;
                        AccountContactReferenceErrors.Add($"{entityReferenceCount} {entityReferenceName}(s)");
                    }

                    AccountContactToReferenceErrorMap.TryAdd(contact.Id, new());
                    AccountContactToReferenceErrorMap[contact.Id].AddRange(AccountContactReferenceErrors);
                }
            }
        }

        context.ContextBag.TryAdd(AccountContactReferenceIndexChecker, AccountContactToReferenceErrorMap);
    }

    private class ContactValidator : AbstractValidator<AccountContactEntity>
    {
        public ContactValidator(BusinessContext<AccountEntity> context)
        {
            RuleFor(x => x)
               .Must(x => NotBeADuplicateContact(x, context.Target))
               .WithMessage(x => $"{x.Name} {x.LastName} is a duplicate contact.")
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_Duplicate);

            RuleFor(x => x.ContactFunctions.List)
               .NotEmpty()
               .WithMessage("Contact functions is required for account")
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_FunctionsRequired);

            RuleFor(x => x.Name)
               .NotEmpty()
               .When(x => x.IsPrimaryAccountContact)
               .WithMessage("First Name for primary contact is required for account")
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_FirstNameRequiredForPrimaryContact);

            RuleFor(x => x.LastName)
               .NotEmpty()
               .When(x => x.IsPrimaryAccountContact)
               .WithMessage("Last Name for primary contact is required for account")
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_LastNameRequiredForPrimaryContact);

            RuleFor(x => x.Email)
               .NotEmpty()
               .When(x => x.IsPrimaryAccountContact || x.ContactFunctions.List.Contains(AccountContactFunctions.FieldSignatoryContact.ToString()) ||
                          (!x.IsPrimaryAccountContact && string.IsNullOrEmpty(x.PhoneNumber)))
               .WithMessage("Contact email is required for account")
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_ContactEmailRequired);

            RuleFor(x => x.Email)
               .EmailAddress()
               .WithMessage("Contact email is not in valid format")
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_ContactEmailInvalidFormat);

            RuleFor(x => x.AccountContactAddress)
               .NotEmpty()
               .When(x => x.IsPrimaryAccountContact ||
                          (x.ContactFunctions != null && x.ContactFunctions.List.Any() &&
                           x.ContactFunctions.List.Contains(AccountContactFunctions.BillingContact.ToString())))
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_Address_Required);

            RuleFor(x => x.AccountContactAddress)
               .SetValidator(new ContactAddressValidator())
               .When(x => x.AccountContactAddress != null && (x.IsPrimaryAccountContact ||
                                                              (x.ContactFunctions != null && x.ContactFunctions.List.Any() &&
                                                               x.ContactFunctions.List.Contains(AccountContactFunctions.BillingContact.ToString()))));

            RuleFor(x => x.AccountContactAddress)
               .SetValidator(new ContactAddressPostalCodeValidator())
               .When(x => x.AccountContactAddress != null);

            RuleFor(x => x.PhoneNumber)
               .NotEmpty()
               .When(x => (x.IsPrimaryAccountContact && !BypassPhoneNumberForPrimaryContactConstraint(context)) ||
                          (!x.IsPrimaryAccountContact && string.IsNullOrEmpty(x.Email)))
               .WithMessage("Contact phone number is required for account")
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_ContactPhoneNumberRequired);
        }

        public bool NotBeADuplicateContact(AccountContactEntity contact, AccountEntity account)
        {
            var isDuplicate = account.Contacts.Any(accountContact => accountContact.Id != contact.Id &&
                                                                     string.Compare(accountContact.Name.ToLower().Trim(), contact.Name.ToLower().Trim(), StringComparison.OrdinalIgnoreCase) == 0 &&
                                                                     string.Compare(accountContact.LastName.ToLower().Trim(), contact.LastName.ToLower().Trim(), StringComparison.OrdinalIgnoreCase) ==
                                                                     0 &&
                                                                     string.Compare(accountContact.Email.ToLower().Trim(), contact.Email.ToLower().Trim(), StringComparison.OrdinalIgnoreCase) == 0);

            return !isDuplicate;
        }
    }

    private class ContactAddressValidator : AbstractValidator<ContactAddressEntity>
    {
        public ContactAddressValidator()
        {
            RuleFor(contact => contact.Street)
               .NotEmpty()
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_Account_StreetRequired);

            RuleFor(contact => contact.City)
               .NotEmpty()
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_Account_CityRequired);

            RuleFor(contact => contact.Country)
               .NotEmpty()
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_Account_CountryCodeRequired);

            RuleFor(contact => contact.ZipCode)
               .NotEmpty()
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_Account_PostalCodeRequired);

            RuleFor(contact => contact.ZipCode)
               .Matches(USZipCodeRagexPattern)
               .When(x => x.Country == CountryCode.US && !string.IsNullOrEmpty(x.ZipCode))
               .WithMessage("Postal Code for address is not in valid format")
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_Address_PostalCodeValidFormat);

            RuleFor(contact => contact.ZipCode)
               .Matches(CanadianZipCodeRagexPattern)
               .When(x => x.Country == CountryCode.CA && !string.IsNullOrEmpty(x.ZipCode))
               .WithMessage("Postal Code for address is not in valid format")
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_Address_PostalCodeValidFormat);

            RuleFor(contact => contact.Province)
               .NotEmpty()
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_Account_StateProvinceRequired);
        }
    }

    private class ContactAddressPostalCodeValidator : AbstractValidator<ContactAddressEntity>
    {
        public ContactAddressPostalCodeValidator()
        {
            RuleFor(contact => contact.ZipCode)
               .Matches(USZipCodeRagexPattern)
               .When(x => x.Country == CountryCode.US && !string.IsNullOrEmpty(x.ZipCode))
               .WithMessage("Postal Code for address is not in valid format")
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_Address_PostalCodeValidFormat);

            RuleFor(contact => contact.ZipCode)
               .Matches(CanadianZipCodeRagexPattern)
               .When(x => x.Country == CountryCode.CA && !string.IsNullOrEmpty(x.ZipCode))
               .WithMessage("Postal Code for address is not in valid format")
               .WithTridentErrorCode(TTErrorCodes.Account_Contact_Address_PostalCodeValidFormat);
        }
    }

    private class AccountAddressValidator : AbstractValidator<AccountAddressEntity>
    {
        public AccountAddressValidator()
        {
            RuleFor(x => x.Street)
               .NotEmpty()
               .When(address => address.AddressType != AddressType.Mail)
               .WithMessage("Street Address for address is required for account")
               .WithTridentErrorCode(TTErrorCodes.Account_Address_StreetRequired);

            RuleFor(x => x.City)
               .NotEmpty()
               .When(address => address.AddressType != AddressType.Mail)
               .WithMessage("City for address is required for account")
               .WithTridentErrorCode(TTErrorCodes.Account_Address_CityRequired);

            RuleFor(x => x.Country)
               .NotEmpty()
               .When(address => address.AddressType != AddressType.Mail)
               .WithMessage("Country for address is required for account")
               .WithTridentErrorCode(TTErrorCodes.Account_Address_CountryCodeRequired);

            RuleFor(x => x.Province)
               .NotEmpty()
               .When(address => address.AddressType != AddressType.Mail)
               .WithMessage("Province for address is required for account")
               .WithTridentErrorCode(TTErrorCodes.Account_Address_StateProvinceRequired);

            RuleFor(x => x.ZipCode)
               .Length(1, 20)
               .When(address => address.AddressType != AddressType.Mail)
               .WithMessage("Postal/Zip Code for address is required")
               .WithTridentErrorCode(TTErrorCodes.Account_Address_PostalCodeRequired);

            RuleFor(x => x.ZipCode)
               .Matches(USZipCodeRagexPattern)
               .When(x => x.Country == CountryCode.US && !string.IsNullOrEmpty(x.ZipCode) && x.AddressType != AddressType.Mail)
               .WithMessage("Format invalid for US Zip Code")
               .WithTridentErrorCode(TTErrorCodes.Account_Address_PostalCodeInvalidFormat);

            RuleFor(x => x.ZipCode)
               .Matches(CanadianZipCodeRagexPattern)
               .When(x => x.Country == CountryCode.CA && !string.IsNullOrEmpty(x.ZipCode) && x.AddressType != AddressType.Mail)
               .WithMessage("Format invalid for Canadian Postal Code")
               .WithTridentErrorCode(TTErrorCodes.Account_Address_PostalCodeInvalidFormat);
        }
    }

    private class AccountAddressPostalCodeValidator : AbstractValidator<AccountAddressEntity>
    {
        public AccountAddressPostalCodeValidator()
        {
            RuleFor(x => x.ZipCode)
               .Matches(USZipCodeRagexPattern)
               .When(x => x.Country == CountryCode.US && !string.IsNullOrEmpty(x.ZipCode) && x.AddressType != AddressType.Mail)
               .WithMessage("Format invalid for US Zip Code")
               .WithTridentErrorCode(TTErrorCodes.Account_Address_PostalCodeInvalidFormat);

            RuleFor(x => x.ZipCode)
               .Matches(CanadianZipCodeRagexPattern)
               .When(x => x.Country == CountryCode.CA && !string.IsNullOrEmpty(x.ZipCode) && x.AddressType != AddressType.Mail)
               .WithMessage("Format invalid for Canadian Postal Code")
               .WithTridentErrorCode(TTErrorCodes.Account_Address_PostalCodeInvalidFormat);
        }
    }

    private class AccountMailingAddressValidator : AbstractValidator<AccountAddressEntity>
    {
        public AccountMailingAddressValidator()
        {
            RuleFor(x => x.Street)
               .NotEmpty()
               .When(address => address.AddressType == AddressType.Mail)
               .WithMessage("City for mailing address is required for account")
               .WithTridentErrorCode(TTErrorCodes.Account_MailingAddress_StreetRequired);

            RuleFor(x => x.City)
               .NotEmpty()
               .When(address => address.AddressType == AddressType.Mail)
               .WithMessage("City for mailing address is required for account")
               .WithTridentErrorCode(TTErrorCodes.Account_MailingAddress_CityRequired);

            RuleFor(x => x.Province)
               .NotEmpty()
               .When(address => address.AddressType == AddressType.Mail)
               .WithMessage("Province for mailing address is required for account")
               .WithTridentErrorCode(TTErrorCodes.Account_MailingAddress_StateProvinceRequired);

            RuleFor(x => x.Country)
               .NotEmpty()
               .When(address => address.AddressType == AddressType.Mail)
               .WithMessage("Country for address is required for account")
               .WithTridentErrorCode(TTErrorCodes.Account_MailingAddress_CountryCodeRequired);

            RuleFor(x => x.ZipCode)
               .Length(1, 20)
               .When(address => address.AddressType == AddressType.Mail)
               .WithMessage("Postal/Zip Code for mailing address is required for account")
               .WithTridentErrorCode(TTErrorCodes.Account_MailingAddress_PostalCodeRequired);

            RuleFor(x => x.ZipCode)
               .Matches(USZipCodeRagexPattern)
               .When(x => x.Country == CountryCode.US && !string.IsNullOrEmpty(x.ZipCode) && x.AddressType == AddressType.Mail)
               .WithMessage("Format invalid for mailing address US Zip Code")
               .WithTridentErrorCode(TTErrorCodes.Account_MailingAddress_PostalCodeValidFormat);

            RuleFor(x => x.ZipCode)
               .Matches(CanadianZipCodeRagexPattern)
               .When(x => x.Country == CountryCode.CA && !string.IsNullOrEmpty(x.ZipCode) && x.AddressType == AddressType.Mail)
               .WithMessage("Format invalid for mailing address Canadian Postal Code")
               .WithTridentErrorCode(TTErrorCodes.Account_MailingAddress_PostalCodeValidFormat);
        }
    }
}
