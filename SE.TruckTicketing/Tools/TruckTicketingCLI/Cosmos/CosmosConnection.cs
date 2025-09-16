using System;
using System.Collections.Generic;
using System.Linq;

namespace TruckTicketingCLI.Cosmos;

public class CosmosConnection
{
    public CosmosConnection(string connectionString)
    {
        ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

        var kvps = GetKeyValuePairs(connectionString);

        string AssertKeyValue(string name)
        {
            if (!kvps.ContainsKey(name))
            {
                throw new ArgumentException($"Connection string does not contain '{name}'");
            }

            return kvps[name];
        }

        AccountEndpoint = AssertKeyValue(nameof(AccountEndpoint));
        AccountKey = AssertKeyValue(nameof(AccountKey));
        Database = AssertKeyValue(nameof(Database));
    }

    public string ConnectionString { get; }

    public string AccountEndpoint { get; }

    public string AccountKey { get; }

    public string Database { get; }

    private IDictionary<string, string> GetKeyValuePairs(string connectionString)
    {
        var result = connectionString
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(str => str.Split('=', StringSplitOptions.RemoveEmptyEntries))
                    .ToDictionary(ss => ss[0], ss => ss[1]);

        return result;
    }
}
