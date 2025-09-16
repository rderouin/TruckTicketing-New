function Write-CosmosDbAccountDetails() {
    Write-Warning "**************************************************"
    Write-Host "Account Name: ${accountName}"
    Write-Host "Resource Group Name: ${rg}"
    Write-Host "Database Name: ${dbName}"
    Write-Warning "**************************************************"
}

function Validate-CosmosDbModule() {
    if (Get-Module -ListAvailable -Name CosmosDB) {
        Write-Host "CosmosDB module already exists"
    } 
    else {
        # This module is a pre-requisite for this script. It needs to be installed one time only. https://github.com/PlagueHO/CosmosDB
        Install-Module -Name CosmosDB
    }    
}

function updateDocuments_CanPriceBeRefreshed($partitionKey) {
    $collectionId = 'Operations'
    $spId = 'oneTime_addPropertyToDocuments_CanPriceBeRefreshed'

    $backoffPolicy = New-CosmosDbBackoffPolicy -MaxRetries 5
    $key = Get-CosmosDbAccountMasterKey -Name $accountName -ResourceGroupName $rg
    $context = New-CosmosDbContext -Account $accountName -Database $dbName -Key $key -BackoffPolicy $backoffPolicy

    $result =  Invoke-CosmosDbStoredProcedure -Context $context -CollectionId $collectionId -Id $spId -PartitionKey $partitionKey -Verbose

    while ($null -ne $result.continuation) {
        $result = Invoke-CosmosDbStoredProcedure -Context $context -CollectionId $collectionId -Id $spId -PartitionKey $partitionKey -StoredProcedureParameter $result.continuation

        if ($null -ne $result.continuation) {
            $parsedContinuation = ConvertFrom-Json $result.continuation

            Write-Host "Updated so far: $($parsedContinuation.updatedSoFar) documents."
        }
    } 

    Write-Host "In total we updated $($result.count) documents."
}

Validate-CosmosDbModule

$accountName = 'zcac-cosmos-devint-truckticketing'
$rg = 'zcac-rg-devint-truckticketing-storage'
$dbName = 'TruckTicketing'

Write-CosmosDbAccountDetails

$partitionKey = 'SalesLine|052023'

# updateDocuments_CanPriceBeRefreshed $partitionKey