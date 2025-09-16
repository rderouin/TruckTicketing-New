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
    Util.GetPassword("tt-dev-cosmos-conn"),
};
#endif

// ---=== MAIN SCRIPT ===---
// init
var q = new Queue<string>(Args);
q.TryDequeue(out var sourceConnectionString);
q.TryDequeue(out var destinationConnectionString);
if (string.IsNullOrWhiteSpace(sourceConnectionString) || string.IsNullOrWhiteSpace(destinationConnectionString))
{
    Log.Error("CommandLine", "The connection strings for source and destinations must be provided.");
    return;
}

// cosmos connections
var sourceClient = new CosmosClient(sourceConnectionString);
var sourceDatabase = sourceClient.GetDatabase("TruckTicketing");
var sourceContainer = sourceDatabase.GetContainer("Accounts");

var destinationClient = new CosmosClient(destinationConnectionString);
var destinationDatabase = destinationClient.GetDatabase("TruckTicketing");
var destinationContainer = destinationDatabase.GetContainer("Accounts");

// vars
int bounces = 0;

// policies
var feedPolicy = ResiliencePipelines.SetupCosmosFeedResiliencePipeline(arg => { bounces++; return ValueTask.CompletedTask; });
var genericPolicy = ResiliencePipelines.SetupCosmosGenericResiliencePipeline(arg => { bounces++; return ValueTask.CompletedTask; });
var policySet = (feedPolicy, genericPolicy);

// progress
var progress = SimpleProgress.StartTracking("Accounts", "updated {1} of {0} (bounces: {2})");

var documentTypes = new List<string>();
var entityTypes = new List<string>();
var documentTypesEntityTypes = new List<DocumentTypeEntityType>();

// kick off
Log.Info("General", $"{DateTime.Now}");
using (SimpleTimer.Show("Entire script"))
{
    await PopulateAllDocumentTypes();
    await PopulateAllEntityTypes();
    await PopulateAllDocumentTypesEntityTypes();

    //await ShowAllTypes();
    //await ShowAllAccountsEntitiesByEntityType("Permission");
    //await ShowAllAccountsEntitiesByEntityType("Role");
    //await ShowAllAccountsEntitiesByEntityType("UserProfile");
    //await ShowAllConfigurationEntitiesByEntityType("NavigationConfiguration");

    await DeleteAllByDocumentTypeEntityType("Accounts", "Permission");
    await CopyAllByDocumentTypeEntityType("Accounts", "Permission");
    
    await DeleteAllByDocumentTypeEntityType("Accounts", "Role");
    await CopyAllByDocumentTypeEntityType("Accounts", "Role");

    await DeleteAllByDocumentTypeEntityType("Configuration", "NavigationConfiguration");
    await CopyAllByDocumentTypeEntityType("Configuration", "NavigationConfiguration");
    
    await FixUpUserProfileRoles();

    if (destinationConnectionString.Contains("devint"))
    {
        await FixUpDeveloperUserProfileRoles();
    }
}
Log.Info("General", $"{DateTime.Now}");

async Task ShowAllTypes()
{
#if LINQPAD
    documentTypes.Dump();
    entityTypes.Dump();
    documentTypesEntityTypes.Dump();
#endif
}

async Task ShowAllAccountsEntitiesByEntityType(string entityType)
{
    await ShowAllEntitiesByDocumentTypeEntityType("Accounts", entityType);
}

async Task ShowAllConfigurationEntitiesByEntityType(string entityType)
{
    await ShowAllEntitiesByDocumentTypeEntityType("Configuration", entityType);
}

async Task ShowAllEntitiesByDocumentTypeEntityType(string documentType, string entityType)
{
    var allDocs = await GetAllByDocumentTypeEntityType(documentType, entityType);

#if LINQPAD
    allDocs.Dump();
#else
    allDocs.ForEach(d => Log.Info("Dump", d.ToString()));
#endif
}

// main method
async Task PopulateAllDocumentTypes()
{
    documentTypes.AddRange(await GetAllDistinct("DocumentType"));
}

async Task PopulateAllEntityTypes()
{
    entityTypes.AddRange(await GetAllDistinct("EntityType"));
}

async Task PopulateAllDocumentTypesEntityTypes()
{
    documentTypesEntityTypes.AddRange((await GetAllDistinct2("DocumentType", "EntityType")).Select(tpl => new DocumentTypeEntityType(tpl.Item1, tpl.Item2)));
}

async Task DeleteAllByDocumentTypeEntityType(string documentType, string entityType)
{
    var allRoles = await GetAllByDocumentTypeEntityType(documentType, entityType, destinationContainer);
    var idsToDelete = allRoles.Select(r => r["id"].ToString()).ToList();

    foreach (var idToDelete in idsToDelete)
    {
        await DeleteEntity(idToDelete, documentType);

        Log.Info("DeleteAllByDocumentTypeEntityType", $"Deleted: {idToDelete}");
    }
}

async Task CopyAllByDocumentTypeEntityType(string documentType, string entityType)
{
    var allDocs = await GetAllByDocumentTypeEntityType(documentType, entityType);

    foreach (var doc in allDocs)
    {
        var item = await UpsertEntity(doc, null, documentType);

        Log.Info("CopyAllByDocumentTypeEntityType", $"Inserted: {item["id"]}");
    }
}

async Task FixUpUserProfileRoles()
{
    var allRoles = await GetAllByDocumentTypeEntityType("Accounts", "Role", destinationContainer);
    var allUserProfiles = await GetAllByDocumentTypeEntityType("Accounts", "UserProfile", destinationContainer);

    foreach (var userProfile in allUserProfiles)
    {
        var userDisplayName = userProfile["DisplayName"].ToString();

        var userProfileToUpdate = false;
        var userRoleIdsToDelete = new List<string>();

        var userRoles = userProfile["Roles"] as JArray ?? new JArray();

        foreach (var userRole in userRoles)
        {
            // Find user's currently assigned role in Roles (synced from source env)
            // If the role doesn't exist remove it
            // If the role exists, ensure that the RoleId matches what is in the Roles
            
            var roleName = userRole["RoleName"].ToString();

            var role = allRoles.Find(r => r["Name"].ToString() == roleName);

            if (role == null)
            {
                Log.Info("FixUpUserProfileRoles", $"{userDisplayName}'s current role {roleName} is not found and would be deleted");

                userRoleIdsToDelete.Add(userRole["RoleId"].ToString());
                userProfileToUpdate = true;
            }
            else
            {
                if (role["Id"].ToString() != userRole["RoleId"].ToString())
                {
                    Log.Info("FixUpUserProfileRoles", $"{userDisplayName}'s current role {roleName} has a new RoleId: {role["Id"]}");

                    userRole["RoleId"] = role["Id"];
                    userProfileToUpdate = true;
                }
            }
        }

        foreach (var userRoleIdToDelete in userRoleIdsToDelete)
        {
            Log.Info("FixUpUserProfileRoles", $"Deleting {userDisplayName}'s invalid role {userRoleIdToDelete}");

            userProfile["Roles"].First(ur => ur["RoleId"].ToString() == userRoleIdToDelete).Remove();
        }

        if (userProfileToUpdate)
        {
            var item = await UpsertEntity(userProfile, null, "Accounts");

            Log.Info("FixUpUserProfileRoles", $"{userDisplayName}'s updated profile saved.");
        }
    }
}

async Task FixUpDeveloperUserProfileRoles()
{
    var roleNames = new[] { "System Administrator", "Admin Process Advisor" };
    var userNames = new[] {
        "Amit Gupta",
        "Arinze Anozie",
        "David Zeyha",
        "Denis Zhurba",
        "Lindsay Sutherland",
        "Nick Pylypow",
		"Nickolaus Pylypow",
        "Panth Shah",
        "Steve Love"
    };

    foreach (var userName in userNames)
    {
        foreach (var roleName in roleNames)
        {
            await UpdateUserProfile_AddRole(userName, roleName);
        }
        
        await UpdateUserProfile_AllowAllFacilityAdministration(userName);
    }
}

async Task UpdateUserProfile_AllowAllFacilityAdministration(string userDisplayName)
{
    var allUserProfiles = await GetAllByDocumentTypeEntityType("Accounts", "UserProfile", destinationContainer);

    var userProfile = allUserProfiles.Find(up => up["DisplayName"].ToString() == userDisplayName);

    if (userProfile == null)
    {
        Log.Info("UpdateUserProfile_AllowAllFacilityAdministration", $"{userDisplayName} not found");

        return;
    }
    
    var enforceSpecificFacilityAccessLevels = userProfile["EnforceSpecificFacilityAccessLevels"]?.ToObject<bool>();
    
    if (enforceSpecificFacilityAccessLevels != false)
    {
        userProfile["EnforceSpecificFacilityAccessLevels"] = false;

        var item = await UpsertEntity(userProfile, null, "Accounts");

        Log.Info("UpdateUserProfile_AllowAllFacilityAdministration", $"{userDisplayName}'s updated profile saved after resetting EnforceSpecificFacilityAccessLevels to false.");
    }
}

async Task UpdateUserProfile_AddRole(string userDisplayName, string roleName)
{
    var allRoles = await GetAllByDocumentTypeEntityType("Accounts", "Role", destinationContainer);
    var allUserProfiles = await GetAllByDocumentTypeEntityType("Accounts", "UserProfile", destinationContainer);

    var userProfile = allUserProfiles.Find(up => up["DisplayName"].ToString() == userDisplayName);

    if (userProfile == null)
    {
        Log.Info("UpdateUserProfile_AddRole", $"{userDisplayName} not found");

        return;
    }

    var userRoles = userProfile["Roles"] as JArray ?? new JArray();

    var role = allRoles.Find(r => r["Name"].ToString() == roleName);

    if (role == null)
    {
        Log.Info("UpdateUserProfile_AddRole", $"{roleName} is not found");

        return;
    }

    // Find user's currently assigned role
    // If it already exists ensure that the RoleId is accurate
    // If the role doesn't add it
    var userRole = userRoles.ToList().Find(r => r["RoleName"].ToString() == roleName);

    if (userRole == null)
    {
        var addedRole = new JObject
        {
            { "Id", Guid.NewGuid() },
            { "RoleId", role["Id"].ToString() },
            { "RoleName", role["Name"].ToString() }
        };

        userRoles.Add(addedRole);

        var item = await UpsertEntity(userProfile, null, "Accounts");

        Log.Info("UpdateUserProfile_AddRole", $"{userDisplayName}'s updated profile saved after adding role {roleName}.");
    }
    else
    {
        if (role["Id"].ToString() != userRole["RoleId"].ToString())
        {
            Log.Info("UpdateUserProfile_AddRole", $"{userDisplayName}'s current role {roleName}'s RoleId is stale. New RoleId: {role["Id"]}");

            userRole["RoleId"] = role["Id"];

            var item = await UpsertEntity(userProfile, null, "Accounts");

            Log.Info("UpdateUserProfile_AddRole", $"{userDisplayName}'s updated profile saved after updating roleId {role["Id"]}.");
        }
    }
}

async Task<List<JObject>> GetAllByDocumentTypeEntityType(string documentType, string entityType, Container container = null)
{
	var allDocs = await GetAll2("DocumentType", documentType, "EntityType", entityType, container);

	return allDocs;
}

async Task<List<string>> GetAllDistinct(string propertyName, Container container = null)
{
	container = container ?? sourceContainer;

	var queryPattern = "SELECT DISTINCT c.{0} as PropertyValue FROM c";
	var query = string.Format(queryPattern, propertyName);

	var allDistinctPropertyValues = await CosmosTools.QueryAll(container, policySet, query, null);

	return allDistinctPropertyValues.Select(x => (string)x["PropertyValue"]).ToList();
}

async Task<List<Tuple<string, string>>> GetAllDistinct2(string propertyName1, string propertyName2, Container container = null)
{
	container = container ?? sourceContainer;

	var queryPattern = "SELECT DISTINCT c.{0} as PropertyValue1, c.{1} as PropertyValue2 FROM c";
	var query = string.Format(queryPattern, propertyName1, propertyName2);

	var allDistinctPropertyValues = await CosmosTools.QueryAll(container, policySet, query, null);

	return allDistinctPropertyValues.Select(x => new Tuple<string, string>((string)x["PropertyValue1"], (string)x["PropertyValue2"])).ToList();
}

async Task<List<JObject>> GetAll2(string propertyName1, string propertyValue1, string propertyName2, string propertyValue2, Container container = null)
{
	container = container ?? sourceContainer;

	var query = $"SELECT * FROM c WHERE c.{propertyName1} = '{propertyValue1}' AND c.{propertyName2} = '{propertyValue2}'";

	var allDocs = await CosmosTools.QueryAll(container, policySet, query, null);

	return allDocs;
}

async Task<JObject> UpsertEntity(JObject doc, string id, string partitionKey)
{
	return await CosmosTools.UpsertEntity(destinationContainer, doc, id, partitionKey);
}

async Task DeleteEntity(string id, string partitionKey = null)
{
	await CosmosTools.DeleteEntity(destinationContainer, id, partitionKey);

	return;
}

public readonly record struct DocumentTypeEntityType(string DocumentType, string EntityType);
