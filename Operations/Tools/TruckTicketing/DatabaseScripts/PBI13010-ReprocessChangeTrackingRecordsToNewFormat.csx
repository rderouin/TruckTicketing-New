// dotnet tool install -g dotnet-script
// dotnet script .\PBI13010-ReprocessChangeTrackingRecordsToNewFormat.csx "<cosmos-connection-string>" "<servicebus-connection-string>" "<date-to-process-from>" "<number-of-days-to-process>"
#r "nuget: Newtonsoft.Json, 13.0.3"
#r "nuget: Microsoft.Azure.Cosmos, 3.36.0"
#r "nuget: Azure.Messaging.ServiceBus, 7.17.0"
#r "nuget: Polly, 8.0.0"

#load "_dev_tools_se_.csx"

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Cosmos;
using Azure.Messaging.ServiceBus;
using Polly;

// ------------------------------------------------------------------------------------------------------------------------ //
/* This script reprocesses and submits Change Tracking records from before Sprint 26 for conversion into new Change format. */
// ------------------------------------------------------------------------------------------------------------------------ //
// variables
var databaseName = "TruckTicketing";
var operationsContainerName = "Operations";
var accountsContainerName = "Accounts";
var topicName = "change-entities";
var progress = SimpleProgress.StartTracking("Change Tracking records", "updated {1} of {0} (day: {2}, date: {3})");
var docTypeQueryOptions = new QueryRequestOptions()
{
	MaxBufferedItemCount = 10000,
	MaxConcurrency = 10,
	MaxItemCount = 10000,
};

// parse arguments, expecting connection string at runtime
var q = new Queue<string>(Args);
q.TryDequeue(out var cosmosConnectionString);
q.TryDequeue(out var sbConnectionString);
q.TryDequeue(out var startDate);
q.TryDequeue(out var daysToProcess);

if (string.IsNullOrWhiteSpace(cosmosConnectionString))
{
	Console.WriteLine("Cosmos connection string was not provided");
	return;
}

if (string.IsNullOrWhiteSpace(sbConnectionString))
{
	Console.WriteLine("Service bus connection string was not provided");
	return;
}

if (string.IsNullOrWhiteSpace(startDate))
{
	Console.WriteLine("Start date was not provided. (YYYY-MM-DD)");
	return;
}

if (string.IsNullOrWhiteSpace(daysToProcess))
{
	Console.WriteLine("The number of days-worth of data to process was not provided");
	return;
}

int startYear;
int startMonth;
int startDay;

try
{
	string[] startDateArray = startDate.Split('-');
	startYear = int.Parse(startDateArray[0]);
	startMonth = int.Parse(startDateArray[1]);
	startDay = int.Parse(startDateArray[2]);
}
catch (Exception e)
{
	Console.WriteLine("Failed to parse start date, please verify input format. (YYYY-MM-DD)");
	return;
}

int runDays = int.Parse(daysToProcess);

if (runDays < 1)
{
	Console.WriteLine("Invalid input; cannot process " + runDays + " days-worth of data.");
	return;
}

Console.WriteLine($"Process {runDays} days of Change Tracking records starting from {startYear}-{startMonth}-{startDay}? Y/N...");

string input = Console.ReadLine();

if (input != "Y" && input != "y") return;

// service bus setup
ServiceBusClient sbClient;
ServiceBusSender sbSender;
sbClient = new ServiceBusClient(sbConnectionString);
sbSender = sbClient.CreateSender(topicName);

// DB setups
var client = new CosmosClient(cosmosConnectionString);
var database = client.GetDatabase(databaseName);
var operationsContainer = database.GetContainer(operationsContainerName);
var accountsContainer = database.GetContainer(accountsContainerName);

// retry logic
int docTypeCount = 0;
int recordCount = 0;
int totalDocTypesPerDay = 0;
int DocTypeCounterPerDay = 0;
int bounces = 0;
var policy = Policy
	.Handle<CosmosException>(e => e.StatusCode == HttpStatusCode.TooManyRequests)
	.WaitAndRetryAsync(
		10,
		retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
		(e, t) => Interlocked.Increment(ref bounces));
		
// build a simple lookup for user emails
Console.WriteLine($"Building Email Lookup...");
Dictionary<string, string> emailsByName = new Dictionary<string, string>();
await BuildEmailLookup();

// process ChangeTracking records
Console.WriteLine($"Processing Change Tracking Records | Started at: {DateTime.Now}");
await ProcessChangeTrackingRecords();
Console.WriteLine($"Processing Change Tracking Records | Finished at: {DateTime.Now}");
Console.WriteLine($"Processed records for {docTypeCount} Document Types: {recordCount} distinct records in total.");

async Task ProcessChangeTrackingRecords()
{
	DateTime firstDay = new DateTime(startYear, startMonth, startDay);
	
	for (int i = 0; i < runDays; i++)
	{
		DocTypeCounterPerDay = 0;
		
		DateTime dayStart = firstDay.AddDays(i);
		DateTime dayEnd = firstDay.AddDays(i + 1);

		using var totalChangeTrackingRecords = operationsContainer.GetItemQueryIterator<JObject>($@"
		SELECT COUNT(1) AS total FROM (SELECT DISTINCT 
			c.DocumentType
		FROM c 
		WHERE c.EntityType = 'ChangeTracking' AND c.CreatedAt >= '{dayStart.ToString("o")}' AND c.CreatedAt < '{dayEnd.ToString("o")}')");

		if (totalChangeTrackingRecords.HasMoreResults)
		{
			var response = await totalChangeTrackingRecords.ReadNextAsync();
			totalDocTypesPerDay = (int)response.First()["total"];
		}

		var distinctChangeTrackingDocTypeQuery = new QueryDefinition($@"
		SELECT DISTINCT 
			c.DocumentType
		FROM c 
		WHERE c.EntityType = 'ChangeTracking' AND c.CreatedAt >= '{dayStart.ToString("o")}' AND c.CreatedAt < '{dayEnd.ToString("o")}'");

		using var distinctChangeTrackingDocTypes = operationsContainer.GetItemQueryIterator<JObject>(distinctChangeTrackingDocTypeQuery, requestOptions: docTypeQueryOptions);

		Console.WriteLine("Began processing records for " + dayStart.ToString("u").Split(" ")[0]);

		while (distinctChangeTrackingDocTypes.HasMoreResults)
		{
			// fetch next
			var response = await policy.ExecuteAsync(async () => await distinctChangeTrackingDocTypes.ReadNextAsync());
			// process
			var tasks = response.Select(j => BuildAndSendChangeMessages(j, dayStart, i+1, dayStart, dayEnd));
			await Task.WhenAll(tasks);
		}
		
		Console.WriteLine("Finished processing records for " + dayStart.ToString("u").Split(" ")[0]);
	}

	// dispose of service bus clients
	await sbSender.DisposeAsync();
	await sbClient.DisposeAsync();
}

async Task BuildAndSendChangeMessages(JObject changeTrackingDocType, DateTime date, int dayNum, DateTime dayStart, DateTime dayEnd)
{
	var distinctChangeTrackingRecordQuery = new QueryDefinition($@"
	SELECT DISTINCT
		c.Original, 
		c.Target, 
		c.CreatedAt, 
		c.CreatedBy,
		c.ReferenceEntityId
	FROM c 
	WHERE c.DocumentType = '" + changeTrackingDocType["DocumentType"] + $"' AND c.CreatedAt >= '{dayStart.ToString("o")}' AND c.CreatedAt < '{dayEnd.ToString("o")}'");

	var recordQueryOptions = new QueryRequestOptions()
	{
		MaxBufferedItemCount = 10000,
		MaxConcurrency = 10,
		MaxItemCount = 10000,
		PartitionKey = new PartitionKey((string)changeTrackingDocType["DocumentType"]),
	};
	
	Guid operationId = Guid.NewGuid();
	
	using var distinctChangeTrackingRecordsForDocType = operationsContainer.GetItemQueryIterator<JObject>(distinctChangeTrackingRecordQuery, requestOptions: recordQueryOptions);
	
	while (distinctChangeTrackingRecordsForDocType.HasMoreResults)
	{
		var response = await policy.ExecuteAsync(async () => await distinctChangeTrackingRecordsForDocType.ReadNextAsync());
		
		var tasks = response.Select(j => BuildAndSendChangeMessage(j, date, dayNum, operationId));
		await Task.WhenAll(tasks);
	}
	
	Interlocked.Increment(ref DocTypeCounterPerDay);
	docTypeCount++;
	progress.Update(totalDocTypesPerDay, DocTypeCounterPerDay, dayNum, date);
}

async Task BuildAndSendChangeMessage(JObject changeTrackingRecord, DateTime date, int dayNum, Guid operationId)
{
	var changeId = Guid.NewGuid();
	
	object functionName = null;
	
	var objectBefore = String.IsNullOrEmpty((string)changeTrackingRecord["Original"]) ? null : JObject.Parse((string)changeTrackingRecord["Original"]);
	var objectAfter = String.IsNullOrEmpty((string)changeTrackingRecord["Target"]) ? null : JObject.Parse((string)changeTrackingRecord["Target"]);
	
	if (objectAfter == null && objectBefore == null){return;}

	validateObjectIds(objectBefore);
	validateObjectIds(objectAfter);

	var entityType = (objectAfter ?? objectBefore)["EntityType"];
	var refEntityDocType = (objectAfter ?? objectBefore)["DocumentType"];
	
	var entityId = (string)changeTrackingRecord["ReferenceEntityId"];
	
	string changedById;
	emailsByName.TryGetValue((string)changeTrackingRecord["CreatedBy"], out changedById);
	changedById = changedById != null ? changedById : string.Empty;
	
	string[] emptyArray = new string[0];
	
	var change = new
	{
		ChangeId = changeId,
		ObjectBefore = objectBefore,
		ObjectAfter = objectAfter,
		ReferenceEntityType = entityType,
		ReferenceEntityId = entityId,
		ReferenceEntityDocumentType = refEntityDocType,
		FunctionName = functionName,
		TransactionId = Guid.Empty,
		CorreleationId = Guid.Empty,
		OperationId = operationId,

		ChangedAt = changeTrackingRecord["CreatedAt"],
		ChangedById = changedById,

		ChangedBy = changeTrackingRecord["CreatedBy"],
	};
	
	var envelope = new
	{
		Source = "TT",
		SourceId = changeId,
		MessageType = "ChangeModel",
		Operation = "Migration",
		CorrelationId = Guid.NewGuid().ToString(),
		MessageDate = DateTime.UtcNow,
		Blobs = emptyArray,

		Payload = change
	};

	await sbSender.SendMessageAsync(new ServiceBusMessage(JsonConvert.SerializeObject(envelope)));
	Interlocked.Increment(ref recordCount);
}

void validateObjectIds(JObject obj)
{
	if (obj == null) return;
	
	foreach (var item in obj.Descendants().OfType<JProperty>().Where(p => p.Name == "Id" && p.Value.ToString().Equals(Guid.Empty.ToString())))
	{
		item.Value = Guid.NewGuid().ToString();
	}
}

async Task BuildEmailLookup()
{
	var userProfileQuery = new QueryDefinition($@"
	SELECT 
		c.DisplayName, 
		c.Email 
	FROM c 
	WHERE c.EntityType = 'UserProfile'");
	
	using var userProfiles = accountsContainer.GetItemQueryIterator<JObject>(userProfileQuery, requestOptions: docTypeQueryOptions);
	
	while (userProfiles.HasMoreResults)
	{
		var profiles = await policy.ExecuteAsync(async () => await userProfiles.ReadNextAsync());
		foreach (var user in profiles)
		{
			if (emailsByName.ContainsKey((string)user["DisplayName"]))
			{
				emailsByName[(string)user["DisplayName"]] = (string)user["Email"];
			}
			else
			{
				emailsByName.Add((string)user["DisplayName"], (string)user["Email"]);
			}
		}
	}
}