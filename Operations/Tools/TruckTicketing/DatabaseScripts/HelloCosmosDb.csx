#r "nuget: Newtonsoft.Json, 13.0.3"
#r "nuget: Microsoft.Azure.Cosmos, 3.35.4"

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Cosmos;

var databaseName = "TruckTicketing";
var containerName = "Accounts";

Console.WriteLine("Hello CosmosDb!");

foreach (var arg in Args)
{
    Console.WriteLine(arg);
}

Console.WriteLine();

var q = new Queue<string>(Args);
q.TryDequeue(out var connectionString);
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("The connection string must be provided.");
    return;
}

// cosmos connection
var client = new CosmosClient(connectionString);
var database = client.GetDatabase(databaseName);
var container = database.GetContainer(containerName);

Console.WriteLine($"Connected to database: {databaseName} and container: {containerName}");

var entityType = "UserProfile";

using (var totalFeed = container.GetItemQueryIterator<JObject>($"SELECT COUNT(1) as total FROM c WHERE c.EntityType = '{entityType}'"))
{
    var total = 0;

    if (totalFeed.HasMoreResults)
    {
        var response = await totalFeed.ReadNextAsync();
        total = (int)response.First()["total"];
    }

    Console.WriteLine( $"Total count of {entityType}: {total}" );
}