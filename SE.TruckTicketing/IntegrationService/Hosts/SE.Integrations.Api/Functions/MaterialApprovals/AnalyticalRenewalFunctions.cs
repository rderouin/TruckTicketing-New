using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

using SE.Shared.Domain.EmailTemplates;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Mapper;

namespace SE.Integrations.Api.Functions.MaterialApprovals;

public class AnalyticalRenewalFunctions
{
    private readonly IEmailTemplateSender _emailTemplateSender;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly ILogger<AnalyticalRenewalFunctions> _logger;

    private readonly IMapperRegistry _mapper;

    private readonly IProvider<Guid, MaterialApprovalEntity> _materialApprovalProvider;

    public AnalyticalRenewalFunctions(IProvider<Guid, MaterialApprovalEntity> materialApprovalProvider,
                                      IProvider<Guid, FacilityEntity> facilityProvider,
                                      ILogger<AnalyticalRenewalFunctions> logger,
                                      IEmailTemplateSender emailTemplateSender,
                                      IMapperRegistry mapper)
    {
        _materialApprovalProvider = materialApprovalProvider;
        _facilityProvider = facilityProvider;
        _logger = logger;
        _emailTemplateSender = emailTemplateSender;
        _mapper = mapper;
    }

    [Function(nameof(TriggerAnalyticalExpiryAlerts))]
    public async Task TriggerAnalyticalExpiryAlerts([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = "send-email")] HttpRequestData req)
    {
        await RunProcess();
    }

    [Function("AnalyticalRenewalAlerts")]
    public async Task Run([TimerTrigger("%Schedule:AnalyticalRenewalAlerts%", RunOnStartup = false)] TimerInfo timer,
                          FunctionContext context)
    {
        try
        {
            await RunProcess();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured while processing analytical renewal alerts.");
        }
    }

    private async Task RunProcess()
    {
        var warningDays = new[] { 30, 21, 14, 7, 6, 5, 4, 3, 2, 1, 0 };
        var warningThreshold = DateTimeOffset.Now.AddDays(30);
        var expiringMaterialApprovals = (await _materialApprovalProvider.Get(materialApproval => materialApproval.AnalyticalExpiryDate <= warningThreshold &&
                                                                                                 (materialApproval.AnalyticalExpiryEmailActive || materialApproval.AnalyticalExpiryAlertActive)))
           .ToArray();

        var facilities = (await _facilityProvider.GetByIds(expiringMaterialApprovals.Select(materialApproval => materialApproval.FacilityId).Distinct()))
           .ToDictionary(facility => facility.Id);

        var materialApprovals = expiringMaterialApprovals.Where(materialApproval =>
                                                                {
                                                                    var daysUntilAnalyticalExpiry = materialApproval.AnalyticalExpiryDate.Date.Subtract(DateTime.Today).Days;
                                                                    return daysUntilAnalyticalExpiry < 0 ||
                                                                           warningDays.Contains(daysUntilAnalyticalExpiry);
                                                                }).ToArray();

        await SendAnalyticalRenewalEmails(materialApprovals.Where(materialApproval => materialApproval.AnalyticalExpiryEmailActive), facilities);
    }

    private async Task SendAnalyticalRenewalEmails(IEnumerable<MaterialApprovalEntity> materialApprovals, Dictionary<Guid, FacilityEntity> facilities)
    {
        foreach (var materialApproval in materialApprovals)
        {
            try
            {
                var materialApprovalModel = _mapper.Map<MaterialApproval>(materialApproval);
                await _emailTemplateSender.Dispatch(new()
                {
                    TemplateKey = EmailTemplateEventNames.AnalyticalRenewal,
                    Recipients = string.Join("; ", materialApproval.ApplicantSignatories.Select(recipient => recipient.Email)),
                    BccRecipients = facilities[materialApproval.FacilityId].AdminEmail,
                    ContextBag = new()
                    {
                        [nameof(MaterialApproval)] = materialApprovalModel.ToJson(),
                    },
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception occured while trying to dispatch analytical expiry email for {0}", materialApproval.ToJson());
            }
        }
    }
}
