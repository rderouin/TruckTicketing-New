using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Bunit.Extensions;

using Moq;
using Moq.Protected;

using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.UI.Tests;

public abstract class ServiceTestScope<T> : TestScope<T>, ITestScope<T> where T : class
{
    public readonly string BaseAddress = "https://testdomain.com";

    public string ServiceName = nameof(T);

    public ServiceTestScope()
    {
        HttpHandlerMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                                                                     ItExpr.IsAny<HttpRequestMessage>(),
                                                                     ItExpr.IsAny<CancellationToken>()).ReturnsAsync((HttpRequestMessage r, CancellationToken c) =>
                                                                                                                     {
                                                                                                                         StreamReader sr = null;
                                                                                                                         string actualContent = null;
                                                                                                                         if (r.Content != null)
                                                                                                                         {
                                                                                                                             sr = new(r.Content.ReadAsStream());
                                                                                                                             actualContent = sr.ReadToEnd();
                                                                                                                         }

                                                                                                                         var actualUri = r.RequestUri?.ToString();

                                                                                                                         var actualRouteMethod = r.Method.ToString();

                                                                                                                         if (!ExpectedRequestContent.IsNullOrEmpty() &&
                                                                                                                             actualContent != ExpectedRequestContent)
                                                                                                                         {
                                                                                                                             TestHttpResponseMessage.StatusCode = HttpStatusCode.NotFound;

                                                                                                                             // set content to empty string - used to test RequestContent was as expected
                                                                                                                             TestHttpResponseMessage.Content = new StringContent("");
                                                                                                                         }

                                                                                                                         // If Expected Uri has been set then check that actual uri is equal to expected uri, otherwise return empty string
                                                                                                                         if (!ExpectedUri.IsNullOrEmpty() && ExpectedUri != actualUri)
                                                                                                                         {
                                                                                                                             TestHttpResponseMessage.StatusCode = HttpStatusCode.NotFound;
                                                                                                                             TestHttpResponseMessage.Content = new StringContent("");
                                                                                                                         }

                                                                                                                         // If set, check route method is as expected
                                                                                                                         if (!ExpectedRouteMethod.IsNullOrEmpty() &&
                                                                                                                             ExpectedRouteMethod.ToLower() != actualRouteMethod.ToLower())
                                                                                                                         {
                                                                                                                             TestHttpResponseMessage.StatusCode = HttpStatusCode.BadRequest;
                                                                                                                             TestHttpResponseMessage.Content = new StringContent("");
                                                                                                                         }

                                                                                                                         return TestHttpResponseMessage;
                                                                                                                     });

        TestHttpClient = GetNewMockedClient();
        HttpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>()))
                             .Returns(TestHttpClient);
    }

    public string UniqueServiceName { get; } = Guid.NewGuid().ToString();

    public HttpClient TestHttpClient { get; }

    public HttpResponseMessage TestHttpResponseMessage { get; } = new();

    public string ExpectedRequestContent { get; set; }

    public string ExpectedUri { get; set; }

    public string ExpectedRouteMethod { get; set; }

    public Mock<IHttpClientFactory> HttpClientFactoryMock { get; } = new();

    public Mock<HttpMessageHandler> HttpHandlerMock { get; } = new(MockBehavior.Strict);

    public HttpClient GetNewMockedClient()
    {
        var client = new HttpClient(HttpHandlerMock.Object);
        client.BaseAddress = new(BaseAddress);
        return client;
    }
}
