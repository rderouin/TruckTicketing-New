using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

using Newtonsoft.Json.Linq;

using TruckTicketingCLI.Cosmos;

namespace TruckTicketingCLI;

internal class Program
{
    /// <summary>
    ///     Custom display message for when the user requests help or there is an error.
    /// </summary>
    private static void DisplayHelp(ParserResult<object> parserResult)
    {
        var helpText = HelpText.AutoBuild(parserResult, h =>
                                                        {
                                                            h.AdditionalNewLineAfterOption = false;
                                                            h.Heading = "Trident CLI - 1.0.0";
                                                            h.Copyright = "";
                                                            return HelpText.DefaultParsingErrorsHandler(parserResult, h);
                                                        }, e => e, true);

        Console.WriteLine(helpText);
        Console.WriteLine("USAGE: \n" +
                          "---------- Cosmos Database ----------\n" +
                          "   TruckTicketingCLI {Verb} --help                                             Show options for any verb command.\n" +
                          "   TruckTicketingCLI rebuild                                                   Rebuild the Trident database.\n" +
                          "   TruckTicketingCLI rebuild --container Person                                Rebuild the Person container only.\n" +
                          "   TruckTicketingCLI rebuild --soft                                            Rebuild the Trident database without deleting.\n" +
                          "   TruckTicketingCLI rebuild -c Person --soft                                  Rebuild the Person container without deleting.\n" +
                          "   TruckTicketingCLI rebuild --soft --update                                   Rebuild without deleting and overwrite existing items.\n" +
                          "   TruckTicketingCLI delete --name Trident                                     Delete the Trident database.\n" +
                          "   TruckTicketingCLI delete -n Trident -c Person                               Delete the Person container.\n" +
                          "   TruckTicketingCLI auth                                                      Set LocalAuthIds on the accounts in DEV.\n" +
                          @"   TruckTicketingCLI auth --env INT --connectionString ""connection_string""   Set LocalAuthIds on the accounts in the INT environment, requiring the INT connection string.\n" +
                          "---------- Service Bus ----------\n" +
                          "   Coming Soon                                    Trident CLI v1.1.0\n");
    }

    private static Type[] LoadVerbs()
    {
        return Assembly.GetExecutingAssembly().GetTypes()
                       .Where(t => t.GetCustomAttribute<VerbAttribute>() != null)
                       .ToArray();
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Running application...");
        try
        {
            // set the default help text to null
            var parser = new Parser(config => config.HelpWriter = null);
            // return a Type[] of all the available Verbs
            var types = LoadVerbs();
            // store the result of ParseArguments to handle errors .WithNotParsed
            var result = parser.ParseArguments(args, types);

            await parser.ParseArguments(args, types)
                        .WithParsedAsync(async x => await Parse(x));

            result.WithNotParsed(errors => DisplayHelp(result));
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e);
        }
        finally
        {
            Console.WriteLine("End of app, press any key to exit.");
            Console.ReadKey();
        }
    }

    /// <summary>
    ///     Parses the type of verb passed as an object through the main method.
    /// </summary>
    public static async Task Parse(object task)
    {
        switch (task)
        {
            case CreateOptions a:
                await CreateDatabase(a);
                break;

            case CreateContainerOptions b:
                await CreateContainer(b);
                break;

            case ScaleOptions f:
                await Scale(f);
                break;

            case DeleteOptions c:
                await Delete(c);
                break;

            case RebuildOptions d:
                await Rebuild(d);
                break;

            case RebuildAllTenantsOptions rt:
                await RebuildAll(rt);
                break;

            case SendOptions e:
                await SendServiceBusMessage(e);
                break;

            case SetLocalAuthIdOptions e:
                await SetLocalAuthIds(e);
                break;

            default:
                Console.WriteLine("Error: Invalid command.");
                break;
        }
    }

    private static async Task CreateDatabase(CreateOptions a)
    {
        a.AssertTenant();
        var config = BuildTenantConfigurationRootFromTenantName(a.Tenant);
        using var db = new CosmosDatabase(config);
        await db.CreateDatabaseAndCleanupAsync();
    }

    private static async Task CreateContainer(CreateContainerOptions b)
    {
        b.AssertTenant();
        var config = BuildTenantConfigurationRootFromTenantName(b.Tenant);
        using var db = new CosmosDatabase(config);
        await db.CreateContainerAndCleanupAsync(b.DatabaseName, b.Container);
    }

    private static async Task Scale(ScaleOptions f)
    {
        f.AssertTenant();
        var config = BuildTenantConfigurationRootFromTenantName(f.Tenant);
        using var db = new CosmosDatabase(config);
        await db.ScaleContainerAsync(f.Container);
    }

    private static async Task Delete(DeleteOptions c)
    {
        c.AssertTenant();
        var config = BuildTenantConfigurationRootFromTenantName(c.Tenant);
        using var db = new CosmosDatabase(config);

        // container argument is specified, delete container only
        if (c.Container != null)
        {
            await db.DeleteContainerAndCleanupAsync(c.Container);
        }
        // delete database
        else
        {
            await db.DeleteDatabaseAndCleanupAsync();
        }
    }

    private static async Task Rebuild(RebuildOptions d)
    {
        d.AssertTenant();
        var config = BuildTenantConfigurationRootFromTenantName(d.Tenant);
        using var db = new CosmosDatabase(config);

        // name is specified, rebuild container
        if (d.Container != null)
        {
            // do not delete
            if (d.Soft)
            {
                await db.RebuildContainerSoft(d.Container, d.Update);
            }
            // delete
            else
            {
                await db.RebuildContainer(d.Container, d.Update);
            }
        }
        // rebuild entire database
        else
        {
            // do not delete
            if (d.Soft)
            {
                await db.RebuildDatabaseSoft(d.Update);
            }
            // delete
            else
            {
                await db.RebuildDatabase(d.Update);
            }
        }
    }

    private static async Task RebuildAll(RebuildAllTenantsOptions d)
    {
        var files = GetAllTenantConfigurationFiles().ToList();
        foreach (var file in files)
        {
            var opt = new RebuildOptions
            {
                Container = d.Container,
                Update = d.Update,
                Soft = d.Soft,
                Tenant = file.Alias,
            };

            await Rebuild(opt);
        }
    }

    private static Task SendServiceBusMessage(SendOptions e)
    {
        if (e.Type == "Topic")
        {
            Console.WriteLine("Still in development.");
        }
        else if (e.Type == "Queue")
        {
            Console.WriteLine("Still in development.");
        }
        else
        {
            throw new("Invalid parameter --type passed. Select Topic or Queue.");
        }

        return Task.CompletedTask;
    }

    private static async Task SetLocalAuthIds(SetLocalAuthIdOptions e)
    {
        e.AssertTenant();
        var config = BuildTenantConfigurationRootFromTenantName(e.Tenant);

        var connectionString = e.ConnectionString ?? config["dbConnectionString"];
        var connection = new CosmosConnection(connectionString);
        var data = config.GetSection("LocalAuthIds");
        if (data == null)
        {
            throw new("'LocalAuthIds' configuration section missing.");
        }

        var envData = data.GetSection(e.Environment);
        if (envData == null)
        {
            throw new($"'LocalAuthIds' does not have an environment section for {e.Environment}.");
        }

        var userData = envData.Get<Dictionary<string, string>>();

        using (var client = new CosmosClient(connectionString))
        {
            var query = "SELECT * FROM c WHERE c.PersonId IN (" +
                        $"{string.Join(",", userData.Keys.Select(k => "\"" + k + "\""))})";

            var queryDef = new QueryDefinition(query);
            var container = client.GetContainer(connection.Database, "Person");
            var iterator = container.GetItemQueryIterator<object>(queryDef);

            while (iterator.HasMoreResults)
            {
                try
                {
                    var items = await iterator.ReadNextAsync();
                    foreach (var item in items)
                    {
                        var obj = (JObject)item;
                        var cosmosId = obj["id"].ToString();
                        obj.Remove("id");
                        var entity = obj.ToObject<Dictionary<string, object>>();
                        entity["LocalAuthId"] = userData[obj["PersonId"].ToString()];
                        entity["id"] = cosmosId;
                        container.ReplaceItemAsync(entity, cosmosId).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error occurred while updating Local Auth IDs");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex);
                    throw;
                }
            }
        }

        Console.WriteLine($"Successfully set local Auth IDs in {e.Environment}.");
    }

    private static string AssertTenantAndGetConfigurationFileName(string tenantName)
    {
        var fileName = $"tenant-{tenantName}.json";
        var path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        if (!File.Exists(path))
        {
            throw new ArgumentException($"Tenant name incorrect or not supported - no config file found for '{fileName}'.");
        }

        return fileName;
    }

    private static IConfigurationRoot BuildTenantConfigurationRootFromTenantName(string tenantName)
    {
        var file = AssertTenantAndGetConfigurationFileName(tenantName);
        var result = new ConfigurationBuilder()
                    .AddJsonFile(file, false)
                    .AddJsonFile("local.settings.json", true)
                    .Build();

        return result;
    }

    private static IEnumerable<(FileInfo File, string Alias)> GetAllTenantConfigurationFiles()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        var result = dir
                    .EnumerateFiles("tenant-*.json", SearchOption.TopDirectoryOnly)
                    .Select(f => (f, f.Name.Replace("tenant-", "").Replace(".json", "")));

        return result;
    }
}
