// dotnet tool install -g dotnet-script
// dotnet script .\onetime_updateLCBillingFrequency_TicketByTicket.csx "<connection-string>" "<True|False>"
#r "nuget: Newtonsoft.Json, 13.0.3"
#r "nuget: Microsoft.Azure.Cosmos, 3.35.4"
#r "nuget: Polly, 8.0.0"

using Polly;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Cosmos;

var q = new Queue<string>(Args);
q.TryDequeue(out var connectionString);
if (string.IsNullOrWhiteSpace(connectionString))
{
   Console.WriteLine("The connection string must be provided.");
   return;
}
q.TryDequeue(out var liveModeStr);
if (string.IsNullOrWhiteSpace(liveModeStr))
{
   Console.WriteLine("The liveMode must be provided and should be either true or false.");
   return;
}
if (!bool.TryParse(liveModeStr, out var liveMode))
{
   Console.WriteLine("liveMode should be either true or false");
   return;
}










// vars
var databaseName = "TruckTicketing";
var containerName = "Operations";

var backupFileName = $"onetime_updateLCBillingFrequency_TicketByTicket_{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.json";

int maxItemsToUpdateInParallel = 5;
int counter = 0;
int totalUpdated = 0;
int bounces = 0;
var policy = Policy
    .Handle<CosmosException>(e => e.StatusCode == HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(
        10,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        (e, t) => Interlocked.Increment(ref bounces));

// cosmos connection
var client = new CosmosClient(connectionString);
var database = client.GetDatabase(databaseName);
var container = database.GetContainer(containerName);

Console.WriteLine($"********** liveMode: ${liveMode} **********");
Console.WriteLine($"********** Started at: {DateTime.Now}");

// await Test_GetBillingConfigurations();
// await Test_GetLoadConfirmationDocumentTypes();
// await Test_LoadLoadConfirmations();
// await Test_LoadLoadConfirmationsForMultipleBillingConfiguration();

Console.WriteLine();

await UpdateLoadConfirmations(database);

Console.WriteLine();

Console.WriteLine($"********** Finished at: {DateTime.Now}");
Console.WriteLine($"********** liveMode: ${liveMode} **********");

# region Test

async Task Test_GetBillingConfigurations()
{
    var billingConfigurations = await GetBillingConfigurations(database);
    PrintLookupData(billingConfigurations, (lookupItem) => Console.WriteLine($"{lookupItem.Id} {lookupItem.Name} {lookupItem.LoadConfirmationFrequency}"));

    Console.WriteLine();
}

async Task Test_GetLoadConfirmationDocumentTypes()
{
    var loadConfirmationDocumentTypes = await GetLoadConfirmationDocumentTypes(database);
    PrintLookupData(loadConfirmationDocumentTypes, (lookupItem) => Console.WriteLine($"{lookupItem.DocumentType}"));

    Console.WriteLine();
}

async Task Test_LoadLoadConfirmations()
{
    var documentType = "LoadConfirmation|082023";

    BillingConfiguration[] billingConfigurations = {
            new BillingConfiguration { Id = "bf63ca6b-646c-43c8-8517-bb168a8e5ac2", LoadConfirmationFrequency = "TicketByTicket", Name = "Test1" },
            new BillingConfiguration { Id = "4b0f6312-99ab-4644-954b-746b608bde76", LoadConfirmationFrequency = "TicketByTicket", Name = "Test2" }
    };

    var billingConfiguration = billingConfigurations[0];

    var loadConfirmations = await LoadLoadConfirmations(database, documentType, billingConfiguration);
    PrintLookupData(loadConfirmations, (lookupItem) => Console.WriteLine($"{lookupItem.Id} {lookupItem.BillingConfigurationId} {lookupItem.BillingConfigurationName} {lookupItem.Frequency} {lookupItem.DocumentType} {lookupItem.Status} {lookupItem.DocItemId}"));
}

async Task Test_LoadLoadConfirmationsForMultipleBillingConfiguration()
{
    var documentType = "LoadConfirmation|082023";

    BillingConfiguration[] billingConfigurations = {
            new BillingConfiguration { Id = "bf63ca6b-646c-43c8-8517-bb168a8e5ac2", LoadConfirmationFrequency = "TicketByTicket", Name = "Test1" },
            new BillingConfiguration { Id = "4b0f6312-99ab-4644-954b-746b608bde76", LoadConfirmationFrequency = "TicketByTicket", Name = "Test2" }
    };

    var loadConfirmations = await LoadLoadConfirmationsForMultipleBillingConfiguration(database, documentType, billingConfigurations);
    PrintLookupData(loadConfirmations, (lookupItem) => Console.WriteLine($"{lookupItem.Id} {lookupItem.BillingConfigurationId} {lookupItem.BillingConfigurationName} {lookupItem.Frequency} {lookupItem.DocumentType} {lookupItem.Status} {lookupItem.DocItemId}"));
}

# endregion Test

async Task<List<BillingConfiguration>> GetBillingConfigurations(Database database)
{
    var containerName = "Billing";
    var container = database.GetContainer(containerName);

    var query = $"SELECT c.Id, c.Name, c.LoadConfirmationFrequency FROM c WHERE c.DocumentType = 'BillingConfiguration' AND c.FieldTicketDeliveryMethod = 1 AND c.LoadConfirmationFrequency = 'TicketByTicket'";

    var lookupItems = await GetLookupData<BillingConfiguration>(container, query);

    return lookupItems;
}

async Task<List<LoadConfirmationDocumentType>> GetLoadConfirmationDocumentTypes(Database database)
{
    var containerName = "Operations";
    var container = database.GetContainer(containerName);

    var query = $"SELECT DISTINCT c.DocumentType FROM c WHERE c.EntityType = 'LoadConfirmation'";

    var lookupItems = await GetLookupData<LoadConfirmationDocumentType>(container, query);

    return lookupItems;
}

async Task<List<TItem>> GetLookupData<TItem>(Container container, string query)
{
    var lookupItems = new List<TItem>();

    var queryDefinition = new QueryDefinition(query);

    using (FeedIterator<TItem> feedIterator = container.GetItemQueryIterator<TItem>(queryDefinition))
    {
        while (feedIterator.HasMoreResults)
        {
            lookupItems.AddRange(await feedIterator.ReadNextAsync());
        }
    }

    return lookupItems;
}

void PrintLookupData<TItem>(List<TItem> lookupItems, Action<TItem> print)
{
    foreach (var lookupItem in lookupItems)
    {
        print(lookupItem);
    }

    Console.WriteLine();
    Console.WriteLine($"Item Count: {lookupItems.Count}");
}

async Task UpdateLoadConfirmations(Database database)
{
    Console.WriteLine($"Getting LoadConfirmation DocumentTypes...");
    var lcDocumentTypes = await GetLoadConfirmationDocumentTypes(database);
    Console.WriteLine($"lcDocumentTypes.Count: {lcDocumentTypes.Count}");

    Console.WriteLine($"Getting Billing Configurations...");
    var billingConfigurations = await GetBillingConfigurations(database);
    Console.WriteLine($"billingConfigurations.Count: {billingConfigurations.Count}");

    foreach (var lcDocumentType in lcDocumentTypes)
    {
        // Load LCs for all billing configurations
        await UpdateLoadConfirmationForMultipleBillingConfiguration(database, lcDocumentType.DocumentType, billingConfigurations);
    }

    Console.WriteLine($"totalUpdated: {totalUpdated}");
}

async Task UpdateLoadConfirmationForMultipleBillingConfiguration(Database database, string documentType, IEnumerable<BillingConfiguration> billingConfigurations)
{
    var containerName = "Operations";
    var container = database.GetContainer(containerName);

    var frequency = "TicketByTicket";

    var loadConfirmations = await LoadLoadConfirmationsForMultipleBillingConfiguration(database, documentType, billingConfigurations);

    Console.WriteLine();
    Console.WriteLine($"loadConfirmations.Count: {loadConfirmations.Count} for {documentType}");
    Console.WriteLine();

    await PersistObjectToJsonFile(backupFileName.Replace(".", $".{documentType}."), loadConfirmations, true);

    // Using in batch mode
    if (loadConfirmations.Count > 0)
    {
        // Using in batch mode
        var chunkSize = Math.Max(loadConfirmations.Count / maxItemsToUpdateInParallel, 1);
        var tasks = loadConfirmations.Chunk(maxItemsToUpdateInParallel).Select(lcBatch => UpdateLoadConfirmationBillingFrequencyBatch(container, lcBatch, frequency));
        await Task.WhenAll(tasks);
    }
}

async Task<List<LoadConfirmation>> LoadLoadConfirmations(Database database, string documentType, BillingConfiguration billingConfiguration)
{
    var containerName = "Operations";
    var container = database.GetContainer(containerName);

    var query = $@"SELECT c.Id, c.BillingConfigurationId, c.BillingConfigurationName, c.Frequency, c.DocumentType, c.Status, c.id as DocItemId FROM c
                    WHERE c.DocumentType = '{documentType}' AND c.EntityType = 'LoadConfirmation'
                    AND c.Status in ('Open', 'SubmittedToGateway', 'Rejected', 'Posted')
                    AND c.BillingConfigurationId = '{billingConfiguration.Id}'
                    AND c.Frequency != '{billingConfiguration.LoadConfirmationFrequency}'";

    // Console.WriteLine(query);

    var lookupItems = await GetLookupData<LoadConfirmation>(container, query);

    return lookupItems;
}

async Task<List<LoadConfirmation>> LoadLoadConfirmationsForMultipleBillingConfiguration(Database database, string documentType, IEnumerable<BillingConfiguration> billingConfigurations)
{
    var containerName = "Operations";
    var container = database.GetContainer(containerName);

    var predicateBuilder = new StringBuilder();

    predicateBuilder.AppendLine($@"1 = 0");

    foreach (var billingConfiguration in billingConfigurations)
    {
        predicateBuilder.AppendLine($@"OR (c.BillingConfigurationId = '{billingConfiguration.Id}' AND c.Frequency != '{billingConfiguration.LoadConfirmationFrequency}')");
    }

    var predicate = predicateBuilder.ToString();

    var query = $@"SELECT c.Id, c.BillingConfigurationId, c.BillingConfigurationName, c.Frequency, c.DocumentType, c.Status, c.id as DocItemId FROM c
                    WHERE c.DocumentType = '{documentType}' AND c.EntityType = 'LoadConfirmation'
                    AND c.Status in ('Open', 'SubmittedToGateway', 'Rejected', 'Posted')
                    AND ({predicate})";

    // Console.WriteLine(query);

    var lookupItems = await GetLookupData<LoadConfirmation>(container, query);

    return lookupItems;
}

async Task UpdateLoadConfirmationBillingFrequencyBatch(Container container, IEnumerable<LoadConfirmation> loadConfirmations, string frequency)
{
    Console.WriteLine("UpdateLoadConfirmationBillingFrequencyBatch... ");

    foreach (var loadConfirmation in loadConfirmations)
    {
        await UpdateLoadConfirmationBillingFrequency(container, loadConfirmation.DocItemId, loadConfirmation.DocumentType, frequency);
    }

    Console.WriteLine($"totalUpdated: {totalUpdated}");
}

async Task UpdateLoadConfirmationBillingFrequency(Container container, string docItemId, string documentType, string frequency)
{
    string propertyName = "Frequency";

    await PatchSingleProperty(container, docItemId, documentType, propertyName, frequency);
}

async Task PatchSingleProperty(Container container, string docItemId, string partitionKey, string propertyName, string propertyValue)
{
    if (liveMode)
    {
        await policy.ExecuteAsync(async () =>
        {
            var patchOps = new List<PatchOperation>
            {
                PatchOperation.Add($"/{propertyName}", propertyValue),
            };

            await container.PatchItemAsync<JObject>(docItemId, new PartitionKey(partitionKey), patchOps);

            Interlocked.Increment(ref totalUpdated);
        });
    }
    else
    {
        // Console.WriteLine($"    PatchOp: {docItemId} {partitionKey} {propertyName} {propertyValue}");
    }
}

async Task PersistObjectToJsonFile<T>(string filePath, T objectToPersist, bool append = false) where T : new()
{
    var validfilePath = Path.GetInvalidFileNameChars().Aggregate(filePath, (f, c) => f.Replace(c, '_'));

    var writer = new StreamWriter(validfilePath, append);

    try
    {
        var contentsToWriteToFile = JsonConvert.SerializeObject(objectToPersist, Formatting.Indented);

        await writer.WriteAsync(contentsToWriteToFile);
    }
    finally
    {
        writer?.Close();
    }
}

public class BillingConfiguration
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string LoadConfirmationFrequency { get; set; }
}

public class LoadConfirmationDocumentType
{
    public string DocumentType { get; set; }
}

public class LoadConfirmation
{
    public string Id { get; set; }
    public string BillingConfigurationId { get; set; }
    public string BillingConfigurationName { get; set; }
    public string Frequency { get; set; }
    public string DocumentType { get; set; }
    public string Status { get; set; }
    public string DocItemId { get; set; }
}
