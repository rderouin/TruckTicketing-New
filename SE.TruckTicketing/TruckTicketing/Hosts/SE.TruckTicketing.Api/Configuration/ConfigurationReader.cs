using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using SE.TruckTicketing.Contracts.Api.Exceptions;

namespace SE.TruckTicketing.Api.Configuration;

public class ConfigurationReader : IConfigurationReader
{
    private readonly IConfiguration _configuration;

    private readonly IOptions<Settings> _options;

    public ConfigurationReader(IOptions<Settings> options, IConfiguration configuration)
    {
        _options = options;
        _configuration = configuration;
    }

    public bool IsKeyExists(string key)
    {
        return _options.Value.Keys.SingleOrDefault(k => key == k) != null;
    }

    public IDictionary<string, string> ReadKeysValues()
    {
        return _options.Value.Keys.ToDictionary(x => x, x => SafeReadKeyValue(x));
    }

    public string ReadKeyValue(string key)
    {
        if (!IsKeyExists(key))
        {
            throw AppConfigurationException.KeyNotFound(key);
        }

        return _configuration[key];
    }

    private string SafeReadKeyValue(string key)
    {
        return IsKeyExists(key) ? _configuration[key] : null;
    }
}
