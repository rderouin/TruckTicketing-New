param(
	[string]$command,
     [string]$tenant,
     [string]$database,
     [string]$container,
     [string]$soft,
     [string]$update,
     [switch]$create,
     [switch]$delete,
     [switch]$rebuild,
     [switch]$rebuildAll,
     [switch]$scale,
     [switch]$auth,
     [switch]$send
)

$cosmosDbCliToolDirectory = Resolve-Path("../Tools/CosmosDBCLI")
$cosmosDbCliToolFilePath = Join-Path -Path $cosmosDbCliToolDirectory -ChildPath "TruckTicketingCLI.exe"
$Color = "Green"
$ArgumentList = New-Object Collections.Generic.List[string]
$ArgumentList.Add($command)

Write-Host 
Write-Host 
Write-Host "======================================" -ForegroundColor $Color
Write-Host "========   CosmosDB CLI...   =========" -ForegroundColor $Color
Write-Host "======================================" -ForegroundColor $Color
Write-Host
Write-Host "TruckTicketingCLI {Verb} --help                                             Show options for any verb command"
Write-Host "TruckTicketingCLI rebuild                                                   Rebuild the Trident database"
Write-Host "TruckTicketingCLI rebuild --container Person                                Rebuild the Person container only"
Write-Host "TruckTicketingCLI rebuild --soft                                            Rebuild the Trident database without deleting"
Write-Host "TruckTicketingCLI rebuild -c Person --soft                                  Rebuild the Person container without deleting"
Write-Host "TruckTicketingCLI rebuild --soft --update                                   Rebuild without deleting and overwrite existing items"
Write-Host "TruckTicketingCLI delete --name Trident                                     Delete the Trident database"
Write-Host "TruckTicketingCLI delete -n Trident -c Person                               Delete the Person container"
Write-Host
Write-Host

if($database){
     $ArgumentList.Add("--name")
     $ArgumentList.Add($database)
}

if($container){
     $ArgumentList.Add("-c")
     $ArgumentList.Add($container)
}

if($soft -eq "Y"){
     $ArgumentList.Add("--soft")
}

if($update -eq "Y"){
     $ArgumentList.Add("--update")
}

if($tenant){
    $ArgumentList.Add("-t")
    $ArgumentList.Add($tenant)
}
Write-Host $ArgumentList
Start-Process -FilePath $cosmosDbCliToolFilePath -ArgumentList $ArgumentList