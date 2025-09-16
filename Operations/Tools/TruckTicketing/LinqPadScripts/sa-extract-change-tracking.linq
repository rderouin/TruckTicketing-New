<Query Kind="Statements">
  <NuGetReference>Azure.Storage.Blobs</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>Polly</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>Azure.Storage.Blobs</Namespace>
  <Namespace>Azure.Storage.Blobs.Models</Namespace>
</Query>


// inputs
var connName = Util.ReadLine("Storage Account Connection Name");    // tt-prod-sa-conn
var entityType = Util.ReadLine("Entity Type");                      // Invoice
var entityId = Util.ReadLine("Entity ID");                          // 9be2607c-fa12-4618-8d04-9196f3b21e07
var folder = Util.ReadLine("Folder path to store the files");       // C:\temp\selected-blobs

////////////////////////////////////////////////////////////////////////////////////////////////////
var conn = Util.GetPassword(connName);
var blobContainerClient = new BlobContainerClient(conn, "changes");

// pattern: $"{entityType}/{utcDate:O}/{utcTime:O}/{entityId}/{userId}/{operationId}/{changeId}.json"
var prefix = $"changes/{entityType}/";

// process the folder
var selectedBlobs = new List<BlobItem>();
var blobs = blobContainerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix);
await foreach (var blob in blobs)
{
	// parse the path
	var pathElements = blob.Name.Split('/');
	if (pathElements.Length != 8) continue;

	// find the changes
	var blobEntityId = pathElements[4];
	if (string.Equals(entityId, blobEntityId, StringComparison.OrdinalIgnoreCase))
	{
		selectedBlobs.Add(blob);
	}
}

$"Files to download: {selectedBlobs.Count}".Dump();

// save all blobs
var counter = 1;
foreach (var blob in selectedBlobs)
{
	blob.Name.Dump($"Downloading {counter++} of {selectedBlobs.Count}...");

	// define a file name for the selected blob
	var pathElements = blob.Name.Split('/');
	var fileName = string.Join("_", pathElements[4], pathElements[2], pathElements[3].Replace(':', '-').Replace('.', '-'), pathElements[7]);
	var filePath = Path.Join(folder, fileName);

	// download the JSON document
	var blobClient = blobContainerClient.GetBlobClient(blob.Name);
	await using var fileStream = new FileStream(filePath, FileMode.Create);
	blobClient.DownloadTo(fileStream);
	fileStream.Close();

	// reformat
	var json = File.ReadAllText(filePath);
	var jToken = JToken.Parse(json);
	var formatted = jToken.ToString(Newtonsoft.Json.Formatting.Indented);
	File.WriteAllText(filePath, formatted);
}

"Done!".Dump();
