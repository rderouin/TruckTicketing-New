using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using AutoMapper;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.EDIFieldDefinition;
using SE.Shared.Domain.Entities.EDIFieldValue;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Infrastructure;
using SE.TruckTicketing.Api.Functions;
using SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Domain.Entities.SalesLine;
using SE.TruckTicketing.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

using Trident.Api.Search;
using Trident.Azure.Functions;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.Search;
using Trident.Testing.TestScopes;
using Trident.Validation;

using ApiMapperProfile = SE.TruckTicketing.Api.Configuration.ApiMapperProfile;

namespace SE.TruckTicketing.Api.Tests.Functions;

[TestClass]
public class TruckTicketFunctionsTest
{
    private const string TicketNumber = "DVFST-10151-LF";

    private const string Landfill_FacilityId = "ae62f28e-fd0b-4594-b235-0e254bc4771a";

    private const string Facility = "Canadian Facility";

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_TruckTicketGetById_OkResponse()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var id = Guid.NewGuid();
        var json = JsonConvert.SerializeObject(new TruckTicket
        {
            FacilityId = Guid.NewGuid(),
            SourceLocationId = Guid.NewGuid(),
            SubstanceId = Guid.NewGuid(),
            TruckingCompanyId = Guid.NewGuid(),
            SaleOrderId = Guid.NewGuid(),
            AdditionalServicesEnabled = false,
            TotalVolume = 0,
            TotalVolumePercent = 0,
            MaterialApprovalId = Guid.NewGuid(),
            Status = TruckTicketStatus.New,
            LoadDate = DateTimeOffset.Now,
            GeneratorId = Guid.NewGuid(),
            UnloadOilDensity = 0,
            OilVolume = 0,
            OilVolumePercent = 0,
            WaterVolume = 0,
            WaterVolumePercent = 0,
            SolidVolume = 0,
            SolidVolumePercent = 0,
            GrossWeight = 0,
            TareWeight = 0,
            NetWeight = 0,
            TimeIn = new(),
            TimeOut = new(),
            WellClassification = WellClassifications.Completions,
            BillingCustomerId = Guid.NewGuid(),
            IsDeleted = false,
            AdditionalServices = new()
            {
                new()
                {
                    AdditionalServiceName = "Service 1",
                    AdditionalServiceNumber = "123456",
                    AdditionalServiceQuantity = 4,
                },
            },
        });

        var mockRequest = scope.CreateHttpRequest(null, json);
        var entity = new TruckTicketEntity { Id = id };

        //Setup
        scope.TruckTicketManagerMock.Setup(x => x.GetById(It.IsAny<Guid>(), false)).ReturnsAsync(entity);

        // act
        var response = await scope.InstanceUnderTest.TruckTicketGetById(mockRequest, id);
        var actual = response;
        var actualContent = await response.ReadJsonToObject<TruckTicket>();

        // assert
        Assert.IsNotNull(actual);
        Assert.IsNotNull(actualContent);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_TruckTicketGetById_NotFoundResponse()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var mockRequest = scope.CreateHttpRequest(null, null);
        var id = Guid.NewGuid();
        TruckTicketEntity role = null;
        scope.TruckTicketManagerMock.Setup(x => x.GetById(It.IsAny<Guid>(), false)).ReturnsAsync(role);

        // act
        var response = await scope.InstanceUnderTest.TruckTicketGetById(mockRequest, id);
        var actual = response;

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, actual.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_CreateManualTruckTicket_ShouldReturnSuccess_ForValidRequest()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var createManualTruckTicketRequest = scope.BuildTruckTicketStubCreationRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createManualTruckTicketRequest));
        scope.TruckTicketManagerMock.Setup(manager => manager.CreatePrePrintedTruckTicketStubs(It.IsAny<Guid>(), It.IsAny<int>()));

        // act
        var httpResponseData = await scope.InstanceUnderTest.CreateTruckTicketStubs(functionsRequest);

        // assert
        scope.TruckTicketManagerMock.Verify(manager => manager.CreatePrePrintedTruckTicketStubs(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<Func<IEnumerable<TruckTicketEntity>, Task>>()));
        Assert.AreEqual(HttpStatusCode.OK, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_ReturnsRenderedPdfBytes_IfPdfGenerationIsRequested()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var request = scope.BuildTruckTicketStubCreationRequest();
        var expectedByteArray = Encoding.UTF8.GetBytes("GenerateSamplePdf");
        request.GeneratePdf = true;
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(request));
        scope.TruckTicketPdfRendererMock.Setup(renderer => renderer.RenderTruckTicketStubs(It.IsAny<ICollection<TruckTicketEntity>>()))
             .Returns(expectedByteArray);

        scope.TruckTicketManagerMock.Setup(manager => manager.CreatePrePrintedTruckTicketStubs(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<Func<IEnumerable<TruckTicketEntity>, Task>>()))
             .Callback((Guid _, int __, Func<IEnumerable<TruckTicketEntity>, Task> beforeSave) =>
                       {
                           beforeSave(new List<TruckTicketEntity>
                           {
                               new()
                               {
                                   TicketNumber = TicketNumber,
                                   CountryCode = CountryCode.CA,
                               },
                           });
                       });

        // act
        var httpResponseData = await scope.InstanceUnderTest.CreateTruckTicketStubs(functionsRequest);

        // assert
        scope.TruckTicketManagerMock.Verify(manager => manager.CreatePrePrintedTruckTicketStubs(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<Func<IEnumerable<TruckTicketEntity>, Task>>()));
        Assert.AreEqual(HttpStatusCode.OK, httpResponseData.StatusCode);
        Assert.AreEqual(expectedByteArray.Length, httpResponseData.Body.Length);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_CreateManualTruckTicket_ShouldReturnBadRequest_ValidationRollupException()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var createManualTruckTicketRequest = scope.BuildTruckTicketStubCreationRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createManualTruckTicketRequest));
        scope.TruckTicketManagerMock.Setup(manager => manager.CreatePrePrintedTruckTicketStubs(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<Func<IEnumerable<TruckTicketEntity>, Task>>()))
             .ThrowsAsync(new ValidationRollupException());

        // act
        var httpResponseData = await scope.InstanceUnderTest.CreateTruckTicketStubs(functionsRequest);
        // assert
        scope.LogMock.Verify(log => log.Error<HttpFunctionApiBase<TruckTicket, TruckTicketEntity, Guid>>(It.IsAny<ArgumentException>(), It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_CreateManualTruckTicket_ShouldReturnBadRequest_ArgumentException()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var createManualTruckTicketRequest = scope.BuildTruckTicketStubCreationRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createManualTruckTicketRequest));
        scope.TruckTicketManagerMock.Setup(manager => manager.CreatePrePrintedTruckTicketStubs(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<Func<IEnumerable<TruckTicketEntity>, Task>>()))
             .ThrowsAsync(new ArgumentException());

        // act
        var httpResponseData = await scope.InstanceUnderTest.CreateTruckTicketStubs(functionsRequest);
        // assert
        scope.LogMock.Verify(log => log.Error<HttpFunctionApiBase<TruckTicket, TruckTicketEntity, Guid>>(It.IsAny<ArgumentException>(), It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_CreateManualTruckTicket_ShouldReturnInternalServerError_UnhandledException()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var createManualTruckTicketRequest = scope.BuildTruckTicketStubCreationRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createManualTruckTicketRequest));
        scope.TruckTicketManagerMock.Setup(manager => manager.CreatePrePrintedTruckTicketStubs(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<Func<IEnumerable<TruckTicketEntity>, Task>>()))
             .ThrowsAsync(new());

        // act
        var httpResponseData = await scope.InstanceUnderTest.CreateTruckTicketStubs(functionsRequest);
        // assert
        scope.LogMock.Verify(log => log.Error<HttpFunctionApiBase<TruckTicket, TruckTicketEntity, Guid>>(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_TruckTicketSearch_ShouldReturnSuccessResponse_NoFilterApplied()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var searchCriteria = new SearchCriteriaModel();
        var json = JsonConvert.SerializeObject(searchCriteria);
        var mockRequest = scope.CreateHttpRequest(null, json);
        // act
        var httpResponseData = await scope.InstanceUnderTest.TruckTicketSearch(mockRequest);
        var actualContent = await httpResponseData.ReadJsonToObject<SearchResultsModel<TruckTicket, SearchCriteriaModel>>();
        // assert
        Assert.AreEqual(HttpStatusCode.OK, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_TruckTicketSearch_ShouldReturnAllFacilities_NoFilterApplied()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var searchCriteria = new SearchCriteriaModel();
        var json = JsonConvert.SerializeObject(searchCriteria);
        var mockRequest = scope.CreateHttpRequest(null, json);
        // act
        var httpResponseData = await scope.InstanceUnderTest.TruckTicketSearch(mockRequest);
        var actualContent = await httpResponseData.ReadJsonToObject<SearchResultsModel<TruckTicket, SearchCriteriaModel>>();
        // assert
        Assert.AreEqual(scope.TruckTicketsEntityRecords.Count(), actualContent.Results.Count());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_TruckTicketSearch_ShouldReturnNoData_FilterNotMatchingData()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var searchCriteria = new SearchCriteriaModel();
        searchCriteria.AddFilter("TicketNumber", "test");
        var json = JsonConvert.SerializeObject(searchCriteria);
        var mockRequest = scope.CreateHttpRequest(null, json);

        // act
        var httpResponseData = await scope.InstanceUnderTest.TruckTicketSearch(mockRequest);
        var actualContent = await httpResponseData.ReadJsonToObject<SearchResultsModel<TruckTicket, SearchCriteriaModel>>();
        // assert
        Assert.IsTrue(actualContent.Results.Count() == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_TruckTicketSearch_ShouldReturnValidData_MultipleFiltersApplied()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var searchCriteria = new SearchCriteriaModel();
        searchCriteria.AddFilter("FacilityName", Facility);
        searchCriteria.AddFilter("TicketNumber", TicketNumber);

        var json = JsonConvert.SerializeObject(searchCriteria);
        var mockRequest = scope.CreateHttpRequest(null, json);

        // act
        var httpResponseData = await scope.InstanceUnderTest.TruckTicketSearch(mockRequest);
        var actualContent = await httpResponseData.ReadJsonToObject<SearchResultsModel<TruckTicket, SearchCriteriaModel>>();
        // assert
        Assert.IsTrue(actualContent.Results.Count() > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_TruckTicketPatch_ShouldReturnOkResponse_ForValidRequest()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var TruckTicketRequestRecord = scope.TruckTicketRequestData[0];
        var TruckTicketEntityRecord = scope.TruckTicketsEntityRecords[0];
        var functionsMockRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(TruckTicketRequestRecord));
        scope.TruckTicketManagerMock.Setup(manager => manager.Patch(It.IsAny<Guid>(), It.IsAny<Dictionary<string, object>>(), null, false))
             .ReturnsAsync(new TruckTicketEntity
              {
                  Status = TruckTicketRequestRecord.Status,
                  FacilityId = TruckTicketRequestRecord.FacilityId,
                  FacilityName = TruckTicketRequestRecord.FacilityName,
                  TicketNumber = TruckTicketRequestRecord.TicketNumber,
              });

        // act
        var httpResponseData = await scope.InstanceUnderTest.TruckTicketPatch(functionsMockRequest, Guid.NewGuid());

        // assert
        Assert.AreEqual(HttpStatusCode.OK, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_TruckTicketPatch_ShouldUpdateFacilityId_ForValidRequest()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var TruckTicketRequestRecord = scope.TruckTicketRequestData[0];
        var TruckTicketEntityRecord = scope.TruckTicketsEntityRecords[0];
        var functionsMockRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(TruckTicketRequestRecord));
        scope.TruckTicketManagerMock.Setup(manager => manager.Patch(It.IsAny<Guid>(), It.IsAny<Dictionary<string, object>>(), null, false))
             .ReturnsAsync(new TruckTicketEntity
              {
                  Status = TruckTicketRequestRecord.Status,
                  FacilityId = TruckTicketRequestRecord.FacilityId,
                  TicketNumber = TruckTicketRequestRecord.TicketNumber,
              });

        // act
        var httpResponseData = await scope.InstanceUnderTest.TruckTicketPatch(functionsMockRequest, Guid.NewGuid());
        var actual = httpResponseData;
        var actualContent = await httpResponseData.ReadJsonToObject<TruckTicket>();

        // assert
        Assert.AreEqual(TruckTicketRequestRecord.FacilityId, actualContent.FacilityId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_TruckTicketUpdate_BadRequestResponse()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var id = Guid.NewGuid();
        var json = JsonConvert.SerializeObject(new TruckTicket { Id = Guid.NewGuid() });
        var mockRequest = scope.CreateHttpRequest(null, json);

        // act
        var response = await scope.InstanceUnderTest.TruckTicketUpdate(mockRequest, id);
        var actual = response;

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, actual.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_TruckTicketUpdate_OkResponse()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var id = Guid.NewGuid();
        var json = JsonConvert.SerializeObject(new TruckTicket { Id = id });
        var mockRequest = scope.CreateHttpRequest(null, json);
        var entity = new TruckTicketEntity { Id = id };

        scope.TruckTicketManagerMock.Setup(x => x.Save(It.IsAny<TruckTicketEntity>(), false)).ReturnsAsync(entity);

        // act
        var response = await scope.InstanceUnderTest.TruckTicketUpdate(mockRequest, id);
        var actual = response;
        var actualContent = await response.ReadJsonToObject<TruckTicket>();
        // assert
        Assert.IsNotNull(actual);
        Assert.IsNotNull(actualContent);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_GetMatchingBillingConfigurations_ShouldReturnInternalServerError_ForInternalSystemException()
    {
        // arrange
        var scope = new DefaultScope();
        scope.MapperRegistry = new ServiceMapperRegistry(new List<Profile>());
        var createTruckTicketRequest = scope.BuildTruckTicketRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createTruckTicketRequest));

        // act
        var httpResponseData = await scope.InstanceUnderTest.GetMatchingBillingConfigurations(functionsRequest);

        // assert
        scope.TruckTicketManagerMock.Verify(manager => manager.GetMatchingBillingConfigurations(It.IsAny<TruckTicketEntity>()), Times.Never);
        Assert.AreEqual(HttpStatusCode.InternalServerError, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_GetMatchingBillingConfigurations_ShouldReturnNotFound_ForNoMatchingBillingConfigurations()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var createTruckTicketRequest = scope.BuildTruckTicketRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createTruckTicketRequest));
        scope.TruckTicketManagerMock.Setup(manager => manager.GetMatchingBillingConfigurations(It.IsAny<TruckTicketEntity>()))
             .ReturnsAsync(new List<BillingConfigurationEntity>());

        // act
        var httpResponseData = await scope.InstanceUnderTest.GetMatchingBillingConfigurations(functionsRequest);

        // assert
        scope.TruckTicketManagerMock.Verify(manager => manager.GetMatchingBillingConfigurations(It.IsAny<TruckTicketEntity>()));
        Assert.AreEqual(HttpStatusCode.OK, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_GetMatchingBillingConfigurations_ShouldReturnBadRequest_ForInvalidRequest()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var functionsRequest = scope.CreateHttpRequest(null, null);
        scope.TruckTicketManagerMock.Setup(manager => manager.GetMatchingBillingConfigurations(It.IsAny<TruckTicketEntity>()))
             .ReturnsAsync(new List<BillingConfigurationEntity>());

        // act
        var httpResponseData = await scope.InstanceUnderTest.GetMatchingBillingConfigurations(functionsRequest);

        // assert
        scope.TruckTicketManagerMock.Verify(manager => manager.GetMatchingBillingConfigurations(It.IsAny<TruckTicketEntity>()), Times.Never);
        Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketFunctions_GetMatchingBillingConfigurations_ShouldReturnSuccess_ForValidRequest()
    {
        // arrange
        var scope = new DefaultScope(new Profile[] { new ApiMapperProfile(), new BillingConfigurationMapperProfile() });
        var createTruckTicketRequest = scope.BuildTruckTicketRequest();
        var functionsRequest = scope.CreateHttpRequest(null, JsonConvert.SerializeObject(createTruckTicketRequest));
        scope.TruckTicketManagerMock.Setup(manager => manager.GetMatchingBillingConfigurations(It.IsAny<TruckTicketEntity>()))
             .ReturnsAsync(new List<BillingConfigurationEntity> { scope.DefaultBillingConfiguration });

        // act
        var httpResponseData = await scope.InstanceUnderTest.GetMatchingBillingConfigurations(functionsRequest);

        // assert
        scope.TruckTicketManagerMock.Verify(manager => manager.GetMatchingBillingConfigurations(It.IsAny<TruckTicketEntity>()));
        Assert.AreEqual(HttpStatusCode.OK, httpResponseData.StatusCode);
    }

    private class DefaultScope : HttpTestScope<TruckTicketFunctions>
    {
        public readonly BillingConfigurationEntity DefaultBillingConfiguration =
            new()
            {
                Id = Guid.NewGuid(),
                BillingConfigurationEnabled = true,
                BillingContactAddress = "599 Harry Square",
                BillingContactId = Guid.NewGuid(),
                BillingContactName = "Dr. Eduardo Lesch",
                BillingCustomerAccountId = Guid.NewGuid(),
                CreatedAt = default,
                CreatedBy = string.Empty,
                CreatedById = Guid.NewGuid().ToString(),
                CustomerGeneratorId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33"),
                CustomerGeneratorName = "Kemmer, Maggio and Reynolds",
                Description = null,
                EmailDeliveryEnabled = true,
                FieldTicketsUploadEnabled = false,
                StartDate = null,
                EndDate = null,
                GeneratorRepresentativeId = Guid.NewGuid(),
                IncludeExternalAttachmentInLC = true,
                IncludeInternalAttachmentInLC = true,
                IsDefaultConfiguration = false,
                IncludeForAutomation = true,
                LastComment = "new comment added",
                LoadConfirmationsEnabled = true,
                LoadConfirmationFrequency = null,
                RigNumber = null,
                ThirdPartyBillingContactAddress = "07958 Althea Ford",
                ThirdPartyBillingContactId = Guid.NewGuid(),
                ThirdPartyBillingContactName = "Barbara McClure II",
                ThirdPartyCompanyId = Guid.NewGuid(),
                ThirdPartyCompanyName = null,
                UpdatedAt = default,
                UpdatedBy = "Panth Shah",
                UpdatedById = Guid.NewGuid().ToString(),
                Facilities = null,
                EDIValueData = new(),
                EmailDeliveryContacts = new(),
                Signatories = new(),
                MatchCriteria = new(),
            };

        public DefaultScope(Profile[] mapperProfiles = null)
        {
            MapperRegistry = new ServiceMapperRegistry(mapperProfiles);
            TruckTicketsEntityRecords = GetTestTruckTickets();
            TruckTicketRequestData = GetTruckTicketsRequestData();
            ConfigureTruckTicketMockSearch();
            InstanceUnderTest = new(LogMock.Object,
                                    MapperRegistry,
                                    TruckTicketManagerMock.Object,
                                    TruckTicketPdfRendererMock.Object,
                                    SalesManagerMock.Object,
                                    TruckTicketInvoiceServiceMock.Object,
                                    BillingConfigurationProviderMock.Object,
                                    BlobStorageMock.Object,
                                    SalesLineProviderMock.Object);
        }

        public Mock<ITruckTicketSalesManager> SalesManagerMock { get; } = new();

        public IMapperRegistry MapperRegistry { get; set; }

        public Mock<ILog> LogMock { get; } = new();

        public Mock<ITruckTicketManager> TruckTicketManagerMock { get; } = new();

        public Mock<ILeaseObjectBlobStorage> BlobStorageMock { get; } = new();

        public Mock<ITruckTicketInvoiceService> TruckTicketInvoiceServiceMock { get; } = new();

        public Mock<IProvider<Guid, BillingConfigurationEntity>> BillingConfigurationProviderMock { get; } = new();

        public Mock<IProvider<Guid, SalesLineEntity>> SalesLineProviderMock { get; } = new();

        public Mock<ITruckTicketPdfRenderer> TruckTicketPdfRendererMock { get; } = new();

        public List<TruckTicketEntity> TruckTicketsEntityRecords { get; }

        public List<TruckTicket> TruckTicketRequestData { get; }

        public TruckTicketStubCreationRequest BuildTruckTicketStubCreationRequest()
        {
            return new()
            {
                FacilityId = Guid.NewGuid(),
                Count = 5,
            };
        }

        public TruckTicket BuildTruckTicketRequest()
        {
            return new()
            {
                Id = Guid.NewGuid(),
                Acknowledgement = "",
                AdditionalServicesEnabled = true,
                BillOfLading = "",
                BillingCustomerId = Guid.NewGuid(),
                BillingCustomerName = "",
                ClassNumber = "",
                CountryCode = CountryCode.CA,
                Destination = "",
                FacilityId = Guid.NewGuid(),
                FacilityName = "",
                FacilityServiceSubstanceId = Guid.NewGuid(),
                ServiceTypeId = Guid.NewGuid(),
                GeneratorId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33"),
                GeneratorName = "",
                GrossWeight = 1,
                IsDeleted = true,
                IsDow = true,
                Level = "",
                LoadDate = DateTimeOffset.UtcNow,
                LocationOperatingStatus = LocationOperatingStatus.Completions,
                ManifestNumber = "",
                MaterialApprovalId = Guid.NewGuid(),
                MaterialApprovalNumber = "",
                NetWeight = 1,
                OilVolume = 1,
                OilVolumePercent = 1,
                Quadrant = "",
                SaleOrderId = Guid.NewGuid(),
                SaleOrderNumber = "",
                Stream = Stream.Pipeline,
                ServiceType = "",
                SolidVolume = 1,
                SolidVolumePercent = 1,
                Source = TruckTicketSource.Manual,
                SourceLocationFormatted = "",
                SourceLocationId = Guid.NewGuid(),
                SourceLocationName = "",
                SpartanProductParameterDisplay = "",
                SpartanProductParameterId = Guid.NewGuid(),
                Status = TruckTicketStatus.Hold,
                SubstanceId = Guid.NewGuid(),
                SubstanceName = "",
                TareWeight = 1,
                TicketNumber = "",
                TimeIn = DateTimeOffset.UtcNow,
                TimeOut = DateTimeOffset.UtcNow,
                Tnorms = "",
                TotalVolume = 1,
                TotalVolumePercent = 1,
                TrackingNumber = "",
                TrailerNumber = "",
                TruckNumber = "",
                TruckingCompanyId = Guid.NewGuid(),
                TruckingCompanyName = "",
                UnNumber = "",
                UnloadOilDensity = 1,
                UpdatedAt = DateTimeOffset.UtcNow,
                UploadFieldTicket = true,
                ValidationStatus = TruckTicketValidationStatus.Valid,
                WaterVolume = 1,
                WaterVolumePercent = 1,
                WellClassification = WellClassifications.Completions,
                AdditionalServices = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AdditionalServiceName = "Service 1",
                        AdditionalServiceNumber = "123",
                        AdditionalServiceQuantity = 1,
                        IsPrimarySalesLine = true,
                        ProductId = Guid.NewGuid(),
                    },
                },
                Attachments = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Container = "",
                        File = "Sample-Work-Ticket-US-Stub.pdf",
                        Path = "66e6ba1e-afc2-4a87-923e-41335a92b98f/Sample-Work-Ticket-US-Stub.pdf",
                    },
                },
                BillingContact = new()
                {
                    AccountContactId = Guid.NewGuid(),
                    Address = "",
                    Email = "",
                    Name = "",
                    PhoneNumber = "",
                },
                EdiFieldValues = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        EDIFieldDefinitionId = Guid.NewGuid(),
                        EDIFieldName = "Invoice Number",
                        EDIFieldValueContent = "123",
                    },
                },
                Signatories = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AccountContactId = Guid.NewGuid(),
                        ContactAddress = "512 Jamarcus Islands",
                        ContactEmail = "Waldo_Harvey32@yahoo.com",
                        ContactName = "Kent Purdy",
                        ContactPhoneNumber = "742-548-1249",
                        IsAuthorized = true,
                    },
                },
            };
        }

        public void ConfigureTruckTicketMockSearch()
        {
            var queryResult = GetTestTruckTickets();
            TruckTicketManagerMock.Setup(x => x.Search(It.IsAny<SearchCriteria<TruckTicketEntity>>(), false))
                                  .ReturnsAsync((SearchCriteria<TruckTicketEntity> criteria, bool loadChildren) =>
                                                {
                                                    var filterBy = criteria.Filters;
                                                    foreach (var key in filterBy.Keys)
                                                    {
                                                        var parameterExpression = Expression.Parameter(typeof(TruckTicketEntity));
                                                        var property = Expression.Property(parameterExpression, key);
                                                        var constant = Expression.Constant(criteria.Filters[key]);
                                                        var expression = Expression.Equal(property, constant);
                                                        var lambda = Expression.Lambda<Func<TruckTicketEntity, bool>>(expression, parameterExpression);
                                                        if (queryResult.Count > 0)
                                                        {
                                                            queryResult = queryResult.Where(lambda.Compile()).ToList();
                                                        }
                                                    }

                                                    var result = new SearchResults<TruckTicketEntity, SearchCriteria>
                                                    {
                                                        Results = queryResult.ToList(),
                                                    };

                                                    return result;
                                                });
        }

        private List<TruckTicketEntity> GetTestTruckTickets()
        {
            return new()
            {
                new()
                {
                    FacilityId = Guid.Parse(Landfill_FacilityId),
                    FacilityName = Facility,
                    TicketNumber = TicketNumber,
                },
            };
        }

        private List<TruckTicket> GetTruckTicketsRequestData()
        {
            return new()
            {
                new()
                {
                    FacilityId = Guid.NewGuid(),
                    FacilityName = Facility,
                    TicketNumber = TicketNumber,
                },
            };
        }

        public void CleanMapperRegistry()
        {
            MapperRegistry = new ServiceMapperRegistry(new List<Profile>());
        }
    }

    private class BillingConfigurationMapperProfile : BaseMapperProfile
    {
        public BillingConfigurationMapperProfile()
        {
            CreateAuditableEntityMap<BillingConfiguration, BillingConfigurationEntity>()
               .ReverseMap();

            CreateOwnedEntityMap<EmailDeliveryContact, EmailDeliveryContactEntity>()
               .ReverseMap();

            CreateOwnedEntityMap<MatchPredicate, MatchPredicateEntity>()
               .ReverseMap();

            CreateOwnedEntityMap<EDIFieldValue, EDIFieldValueEntity>()
               .ReverseMap();

            CreateAuditableEntityMap<EDIFieldDefinition, EDIFieldDefinitionEntity>()
               .ReverseMap();

            CreateOwnedEntityMap<SignatoryContact, SignatoryContactEntity>()
               .ReverseMap();
        }
    }
}
