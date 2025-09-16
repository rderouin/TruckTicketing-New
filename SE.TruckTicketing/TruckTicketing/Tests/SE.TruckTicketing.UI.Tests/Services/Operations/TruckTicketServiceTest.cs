using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.Services;

using Trident.Contracts.Api.Client;
using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Tests.Services.Operations;

[TestClass]
public class TruckTicketServiceTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public void TruckTicketService_ScopeInvoke_ValidInstanceType()
    {
        // arrange
        var scope = new DefaultScope();
        // act / assert
        Assert.IsInstanceOfType(scope.InstanceUnderTest, typeof(ServiceBase<TruckTicketService, TruckTicket, Guid>));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task BulkPrePrintedTruckTicketCreatet_Should_Invoke_CreateRequest_With_The_Expected_Request()
    {
        // arrange
        var scope = new DefaultScope();
        scope.ExpectedUri = $"{scope.BaseAddress}/trucktickets/";
        scope.ExpectedRouteMethod = "POST";
        scope.ExpectedRequestContent = JsonConvert.SerializeObject(scope.TruckTicketStubCreationRequest);

        // act
        var result = await scope.InstanceUnderTest.CreateTruckTicketStubs(scope.TruckTicketStubCreationRequest);

        // assert
        Assert.IsInstanceOfType(result, typeof(Response<TruckTicketStubCreationRequest>));
        Assert.IsNotNull(result);
    }

    private class DefaultScope : ServiceTestScope<ITruckTicketService>
    {
        public DefaultScope()
        {
            TruckTicketStubCreationRequest = CreateTruckTicketCreateRequest();
            InstanceUnderTest = new TestTruckTicketService(UniqueServiceName,
                                                           LoggerMock.Object,
                                                           HttpClientFactoryMock.Object);
        }

        public Mock<ILogger<TruckTicketService>> LoggerMock { get; } = new();

        public TruckTicketStubCreationRequest TruckTicketStubCreationRequest { get; } = new();

        public TruckTicketStubCreationRequest CreateTruckTicketCreateRequest()
        {
            return new()
            {
                FacilityId = Guid.NewGuid(),
                Count = 5,
            };
        }

        [Service("TestService", Service.Resources.trucktickets)]
        private class TestTruckTicketService : TruckTicketService
        {
            public TestTruckTicketService(string serviceName,
                                          ILogger<TruckTicketService> logger,
                                          IHttpClientFactory httpClientFactory) : base(logger,
                                                                                       httpClientFactory)
            {
                HttpServiceName = serviceName;
            }
        }
    }
}
