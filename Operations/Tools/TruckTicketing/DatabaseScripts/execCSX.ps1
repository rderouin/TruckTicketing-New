param
(
    [System.IO.FileInfo]
    $csxFile,

    [string]
    $dbConnectionString
)

# if all parameters are provided - execute the script
if ($csxFile -and $dbConnectionString) {
    Write-Host "Executing CSX script: $csxFile"
    dotnet script $csxFile $dbConnectionString
    exit
}

# if some parameters are missing - prompt a user to install tools
$response = Read-Host "Would you like to install the 'dotnet-script' tool? (Y/N)"
if ($response -and $response.ToUpper() -eq "Y") {
    Write-Host "Installing 'dotnet-script' tool..."
    dotnet tool install -g dotnet-script
}
