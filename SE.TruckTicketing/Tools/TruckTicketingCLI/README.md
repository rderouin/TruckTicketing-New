# .NET Core 5.0 Console Application

Building a custom command-line interface to simplify the Cosmos database onboarding.

1. [Setup](#1)
2. [Commands](#2)
3. [Migration Tool](#3)
4. [Configuration Files](#4)
5. [Dependencies](#5)

## Setup <a id="1"></a>

### Installation

The rebuild command migrates Seed data from your local trident repository.

* Extract the Developer CLI contents at the same directory as your trident repo - creates a folder called '
  TruckTicketingCLI' which contains the published console application.

![image](https://user-images.githubusercontent.com/58488875/126681909-5f90ed96-d10c-4d37-a938-ca5133d404db.png)

### Azure Cosmos DB Emulator

In order to avoid an HTTP request error, the console application will attempt to launch the Cosmos Emulator. If not
running, the application will throw an exception since a connection was not established.

* Run the Azure Cosmos DB Emulator with local admin privileges

![image](https://user-images.githubusercontent.com/58488875/127343432-477b5dfa-0510-473d-bc2e-4c072419490d.png)

### Command Line Interface

* Enter the TruckTicketingCLI folder ```cd TruckTicketingCLI``` and run the application ```TruckTicketingCLI``` in the
  command prompt or powershell.

e.g. Windows Powershell:

![image](https://user-images.githubusercontent.com/58488875/128211288-4057d818-1458-42e5-87d3-478c9d6866a2.png)

## Commands <a id="2"></a>

### Global

* --help
* --version

### Cosmos Database

* create_db - Create a database (default: Trident)
    * -n, --name
* create_container - Create a container
    * -n, --name
    * -c, --container (Required)
* delete - Delete a database or container
    * -n, --name (Required)
    * -c, --container
* rebuild - Rebuild the Trident database with seed data
    * -c, --container
    * --soft
    * --update
* scale - Scale the throughput of a container
    * -n, --name
    * -c, --container (Required)

### Service Bus

COMING SOON - feature Release v1.1.0

## DocumentDB Data Migration Tool <a id="3"></a>

```TruckTicketingCLI.exe rebuild``` command utilizes the Migration tool to delete and rebuild your local Cosmos emulator
database.

### Default Path

This program launches the DocumentDB Migration Tool from your default UserProfile diretory. See the
Dev_Environment_Setup document for more information.

* Here is an example on how the console application uses Environment Variables to launch the migration tool:
  ```Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)```

* Install the [Document DB Migration Tool](https://azure.microsoft.com/en-us/updates/documentdb-data-migration-tool/)
  into default UserProfile directory. This should create a folder called dt-1.7 in your C:/Users/{username} directory.
  Example:
  ![image](https://user-images.githubusercontent.com/58488875/126329440-3f00cee8-7169-421b-8e83-0a56374ac098.png)

### Command Syntax

The console applcation uses string interpolation in a private method to write data from existing folder locations.
Should you choose to write your own command, here is how it was built:

* To determine which files to upload by folder, see [this](https://docs.microsoft.com/en-us/azure/cosmos-db/import-data)
  .

```dt.exe /s:JsonFile /s.Files:{Enter path} /t:DocumentDB /t.ConnectionString:AccountEndpoint={Enter Key Here} /t.Collection:{Enter container name} /t.PartitionKey:/DocType```

## Modifying the Configuration Files <a id="4"></a>

### Update 1.0.2

You can customize appsettings.json to include a custom path to your installation of the Migration tool. Change the "
migrationPath" JSON key in appsettings.json. See the example below:

![config filepath](https://user-images.githubusercontent.com/58488875/127341048-1dee0839-73b4-46ac-b523-56efab230810.png)

### Update 1.0.3

You can also customize the Cosmos connection info from appsettings.json, see the list of strings below:

* EndpointUri
* PrimaryKey
* ConnectionString
* DataPath

## Dependencies <a id="5"></a>

This console application is running .NET Core 5 and it makes use of
the [Command Line Parser](https://www.nuget.org/packages/CommandLineParser).
Also, import [Microsoft.Azure.Cosmos](https://github.com/Azure/azure-cosmos-dotnet-v3) from NuGet package explorer.

```<TargetFramework>net5.0</TargetFramework>```

```<PackageReference Include="CommandLineParser" Version="2.8.0" />```

```<PackageReference Include="Microsoft.Azure.Cosmos" Version="3.20.0" />```

```<PackageReference Include="Microsoft.Extensions.Configuration" Version="5.0.0" />```

```<PackageReference Include="Microsoft.Extensions.Configuration.FileExtensions" Version="5.0.0" />```

```<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="5.0.0" />```

* In order to migrate to 5.0, clear the NuGet package caches, delete the obj and bin folders, then rebuild the app.
  ```dotnet nuget locals --clear all```
  ```dotnet build```

* To install the Command Line Parser, look for the extension or copy the command below:
  ```dotnet add package CommandLineParser --version 2.8.0```
