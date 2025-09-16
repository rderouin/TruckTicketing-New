using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.EmailTemplates;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.EDIFieldDefinition;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Accounts;

using Trident.Contracts;
using Trident.Mapper;

using AccountTypes = SE.Shared.Common.Lookups.AccountTypes;

namespace SE.Shared.Domain.Entities.NewAccount;

public class NewAccountManager : INewAccountManager
{
    private readonly IAccountAttachmentsBlobStorage _accountAttachmentsBlobStorage;

    private readonly IManager<Guid, AccountEntity> _accountManager;

    private readonly IManager<Guid, BillingConfigurationEntity> _billingConfigurationManager;

    private readonly IManager<Guid, EDIFieldDefinitionEntity> _EDIFieldDefinitionManager;

    private readonly IEmailTemplateSender _emailTemplateSender;

    private readonly IManager<Guid, InvoiceConfigurationEntity> _invoiceConfigurationManager;

    private readonly IMapperRegistry _mapper;

    private readonly IUserContextAccessor _userContextAccessor;

    private Guid customerId;

    public NewAccountManager(IManager<Guid, AccountEntity> accountManager,
                             IManager<Guid, BillingConfigurationEntity> billingConfigurationManager,
                             IManager<Guid, EDIFieldDefinitionEntity> EDIFieldDefinitionManager,
                             IManager<Guid, InvoiceConfigurationEntity> invoiceConfigurationManager,
                             IMapperRegistry mapper,
                             IAccountAttachmentsBlobStorage accountAttachmentsBlobStorage,
                             IUserContextAccessor userContextAccessor,
                             IEmailTemplateSender emailTemplateSender)
    {
        _accountManager = accountManager;
        _billingConfigurationManager = billingConfigurationManager;
        _EDIFieldDefinitionManager = EDIFieldDefinitionManager;
        _mapper = mapper;
        _accountAttachmentsBlobStorage = accountAttachmentsBlobStorage;
        _userContextAccessor = userContextAccessor;
        _emailTemplateSender = emailTemplateSender;
        _invoiceConfigurationManager = invoiceConfigurationManager;
    }

    public async Task CreateNewAccount(NewAccountModel newAccount)
    {
        bool isDeferCommit;
        var account = _mapper.Map<AccountEntity>(newAccount.Account);
        isDeferCommit = newAccount.Account.AccountTypes.Except(new[] { AccountTypes.ThirdPartyAnalytical.ToString(), AccountTypes.TruckingCompany.ToString() }).Any();

        customerId = account.Id;
        var newAccountEntity = await _accountManager.Save(account, isDeferCommit);

        var customerAccount = _mapper.Map<AccountEntity>(newAccount.BillingCustomer);
        if (customerAccount.Id != default)
        {
            customerId = customerAccount.Id;
            await CreateNewCustomer(_mapper.Map<AccountEntity>(newAccount.BillingCustomer));
        }

        await CreateNewEDIFieldDefinitions(_mapper.Map<List<EDIFieldDefinitionEntity>>(newAccount.EDIFieldDefinitions));

        var billingConfiguration = _mapper.Map<BillingConfigurationEntity>(newAccount.BillingConfiguration);
        if (billingConfiguration.Id != default)
        {
            await CreateNewBillingConfiguration(_mapper.Map<BillingConfigurationEntity>(newAccount.BillingConfiguration), customerAccount, newAccountEntity);
        }
    }

    public async Task InitiateNewAccountCreditReviewal(InitiateAccountCreditReviewalRequest request)
    {
        var account = await _accountManager.GetById(request.AccountId);
        var userContext = _userContextAccessor.UserContext;

        // send email
        await _emailTemplateSender.Dispatch(new()
        {
            TemplateKey = EmailTemplateEventNames.CreditApplicationRequestDetails,
            Recipients = request.ToEmail,
            CcRecipients = string.Join(", ", request.CcEmails ?? Enumerable.Empty<string>()),
            BccRecipients = string.Empty,
            AdHocNote = string.Empty,
            AdHocAttachments = new(),
            ContextBag = new()
            {
                [nameof(AccountEntity)] = account,
                [nameof(UserContext)] = userContext,
            },
        });
    }

    public async Task<string> GetAttachmentDownloadUri(Guid accountId, Guid attachmentId)
    {
        var account = await _accountManager.GetById(accountId);
        var attachments = account.Attachments.Where(x => x.Id == attachmentId);
        var attachment = attachments.FirstOrDefault();

        if (attachment != null && await _accountAttachmentsBlobStorage.Exists(attachment.ContainerName, attachment.Blob))
        {
            var uri = _accountAttachmentsBlobStorage.GetDownloadUri(attachment.ContainerName, attachment.Blob, $"attachment; filename=\"{attachment.FileName}\"", null);
            return uri.ToString();
        }

        return null;
    }

    private async Task CreateNewCustomer(AccountEntity Account)
    {
        await _accountManager.Save(Account, true);
    }

    private async Task CreateNewEDIFieldDefinitions(List<EDIFieldDefinitionEntity> eDIFieldDefinitionEntities)
    {
        foreach (var EDIFieldDefinitionEntity in eDIFieldDefinitionEntities)
        {
            EDIFieldDefinitionEntity.CustomerId = customerId;
            await _EDIFieldDefinitionManager.Save(EDIFieldDefinitionEntity, true);
        }
    }

    private async Task CreateNewBillingConfiguration(BillingConfigurationEntity billingConfigurationEntity, AccountEntity customer, AccountEntity account)
    {
        InvoiceConfigurationEntity invoiceConfigurationEntity;
        var existingCatchAllInvoiceConfigForCustomer = await _invoiceConfigurationManager.Get(x => x.CustomerId == billingConfigurationEntity.BillingCustomerAccountId && x.CatchAll);
        var catchAllInvoiceConfigForCustomer = existingCatchAllInvoiceConfigForCustomer?.ToList();
        if (catchAllInvoiceConfigForCustomer == null || !catchAllInvoiceConfigForCustomer.Any())
        {
            invoiceConfigurationEntity = await _invoiceConfigurationManager.Save(new()
            {
                Name = billingConfigurationEntity.Name,
                CatchAll = true,
                AllFacilities = true,
                AllWellClassifications = true,
                AllSubstances = true,
                AllServiceTypes = true,
                AllSourceLocations = true,
                CustomerId = billingConfigurationEntity.BillingCustomerAccountId,
                CustomerName = customer.Id != default ? customer.Name : account.Name,
                IncludeExternalDocumentAttachment = account.IncludeExternalDocumentAttachmentInLC,
                SplittingCategories = new()
                {
                    List = new()
                    {
                        InvoiceSplittingCategories.Facility.ToString(),
                        InvoiceSplittingCategories.SourceLocation.ToString(),
                        InvoiceSplittingCategories.WellClassification.ToString(),
                    },
                },
            }, true);
        }
        else
        {
            invoiceConfigurationEntity = catchAllInvoiceConfigForCustomer.First();
        }

        billingConfigurationEntity.InvoiceConfigurationId = invoiceConfigurationEntity.Id;
        billingConfigurationEntity.BillingCustomerName = customer.Id != Guid.Empty ? customer.Name : account.Name;
        billingConfigurationEntity.LoadConfirmationFrequency ??= LoadConfirmationFrequency.Monthly;
        await _billingConfigurationManager.Save(billingConfigurationEntity);
    }
}
