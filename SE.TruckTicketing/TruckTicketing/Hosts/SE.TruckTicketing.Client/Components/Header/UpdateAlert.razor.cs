using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

using SE.Shared.Common.Extensions;

namespace SE.TruckTicketing.Client.Components.Header;

public partial class UpdateAlert
{
    private const double TimerDuration = 60000.0;

    private string _cachedContent;

    private HttpClient _httpClient;

    private Timer _pollingTimer;

    [Inject]
    public IHttpClientFactory HttpClientFactory { get; set; }

    [Inject]
    public NavigationManager NavigationManager { get; set; }

    private bool ShowAlert { get; set; }

    protected override Task OnInitializedAsync()
    {
        // HTTP client
        _httpClient = HttpClientFactory.CreateClient();
        _httpClient.BaseAddress = new(NavigationManager.BaseUri);

        // polling timer
        _pollingTimer = new(TimerDuration);
        _pollingTimer.Elapsed += PollingTimerElapsed;
        _pollingTimer.AutoReset = true;
        _pollingTimer.Start();

        return base.OnInitializedAsync();
    }

    private async void PollingTimerElapsed(object sender, ElapsedEventArgs e)
    {
        try
        {
            // alert is shown, no need to poll
            if (ShowAlert)
            {
                return;
            }

            // poll the version file
            var request = new HttpRequestMessage(HttpMethod.Get, "app-version.json");
            request.SetBrowserRequestCache(BrowserRequestCache.NoStore);
            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                // read as is
                var newContent = await response.Content.ReadAsStringAsync();

                // if the component is already initialized
                if (_cachedContent.HasText())
                {
                    // compare fetched text with cached text
                    if (_cachedContent != newContent)
                    {
                        // show alert if contents are different
                        ShowAlert = true;
                        StateHasChanged();
                    }
                }
                else
                {
                    // init
                    _cachedContent = newContent;
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
        }
    }
}
