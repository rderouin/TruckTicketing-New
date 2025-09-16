using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Azure.Functions.Worker;

using Moq;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Integrations.Api.Functions;
using SE.Shared.Domain.EmailTemplates;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;

namespace SE.Integrations.Api.Tests.Functions;

[TestClass]
public class MaterialApprovalPrepareLoadSummaryReportProcessorTest
{
    private Mock<BindingContext> _bindingContextMock = null!;

    private Dictionary<string, object> _bindingData = null!;

    private Mock<FunctionContext> _functionContextMock = null!;

    private Mock<ILog> _log = null!;

    private Mock<IManager<Guid, MaterialApprovalEntity>> _materialApprovalManagerMock;

    private Mock<ITruckTicketPdfManager> _truckTicketPdfManager = new();

    private Mock<IEmailTemplateSender> _emailTemplateSender = new();
    
    public Mock<IMapperRegistry> MapperMock { get; } = new();

    private MaterialApprovalPrepareLoadSummaryReportProcessor _materialApprovalPrepareLoadSummaryReportProcessor = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        //_entityProcessor = new Mock<IEntityProcessor<CustomerModel>>();
        _log = new();
        //_serviceLocator = new Mock<IIoCServiceLocator>();
        _materialApprovalManagerMock = new();
        _materialApprovalPrepareLoadSummaryReportProcessor =
            new(_log.Object, _materialApprovalManagerMock.Object, _truckTicketPdfManager.Object, _emailTemplateSender.Object, MapperMock.Object);

        _functionContextMock = new();
        _bindingContextMock = new();
        _functionContextMock.Setup(c => c.BindingContext)!.Returns(_bindingContextMock.Object);
        _bindingData = new();
        _bindingContextMock.Setup(c => c.BindingData)!.Returns(_bindingData);
    }

    [TestMethod("Run MaterialApprovalPrepareLoadSummaryReportProcessorTests should not process a message other than Material Approval type.")]
    public async Task MaterialApprovalPrepareLoadSummaryReportProcessor_Run_OtherThanMaterialApproval()
    {
        // Arrange
        var message = ReadTheFile()!;
        UpdateMetadataBasedOnMessage(_bindingData, message);
        _bindingData[MessageConstants.EntityUpdate.MessageType] = "NonMaterialApproval";

        // Act
        await _materialApprovalPrepareLoadSummaryReportProcessor.Run(message, _functionContextMock.Object!);

        // Assert
        _log.Verify(l => l.Information(It.IsAny<Type>()!, It.IsAny<Exception>()!, It.Is<string>(m => m.Contains("Message Type is not MaterialApproval"))!, It.IsAny<object[]>()!));
    }

    [TestMethod("Run TruckTicketAttachmentQueueFunctionsTests for new Material Approval.")]
    public async Task TruckTicketAttachmentQueueFunctions_Run_NewMaterialApprovalAsync()
    {
        // Arrange
        var message = ReadTheFile()!;
        UpdateMetadataBasedOnMessage(_bindingData, message);

        // Act
        Exception exception = null;
        try
        {
            await _materialApprovalPrepareLoadSummaryReportProcessor.Run(message, _functionContextMock.Object!);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        //Assert
        exception.Should().BeNull();
    }

    private string ReadTheFile()
    {
        var assemblyName = typeof(MaterialApprovalPrepareLoadSummaryReportProcessorTest).Assembly.GetName().Name;
        var directory = string.Join(".", "Resources");
        var fileName = "LoadSummaryReportprocessor-Sample.json";
        var fullPath = $@"{assemblyName}.{directory}.{fileName}";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullPath);

        // read contents as string
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static void UpdateMetadataBasedOnMessage(Dictionary<string, object> metadata, string message)
    {
        var model = JsonConvert.DeserializeObject<EntityEnvelopeModel<MaterialApprovalLoadSummary>>(message)!;
        metadata[MessageConstants.EntityUpdate.MessageId] = $"{Guid.NewGuid():N}";
        metadata[MessageConstants.EntityUpdate.MessageType] = model.MessageType;
        metadata[MessageConstants.CorrelationId] = model.CorrelationId;
    }
}
