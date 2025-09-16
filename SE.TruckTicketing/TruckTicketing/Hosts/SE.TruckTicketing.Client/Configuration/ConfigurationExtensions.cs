using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SE.TruckTicketing.Client.Security;

namespace SE.TruckTicketing.Client.Configuration;

public static class ConfigurationExtensions
{
    internal static void ConfigureSecurity(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddSingleton<StateContainer>();
        builder.Services.AddMsalAuthentication(options =>
                                               {
                                                   builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);

                                                   options.ProviderOptions.DefaultAccessTokenScopes.Add("openid");

                                                   options.ProviderOptions.LoginMode = "redirect";
                                               });

        builder.Services.AddAuthorizationCore(config =>
                                              {
                                                  config.AddPolicy(ClaimsAuthorizeView.PolicyName, policy =>
                                                                                                   {
                                                                                                       var cr = new ClaimsRequirement();
                                                                                                       policy.Requirements.Add(cr);
                                                                                                   });

                                                  config.DefaultPolicy = config.GetPolicy(ClaimsAuthorizeView.PolicyName);
                                              });
    }

    internal static void ConfigureHttpClients(this WebAssemblyHostBuilder builder)
    {
        var apiConfigs = builder.Configuration.GetSection("Apis").Get<ApiConfig[]>();

        foreach (var apiConfig in apiConfigs)
        {
            builder.Services.AddHttpClient(apiConfig.Name,
                                           client =>
                                           {
                                               client.BaseAddress = new(apiConfig.BaseUrl);
                                           })
                   .AddHttpMessageHandler(sp =>
                                          {
                                              var x = sp.GetRequiredService<AuthorizationMessageHandler>();
                                              x.ConfigureHandler(new[] { apiConfig.BaseUrl },
                                                                 new[] { apiConfig.Scopes });

                                              return x;
                                          });
        }
    }
}
