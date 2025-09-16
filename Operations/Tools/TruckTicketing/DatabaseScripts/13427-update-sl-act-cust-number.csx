//
//  ______  ______   __  __   ______   __  __   ______  __   ______   __  __   ______  ______  __   __   __   ______   
// /\__  _\/\  == \ /\ \/\ \ /\  ___\ /\ \/ /  /\__  _\/\ \ /\  ___\ /\ \/ /  /\  ___\/\__  _\/\ \ /\ "-.\ \ /\  ___\  
// \/_/\ \/\ \  __< \ \ \_\ \\ \ \____\ \  _"-.\/_/\ \/\ \ \\ \ \____\ \  _"-.\ \  __\\/_/\ \/\ \ \\ \ \-.  \\ \ \__ \ 
//    \ \_\ \ \_\ \_\\ \_____\\ \_____\\ \_\ \_\  \ \_\ \ \_\\ \_____\\ \_\ \_\\ \_____\ \ \_\ \ \_\\ \_\\"\_\\ \_____\
//     \/_/  \/_/ /_/ \/_____/ \/_____/ \/_/\/_/   \/_/  \/_/ \/_____/ \/_/\/_/ \/_____/  \/_/  \/_/ \/_/ \/_/ \/_____/
//                                                                                                                     
//

#r "nuget: Newtonsoft.Json, 13.0.3"
#r "nuget: Microsoft.Azure.Cosmos, 3.37.0"
#r "nuget: Polly, 8.1.0"

#load "_dev_tools_se_.csx"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Cosmos;
using Polly;
using Polly.Retry;
////////////////////////////// CUT HERE //////////////////////////////

// ---=== MAIN SCRIPT ===---

// init
var q = new Queue<string>(Args);
q.TryDequeue(out var connectionString);
if (string.IsNullOrWhiteSpace(connectionString))
{
    Log.Info("CommandLine", "The connection string must be provided.");
    return;
}

// cosmos connection
var client = new CosmosClient(connectionString);
var database = client.GetDatabase("TruckTicketing");
var operationsContainer = database.GetContainer("Operations");

// vars
int salesLinesProcessed = 0;
int salesLinesTotal = 0;
int bounces = 0;

// policies
var feedPolicy = ResiliencePipelines.SetupCosmosFeedResiliencePipeline(arg => { bounces++; return ValueTask.CompletedTask; });
var genericPolicy = ResiliencePipelines.SetupCosmosGenericResiliencePipeline(arg => { bounces++; return ValueTask.CompletedTask; });
var policySet = (feedPolicy, genericPolicy);

// progress
var progress = SimpleProgress.StartTracking("Sales Lines", "updated {1} of {0} (bounces: {2})");

// kick off
Log.Info("General", $"{DateTime.Now}");
using (SimpleTimer.Show("Entire script"))
{
    await ProcessAll();
}
Log.Info("General", $"{DateTime.Now}");


// main method
async Task ProcessAll()
{
    var queryPattern = "SELECT {0} FROM c WHERE c.EntityType = 'SalesLine' AND (c.CustomerNumber = null OR c.AccountNumber = null)";
    var totalQuery = string.Format(queryPattern, "COUNT(1) as total");
    var mainQuery = string.Format(queryPattern, "c.id, c.Id, c.DocumentType, c.CustomerId");

    // totals
    var totalDoc = await CosmosTools.QueryOne(operationsContainer, policySet, totalQuery, null);
    salesLinesTotal = (int)totalDoc["total"];

    // main query
    await CosmosTools.QueryStreaming(operationsContainer, policySet, mainQuery, null, ProcessSalesLinesChunked, new(), new QueryRequestOptions
    {
        MaxBufferedItemCount = 1_000,
        MaxConcurrency = 10,
        MaxItemCount = 1_000,
    });

    async Task ProcessSalesLinesChunked(FeedResponse<JObject> batch)
    {
        var tasks = batch.Chunk(100).Select(ProcessSalesLines);
        await Task.WhenAll(tasks);
    }

    Log.Info(nameof(ProcessAll), $"Actual updates: {salesLinesProcessed}");
}

async Task ProcessSalesLines(JObject[] salesLines)
{
    foreach (var salesLine in salesLines)
    {
        await ProcessSalesLine(salesLine);
    }
}

async Task ProcessSalesLine(JObject salesLine)
{
    // check all approvals
    if ((string)salesLine["AccountNumber"] != null && (string)salesLine["CustomerNumber"] != null)
    {
        return;
    }
    var customer = await FetchCustomerForSalesLine((string)salesLine["CustomerId"]);
    var propertiesToUpdate = new Dictionary<string, string>();
    if (customer == null)
    {
        Console.WriteLine($"No Customer Exist!!!");
        return;
    }
    if ((string)salesLine["AccountNumber"] == null)
    {
        propertiesToUpdate.TryAdd($"/AccountNumber", (string)customer["AccountNumber"]);
    }        
    if ((string)salesLine["CustomerNumber"] == null)
    {
        propertiesToUpdate.TryAdd($"/CustomerNumber", (string)customer["CustomerNumber"]);
    }

    // update the sales line flag
    await PatchSalesLine((string)salesLine["id"], (string)salesLine["DocumentType"], propertiesToUpdate);

    // stats
    Interlocked.Increment(ref salesLinesProcessed);
    progress.Update(salesLinesTotal, salesLinesProcessed, bounces);
}


async Task<JObject> FetchCustomerForSalesLine(string customerId)
{
    var query = @"
SELECT
    c.Id,
    c.AccountNumber,
    c.CustomerNumber
FROM c
WHERE
    c.EntityType = 'Account'
AND c.Id = @customerId";

    var customer = await CosmosTools.QueryAll(
        operationsContainer,
        policySet,
        query,
        null,
        new()
        {
            ["@customerId"] = customerId
        },
        new()
        {
            MaxBufferedItemCount = 10_000,
            MaxConcurrency = 10,
            MaxItemCount = 10_000,
        });

    return customer.FirstOrDefault();
}

async Task PatchSalesLine(string salesLineId,  string partitionKey ,Dictionary<string, string> propertyValueMap)
{
    await CosmosTools.PatchProperties(operationsContainer, genericPolicy, salesLineId, partitionKey, propertyValueMap);
}