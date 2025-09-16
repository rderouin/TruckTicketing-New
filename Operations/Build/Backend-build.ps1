param(
	 [string]$AppName,
	 [string]$Operations,
	 [switch]$clean,
     [switch]$restore,
     [switch]$build,
     [switch]$test,
     [switch]$coverage
)

$Color = "Green"
$solutionsFilePath = Resolve-Path("..\..\")

$SplitOps = -split $Operations
foreach($Op in $SplitOps){
	Switch($Op.ToUpper()){
		"C" {$clean = $true}
		"B" {$build = $true}
		"T" {$test = $true}
		"R" {$restore = $true}
		"CC" {$coverage = $true}
		"ALL" {$clean = $true
			   $build = $true
			   $restore = $true
			   $test = $true
			   $coverage = $true
		}
		default {"Invalid Selection"}
	}
}

$Color = "Green"
$solutionsFilePath = Resolve-Path("..\..\SE.TruckTicketing")

# TruckTicketing Solution Variables
$ProjectPath = Join-Path -Path $solutionsFilePath -ChildPath "\*.$($AppName).*.csproj"
$TTRunSettingsPath = Join-Path -Path $solutionsFilePath -ChildPath "\test.runsettings"
$TTTestResultsPath =  Join-Path -Path $solutionsFilePath -ChildPath "..\TestResults"


function Remove-OldCoverage{
    if (Test-Path($TTTestResultsPath)) {
        $TestResultsPath = Resolve-Path($TTTestResultsPath)

        Remove-Item $TestResultsPath -Recurse -Force
        Write-Host -ForegroundColor Cyan "CleanUp: Old code coverage file removed successfully"
    }
}

function Exit-Code-Check([string]$stage) {
    if($LASTEXITCODE -ne 0) {
		Write-Host 
		Write-Host 
        Write-Host -BackgroundColor Black -ForegroundColor Red "=================================================="
        Write-Host -BackgroundColor Black -ForegroundColor Red "  '$($stage)' Stage has caused the build to fail"
        Write-Host -BackgroundColor Black -ForegroundColor Red "=================================================="
		Write-Host 
		Write-Host 
        exit
    }
}

if($clean){
	Write-Host 
	Write-Host 
	Write-Host "======================================" -ForegroundColor $Color
	Write-Host "===    Executing local clean...    ===" -ForegroundColor $Color
	Write-Host "======================================" -ForegroundColor $Color
	Write-Host 
	Write-Host 
	Write-Host $ProjectPath
	dir $ProjectPath -Recurse | %{dotnet clean $PSItem.FullName}
    Exit-Code-Check -stage "Clean"
}

if($restore){
	Write-Host 
	Write-Host 
	Write-Host "======================================" -ForegroundColor $Color
	Write-Host "===    Executing local restore...  ===" -ForegroundColor $Color
	Write-Host "======================================" -ForegroundColor $Color
	Write-Host 
	Write-Host     
	Write-Host $ProjectPath
	dir $ProjectPath -Recurse | %{dotnet restore $PSItem.FullName}
	Exit-Code-Check -stage "Restore"
}

if($build){
	Write-Host 
	Write-Host 
	Write-Host "======================================" -ForegroundColor $Color
	Write-Host "===    Executing local build...    ===" -ForegroundColor $Color
	Write-Host "======================================" -ForegroundColor $Color
	Write-Host 
	Write-Host 
	Write-Host $ProjectPath
	dir $ProjectPath -Recurse | %{dotnet build $PSItem.FullName --no-restore}
    Exit-Code-Check -stage "Build"
}

if($test){

    Write-Host 
	Write-Host 
	Write-Host "======================================" -ForegroundColor $Color
	Write-Host '===     Executing all tests...     ===' -ForegroundColor $Color
	Write-Host "======================================" -ForegroundColor $Color
	Write-Host 
	Write-Host 

	Remove-OldCoverage
       
	Write-Host $ProjectPath
	dir $ProjectPath -Recurse | %{dotnet test $PSItem.FullName --settings:$TTRunSettingsPath --collect:"Code Coverage" --no-build --logger trx --logger html --filter TestCategory!=Acceptance --verbosity normal}
    Exit-Code-Check -stage "Test"
}

if($coverage){
    # Coverage Tool File Path
    $coverageToolFilePath = Resolve-Path(".\Coverage\MSTest.Analyzer\Capax.CodeCoverage.MSTest.Analyzer.exe")
    $TTCoverageFilePath = Resolve-Path(Join-Path -Path $solutionsFilePath -ChildPath "..\TestResults\**\*.coverage")
    $TTCoverageTargetsFilePath = Resolve-Path(Join-Path -Path $solutionsFilePath -ChildPath "CoverageTargets.json")
	$coverageOutputFilePath = Join-Path $TTTestResultsPath "coverage-output.TruckTicketing.$((get-date).ToString("yyyy-MM-dd_HH_mm_ss")).json"

    Write-Host 
	Write-Host 
	Write-Host "======================================" -ForegroundColor $Color
	Write-Host "===   Verifying code coverage...   ===" -ForegroundColor $Color
	Write-Host "======================================" -ForegroundColor $Color
	Write-Host 
	Write-Host 


	write-host "coverageToolFilePath: $coverageToolFilePath"
	write-host "TTCoverageFilePath: $TTCoverageFilePath"
	write-host "TTCoverageTargetsFilePath: $TTCoverageTargetsFilePath"
	write-host "coverageOutputFilePath: $coverageOutputFilePath"

	& $coverageToolFilePath `
		--coverage-file $TTCoverageFilePath `
		--targets-file $TTCoverageTargetsFilePath `
		--output-file $coverageOutputFilePath `
		-e 1

        Exit-Code-Check -stage "Coverage"
}