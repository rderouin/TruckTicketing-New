using System.Collections.Generic;

namespace SE.TruckTicketing.Api.Configuration;

public interface IConfigurationReader
{
    bool IsKeyExists(string key);

    string ReadKeyValue(string key);

    IDictionary<string, string> ReadKeysValues();
}
