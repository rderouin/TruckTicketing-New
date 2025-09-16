// dotnet tool install -g dotnet-script
// dotnet script .\onetime_ma_accumulatedTonnageFix.csx "<connection-string>"
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

// ================================================================================

// vars
var databaseName = "TruckTicketing";
var containerName = "Operations";
var propertyName = "AccumulatedTonnage";

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

int counter = 0;
int total = 0;
int bounces = 0;
int chunkSize = 1;
var policy = Policy
	.Handle<CosmosException>(e => e.StatusCode == HttpStatusCode.TooManyRequests)
	.WaitAndRetryAsync(
		10,
		retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
		(e, t) => Interlocked.Increment(ref bounces));

// process all LCs
Console.WriteLine($"Started at: {DateTime.Now}");
await ProcessMaterialApprovals();
Console.WriteLine();
Console.WriteLine($"Finished at: {DateTime.Now}");

async Task ProcessMaterialApprovals()
{
	// get total MAs
	using var totalLfFacilities = container.GetItemQueryIterator<JObject>(@"
        SELECT 
            COUNT(1) AS total 
        FROM c 
        WHERE 
            c.EntityType = 'TruckTicket' 
            AND c.FacilityType = 'Lf' 
            AND c.MaterialApprovalNumber != null ");/*68,368 in QA*/
	
    if (totalLfFacilities.HasMoreResults)
	{
		var response = await totalLfFacilities.ReadNextAsync();
		total = (int)response.First()["total"];
	}

	// get all MANumbers and summed netweight.
	var allTruckTicketsOfTypeLfQuery = new QueryDefinition($@"
    SELECT 
		c.MaterialApprovalNumber,
		SUM(c.NetWeight) AS TotalNetWeight
	FROM c 
	where 
		c.EntityType = 'TruckTicket' 
		and c.FacilityType = 'Lf' 
		AND c.MaterialApprovalNumber != null
	GROUP BY
		c.MaterialApprovalNumber
	ORDER BY 
		c.MaterialApprovalNumber ");
	
    var queryOptions = new QueryRequestOptions()
	{
		MaxBufferedItemCount = 10000,
		MaxConcurrency = 10,
		MaxItemCount = 10000,
	};

	// iterate over all MAs
	using var allLandFillFacilityTruckTickets = container.GetItemQueryIterator<JObject>(allTruckTicketsOfTypeLfQuery, requestOptions: queryOptions);

	while (allLandFillFacilityTruckTickets.HasMoreResults)
	{
		// fetch items
		var response = await policy.ExecuteAsync(async () => await allLandFillFacilityTruckTickets.ReadNextAsync());

		// process each item
		var tasks = response.Chunk(chunkSize).Select(FetchThenUpdateMaterialApprovals);
		await Task.WhenAll(tasks);
	}
}

async Task FetchThenUpdateMaterialApprovals(JObject[] landfillFacilityTruckTickets)
{     
	foreach (var landfillFacilityTruckTicket in landfillFacilityTruckTickets)
	{       
		await FetchThenUpdateOneMaterialApproval(landfillFacilityTruckTicket);
		Interlocked.Increment(ref counter);
        Console.Write($"\rMaterial Approvals updated: {counter} of {total} (bounces: {bounces})");
	}
}

async Task FetchThenUpdateOneMaterialApproval(JObject landfillFacilityTruckTicket)
{
    /*find the MA for this truck ticket.*/	
    var maNumber = (string)landfillFacilityTruckTicket["MaterialApprovalNumber"];
    var totalNetWeight = (double)landfillFacilityTruckTicket["TotalNetWeight"];

    var materialApproval = await FetchMaterialApproval(maNumber);

    if (materialApproval != null){
        
        var accumulatedTonnage = (double)materialApproval["AccumulatedTonnage"];
        
        var roundedTotalNetWeight = Math.Round(totalNetWeight, 2);
        var roundedAccumulatedTonnage = Math.Round(accumulatedTonnage, 2);

        //compare the truckTicket.totalNetWeight with the ma.AccumulatedTonnage, if they aren't equal, update the MA.
        if(roundedTotalNetWeight != roundedAccumulatedTonnage){
            //we need to update the accumulated tonnage on the MA with the SUM of the tickets' Net weight, a.k.a TotalNetWeight
            await PatchMaterialApproval((string)materialApproval["id"], (string)materialApproval["DocumentType"], totalNetWeight);                        
        }
    }	
}

async Task<JObject> FetchMaterialApproval(string materialApprovalNumber)
{
	return await policy.ExecuteAsync(async () =>
	{		
		if (string.IsNullOrEmpty(materialApprovalNumber)) return null;

		var maQuery = new QueryDefinition(@$"
            SELECT 
                c.id, 
                c.DocumentType, 
                c.AccumulatedTonnage 
            FROM c 
            WHERE 
                c.EntityType = 'MaterialApproval' 
                AND c.MaterialApprovalNumber = @materialApprovalNumber 
				AND c.DocumentType = @documentType");

		maQuery.WithParameter("@materialApprovalNumber", materialApprovalNumber);
		maQuery.WithParameter("@documentType", "MaterialApprovalEntity");
        
        var maQueryOptions = new QueryRequestOptions()
        {
            MaxBufferedItemCount = 10000,
            MaxConcurrency = 10,
            MaxItemCount = 10000,
            PartitionKey = new PartitionKey("MaterialApprovalEntity")           
        };

		using var maResults = container.GetItemQueryIterator<JObject>(maQuery, requestOptions: maQueryOptions);
        
		// fetch the list
		var list = new List<JObject>();
		
        while (maResults.HasMoreResults)
		{
			var response = await maResults.ReadNextAsync();
			list.AddRange(response);
		}

		// take the first
		return list.FirstOrDefault();
	});
}

async Task PatchMaterialApproval(string maId, string partitionKey, double newAccumulatedTonnage)
{   
	await policy.ExecuteAsync(async () =>
	{
		var patchOps = new List<PatchOperation>
		{
			PatchOperation.Add($"/{propertyName}", newAccumulatedTonnage),
		};

        await container.PatchItemAsync<JObject>(maId, new PartitionKey(partitionKey), patchOps);
	});
}
