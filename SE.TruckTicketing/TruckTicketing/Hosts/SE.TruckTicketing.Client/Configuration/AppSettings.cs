using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

using Trident.Contracts.Configuration;
using Trident.Extensions;

namespace SE.TruckTicketing.Client.Configuration;

public class AppSettings : IAppSettings
{
    protected IConfigurationRoot _appSettings;

    private IConfigurationSection appSettingsSection;

    private IDisposable disposibleRefHandled;

    private IChangeToken reloadToken;

    private Dictionary<string, string> settings;

    private List<string> settingsIndex;

    public AppSettings(IConfigurationBuilder configbuilder)
    {
        Init(configbuilder);
    }

    public static bool CoalesceEnvironmentVariables { get; set; } = false;

    public string this[string key] => _appSettings[key];

    public T GetSection<T>(string sectionName = null)
        where T : class
    {
        var section = _appSettings.Get<T>();
        _appSettings.Bind(sectionName ?? typeof(T).Name, section);
        return section;
    }

    public string GetKeyOrDefault(string key, string defaultValue = default)
    {
        var value = DictionaryExtensions.GetValueOrDefault(settings, key, defaultValue);
        if (CoalesceEnvironmentVariables && value == default)
        {
            value = Environment.GetEnvironmentVariable(key);
        }

        return value;
    }

    public IConnectionStringSettings ConnectionStrings { get; private set; }

    protected virtual void Init(IConfigurationBuilder configurationBuilder)
    {
        _appSettings = configurationBuilder.Build();
        ConfigChangedCallback(null);
    }

    protected void ConfigChangedCallback(object state)
    {
        using (disposibleRefHandled) { }

        appSettingsSection = _appSettings.GetSection("AppSettings");
        reloadToken = appSettingsSection.GetReloadToken();
        disposibleRefHandled = reloadToken.RegisterChangeCallback(ConfigChangedCallback, null);
        settings = appSettingsSection.GetChildren().ToDictionary(x => x.Key, x => x.Value);
        settingsIndex = settings.Keys.ToList();
    }
}
