using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.EmailTemplates;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

using Trident.Contracts;
using Trident.Extensions;
using Trident.Logging;
using Trident.Mapper;

namespace SE.Integrations.Api.Functions;

public class MaterialApprovalPrepareLoadSummaryReportProcessor
{
    private readonly IEmailTemplateSender _emailTemplateSender;

    private readonly ILog _logger;

    private readonly IMapperRegistry _mapper;

    private readonly IManager<Guid, MaterialApprovalEntity> _materialApprovalManager;

    private readonly ITruckTicketPdfManager _truckTicketPdfManager;

    public MaterialApprovalPrepareLoadSummaryReportProcessor(ILog logger,
                                                             IManager<Guid, MaterialApprovalEntity> materialApprovalManager,
                                                             ITruckTicketPdfManager truckTicketPdfManager,
                                                             IEmailTemplateSender emailTemplateSender,
                                                             IMapperRegistry mapper)
    {
        _logger = logger;
        _materialApprovalManager = materialApprovalManager;
        _truckTicketPdfManager = truckTicketPdfManager;
        _emailTemplateSender = emailTemplateSender;
        _mapper = mapper;
    }

    [Function("MaterialApprovalPrepareLoadSummaryReportProcessor")]
    public async Task Run([ServiceBusTrigger(ServiceBusConstants.Queue.MaterialApprovalLoadSummaryReport, Connection = ServiceBusConstants.PrivateServiceBusNamespace)] string message,
                          FunctionContext context)
    {
        var messageId = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.EntityUpdate.MessageId);
        var messageType = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.EntityUpdate.MessageType);
        var correlationId = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.CorrelationId);
        try
        {
            var messageTypeValue = messageType as string;
            // only process MaterialApprovalLoadSummaryReport messages
            if (messageTypeValue == nameof(ServiceBusConstants.EntityMessageTypes.MaterialApprovalLoadSummaryReport))
            {
                if (string.IsNullOrWhiteSpace(messageTypeValue))
                {
                    _logger.Warning(messageTemplate: $"Message Type is blank. ({GetLogMessageContext()})");
                    return;
                }

                var materialApproval = JsonConvert.DeserializeObject<EntityEnvelopeModel<MaterialApprovalLoadSummary>>(message);

                var materialApprovalEntity = await _materialApprovalManager.GetById(materialApproval?.Payload.MaterialApprovalId);

                if (materialApprovalEntity != null)
                {
                    var today = DateTime.Today;
                    var previousMonth = today.AddMonths(-1);
                    var previousMonthStartDate = new DateTime(previousMonth.Year, previousMonth.Month, 1);
                    var daysInPreviousMonth = DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);
                    var previousMonthEndDate = new DateTime(previousMonth.Year, previousMonth.Month, daysInPreviousMonth);

                    var (startDate, endDate) = materialApprovalEntity.LoadSummaryReportFrequency switch
                                               {
                                                   LoadSummaryReportFrequency.Daily => (today.AddDays(-1), today.AddDays(-1)),
                                                   LoadSummaryReportFrequency.Weekly => (today.AddDays(-7), today.AddDays(-1)),
                                                   LoadSummaryReportFrequency.Monthly =>
                                                       materialApprovalEntity.LoadSummaryReportFrequencyMonthlyDate == null
                                                           ? (previousMonthStartDate, previousMonthEndDate)
                                                           : (today.AddMonths(-1), today.AddDays(-1)),
                                                   _ => (today.AddDays(-1), today.AddDays(-1)),
                                               };

                    var sourceLocationName = materialApprovalEntity.SourceLocation; //Lindsay, June 19, 2023
                    var facilityName = materialApprovalEntity.Facility;
                    var legalEntity = materialApprovalEntity.LegalEntity;

                    var response = await _truckTicketPdfManager.CreateLoadSummaryReport(new()
                    {
                        FromDate = startDate,
                        ToDate = endDate,
                        ReportExecutionTime = new DateTimeOffset(DateTime.UtcNow).ToAlbertaOffsetBasedonDST().DateTime,
                        MaterialApprovalIds = new() { materialApprovalEntity.Id },
                        SourceLocationName = sourceLocationName,
                        FacilityName = facilityName,
                        LegalEntity = legalEntity,
                    });

                    if (response is { Length: > 0 })
                    {
                        var signatoryEmails = string.Join("; ", materialApprovalEntity.LoadSummaryReportRecipients.Where(s => s.Email.HasText()).Select(s => s.Email));
                        var pdfData = response.ToJson();
                        await SendEmail(EmailTemplateEventNames.LoadSummaryReport, signatoryEmails, pdfData, materialApprovalEntity);
                    }
                    else
                    {
                        _logger.Warning(messageTemplate: $"No load data for Material Approval ID found. (MaterialApproval: {materialApproval.Payload.MaterialApprovalId})");
                    }
                }
                else
                {
                    _logger.Warning(messageTemplate: $"Material Approval ID is not valid. (MaterialApproval: {materialApproval.Payload.MaterialApprovalId})");
                }
            }
            else
            {
                _logger.Information(messageTemplate: $"Message Type is not MaterialApproval. ({GetLogMessageContext()})");
            }
        }
        catch (Exception e)
        {
            _logger.Error(exception: e, messageTemplate: $"Unable to process a MaterialApproval message. (msgId: {GetLogMessageContext()})");
            throw;
        }

        string GetLogMessageContext()
        {
            return JsonConvert.SerializeObject(new
            {
                messageId,
                messageType,
                correlationId,
            });
        }
    }

    private async Task SendEmail(string templateKey,
                                 string recipients,
                                 string pdfData,
                                 MaterialApprovalEntity materialApproval)
    {
        var materialApprovalModel = _mapper.Map<MaterialApproval>(materialApproval);
        await _emailTemplateSender.Dispatch(new()
        {
            TemplateKey = templateKey,
            Recipients = recipients,
            CcRecipients = string.Empty,
            BccRecipients = string.Empty,
            AdHocNote = string.Empty,
            ContextBag = new()
            {
                ["PDF"] = pdfData,
                [nameof(MaterialApproval)] = materialApprovalModel.ToJson(),
            },
        });
    }
}
