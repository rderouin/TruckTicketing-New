using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Entities.UserProfile;
using SE.Shared.Domain.Product;
using SE.TruckTicketing.Domain.Entities.FacilityService;
using SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.MaterialApproval;

public class MaterialApprovalManager : ManagerBase<Guid, MaterialApprovalEntity>, IMaterialApprovalManager
{
    private readonly IProvider<Guid, AccountContactIndexEntity> _accountContactIndexProvider;

    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IProvider<Guid, FacilityServiceEntity> _facilityServiceProvider;

    private readonly IProvider<Guid, MaterialApprovalEntity> _materialApprovalProvider;

    private readonly IProvider<Guid, ProductEntity> _productProvider;

    private readonly IProvider<Guid, ServiceTypeEntity> _serviceTypeProvider;

    private readonly ITruckTicketPdfRenderer _truckTicketPdfRenderer;

    private readonly IUserProfileManager _userManager;

    public MaterialApprovalManager(ILog logger,
                                   IProvider<Guid, MaterialApprovalEntity> provider,
                                   IProvider<Guid, FacilityServiceEntity> facilityServiceProvider,
                                   IProvider<Guid, ServiceTypeEntity> serviceTypeProvider,
                                   IProvider<Guid, ProductEntity> productProvider,
                                   IProvider<Guid, MaterialApprovalEntity> materialApprovalProvider,
                                   IProvider<Guid, AccountContactIndexEntity> accountContactIndexProvider,
                                   IProvider<Guid, AccountEntity> accountProvider,
                                   IProvider<Guid, FacilityEntity> facilityProvider,
                                   ITruckTicketPdfRenderer truckTicketPdfRenderer,
                                   IUserProfileManager userManager,
                                   IValidationManager<MaterialApprovalEntity> validationManager = null,
                                   IWorkflowManager<MaterialApprovalEntity> workflowManager = null)
        : base(logger, provider, validationManager, workflowManager)
    {
        _facilityServiceProvider = facilityServiceProvider;
        _serviceTypeProvider = serviceTypeProvider;
        _productProvider = productProvider;
        _materialApprovalProvider = materialApprovalProvider;
        _truckTicketPdfRenderer = truckTicketPdfRenderer;
        _accountProvider = accountProvider;
        _facilityProvider = facilityProvider;
        _accountContactIndexProvider = accountContactIndexProvider;
        _userManager = userManager;
    }

    public async Task<List<string>> GetWasteCodeByFacility(Guid facilityId)
    {
        List<string> wasteCodeByFacility = new();

        var results = await _facilityServiceProvider.Get(x => x.FacilityId == facilityId);

        var facilityServiceForFacility = results == null || !results.Any() ? new List<FacilityServiceEntity>() : results;

        foreach (var facilityServiceEntity in facilityServiceForFacility)
        {
            var serviceType = await _serviceTypeProvider.GetById(facilityServiceEntity.ServiceTypeId);
            var productResult = serviceType is null ? new() : await _productProvider.GetById(serviceType.TotalItemId);
            wasteCodeByFacility.AddRange(productResult.Substances.Select(a => a.WasteCode));
        }

        return wasteCodeByFacility.Distinct().ToList();
    }

    public async Task<byte[]> CreateMaterialApprovalPdf(Guid id)
    {
        var accounts = new Dictionary<Guid, AccountEntity>();
        var accountContacts = new Dictionary<Guid, AccountContactEntity>();

        var materialApproval = await _materialApprovalProvider.GetById(id);

        var billingCustomer = await _accountProvider.GetById(materialApproval.BillingCustomerId);
        if (materialApproval.BillingCustomerId != default && !accounts.ContainsKey(materialApproval.BillingCustomerId))
        {
            accounts.Add(materialApproval.BillingCustomerId, billingCustomer);
        }

        if (materialApproval.BillingCustomerContactId != default && !accountContacts.ContainsKey(materialApproval.BillingCustomerContactId.Value))
        {
            var billingContactEntity = billingCustomer?.Contacts.FirstOrDefault(s => s.Id == materialApproval.BillingCustomerContactId);
            accountContacts.Add(materialApproval.BillingCustomerContactId.Value, billingContactEntity);
        }

        var generator = await _accountProvider.GetById(materialApproval.GeneratorId);
        if (materialApproval.GeneratorId != default && !accounts.ContainsKey(materialApproval.GeneratorId))
        {
            accounts.Add(materialApproval.GeneratorId, generator);
        }

        if (materialApproval.GeneratorRepresenativeId != default && !accountContacts.ContainsKey(materialApproval.GeneratorRepresenativeId))
        {
            var generatorRep = generator?.Contacts.FirstOrDefault(s => s.Id == materialApproval.GeneratorRepresenativeId);
            accountContacts.Add(materialApproval.GeneratorRepresenativeId, generatorRep);
        }

        var thirdParty = await _accountProvider.GetById(materialApproval.ThirdPartyAnalyticalCompanyId);
        if (materialApproval.ThirdPartyAnalyticalCompanyId != default && !accounts.ContainsKey(materialApproval.ThirdPartyAnalyticalCompanyId))
        {
            accounts.Add(materialApproval.ThirdPartyAnalyticalCompanyId, thirdParty);
        }

        if (materialApproval.ThirdPartyAnalyticalCompanyContactId != default && !accountContacts.ContainsKey(materialApproval.ThirdPartyAnalyticalCompanyContactId.Value))
        {
            var thirdPartyContact = thirdParty?.Contacts.FirstOrDefault(s => s.Id == materialApproval.ThirdPartyAnalyticalCompanyContactId);
            accountContacts.Add(materialApproval.ThirdPartyAnalyticalCompanyContactId.Value, thirdPartyContact);
        }

        var entity = await _facilityProvider.GetById(materialApproval.FacilityId);

        var signature = string.Empty;

        if (!string.IsNullOrEmpty(materialApproval.SecureRepresentativeId))
        {
            await using var stream = await _userManager.DownloadSignature(materialApproval.SecureRepresentativeId);
            if (stream != null)
            {
                var byteSignature = await stream.ReadAll();
                signature = Convert.ToBase64String(byteSignature);
            }
        }

        var renderedTicket = _truckTicketPdfRenderer.RenderMaterialApprovalPdf(materialApproval, accounts, accountContacts,
                                                                               signature, entity.LocationCode, entity.Type == FacilityType.Fst);

        return renderedTicket;
    }
}
