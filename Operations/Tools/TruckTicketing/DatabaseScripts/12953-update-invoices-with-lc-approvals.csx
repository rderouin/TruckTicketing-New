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

// substitute the 'Args' argument array when running in LINQPAD
#if LINQPAD
var Args = new List<string>
{
	Util.GetPassword("tt-qa-cosmos-conn"),
};
#endif

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
var billingContainer = database.GetContainer("Billing");

// vars
int invoicesProcessed = 0;
int invoicesTotal = 0;
int bounces = 0;

// policies
var feedPolicy = ResiliencePipelines.SetupCosmosFeedResiliencePipeline(arg => { bounces++; return ValueTask.CompletedTask; });
var genericPolicy = ResiliencePipelines.SetupCosmosGenericResiliencePipeline(arg => { bounces++; return ValueTask.CompletedTask; });
var policySet = (feedPolicy, genericPolicy);

// progress
var progress = SimpleProgress.StartTracking("Invoices", "updated {1} of {0} (bounces: {2})");

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
	var queryPattern = "SELECT {0} FROM c WHERE c.EntityType = 'Invoice'";
	var totalQuery = string.Format(queryPattern, "COUNT(1) as total");
	var mainQuery = string.Format(queryPattern, "c.id, c.Id, c.DocumentType");

	// totals
	var totalDoc = await CosmosTools.QueryOne(operationsContainer, policySet, totalQuery, null);
	invoicesTotal = (int)totalDoc["total"];

	// main query
	await CosmosTools.QueryStreaming(operationsContainer, policySet, mainQuery, null, ProcessInvoicesChunked, new(), new QueryRequestOptions
	{
		MaxBufferedItemCount = 1_000,
		MaxConcurrency = 10,
		MaxItemCount = 1_000,
	});

	async Task ProcessInvoicesChunked(FeedResponse<JObject> batch)
	{
		var tasks = batch.Chunk(100).Select(ProcessInvoices);
		await Task.WhenAll(tasks);
	}

	Log.Info(nameof(ProcessAll), $"Actual updates: {invoicesProcessed}");
}

async Task ProcessInvoices(JObject[] invoices)
{
	foreach (var invoice in invoices)
	{
		await ProcessInvoice(invoice);
	}
}

async Task ProcessInvoice(JObject invoice)
{
	// check all approvals
	var hasAllApprovals = await CheckAllLoadConfirmationsForApprovals((string)invoice["Id"]);

	// update the invoice flag
	await PatchInvoice((string)invoice["id"], (string)invoice["DocumentType"], hasAllApprovals);

	// stats
	Interlocked.Increment(ref invoicesProcessed);
	progress.Update(invoicesTotal, invoicesProcessed, bounces);
}

async Task<bool> CheckAllLoadConfirmationsForApprovals(string invoiceId)
{
	var loadConfirmations = await FetchLoadConfirmationsByInvoiceId(invoiceId);

	// prefilter
	var groupedLoadConfirmations = loadConfirmations.Select<JObject, (JObject lc, string status)>(lc => (lc, (string)lc["Status"])).GroupBy(p => p.status);

	// remove terminal statuses
	var targetedLCs = groupedLoadConfirmations.Where(g => g.Key != "Posted" && g.Key != "Void");

	// check early statuses
	if (targetedLCs.Where(g => g.Key != "WaitingForInvoice").Any())
	{
		return false;
	}

	var lastGroup = targetedLCs.Where(g => g.Key == "WaitingForInvoice").FirstOrDefault();
	if (lastGroup != null)
	{
		foreach (var p in lastGroup)
		{
			var lc = p.lc;

			// check billing config here. (See Acceptance Criteria for this logic) https://dev.azure.com/secure-energy-services/D365FO/_workitems/edit/12953
			var billingConfigurationId = (string)lc["BillingConfigurationId"];
			if (string.IsNullOrEmpty(billingConfigurationId))
			{
				Log.Info(nameof(CheckAllLoadConfirmationsForApprovals), "BillingConfigurationId from LC is null or empty");
				return false;
			}

			// billing config
			var billingConfig = await FetchBillingConfigurationById(billingConfigurationId);
			if (billingConfig != null)
			{
				var isSignatureRequired = (bool)billingConfig["IsSignatureRequired"]!;
				if (isSignatureRequired)
				{
					// check the attachments
					if (HasApprovedAttachment(lc) == false)
					{
						return false;
					}
				}
			}
			else
			{
				return false;
			}
		}
	}

	return true;
}

bool HasApprovedAttachment(JObject lc)
{
	var attachments = lc["Attachments"];
	if (attachments != null)
	{
		foreach (var attachment in attachments)
		{
			var IsIncludedInInvoiceToken = attachment["IsIncludedInInvoice"];
			if (IsIncludedInInvoiceToken != null)
			{
				var isIncludedInInvoice = (bool?)IsIncludedInInvoiceToken;
				if (isIncludedInInvoice.HasValue && isIncludedInInvoice.Value)
				{
					return true;
				}
			}
		}
	}

	return false;
}

async Task<JObject> FetchBillingConfigurationById(string billingConfigurationId)
{
	var query = @"
SELECT
	c.id,
	c.DocumentType,
	c.Id,
	c.IsSignatureRequired
FROM c
WHERE
	c.EntityType = 'BillingConfiguration'
AND c.DocumentType = 'BillingConfiguration'
AND c.Id = @id";

	var billingConfiguration = await CosmosTools.QueryOne(
		billingContainer,
		policySet,
		query,
		"BillingConfiguration",
		new()
		{
			["@id"] = billingConfigurationId
		},
		new()
		{
			MaxBufferedItemCount = 10_000,
			MaxConcurrency = 10,
			MaxItemCount = 10_000,
		});

	return billingConfiguration;
}

async Task<List<JObject>> FetchLoadConfirmationsByInvoiceId(string invoiceId)
{
	var query = @"
SELECT
	c.Id,
	c.Status,
	c.BillingConfigurationId,
	c.Attachments
FROM c
WHERE
	c.EntityType = 'LoadConfirmation'
AND c.InvoiceId = @invoiceId";

	var loadConfirmations = await CosmosTools.QueryAll(
		operationsContainer,
		policySet,
		query,
		null,
		new()
		{
			["@invoiceId"] = invoiceId
		},
		new()
		{
			MaxBufferedItemCount = 10_000,
			MaxConcurrency = 10,
			MaxItemCount = 10_000,
		});

	return loadConfirmations;
}

async Task PatchInvoice(string invoiceId, string partitionKey, bool propertyValue)
{
	await CosmosTools.PatchProperty(operationsContainer, genericPolicy, invoiceId, partitionKey, $"/HasAllLoadConfirmationApprovals", propertyValue);
}
