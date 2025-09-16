using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.TruckTicketing.Domain.Configuration;

using Trident.Contracts;
using Trident.Contracts.Configuration;
using Trident.Logging;

namespace SE.Integrations.Api.Functions;

public class MaterialApprovalLoadSummaryReportProcessor
{
    private readonly IAccountSettingsConfiguration _accountSettingsConfiguration;

    private readonly IAppSettings _appSettings;

    private readonly ILog _logger;

    private readonly IManager<Guid, MaterialApprovalEntity> _materialApprovalManager;

    public MaterialApprovalLoadSummaryReportProcessor(ILog logger,
                                                      IAccountSettingsConfiguration accountSettingsConfiguration,
                                                      IManager<Guid, MaterialApprovalEntity> materialApprovalManager,
                                                      IAppSettings appSettings)
    {
        _logger = logger;
        _accountSettingsConfiguration = accountSettingsConfiguration;
        _materialApprovalManager = materialApprovalManager;
        _appSettings = appSettings;
    }

    /// <summary>
    ///     Runs the specified timer.
    /// </summary>
    /// <param name="timer">The timer.</param>
    /// <param name="context">The context.</param>
    [Function("MaterialApprovalLoadSummaryReportProcessor")]
    public async Task<DispatchedMessages> Run([TimerTrigger("30 15 * * *", RunOnStartup = false)] TimerInfo timer, FunctionContext context)
    {
        var messages = new List<EntityEnvelopeModel<MaterialApprovalLoadSummary>>();
        try
        {
            messages = await RunMaterialApprovalLoadSummaryReportProcessor();
        }

        catch (Exception ex)
        {
            // log exception and throw to put the message back on the bus for retry.
            _logger.Error(exception: ex, messageTemplate: "Material Approval Load Summary Report - Exception");
        }
        finally
        {
            _logger.Information(messageTemplate: "Material Approval Load Summary Report Processor - Complete");
        }

        return new()
        {
            MaterialApprovalIds = messages.Select(x => x),
        };
    }

    private async Task<List<EntityEnvelopeModel<MaterialApprovalLoadSummary>>> RunMaterialApprovalLoadSummaryReportProcessor()
    {
        try
        {
            var runMaterialApprovalLoadSummaryReportProcessor = _accountSettingsConfiguration.RunMaterialApprovalLoadSummaryReportProcessor;
            _logger.Information(messageTemplate:
                                $"RunMaterialApprovalLoadSummaryReportProcess - Run Material Approval Load Summary report [runMaterialApprovalLoadSummaryReportProcessor: {runMaterialApprovalLoadSummaryReportProcessor}] - Starting");

            var list = new List<EntityEnvelopeModel<MaterialApprovalLoadSummary>>();

            if (runMaterialApprovalLoadSummaryReportProcessor)
            {
                var day = DateTime.Today.Day;
                var weekday = DateTime.Today.DayOfWeek;

                var searchMaterialApprovalResults = (await _materialApprovalManager.Get(x => x.LoadSummaryReport == true &&
                                                                                             (x.LoadSummaryReportFrequency == LoadSummaryReportFrequency.Daily ||
                                                                                              (x.LoadSummaryReportFrequency == LoadSummaryReportFrequency.Weekly &&
                                                                                               x.LoadSummaryReportFrequencyWeekDay == weekday) ||
                                                                                              x.LoadSummaryReportFrequencyMonthlyDate == day ||
                                                                                              (day == 1 && x.LoadSummaryReportFrequency == LoadSummaryReportFrequency.Monthly &&
                                                                                               x.LoadSummaryReportFrequencyMonthlyDate == null)))).ToList();

                _logger.Information(messageTemplate: $"RunMaterialApprovalLoadSummaryReportProcess - Search Total Records: {searchMaterialApprovalResults.Count}");

                foreach (var entity in searchMaterialApprovalResults)
                {
                    list.Add(ProcessEntity(entity));
                }
            }

            _logger.Information(messageTemplate: "RunMaterialApprovalLoadSummaryReportProcess - Finished", propertyValues:
                                new Dictionary<string, object> { { "Details", string.Empty } });

            return list;
        }
        catch (Exception ex)
        {
            _logger.Error(exception: ex, messageTemplate: "RunMaterialApprovalLoadSummaryReportProcess - Exception");
            throw;
        }
    }

    private EntityEnvelopeModel<MaterialApprovalLoadSummary> ProcessEntity(MaterialApprovalEntity entity)
    {
        var model = new EntityEnvelopeModel<MaterialApprovalLoadSummary>
        {
            Payload = new()
            {
                MaterialApprovalId = entity.Id,
                Container = "materialapproval-loadsummary",
            },
            EnterpriseId = entity.Id,
            Source = "Attachments",
            CorrelationId = Guid.NewGuid().ToString(),
            MessageType = nameof(ServiceBusConstants.EntityMessageTypes.MaterialApprovalLoadSummaryReport),
            Operation = "Process",
        };

        return model;
    }

    public class DispatchedMessages
    {
        [ServiceBusOutput(ServiceBusConstants.Queue.MaterialApprovalLoadSummaryReport,
                          Connection = ServiceBusConstants.PrivateServiceBusNamespace)]
        public IEnumerable<EntityEnvelopeModel<MaterialApprovalLoadSummary>> MaterialApprovalIds { get; set; }
    }
}
