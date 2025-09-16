using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SE.BillingService.Domain.Tests;

internal sealed class PlaceholderHttpClient : HttpClient
{
    private const string PlaceholderBaseAddress = "http://localhost";

    public PlaceholderHttpClient(HttpContent responseContent = null) : this(new() { ResponseContent = responseContent }, true)
    {
        BaseAddress = new(PlaceholderBaseAddress);
    }

    private PlaceholderHttpClient(PlaceholderHttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler)
    {
        Handler = handler;
    }

    public PlaceholderHttpMessageHandler Handler { get; set; }

    public sealed class PlaceholderHttpMessageHandler : HttpMessageHandler
    {
        public bool IsCalled { get; set; }

        public HttpRequestMessage LastRequestMessage { get; set; }

        public HttpContent ResponseContent { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            IsCalled = true;
            LastRequestMessage = request;

            var response = request.CreateResponse(HttpStatusCode.OK);
            if (ResponseContent != null)
            {
                response.Content = ResponseContent;
            }

            return Task.FromResult(response);
        }
    }
}
