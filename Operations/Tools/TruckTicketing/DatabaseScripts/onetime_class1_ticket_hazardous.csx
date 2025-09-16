// dotnet tool install -g dotnet-script
// dotnet script .\onetime_class1_ticket_hazardous.csx "<connection-string>"
#r "nuget: Newtonsoft.Json, 13.0.3"
#r "nuget: Microsoft.Azure.Cosmos, 3.35.4"
#r "nuget: Polly, 8.0.0"
#r "nuget: EPPlus, 7.0.1"
#load "ReadExcelData.csx"

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Cosmos;
using Polly;

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

// Specify the path to your Excel file
string materialApprovalFilePath = "./MA-Haz-State-Undef-Data-Updates-FINAL.xlsx";

// Specify the sheet name you want to read
string sheetName = "MAs with Hazard State";

// Specify the key & value header name for fetching data in dictionary
string keyColumnHeaderName = "Material Approval #";
string valueColumnHeaderName = "Haz/Non-Haz";

// cosmos connection
var client = new CosmosClient(connectionString);
var database = client.GetDatabase(databaseName);
var container = database.GetContainer(containerName);
int processingCounter = 0;
int truckTicketCounter = 0;
int salesLineCounter = 0;
int truckTicketTotal = 0;
int bounces = 0;
int maxRetries = 10;
int concurrencyRetry = 0;
#region Policy
var policy = Policy
            .Handle<CosmosException>(e => IsTransientCode(e.StatusCode))
            .WaitAndRetryAsync(
                               20,
                               retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                               (e, t) => Interlocked.Increment(ref bounces));

var concurrencyRetriesPolicy = Policy
                              .Handle<CosmosException>(e => IsTransientCode(e.StatusCode))
                              .WaitAndRetryAsync(
                                                 maxRetries,
                                                 retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                                                 (e, t) => Interlocked.Increment(ref concurrencyRetry));

private static bool IsTransientCode(HttpStatusCode statusCode)
{
    return statusCode is HttpStatusCode.TooManyRequests or
                         HttpStatusCode.RequestTimeout or
                         HttpStatusCode.ServiceUnavailable or
                         HttpStatusCode.Gone or
                         (HttpStatusCode)449;
}
#endregion

// process all LCs
Console.WriteLine($"Started at: {DateTime.Now}");
Dictionary<string, string> materialApprovalLoadedData = ReadExcelToDictionary(materialApprovalFilePath, sheetName, keyColumnHeaderName, valueColumnHeaderName, true);
await ProcessTruckTickets();
Console.WriteLine();
Console.WriteLine($"Finished at: {DateTime.Now}");

async Task ProcessTruckTickets()
{
    //query builder
    var queryPattern = "SELECT {0} FROM c WHERE c.EntityType = 'TruckTicket' AND c.FacilityType = 'Lf'";
    var totalQuery = string.Format(queryPattern, "COUNT(1) as total");
    var mainQuery = string.Format(queryPattern, "c.Id, c.id, c.DocumentType, c.MaterialApprovalId");

    // get total TruckTickets
    using var totalFeed = container.GetItemQueryIterator<JObject>(totalQuery);
    if (totalFeed.HasMoreResults)
    {
        var response = await totalFeed.ReadNextAsync();
        truckTicketTotal = (int)response.First()["total"];
    }

    // get all LCs
    var mainQueryDefinition = new QueryDefinition(mainQuery);
    var mainQueryOptions = new QueryRequestOptions()
    {
        MaxBufferedItemCount = 10000,
        MaxConcurrency = 10,
        MaxItemCount = 10000,
    };

    // iterate over all LCs
    using var mainFeed = container.GetItemQueryIterator<JObject>(mainQueryDefinition, requestOptions: mainQueryOptions);

    while (mainFeed.HasMoreResults)
    {
        // fetch items
        var response = await policy.ExecuteAsync(async () => await mainFeed.ReadNextAsync());

        // process each item
        var tasks = response.Chunk(100).Select(UpdateTruckTicketBatch);
        await Task.WhenAll(tasks);
    }
}

async Task UpdateTruckTicketBatch(JObject[] truckTickets)
{
    var processTicketTasks = new List<Task>();
    foreach (var ticket in truckTickets)
    {
        processTicketTasks.Add(ProcessTruckTicketHandler(ticket));
    }
    await Task.WhenAll(processTicketTasks);
    Interlocked.Add(ref processingCounter, truckTickets.Count());
    Console.WriteLine($"Completed processing {truckTickets.Count()} TruckTickets & {salesLineCounter} SalesLines | Total Completed {processingCounter} of {truckTicketTotal} ");
}

async Task ProcessTruckTicketHandler(JObject truckTicketRecord)
{
    //Console.WriteLine($"Processing {(string)truckTicketRecord["Id"]} Truck Ticket");
    //Fetch Matertial Approval associated with Truck Ticket
    var materialApproval = await FetchMaterialApproval((string)truckTicketRecord["MaterialApprovalId"]);
    //Check if selected MaterialApproval is present in excel sheet, if it does select value from there 
    if (materialApproval == null)
    {
        Console.WriteLine($"No Material Approval Exist!!!");
        return;
    }
    var materialApprovalHazNonHaz = (string)materialApproval["HazardousNonhazardous"];

    if (materialApprovalLoadedData != null && materialApprovalLoadedData.Any() && materialApprovalLoadedData.TryGetValue((string)materialApproval["MaterialApprovalNumber"], out var HazNonHazForMA))
    {
        materialApprovalHazNonHaz = HazNonHazForMA;
    }

    if (string.IsNullOrEmpty(materialApprovalHazNonHaz))
    {
        return;
    }

    //Fetch & Update TruckTicket
    await concurrencyRetriesPolicy.ExecuteAsync(async () => await UpdateTruckTicket(materialApprovalHazNonHaz, truckTicketRecord));
    Interlocked.Increment(ref truckTicketCounter);

    //Fetch Sales Line associated to TruckTicket
    var salesLineQuery = new QueryDefinition($"SELECT c.id, c.Id, c.DocumentType FROM c WHERE c.EntityType = 'SalesLine' AND c.TruckTicketId = @id");
    salesLineQuery.WithParameter("@id", (string)truckTicketRecord["Id"]);
    var salesLines = await GetLookupData<JObject>(container, salesLineQuery);
    //Fetch & Update SalesLines
    foreach (var line in salesLines)
    {
        concurrencyRetriesPolicy.ExecuteAsync(async () => await UpdatedSalesLine(materialApprovalHazNonHaz, line));
    }
    Interlocked.Add(ref salesLineCounter, salesLines.Count());
}

async Task UpdateTruckTicket(string materialApprovalHazNonHaz, JObject truckTicket)
{
    // Fetch Truck ticket
    string truckTicketETag = null;
    string truckTicketDocumentType = null;
    string truckTicketId = null;
    truckTicketDocumentType = (string)truckTicket["DocumentType"];
    truckTicketId = (string)truckTicket["id"];
    var truckTicketEntity = await FetchTruckTicket(truckTicketId, truckTicketDocumentType);
    string propertyName = "DowNonDow";
    string proprtyValue = string.Empty;
    if (truckTicketEntity != null)
    {
        // get truck ticket eTag
        truckTicketETag = (string)truckTicketEntity["_etag"];
        // Field update

        switch (materialApprovalHazNonHaz)
        {
            case "Undefined":
                proprtyValue = "NonHazardous";
                break;
            case "Hazardous":
                proprtyValue = "Hazardous";
                break;
            case "Nonhazardous":
            case "Non-Hazardous":
                proprtyValue = "NonHazardous";
                break;
        }

        // Check if data needs updated
        if ((string)truckTicketEntity[propertyName] == proprtyValue)
        {
            Console.WriteLine($"No update needed for TruckTicket {truckTicketId}");
            return;
        }
        // Update Truck Ticket
        await PatchTruckTicket((string)truckTicketEntity["id"], (string)truckTicketEntity["DocumentType"], truckTicketETag, propertyName, proprtyValue);
        // The update was successful
        Console.WriteLine($"TruckTicket {truckTicketId} updated successfully");
    }
}

async Task UpdatedSalesLine(string materialApprovalHazNonHaz, JObject salesLine)
{
    //Fetch Sales Line
    string salesLineETag = null;
    string salesLineDocumentType = null;
    string salesLineId = null;
    salesLineDocumentType = (string)salesLine["DocumentType"];
    salesLineId = (string)salesLine["id"];
    var salesLineEntity = await FetchSalesLine(salesLineId, salesLineDocumentType);
    string propertyName = "DowNonDow";
    string proprtyValue = string.Empty;
    if (salesLineEntity != null)
    {
        //Get sales line eTag
        salesLineETag = (string)salesLineEntity["_etag"];
        //Field update
        if (salesLineEntity != null)
        {
            switch (materialApprovalHazNonHaz)
            {
                case "Undefined":
                    proprtyValue = "NonHazardous";
                    break;
                case "Hazardous":
                    proprtyValue = "Hazardous";
                    break;
                case "Nonhazardous":
                case "Non-Hazardous":
                    proprtyValue = "NonHazardous";
                    break;
            }
        }
        //Check if data needs updated
        if ((string)salesLineEntity[propertyName] == proprtyValue)
        {
            Console.WriteLine($"No update needed for Sales Line {salesLineId}");
            return;
        }
        //Update Sales Lines
        await PatchSalesLine((string)salesLineEntity["id"], (string)salesLineEntity["DocumentType"], salesLineETag, propertyName, proprtyValue);
        // The update was successful
        Console.WriteLine($"Sales Line {salesLineId} updated successfully");
    }

}
#region Helper Methods
async Task<List<TItem>> GetLookupData<TItem>(Container container, QueryDefinition queryDefinition)
{
    var lookupItems = new List<TItem>();

    using (FeedIterator<TItem> feedIterator = container.GetItemQueryIterator<TItem>(queryDefinition, requestOptions: new QueryRequestOptions()
    {
        MaxBufferedItemCount = 10000,
        MaxConcurrency = 10,
        MaxItemCount = 10000,
    }))
    {
        while (feedIterator.HasMoreResults)
        {
            var response = await policy.ExecuteAsync(async () => await feedIterator.ReadNextAsync());
            lookupItems.AddRange(response);
        }
    }

    return lookupItems;
}
#endregion
#region Patch
async Task PatchSalesLine(string salesLineId, string salesLinePk, string salesLineETag, string propertyName, string propertyValue)
{
    //Update existing property
    await policy.ExecuteAsync(async () =>
    {
        var patchOps = new List<PatchOperation>
                                  {
                                      PatchOperation.Add($"/{propertyName}", propertyValue),
                                  };

        await container.PatchItemAsync<JObject>(salesLineId, new PartitionKey(salesLinePk), patchOps, new PatchItemRequestOptions
        {
            IfMatchEtag = salesLineETag // Use the ETag from the response for optimistic concurrency
        });
    });
}
async Task PatchTruckTicket(string truckTicketId, string truckTicketPk, string truckTicketETag, string propertyName, string propertyValue)
{
    //Update existing property
    await policy.ExecuteAsync(async () =>
    {
        var patchOps = new List<PatchOperation>
                                  {
                                      PatchOperation.Add($"/{propertyName}", propertyValue),
                                  };

        await container.PatchItemAsync<JObject>(truckTicketId, new PartitionKey(truckTicketPk), patchOps, new PatchItemRequestOptions
        {
            IfMatchEtag = truckTicketETag // Use the ETag from the response for optimistic concurrency
        });
    });
}
#endregion
#region Fetch Entities
async Task<JObject> FetchSalesLine(string salesLineId, string partitionKey)
{
    return await policy.ExecuteAsync(async () =>
    {
        // no salesLine ID = no salesLine
        if (string.IsNullOrEmpty(salesLineId)) return null;

        // sales line query
        var salesLineQuery = new QueryDefinition($"SELECT * FROM c WHERE c.EntityType = 'SalesLine' AND c.id = @id");
        salesLineQuery.WithParameter("@id", salesLineId);
        // query the database
        using var salesLineFeed = container.GetItemQueryIterator<JObject>(salesLineQuery, requestOptions: new QueryRequestOptions()
        {
            PartitionKey = new PartitionKey(partitionKey),
            MaxBufferedItemCount = 10000,
            MaxConcurrency = 10,
            MaxItemCount = 10000,
        });

        List<JObject> elements = new();

        while (salesLineFeed.HasMoreResults)
        {
            // fetch the sales lines
            var response = await policy.ExecuteAsync(async () => await salesLineFeed.ReadNextAsync());
            elements.AddRange(response);
        }
        return elements.FirstOrDefault();
    });
}
async Task<JObject> FetchTruckTicket(string truckTicketId, string partitionKey)
{
    return await policy.ExecuteAsync(async () =>
    {
        // no truckticket ID = no truck ticket
        if (string.IsNullOrEmpty(truckTicketId)) return null;

        // truck ticket query
        var truckTicketQuery = new QueryDefinition($"SELECT * FROM c WHERE c.EntityType = 'TruckTicket' AND c.id = @id");
        truckTicketQuery.WithParameter("@id", truckTicketId);
        // query the database
        using var truckTicketFeed = container.GetItemQueryIterator<JObject>(truckTicketQuery, requestOptions: new QueryRequestOptions()
        {
            PartitionKey = new PartitionKey(partitionKey),
            MaxBufferedItemCount = 10000,
            MaxConcurrency = 10,
            MaxItemCount = 10000,
        });
        //fetch truck ticket
        List<JObject> elements = new();

        while (truckTicketFeed.HasMoreResults)
        {
            // fetch the truck ticket
            var response = await policy.ExecuteAsync(async () => await truckTicketFeed.ReadNextAsync());
            elements.AddRange(response);
        }
        return elements.FirstOrDefault();
    });
}
async Task<JObject> FetchMaterialApproval(string materialApprovalId)
{
    return await policy.ExecuteAsync(async () =>
    {
        // no materialApproval ID = no materialApproval
        if (string.IsNullOrEmpty(materialApprovalId)) return null;

        // materialApproval query
        var materialApprovalQuery = new QueryDefinition($"SELECT c.HazardousNonhazardous, c.MaterialApprovalNumber FROM c WHERE c.EntityType = 'MaterialApproval' AND c.Id = @id");
        materialApprovalQuery.WithParameter("@id", materialApprovalId);
        var materialApprovalQueryOptions = new QueryRequestOptions()
        {
            MaxBufferedItemCount = 10000,
            MaxConcurrency = 10,
            MaxItemCount = 10000,
        };
        // query the database
        using var materialApprovalFeed = container.GetItemQueryIterator<JObject>(materialApprovalQuery, requestOptions: materialApprovalQueryOptions);

        // fetch the materialApproval
        var list = new List<JObject>();
        while (materialApprovalFeed.HasMoreResults)
        {
            // fetch items
            var response = await policy.ExecuteAsync(async () => await materialApprovalFeed.ReadNextAsync());
            list.AddRange(response);
        }
        // take the first invoice
        return list.FirstOrDefault();
    });
}
#endregion