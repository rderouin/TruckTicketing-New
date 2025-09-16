using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.BillingConfiguration.Tasks;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.TruckTicket;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Logging;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.Account.Tasks;

public class AccountContactIndexEntityProvisioningTask : WorkflowTaskBase<BusinessContext<AccountContactIndexEntity>>
{
    private readonly IProvider<Guid, BillingConfigurationEntity> _billingConfigurationProvider;

    private readonly IRepository<BillingConfigurationEntity> _billingConfigurationRepository;

    private readonly LoadConfirmationSignatoryUpdateWithBillingConfigurationTask _lcTask;

    private readonly ILog _log;

    private readonly IProvider<Guid, MaterialApprovalEntity> _materialApprovalProvider;

    private readonly IRepository<MaterialApprovalEntity> _materialApprovalRepository;

    private readonly IProvider<Guid, TruckTicketEntity> _truckTicketProvider;

    private readonly IRepository<TruckTicketEntity> _truckTicketRepository;

    public AccountContactIndexEntityProvisioningTask(IProvider<Guid, BillingConfigurationEntity> billingConfigurationProvider,
                                                     IRepository<BillingConfigurationEntity> billingConfigurationRepository,
                                                     IProvider<Guid, LoadConfirmationEntity> loadConfirmationProvider,
                                                     IProvider<Guid, TruckTicketEntity> truckTicketProvider,
                                                     IRepository<TruckTicketEntity> truckTicketRepository,
                                                     IProvider<Guid, MaterialApprovalEntity> materialApprovalProvider,
                                                     IRepository<MaterialApprovalEntity> materialApprovalRepository,
                                                     ILog log,
                                                     LoadConfirmationSignatoryUpdateWithBillingConfigurationTask lcTask)
    {
        _billingConfigurationProvider = billingConfigurationProvider;
        _billingConfigurationRepository = billingConfigurationRepository;
        _truckTicketProvider = truckTicketProvider;
        _truckTicketRepository = truckTicketRepository;
        _materialApprovalProvider = materialApprovalProvider;
        _materialApprovalRepository = materialApprovalRepository;
        _log = log;
        _lcTask = lcTask;
    }

    public override int RunOrder => 500;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override Task<bool> ShouldRun(BusinessContext<AccountContactIndexEntity> context)
    {
        if (context.Operation != Operation.Update)
        {
            return Task.FromResult(false);
        }

        var shouldRun = false;

        if (context.Original != null && context.Target != null)
        {
            var original = GetTrackedFieldsData(context.Original).ToJson();
            var target = GetTrackedFieldsData(context.Target).ToJson();
            shouldRun = original != target;
        }

        return Task.FromResult(shouldRun);

        List<string> GetTrackedFieldsData(AccountContactIndexEntity entity)
        {
            return new()
            {
                entity.Name,
                entity.LastName,
                entity.PhoneNumber,
                entity.Email,
                entity.Street,
                entity.City,
                entity.ZipCode,
                entity.Province.ToString(),
                entity.Country.ToString(),
            };
        }
    }

    public override async Task<bool> Run(BusinessContext<AccountContactIndexEntity> businessContext)
    {
        var operationContext = new OperationContext();
        await UpdateBillingConfigurations(businessContext.Target, operationContext);
        await UpdateTruckTickets(businessContext.Target, operationContext);
        await UpdateMaterialApprovals(businessContext.Target, operationContext);
        _log.Information(messageTemplate: $"{nameof(AccountContactIndexEntityProvisioningTask)} has finished, stats:{Environment.NewLine}{operationContext.FormatCounts()}");
        return true;
    }

    private async Task UpdateBillingConfigurations(AccountContactIndexEntity contact, OperationContext operationContext)
    {
        // billing contact
        var bcs1 = await _billingConfigurationProvider.Get(bc => bc.BillingContactId == contact.Id);
        foreach (var bc in bcs1)
        {
            bc.BillingContactName = contact.GetFullName();
            bc.BillingContactAddress = contact.GetFullAddress();
            await SaveIt(bc);
        }

        // third party billing contact
        var bcs2 = await _billingConfigurationProvider.Get(bc => bc.ThirdPartyBillingContactId == contact.Id);
        foreach (var bc in bcs2)
        {
            bc.ThirdPartyBillingContactName = contact.GetFullName();
            bc.ThirdPartyBillingContactAddress = contact.GetFullAddress();
            await SaveIt(bc);
        }

        // signatories
        var bcs3 = await _billingConfigurationRepository.GetNativeAsync<BillingConfigurationEntity, Guid>(bc => bc.Signatories.Any(s => s.AccountContactId == contact.Id));
        foreach (var bc in bcs3)
        {
            if (UpdateSignatoryContact(bc.Signatories, contact))
            {
                await SaveIt(bc);

                // NOTE: load confirmation signatories are updated by cloning signatories from a billing configuration
                // NOTE: task LoadConfirmationSignatoryUpdateWithBillingConfigurationTask already has required rules
                await _lcTask.Run(new(bc));
            }
        }

        // email recipients
        var bc4 = await _billingConfigurationRepository.GetNativeAsync<BillingConfigurationEntity, Guid>(bc => bc.EmailDeliveryContacts.Any(c => c.AccountContactId == contact.Id));
        foreach (var bc in bc4)
        {
            if (UpdateEmailContact(bc.EmailDeliveryContacts, contact))
            {
                await SaveIt(bc);
            }
        }

        bool UpdateSignatoryContact(IEnumerable<SignatoryContactEntity> signatoryContacts, AccountContactIndexEntity accountContact)
        {
            if (signatoryContacts == null)
            {
                return false;
            }

            var signatory = signatoryContacts.FirstOrDefault(s => s.AccountContactId == accountContact.Id);
            if (signatory != null)
            {
                signatory.FirstName = accountContact.Name;
                signatory.LastName = accountContact.LastName;
                signatory.Address = accountContact.GetFullAddress();
                signatory.PhoneNumber = accountContact.PhoneNumber;
                signatory.Email = accountContact.Email;
                return true;
            }

            return false;
        }

        bool UpdateEmailContact(IEnumerable<EmailDeliveryContactEntity> emailDeliveryContacts, AccountContactIndexEntity accountContact)
        {
            if (emailDeliveryContacts == null)
            {
                return false;
            }

            var emailContact = emailDeliveryContacts.FirstOrDefault(c => c.AccountContactId == accountContact.Id);
            if (emailContact != null)
            {
                emailContact.EmailAddress = accountContact.Email;
                emailContact.SignatoryContact = accountContact.GetFullName();
                return true;
            }

            return false;
        }

        async Task SaveIt(BillingConfigurationEntity bc)
        {
            await _billingConfigurationProvider.Update(bc, true);
            operationContext.BillingConfigurationIds.Add(bc.Id);
        }
    }

    private async Task UpdateTruckTickets(AccountContactIndexEntity contact, OperationContext operationContext)
    {
        // billing contacts
        var tts1 = await _truckTicketProvider.Get(tt => tt.BillingContact.AccountContactId == contact.Id);
        foreach (var tt in tts1)
        {
            tt.BillingContact.Name = contact.GetFullName();
            tt.BillingContact.Address = contact.GetFullAddress();
            tt.BillingContact.Email = contact.Email;
            tt.BillingContact.PhoneNumber = contact.PhoneNumber;
            await SaveIt(tt);
        }

        // signatories
        var tts2 = await _truckTicketRepository.GetNativeAsync<TruckTicketEntity, Guid>(tt => tt.Signatories.Any(s => s.AccountContactId == contact.Id));
        foreach (var tt in tts2)
        {
            if (UpdateSignatoryContact(tt.Signatories, contact))
            {
                await SaveIt(tt);
            }
        }

        bool UpdateSignatoryContact(IEnumerable<SignatoryEntity> signatoryContacts, AccountContactIndexEntity accountContact)
        {
            if (signatoryContacts == null)
            {
                return false;
            }

            var signatory = signatoryContacts.FirstOrDefault(s => s.AccountContactId == accountContact.Id);
            if (signatory != null)
            {
                signatory.ContactName = accountContact.GetFullName();
                signatory.ContactAddress = accountContact.GetFullAddress();
                signatory.ContactEmail = accountContact.Email;
                signatory.ContactPhoneNumber = accountContact.PhoneNumber;
                return true;
            }

            return false;
        }

        async Task SaveIt(TruckTicketEntity tt)
        {
            await _truckTicketProvider.Update(tt, true);
            operationContext.TruckTicketIds.Add(tt.Id);
        }
    }

    private async Task UpdateMaterialApprovals(AccountContactIndexEntity contact, OperationContext operationContext)
    {
        // billing customer contact
        var mas1 = await _materialApprovalProvider.Get(ma => ma.BillingCustomerContactId == contact.Id);
        foreach (var ma in mas1)
        {
            ma.BillingCustomerContact = contact.GetFullName();
            ma.BillingCustomerContactAddress = contact.GetFullAddress();
            await SaveIt(ma);
        }

        // tracking company contact
        var mas2 = await _materialApprovalProvider.Get(ma => ma.TruckingCompanyContactId == contact.Id);
        foreach (var ma in mas2)
        {
            ma.TruckingCompanyContact = contact.GetFullName();
            await SaveIt(ma);
        }

        // third party analytical company contact
        var mas3 = await _materialApprovalProvider.Get(ma => ma.ThirdPartyAnalyticalCompanyContactId == contact.Id);
        foreach (var ma in mas3)
        {
            ma.ThirdPartyAnalyticalCompanyContact = contact.GetFullName();
            await SaveIt(ma);
        }

        // applicant signatories
        var mas4 = await _materialApprovalRepository.GetNativeAsync<MaterialApprovalEntity, Guid>(ma => ma.ApplicantSignatories.Any(s => s.AccountContactId == contact.Id));
        foreach (var ma in mas4)
        {
            if (UpdateSignatoryContact(ma.ApplicantSignatories, contact))
            {
                await SaveIt(ma);
            }
        }

        bool UpdateSignatoryContact(IEnumerable<ApplicantSignatoryEntity> signatoryContacts, AccountContactIndexEntity accountContact)
        {
            if (signatoryContacts == null)
            {
                return false;
            }

            var signatory = signatoryContacts.FirstOrDefault(s => s.AccountContactId == accountContact.Id);
            if (signatory != null)
            {
                signatory.SignatoryName = accountContact.GetFullName();
                signatory.Email = accountContact.Email;
                signatory.PhoneNumber = accountContact.PhoneNumber;
                return true;
            }

            return false;
        }

        async Task SaveIt(MaterialApprovalEntity ma)
        {
            await _materialApprovalProvider.Update(ma, true);
            operationContext.MaterialApprovalIds.Add(ma.Id);
        }
    }

    private class OperationContext
    {
        public HashSet<Guid> BillingConfigurationIds { get; } = new();

        public HashSet<Guid> TruckTicketIds { get; } = new();

        public HashSet<Guid> MaterialApprovalIds { get; } = new();

        public string FormatCounts()
        {
            var bcs = $"Billing Configurations updated: {BillingConfigurationIds.Count}";
            var tts = $"Truck Tickets updated: {TruckTicketIds.Count}";
            var mas = $"Material Approvals updated: {MaterialApprovalIds.Count}";
            var total = $"Total updated: {BillingConfigurationIds.Count + TruckTicketIds.Count + MaterialApprovalIds.Count}";
            return string.Join(Environment.NewLine, bcs, tts, mas, total);
        }
    }
}
