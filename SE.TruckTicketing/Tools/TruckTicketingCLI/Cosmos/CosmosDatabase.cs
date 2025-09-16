using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace TruckTicketingCLI.Cosmos;

public sealed class CosmosDatabase : IDisposable
{
    // The name of the database and container we will create
    private const string DEFAULT_PARTITION_KEY = "/DocumentType";

    private readonly IConfigurationRoot _config;

    private readonly CosmosClient _cosmosClient;

    public CosmosDatabase(IConfigurationRoot config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cosmosClient ??= new(EndpointUri, PrimaryKey);
    }

    // The Azure Cosmos DB endpoint for running this sample.
    private string EndpointUri => _config["EndpointUri"];

    // The primary key for the Azure Cosmos account.
    private string PrimaryKey => _config["PrimaryKey"];

    // The connection string for the Azure Cosmos account.
    private string ConnectionString => GetConnectionString();

    // The flag for autoscale throughput setting
    private bool IsAutoscale => _config.GetValue<bool>("autoscale");

    // The max throughput clamp on an autoscale container
    private int AutoscaleMaxThroughput => _config.GetValue<int>("autoscaleMaxThroughput");

    // The throughput RU/s when the database is not autoscaled
    private int Throughput => _config.GetValue<int>("throughput");

    // The name of the db to create
    private string DatabaseName => _config.GetValue<string>("databaseName");

    public void Dispose()
    {
        _cosmosClient?.Dispose();
    }

    private Database GetDatabase()
    {
        return _cosmosClient.GetDatabase(DatabaseName);
    }

    private string GetConnectionString()
    {
        var result = _config["dbConnectionString"];
        var tokens = Mustache.GetContents(result);
        if (tokens.Any())
        {
            result = Mustache.ReplaceContents(result, DatabaseName);
        }

        return result;
    }

    /// <summary>
    ///     Creates a new database if it does not exist.
    /// </summary>
    internal async Task<Database> CreateDatabaseAsync()
    {
        try
        {
            var response = await _cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
            if (response.StatusCode == HttpStatusCode.Created)
            {
                Console.WriteLine("Created Database: {0}", DatabaseName);
            }
            else if (response.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine("Database {0} was found, no changes made.", DatabaseName);
            }

            var result = response.Database;
            return result;
        }
        catch (CosmosException de)
        {
            _ = de.GetBaseException();
            Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
            return null;
        }
    }

    /// <summary>
    ///     Creates a new database if it does not exist.
    ///     Disposes the Cosmos client.
    /// </summary>
    internal async Task CreateDatabaseAndCleanupAsync()
    {
        try
        {
            var properties = ThroughputProperties.CreateManualThroughput(Throughput);
            if (IsAutoscale)
            {
                properties = ThroughputProperties.CreateAutoscaleThroughput(AutoscaleMaxThroughput);
            }

            var response = await _cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName, properties);
            if (response.StatusCode == HttpStatusCode.Created)
            {
                Console.WriteLine("Created Database: {0}\n", DatabaseName);
            }
            else if (response.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine("Database {0} was found, throughput settings not changed.\n", DatabaseName);
            }
        }
        catch (CosmosException de)
        {
            _ = de.GetBaseException();
            Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
        }

        Dispose();
    }

    /// <summary>
    ///     Create the container if it does not exist.
    ///     Specify "/DocType" as the partition key.
    /// </summary>
    /// <returns></returns>
    internal async Task CreateContainerAsync(string containerName, string partitionKey = DEFAULT_PARTITION_KEY)
    {
        try
        {
            ContainerProperties containerProperties = new()
            {
                Id = containerName,
                PartitionKeyPath = partitionKey,
            };

            var properties = ThroughputProperties.CreateManualThroughput(Throughput);
            if (IsAutoscale)
            {
                properties = ThroughputProperties.CreateAutoscaleThroughput(AutoscaleMaxThroughput);
            }

            var database = GetDatabase();
            var response = await database.CreateContainerIfNotExistsAsync(containerProperties, properties);
            if (response.StatusCode == HttpStatusCode.Created)
            {
                Console.WriteLine("Created Container: {0}", containerName);
            }
            else if (response.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine("Container {0} was found.", containerName);
            }

            await CreateStoredProceduresAsync(response.Container);
        }
        catch (CosmosException de)
        {
            _ = de.GetBaseException();
            Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
        }
    }

    /// <summary>
    ///     Create the container if it does not exist.
    ///     Specify "/DocType" as the partition key
    /// </summary>
    /// <returns></returns>
    internal async Task CreateContainerAndCleanupAsync(string containerName, string partitionKey = DEFAULT_PARTITION_KEY)
    {
        try
        {
            var database = GetDatabase();
            var response = await database.CreateContainerIfNotExistsAsync(containerName, partitionKey);
            if (response.StatusCode == HttpStatusCode.Created)
            {
                Console.WriteLine("Created Container: {0}\n", containerName);
            }
            else if (response.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine("Container {0} was found.\n", containerName);
            }
        }
        catch (CosmosException de)
        {
            _ = de.GetBaseException();
            Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
        }

        Dispose();
    }

    /// <summary>
    ///     Scale the throughput provisioned on an existing Container.
    ///     You can scale the throughput (RU/s) of your container up and down to meet the needs of the workload. Learn more: https://aka.ms/cosmos-request-units
    /// </summary>
    /// <returns></returns>
    internal async Task ScaleContainerAsync(string containerName)
    {
        try
        {
            var database = GetDatabase();
            var container = database.GetContainer(containerName);
            await container.ReplaceThroughputAsync(Throughput);

            Console.WriteLine("{0} new provisioned throughput: {1}", containerName, Throughput);
        }
        catch (CosmosException ce)
        {
            Console.WriteLine(ce.StatusCode);
        }

        Dispose();
    }

    /// <summary>
    ///     Delete the database without disposing the Cosmos client instance.
    /// </summary>
    internal async Task DeleteDatabaseAsync()
    {
        try
        {
            var database = GetDatabase();
            await database.DeleteAsync();
            Console.WriteLine("Deleted Database: {0}\n", DatabaseName);
        }
        catch (CosmosException de)
        {
            if (de.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Database {0} was not found, unable to delete.\n", DatabaseName);
            }
            else
            {
                _ = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
            }
        }
    }

    /// <summary>
    ///     Delete the database and dispose of the Cosmos Client instance.
    /// </summary>
    internal async Task DeleteDatabaseAndCleanupAsync()
    {
        try
        {
            var database = GetDatabase();
            await database.DeleteAsync();
        }
        catch (CosmosException de)
        {
            if (de.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Database {0} was not found, unable to delete.\n", DatabaseName);
                throw new("Please specify an existing database.");
            }

            _ = de.GetBaseException();
            Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
        }

        Console.WriteLine("Deleted Database: {0}\n", DatabaseName);

        Dispose();
    }

    /// <summary>
    ///     Delete a container with given databaseId and containerId.
    /// </summary>
    internal async Task DeleteContainerAsync(string containerName)
    {
        try
        {
            var database = GetDatabase();
            var container = database.GetContainer(containerName);
            await container.DeleteContainerAsync();
        }
        catch (CosmosException de)
        {
            if (de.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Container {0} was not found, unable to delete.\n", containerName);
                throw new("Please specify an existing container.");
            }

            _ = de.GetBaseException();
            Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
        }

        Console.WriteLine("Deleted Container: {0}\n", containerName);
    }

    /// <summary>
    ///     Delete a container with given databaseId and containerId.
    /// </summary>
    internal async Task DeleteContainerAndCleanupAsync(string containerName)
    {
        try
        {
            var database = GetDatabase();
            var container = database.GetContainer(containerName);
            await container.DeleteContainerAsync();
        }
        catch (CosmosException de)
        {
            if (de.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Container {0} was not found, unable to delete.\n", containerName);
                throw new("Please specify an existing container.");
            }

            _ = de.GetBaseException();
            Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
        }

        Console.WriteLine("Deleted Container: {0}\n", containerName);

        Dispose();
    }

    /// <summary>
    ///     Delete and rebuild the Trident database.
    /// </summary>
    internal async Task RebuildDatabase(bool update)
    {
        // initialize set data structure to create containers only once
        var set = new HashSet<string>();

        await DeleteDatabaseAsync();
        await CreateDatabaseAsync();

        foreach (var item in _config.GetSection("Data").GetChildren())
        {
            var container = item["container"];
            if (!set.Contains(container))
            {
                set.Add(container);
                await CreateContainerAsync(container);
            }
        }

        Console.WriteLine("\nMigrating data...");
        await MigrateDatabase(update);

        Dispose();
    }

    /// <summary>
    ///     Rebuild the Trident database without deleting.
    /// </summary>
    internal async Task RebuildDatabaseSoft(bool update)
    {
        // initialize set data structure to create containers only once
        var set = new HashSet<string>();

        await CreateDatabaseAsync();
        foreach (var item in _config.GetSection("Data").GetChildren())
        {
            var container = item["container"];
            if (!set.Contains(container))
            {
                set.Add(container);
                await CreateContainerAsync(container);
            }
        }

        Console.WriteLine("\nMigrating data...");
        await MigrateDatabase(update);

        Dispose();
    }

    /// <summary>
    ///     Delete and rebuild the chosen container.
    /// </summary>
    internal async Task RebuildContainer(string containerName, bool update)
    {
        await DeleteContainerAsync(containerName);
        await CreateContainerAsync(containerName);

        Console.WriteLine("\nMigrating {0} container data...", containerName);
        await MigrateContainer(containerName, update);

        Dispose();
    }

    /// <summary>
    ///     Delete and rebuild the chosen container, without deleting.
    /// </summary>
    internal async Task RebuildContainerSoft(string containerName, bool update)
    {
        await CreateContainerAsync(containerName);
        Console.WriteLine("\nMigrating {0} container data...", containerName);
        await MigrateContainer(containerName, update);

        Dispose();
    }

    /// <summary>
    ///     Import JSON data using the DocumentDB Data Migration Tool - entire database.
    /// </summary>
    internal async Task MigrateDatabase(bool update)
    {
        // Loop through each file in Data array
        foreach (var item in _config.GetSection("Data").GetChildren())
        {
            if (string.IsNullOrEmpty(item["path"]))
            {
                continue;
            }

            var path = GetDataPath(_config["DataPath"], item, "path");

            // Get custom path strings
            var command = ReplaceCommand(item["container"], path);
            var args = "/c " + GetDtPath(_config["MigrationPath"]) + " " + command;

            if (update)
            {
                args += " /t.UpdateExisting";
            }

            // Start the process
            Process p = new()
            {
                StartInfo = new()
                {
                    FileName = "cmd.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = true,
                },
            };

            p.Start();
            Console.WriteLine($"Rebuilding {item["label"]}...");

            // Prevent next item from starting until this one is done.
            await p.WaitForExitAsync();

            Console.WriteLine("Done.\n");
        }
    }

    private static string GetDataPath(string path, IConfigurationSection item, string value)
    {
        if (!string.IsNullOrEmpty(path))
        {
            return Path.Join(path, item[value]);
        }

        // DEBUG: ".\\..\\..\\..\\..\\..\\..\\..\\scholar\\System\\Data"
        // RELEASE: ".\\..\\scholar\\System\\Data"
        return Path.Join(".\\..\\scholar\\System\\Data", item[value]);
    }

    // Helper functions
    private static string GetDtPath(string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            return Path.Join(path, "dt.exe");
        }

        return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dt-1.7", "dt.exe");
    }

    private string ReplaceCommand(string name, string path, string partitionKey = DEFAULT_PARTITION_KEY)
    {
        return "/s:JsonFile " +
               $"/s.Files:{path} " +
               "/t:DocumentDB " +
               $"/t.ConnectionString:{ConnectionString} " +
               $"/t.Collection:{name} " +
               $"/t.PartitionKey:{partitionKey}";
    }
    // <MigrateDatabase>

    // <MigrateContainer>
    /// <summary>
    ///     Import JSON data using the DocumentDB Data Migration Tool - one container.
    /// </summary>
    internal async Task MigrateContainer(string containerName, bool update)
    {
        // Loop through each file in Data array
        foreach (var item in _config.GetSection("Data").GetChildren())
        {
            // linear search for the container in appSettings
            var container = item["container"];
            if (container.Equals(containerName))
            {
                if (string.IsNullOrEmpty(item["path"]))
                {
                    continue;
                }

                var path = GetDataPath(_config["DataPath"], item, "path");

                // Get custom path strings
                var command = ReplaceCommand(container, path);
                var args = "/c " + GetDtPath(_config["MigrationPath"]) + " " + command;

                if (update)
                {
                    args += " /t.UpdateExisting";
                }

                // Start the process
                Process p = new()
                {
                    StartInfo = new()
                    {
                        FileName = "cmd.exe",
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = true,
                    },
                };

                p.Start();
                Console.WriteLine($"Rebuilding {item["label"]}...");

                // Prevent next item from starting until this one is done.
                await p.WaitForExitAsync();

                Console.WriteLine("Done.\n");
            }
        }
    }

    internal async Task CreateStoredProceduresAsync(Container container)
    {
        if (container != null)
        {
            try
            {
                //Foreach loop to iterate over children in data
                foreach (var item in _config.GetSection("Data").GetChildren())
                {
                    if (!string.IsNullOrEmpty(item["container"]) && item["container"] == container.Id)
                    {
                        var storedProcedurePath = GetDataPath(_config["DataPath"], item, "storedProcedurePath");
                        if (Directory.Exists(Path.GetDirectoryName(storedProcedurePath)))
                        {
                            //Dir name, file name+search rules
                            foreach (var file in Directory.GetFiles(Path.GetDirectoryName(storedProcedurePath), Path.GetFileName(storedProcedurePath)))
                            {
                                var fileContents = await File.ReadAllTextAsync(file);
                                var storedProcedureId = Path.GetFileNameWithoutExtension(file);
                                Console.WriteLine($"Stored Procedure: {storedProcedureId}");

                                try
                                {
                                    var storedProcedureResponse = await container.Scripts.ReadStoredProcedureAsync(storedProcedureId);
                                    if (storedProcedureResponse.StatusCode == HttpStatusCode.OK)
                                    {
                                        storedProcedureResponse = await container.Scripts.ReplaceStoredProcedureAsync(new()
                                        {
                                            Id = storedProcedureId,
                                            Body = fileContents,
                                        });

                                        if (storedProcedureResponse.StatusCode == HttpStatusCode.OK)
                                        {
                                            Console.WriteLine("Stored procedure successfully replaced.");
                                        }
                                        else
                                        {
                                            Console.WriteLine("Stored procedure was not replaced. Status code {0}", storedProcedureResponse.StatusCode);
                                        }
                                    }
                                }
                                catch (CosmosException de)
                                {
                                    if (de.StatusCode == HttpStatusCode.NotFound)
                                    {
                                        var storedProcedureResponse = await container.Scripts.CreateStoredProcedureAsync(new()
                                        {
                                            Id = storedProcedureId,
                                            Body = fileContents,
                                        });

                                        if (storedProcedureResponse.StatusCode == HttpStatusCode.Created)
                                        {
                                            Console.WriteLine("Stored procedure successfully created.");
                                        }
                                        else
                                        {
                                            Console.WriteLine("Stored procedure failed to create. Status code {0}", storedProcedureResponse.StatusCode);
                                        }
                                    }
                                    else
                                    {
                                        _ = de.GetBaseException();
                                        Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (CosmosException de)
            {
                _ = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
            }
        }
        else
        {
            Console.WriteLine("Container does not exist.");
        }
    }
}
