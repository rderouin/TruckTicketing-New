using System;
using System.Collections.Generic;

namespace SE.TruckTicketing.Api.Configuration;

public class Settings
{
    public string AzureAppConfigurationConnectionString { get; set; }

    public int ServiceDelayInSeconds { get; set; }

    public int CacheExpirationInSeconds { get; set; }

    public TimeSpan CacheExpiration => TimeSpan.FromSeconds(CacheExpirationInSeconds);

    public ICollection<string> Keys { get; set; }

    public string TenantId { get; set; }
}
