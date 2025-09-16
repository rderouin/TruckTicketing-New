// dotnet tool install -g dotnet-script
// dotnet script .\onetime_provision_glnumber_lc.csx "<connection-string>"
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
var propertyName = "GlInvoiceNumber";

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
var policy = Policy
	.Handle<CosmosException>(e => e.StatusCode == HttpStatusCode.TooManyRequests)
	.WaitAndRetryAsync(
		10,
		retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
		(e, t) => Interlocked.Increment(ref bounces));

// process all LCs
Console.WriteLine($"Started at: {DateTime.Now}");
await ProcessLoadConfirmations();
Console.WriteLine();
Console.WriteLine($"Finished at: {DateTime.Now}");

async Task ProcessLoadConfirmations()
{
	// get total LCs
	using var totalFeed = container.GetItemQueryIterator<JObject>("SELECT COUNT(1) as total FROM c WHERE c.EntityType = 'LoadConfirmation'");
	if (totalFeed.HasMoreResults)
	{
		var response = await totalFeed.ReadNextAsync();
		total = (int)response.First()["total"];
	}

	// get all LCs
	var mainQuery = new QueryDefinition($"SELECT c.id,c.DocumentType,c.InvoiceId FROM c WHERE c.EntityType = 'LoadConfirmation'");
	var mainQueryOptions = new QueryRequestOptions()
	{
		MaxBufferedItemCount = 10000,
		MaxConcurrency = 10,
		MaxItemCount = 10000,
	};

	// iterate over all LCs
	using var mainFeed = container.GetItemQueryIterator<JObject>(mainQuery, requestOptions: mainQueryOptions);
	while (mainFeed.HasMoreResults)
	{
		// fetch items
		var response = await policy.ExecuteAsync(async () => await mainFeed.ReadNextAsync());

		// process each item
		var tasks = response.Chunk(100).Select(UpdateLoadConfirmationBatch);
		await Task.WhenAll(tasks);
	}
}

async Task UpdateLoadConfirmationBatch(JObject[] lcs)
{
	foreach (var lc in lcs)
	{
		await UpdateLoadConfirmation(lc);
		Interlocked.Increment(ref counter);

		Console.Write($"\rLoad Confirmations updated: {counter} of {total} (bounces: {bounces})");
	}
}

async Task UpdateLoadConfirmation(JObject lc)
{
	string glNumber = null;

	// find the invoice
	var invoice = await FetchInvoice((string)lc["InvoiceId"]);
	if (invoice != null)
	{
		// get the GL number
		glNumber = (string)invoice["GlInvoiceNumber"];
	}

	// patch the LC
	await PatchLoadConfirmation((string)lc["id"], (string)lc["DocumentType"], glNumber);
}

async Task<JObject> FetchInvoice(string invoiceId)
{
	return await policy.ExecuteAsync(async () =>
	{
		// no invoice ID = no invoice
		if (string.IsNullOrEmpty(invoiceId)) return null;

		// invoice query
		var invoiceQuery = new QueryDefinition($"SELECT c.GlInvoiceNumber FROM c WHERE c.EntityType = 'Invoice' AND c.Id = @id");
		invoiceQuery.WithParameter("@id", invoiceId);

		// query the database
		using var invoiceFeed = container.GetItemQueryIterator<JObject>(invoiceQuery);

		// fetch the invoice
		var list = new List<JObject>();
		while (invoiceFeed.HasMoreResults)
		{
			var response = await invoiceFeed.ReadNextAsync();
			list.AddRange(response);
		}

		// take the first invoice
		return list.FirstOrDefault();
	});
}

async Task PatchLoadConfirmation(string lcId, string lcPk, string glNumber)
{
	await policy.ExecuteAsync(async () =>
	{
		var patchOps = new List<PatchOperation>
		{
			PatchOperation.Add($"/{propertyName}", glNumber),
		};

		await container.PatchItemAsync<JObject>(lcId, new PartitionKey(lcPk), patchOps);
	});
}
