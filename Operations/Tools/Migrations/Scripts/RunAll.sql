
--$(path) is passed into this file

--:setvar path "C:\Dev\Secure\D365FO-Azure\Migrations\Scripts\"

--declare @Start datetimeoffset = sysDatetimeoffset();
print 'starting DM: ' + convert(nvarchar(300), sysdatetime())
PRINT 'RESETTING DB - Dropping all Staging Tables'
:r $(path)DropAll.sql
PRINT 'Running DATA PREP SCRIPTS';

PRINT 'Running DATA PREP SCRIPTS -  DDL';
:r $(path)DataPrep\1.DDL.sql

PRINT 'Running DATA PREP SCRIPTS -  Seed Data Load';

:r $(path)DataPrep\2.SeedDataLoad.sql
PRINT 'Running DATA PREP SCRIPTS -  Account Master Data';
:r $(path)DataPrep\3.AccountMaster.sql
PRINT 'Running DATA PREP SCRIPTS -  Product Categories Data';
:r $(path)DataPrep\4.ProductCategory.sql
PRINT 'Running DATA PREP SCRIPTS -  Source Locaton Data';
:r $(path)DataPrep\5.SourceLocationMissingData.sql
:r $(path)DataPrep\6.SourcelocationTypeInsertData.sql
:r $(path)DataPrep\7.SourceLocations.sql
PRINT 'Running DATA PREP SCRIPTS - Substances and Waste Codes';
:r $(path)DataPrep\7.5.SubstancesAndWasteCodes.sql
PRINT 'Running DATA PREP SCRIPTS -  Billing Config Data';
:r $(path)DataPrep\8.BillingConfigs.sql

:r $(path)DataPrep\9.BusinessStreamInsert.sql

PRINT 'Running DATA PREP SCRIPTS - COMPLETED';
PRINT ' ';
PRINT 'Loading Data Message QUEUES';

:r $(path)RunMessageQueueing.sql

print ' DM Completed: ' + convert(nvarchar(300), sysdatetime())