using System;

using CommandLine;

namespace TruckTicketingCLI;

public interface ITenantSpecific
{
    string Tenant { get; set; }

    void AssertTenant();
}

public abstract class TenantSpecificOptions : ITenantSpecific
{
    public abstract string Tenant { get; set; }

    public void AssertTenant()
    {
        AssertTenantName(Tenant);
    }

    public static void AssertTenantName(string tenantName)
    {
        if (tenantName == null)
        {
            throw new ArgumentNullException(nameof(tenantName), "Tenant name must be provided.");
        }
    }
}

// Cosmos Commands
[Verb("create_db", HelpText = "Create a database.")]
public class CreateOptions : TenantSpecificOptions
{
    [Option('n', "name", Required = false, HelpText = "Name of database (default: Trident).")]
    public string DatabaseName { get; set; }

    [Option('t', "tenant", Required = false, HelpText = "Name of the tenant")]
    public override string Tenant { get; set; }
}

[Verb("create_container", HelpText = "Create a container.")]
public class CreateContainerOptions : TenantSpecificOptions
{
    [Option('n', "name", Required = false, HelpText = "Name of database (e.g. Trident).")]
    public string DatabaseName { get; set; }

    [Option('c', "container", Required = true, HelpText = "Name of container (e.g. Person).")]
    public string Container { get; set; }

    [Option('t', "tenant", Required = false, HelpText = "Name of the tenant")]
    public override string Tenant { get; set; }
}

[Verb("scale", HelpText = "Scale the throughput of a container.")]
public class ScaleOptions : TenantSpecificOptions
{
    [Option('n', "name", Required = false, HelpText = "Name of database (e.g. Trident).")]
    public string DatabaseName { get; set; }

    [Option('c', "container", Required = true, HelpText = "Name of container (e.g. Person).")]
    public string Container { get; set; }

    [Option('t', "tenant", Required = false, HelpText = "Name of the tenant")]
    public override string Tenant { get; set; }
}

[Verb("delete", HelpText = "Delete a database or container.")]
public class DeleteOptions : TenantSpecificOptions
{
    [Option('n', "name", Required = true, HelpText = "Name of database (e.g. Trident).")]
    public string DatabaseName { get; set; }

    [Option('c', "container", Required = false, HelpText = "Name of container (e.g. Person).")]
    public string Container { get; set; }

    [Option('t', "tenant", Required = false, HelpText = "Name of the tenant")]
    public override string Tenant { get; set; }
}

[Verb("rebuild-all", HelpText = "Rebuild all tenant databases with seed data.")]
public class RebuildAllTenantsOptions
{
    [Option('c', "container", Required = false, HelpText = "Name of container (e.g. Person).")]
    public string Container { get; set; }

    [Option("soft", Required = false, HelpText = "Flag to rebuild without deleting.")]
    public bool Soft { get; set; }

    [Option("update", Required = false, HelpText = "Flag to update existing documents.")]
    public bool Update { get; set; }
}

[Verb("rebuild", HelpText = "Rebuild the database with seed data.")]
public class RebuildOptions : RebuildAllTenantsOptions, ITenantSpecific
{
    [Option('t', "tenant", Required = false, HelpText = "Name of the tenant")]
    public string Tenant { get; set; }

    public void AssertTenant()
    {
        TenantSpecificOptions.AssertTenantName(Tenant);
    }
}

// Service Bus Commands
[Verb("send", HelpText = "Send a message through the Service Bus.")]
public class SendOptions // (e)
{
    [Option('t', "type", Required = true, HelpText = "Type of message entity: Topic/Queue.")]
    public string Type { get; set; }

    [Option('n', "name", Required = true, HelpText = "Name of message entity bus.")]
    public string Name { get; set; }

    [Option('p', "path", Required = true, HelpText = "Path to message file location.")]
    public string Path { get; set; }

    [Option('f', "file", Required = true, HelpText = "Name of file to be serialized.")]
    public string File { get; set; }

    [Option("filter", Required = false, HelpText = "Filter assigned with format: Key|Value;Key|Value ")]
    public string Filter { get; set; }
}

[Verb("auth", HelpText = "Set Local Auth Ids in specified environment")]
public class SetLocalAuthIdOptions : TenantSpecificOptions
{
    [Option('e', "env", Required = false, Default = "TEST", HelpText = "Name of the environment to set")]
    public string Environment { get; set; }

    [Option('c', "connectionString", Required = false, HelpText = "Conditionally pass the connection string as part of the command")]
    public string ConnectionString { get; set; }

    [Option('t', "tenant", Required = false, HelpText = "Name of the tenant")]
    public override string Tenant { get; set; }
}
