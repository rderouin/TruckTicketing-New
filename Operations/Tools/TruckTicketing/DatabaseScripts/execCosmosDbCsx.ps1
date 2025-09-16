<#
    .DESCRIPTION
        This script executes a given csx file in a given environment. It extracts the connection string and passes it as
        the first argument to the csx file. It then passes any additional arguments passed in the command line to the csx file.
    .PARAMETER csxFilePath
        The path to the csx file
    .PARAMETER environmentName
        The CosmosDB environment to execute csx file against e.g. devint, qa, uat, prod
    .EXAMPLE
        .\execCosmosDbCsx.ps1 ./HelloCosmosDb.csx devint
        Executes ./HelloCosmosDb.csx against devint, passing connection string as first argument

        .\execCosmosDbCsx.ps1 ./HelloCosmosDb.csx qa Param01 Param02
        Executes ./HelloCosmosDb.csx against qa, passing connection string as first argument and Param01 and Param02 as additional arguments
#>
param
(
    [Parameter(Mandatory=$true, Position=0)]
    [string] $csxFilePath,

    [Parameter(Mandatory=$true, Position=1)]
    [string] $environmentName,

    [Parameter(Mandatory=$false, Position=3, ValueFromRemainingArguments)]
    [string[]] $args
)

function Write-CosmosDbAccountDetails() {
    Write-Warning "**************************************************"
    Write-Host "Account Name: ${accountName}"
    Write-Host "Resource Group Name: ${rg}"
    Write-Host "Database Name: ${dbName}"
    Write-Warning "**************************************************"
}

function Validate-AzCosmosDbModule() {
    if (Get-Module -ListAvailable -Name "Az.CosmosDB") {
        Write-Host "Az.CosmosDB module already exists"
    } 
    else {
        # This module is a pre-requisite for this script. It needs to be installed one time only. https://learn.microsoft.com/en-us/powershell/module/az.cosmosdb/?view=azps-10.4.1
        Install-Module -Name "Az.CosmosDB"
    }    
}

function Validate-Params() {
    Validate-ParamsCsxFile $csxFilePath
    Validate-ParamsEnvironmentName $environmentName
}

function Validate-ParamsCsxFile($csxFilePath) {
    if( !(Test-Path $csxFilePath) ) {
        throw "Could not find csx file: $(${csxFilePath})"
    }
}

function Validate-ParamsEnvironmentName($environmentName) {
    $validEnvironmentNames = "devint", "qa", "uat", "prod"

    if (! $validEnvironmentNames.Contains($environmentName)) {
        throw "Invalid environment name: $($environmentName)"
    }  
}

function Get-ConnectionString() {
    $keys = Get-AzCosmosDBAccountKey -ResourceGroupName $rg -Name $accountName -Type "ConnectionStrings"
    $connectionString = $keys["Primary SQL Connection String"]

    return $connectionString
}

function Execute-CsxFile($csxFilePath, $connectionString) {
    Write-Host ""
    Write-Host "Executing $(${csxFilePath})..."
    Write-Host ""

    dotnet script $csxFilePath $connectionString @Args
}

$ErrorActionPreference = "Stop"

Validate-AzCosmosDbModule
Validate-Params

$accountName = $("zcac-cosmos-${environmentName}-truckticketing")
$rg = $("zcac-rg-${environmentName}-truckticketing-storage")
$dbName = "TruckTicketing"

Write-CosmosDbAccountDetails

$answer = Read-Host "Execute $() in $($environmentName) (Yes/No)?  "

if ($answer -eq 'Yes') {
    $connectionString = Get-ConnectionString
    Execute-CsxFile $csxFilePath $connectionString $args
} else {
    Write-Host "Exiting without executing..."
}
