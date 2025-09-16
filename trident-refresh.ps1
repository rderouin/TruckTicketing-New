$ErrorActionPreference = 'Stop'

$TridentPath = Join-Path $PSScriptRoot "..\Trident\"
$TridentCopyPath = Join-Path $PSScriptRoot ".\Trident\"
$TridentCopyGitPath = Join-Path $TridentCopyPath ".\.git\"

# Start from Trident
Set-Location $TridentPath

# Prepare location
if (Test-Path $TridentCopyPath) {
    Remove-Item $TridentCopyPath -Recurse -Force
}

# Copy files
git fetch
git pull
git clean -fdx
git clone --branch=master . ..\TruckTicketing\Trident

# Remove .git folder
Remove-Item $TridentCopyGitPath -Recurse -Force

# Restore location 
Set-Location $PSScriptRoot
