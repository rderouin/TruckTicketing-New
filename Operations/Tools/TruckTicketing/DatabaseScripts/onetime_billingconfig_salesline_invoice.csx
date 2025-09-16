// dotnet tool install -g dotnet-script
// dotnet script .\onetime_billingconfig_salesline_invoice.csx "<connection-string>"
#r "nuget: Newtonsoft.Json, 13.0.3"
#r "nuget: Microsoft.Azure.Cosmos, 3.35.4"
#r "nuget: Polly, 8.0.0"

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Cosmos;
using Polly;
using System.Net.Http;
using System.Diagnostics;
using System.Collections.Concurrent;

// ================================================================================
// vars
var databaseName = "TruckTicketing";
var containerName = "Operations";

// init
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

//vars
int counter = 0;
int invoiceUpdateCounter = 0;
int billingConfigPerInvoiceCounter = 0;
int total = 0;
int bounces = 0;
int maxRetries = 10;
int invoiceRetryBounces = 0;
ConcurrentDictionary<string, int> invoiceKeeper = new();
Dictionary<string, (string id, string documentType)> truckTicketPartitionKeyMap = new();
List<string> stopWatchLog = new();

ConcurrentDictionary<(string invoiceId, string billingConfigurationKey), int> invoiceBillingConfigurationUpdateMap = new();
ConcurrentDictionary<string, int> SalesLineProcessCounter = new();
#region Policy
var policy = Policy
    .Handle<CosmosException>(e => e.StatusCode == HttpStatusCode.TooManyRequests || e.StatusCode == HttpStatusCode.RequestTimeout || e.StatusCode == HttpStatusCode.ServiceUnavailable)
    .WaitAndRetryAsync(
        10,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        (e, t) => Interlocked.Increment(ref bounces));
var invoiceConcurrentRetriesPolicy = Policy
    .Handle<CosmosException>(e => e.StatusCode == HttpStatusCode.PreconditionFailed)
    .WaitAndRetryAsync(
        maxRetries,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        (e, t) => Interlocked.Increment(ref invoiceRetryBounces));
#endregion

// process all LCs
Console.WriteLine($"Started at: {DateTime.Now}");
//Step 1: Update SalesLines & add BillingConfigurationName & BillingConfigurationId from associated TruckTicket
//Step 2: Find Invoice associated to updated SalesLine & add BillingConfiguration reference from SalesLine to Invoice

Console.WriteLine($"Building Truck Ticket Document Type map...");
var truckTicketDocumentTypeQuery = $"SELECT c.Id, c.id, c.DocumentType FROM c WHERE c.EntityType = 'TruckTicket'";
var truckTicketDocumentTypes = await GetLookupData<JObject>(container, truckTicketDocumentTypeQuery);
truckTicketPartitionKeyMap = truckTicketDocumentTypes.ToDictionary(x => (string)x["Id"], x => ((string)x["id"], (string)x["DocumentType"]));
Console.WriteLine($"Completed building Truck Ticket Document Type map!!");

var query = $"SELECT DISTINCT c.DocumentType FROM c WHERE c.EntityType = 'SalesLine'";
var slDocumentTypes = await GetLookupData<JObject>(container, query);
var processSLsByPartition = new List<Task>();
// process each item
foreach (var slDocumentType in slDocumentTypes)
{
    processSLsByPartition.Add(ProcessSalesLines((string)slDocumentType["DocumentType"]));
}
await Task.WhenAll(processSLsByPartition);

Console.WriteLine($"Total Records to process {invoiceBillingConfigurationUpdateMap.Count()}");

foreach (var invoiceBillingConfigurationRecord in invoiceBillingConfigurationUpdateMap)
{
    await invoiceConcurrentRetriesPolicy.ExecuteAsync(async () => await FetchAndUpdateInvoice(invoiceBillingConfigurationRecord.Key, invoiceBillingConfigurationRecord.Value));
}

Console.Write($"\rSales Lines updated: {counter} of {total} (bounces: {bounces})");
Console.Write($"\rInvoices updated: {invoiceUpdateCounter}");

Console.WriteLine();
Console.WriteLine($"Finished at: {DateTime.Now}");

async Task ProcessSalesLines(string DocumentType)
{
    // get total Sales Lines
    var totalSalesLinesPerParition = 0;
    using var totalFeed = container.GetItemQueryIterator<JObject>("SELECT COUNT(1) as total FROM c WHERE c.EntityType = 'SalesLine' AND c.Status != 'Preview' AND c.Status != 'Void' AND c.Status != 'Exception'",
    requestOptions: new QueryRequestOptions()
    {
        PartitionKey = new PartitionKey(DocumentType)
    });
    if (totalFeed.HasMoreResults)
    {
        var response = await totalFeed.ReadNextAsync();
        totalSalesLinesPerParition = (int)response.First()["total"];
        Console.WriteLine($"{(int)response.First()["total"]} SalesLines in Partition {DocumentType}");
        Interlocked.Add(ref total, (int)response.First()["total"]);
    }


    // get all Sales Lines
    var mainQuery = new QueryDefinition($"SELECT c.id,c.DocumentType,c.InvoiceId,c.TruckTicketId FROM c WHERE c.EntityType = 'SalesLine' AND c.Status != 'Preview' AND c.Status != 'Void' AND c.Status != 'Exception'");
    var mainQueryOptions = new QueryRequestOptions()
    {
        MaxBufferedItemCount = 10000,
        MaxConcurrency = 10,
        MaxItemCount = 10000,
        PartitionKey = new PartitionKey(DocumentType)
    };

    // iterate over all Sales Lines
    using var mainFeed = container.GetItemQueryIterator<JObject>(mainQuery, requestOptions: mainQueryOptions);
    while (mainFeed.HasMoreResults)
    {
        // fetch items
        var mainFeedResponse = await policy.ExecuteAsync(async () => await mainFeed.ReadNextAsync());
        if (mainFeedResponse == null || !mainFeedResponse.Any())
        {
            continue;
        }
        SalesLineProcessCounter.AddOrUpdate(DocumentType, mainFeedResponse.Count, (key, oldValue) => oldValue + mainFeedResponse.Count);
        Console.WriteLine($"Start processing {mainFeedResponse.Count} of {totalSalesLinesPerParition} SalesLines from Partition {DocumentType}");
        // Grouping by the 'InvoiceId' property
        var groupedSalesLines = mainFeedResponse
            .GroupBy(obj => obj["InvoiceId"])
            .ToDictionary(
                group => group.Key,
                group => group.ToList()
            );
        var batchTask = new List<Task>();
        // process each item

        foreach (var salesLineGroup in groupedSalesLines)
        {
            var invoiceId = (string)salesLineGroup.Key;
            var salesLines = salesLineGroup.Value;
            batchTask.Add(UpdateSalesLineBatch(salesLines, invoiceId));
        }
        await Task.WhenAll(batchTask);
        Console.WriteLine($"Completed processing {mainFeedResponse.Count} of {totalSalesLinesPerParition} SalesLines from Partition {DocumentType} | Total Completed {SalesLineProcessCounter[DocumentType]}");
    }
}
async Task UpdateSalesLineBatch(List<JObject> sls, string invoiceId)
{
    var tasks = new List<Task>();

    foreach (var sl in sls)
    {
        tasks.Add(ProcessSalesLineUpdate(sl, invoiceId));
    }
    await Task.WhenAll(tasks);
}

#region Helper Methods

async Task ProcessSalesLineUpdate(JObject sl, string invoiceId)
{
    var bc = await UpdateSalesLine(sl, invoiceId);
    if (bc.billingConfigurationId == null)
    {
        return;
    }
    var key = $"{bc.billingConfigurationId}|{bc.billingConfigurationName}";
    invoiceBillingConfigurationUpdateMap.AddOrUpdate((invoiceId, key), 1, (key, oldValue) => oldValue + 1);

    Interlocked.Increment(ref counter);
}


async Task<(string billingConfigurationId, string billingConfigurationName)> UpdateSalesLine(JObject sl, string invoiceId)
{
    string billingConfigurationId = null;
    string billingConfigurationName = null;
    // fetch truckticket
    if (!truckTicketPartitionKeyMap.TryGetValue((string)sl["TruckTicketId"], out var truckTicketParitionInfo))
    {
        return (billingConfigurationId, billingConfigurationName);
    }
    //    Console.WriteLine($"TruckTicket for {(string)sl["Id"]} {truckTicketParitionInfo.ToString()}");
    var truckTicket = await FetchTruckTicket(truckTicketParitionInfo.id, truckTicketParitionInfo.documentType);

    if (truckTicket != null)
    {
        // get the billingConfiguration information
        billingConfigurationId = (string)truckTicket["BillingConfigurationId"];
        billingConfigurationName = (string)truckTicket["BillingConfigurationName"];

        //patch the SalesLine

        //Console.WriteLine($"Update Sales Line {(string)sl["id"]} with Billing Configuration {$"{billingConfigurationId}|{billingConfigurationName}"}");

        if (!sl.ContainsKey("BillingConifgurationId") || !sl.ContainsKey("BillingConfigurationName"))
        {
            await PatchSalesLine((string)sl["id"], (string)sl["DocumentType"], billingConfigurationId, billingConfigurationName);
        }
    }
    return (billingConfigurationId, billingConfigurationName);

}

async Task FetchAndUpdateInvoice((string invoiceId, string billingConfigurationKey) invoiceRecordKey, int salesLineCount)
{
    //Console.WriteLine($"Starting {invoiceId} update....");
    string invoiceETag = null;
    string invoiceDocumentType = null;

    var invoice = await FetchInvoice(invoiceRecordKey.invoiceId);

    if (invoice != null)
    {
        // NOTE: This check is needed only for the first time update; remove once one time script execution is completed
        if (!invoiceKeeper.ContainsKey(invoiceRecordKey.invoiceId))
        {
            Interlocked.Increment(ref invoiceUpdateCounter);
            if (invoice.ContainsKey("BillingConfigurations"))
            {
                invoice.Remove("BillingConfigurations");
            }
            invoiceKeeper.AddOrUpdate(invoiceRecordKey.invoiceId, 1, (key, oldValue) => oldValue + 1);
        }
        //
        invoiceETag = (string)invoice["_etag"];
        // get invoice document type
        invoiceDocumentType = (string)invoice["DocumentType"];
        // Replace the document with the updated properties
        var updatedInvoice = AddOrUpdateInvoiceBillingConfigurations(invoice, invoiceRecordKey.billingConfigurationKey, salesLineCount);

        await PatchInvoice((string)updatedInvoice["id"], invoiceDocumentType, updatedInvoice, invoiceETag);
        // The update was successful
        //Console.WriteLine(updatedInvoice["BillingConfigurations"].ToString());
        //Console.WriteLine($"Invoice {invoiceId} updated successfully");

        Interlocked.Increment(ref billingConfigPerInvoiceCounter);
        Console.WriteLine($"Completed processing {billingConfigPerInvoiceCounter} billing configuration/s for {invoiceUpdateCounter} invoices.");
    }
}


JObject AddOrUpdateInvoiceBillingConfigurations(JObject invoice, string key, int salesLineCount)
{

    //Add/Update BillingConfiguration in Invoice
    var billingConfigurationId = key.Split('|')[0];
    var billingConfigurationName = key.Split('|')[1];

    JObject newBillingConfiguration = new JObject
        {
            { "Id", Guid.NewGuid() },
            { "AssociatedSalesLinesCount", salesLineCount },
            { "BillingConfigurationId", billingConfigurationId },
            { "BillingConfigurationName", billingConfigurationName },
        };

    if (!invoice.ContainsKey("BillingConfigurations"))
    {
        //Insert
        invoice.Add(new JProperty("BillingConfigurations", new JArray(newBillingConfiguration)));
    }
    else
    {
        JArray invoiceBillingConfigurations = (JArray)invoice["BillingConfigurations"];
        JToken existingBillingConfiguration = invoiceBillingConfigurations.FirstOrDefault(o => (string)o["BillingConfigurationId"] == billingConfigurationId);
        if (existingBillingConfiguration != null)
        {
            // Update an existing object
            var associatedSalesLineCount = (int)existingBillingConfiguration["AssociatedSalesLinesCount"];
            existingBillingConfiguration["AssociatedSalesLinesCount"] = associatedSalesLineCount + salesLineCount;
        }
        else
        {

            invoiceBillingConfigurations.Add(newBillingConfiguration);
        }
        invoice["BillingConfigurations"] = invoiceBillingConfigurations;
    }

    if (!invoice.ContainsKey("BillingConfigurationNames"))
    {
        //Insert
        invoice.Add(new JProperty("BillingConfigurationNames", billingConfigurationName));
    }
    else
    {
        //Use updated billing configurations collection to build BillingConfigurationsNames field
        var billingConfigurationNames = invoice["BillingConfigurations"]
        .Select(product => product["BillingConfigurationName"].ToString());
        // Concatenate the product names into a single string
        string concatenatedNames = string.Join(", ", billingConfigurationNames);
        invoice["BillingConfigurationNames"] = concatenatedNames;
    }

    return invoice;
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
#endregion

#region Fetch Entities
async Task<JObject> FetchTruckTicket(string truckTicketId, string partitionKey)
{
    return await policy.ExecuteAsync(async () =>
    {
        // no truckticket ID = no truck ticket
        if (string.IsNullOrEmpty(truckTicketId)) return null;

        // truck ticket query
        var truckTicketQuery = new QueryDefinition($"SELECT c.BillingConfigurationId, c.BillingConfigurationName FROM c WHERE c.EntityType = 'TruckTicket' AND c.id = @id");
        truckTicketQuery.WithParameter("@id", truckTicketId);
        // query the database
        using var truckTicketFeed = container.GetItemQueryIterator<JObject>(truckTicketQuery, requestOptions: new QueryRequestOptions()
        {
            PartitionKey = new PartitionKey(partitionKey)
        });
        //fetch truck ticket
        List<JObject> elements = new();

        while (truckTicketFeed.HasMoreResults)
        {
            // fetch the truck ticket
            var response = await truckTicketFeed.ReadNextAsync();
            elements.AddRange(response);
        }
        return elements.FirstOrDefault();
    });
}
async Task<JObject> FetchInvoice(string invoiceId)
{
    return await policy.ExecuteAsync(async () =>
    {
        // no invoice ID = no invoice
        if (string.IsNullOrEmpty(invoiceId)) return null;

        // invoice query
        var invoiceQuery = new QueryDefinition($"SELECT * FROM c WHERE c.EntityType = 'Invoice' AND c.Id = @id");
        invoiceQuery.WithParameter("@id", invoiceId);

        // query the database
        using var invoiceFeed = container.GetItemQueryIterator<JObject>(invoiceQuery);
        //fetch invoices
        List<JObject> elements = new();

        while (invoiceFeed.HasMoreResults)
        {
            var response = await invoiceFeed.ReadNextAsync();
            elements.AddRange(response);
        }
        return elements.FirstOrDefault();
    });
}
#endregion

#region Patch
async Task PatchSalesLine(string slId, string slPk, string billingConfigurationId, string billingConfigurationName)
{
    await policy.ExecuteAsync(async () =>
    {
        var patchOps = new List<PatchOperation>
                                  {
                                      PatchOperation.Add($"/BillingConfigurationId", billingConfigurationId),
                                      PatchOperation.Add($"/BillingConfigurationName", billingConfigurationName),
                                  };

        await container.PatchItemAsync<JObject>(slId, new PartitionKey(slPk), patchOps);
    });
}
async Task PatchInvoice(string invoiceId, string invoicePk, JObject updatedInvoice, string invoiceETag)
{
    await policy.ExecuteAsync(async () =>
    {
        // Replace the document with the updated properties
        await container.ReplaceItemAsync<JObject>(updatedInvoice, invoiceId, new PartitionKey(invoicePk), new ItemRequestOptions
        {
            IfMatchEtag = invoiceETag // Use the ETag from the response for optimistic concurrency
        });
    });
}
#endregion
public class StopwatchFactory
{
    private List<string> StopWatchLog = new();
    public Stopwatch CreateStopwatch()
    {
        return new Stopwatch();
    }

    public void LogTimeSpan(Stopwatch stopWatch, string OperationName)
    {
        TimeSpan groupingTimeTaken = stopWatch.Elapsed;
        StopWatchLog.Add($"{OperationName} Time taken: " + groupingTimeTaken.ToString(@"m\:ss\.fff"));
    }

    public List<string> GetStopWatchLogs()
    {
        return StopWatchLog;
    }
}
