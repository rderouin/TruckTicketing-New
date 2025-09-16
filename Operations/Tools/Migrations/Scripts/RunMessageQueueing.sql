--:setvar path "C:\Dev\Secure\D365FO-Azure\Migrations\Scripts\"

PRINT 'Loading Data Message QUEUES';

PRINT 'Loading Data Message - Business Streams';
:r $(path)BusinessSteam.sql

PRINT 'Loading Data Message - Legal Entity';
:r $(path)LegalEntity.sql

PRINT 'Loading Data Message - Facility';
:r $(path)Facilities.sql

PRINT 'Loading Data Message - Products';
:r $(path)Products.sql

PRINT 'Loading Data Message - Accounts';
:r $(path)Accounts.sql

PRINT 'Loading Data Message - Service Types';
:r $(path)ServiceTypes.sql

PRINT 'Loading Data Message - Spartan Product Parameters';
:r $(path)SpartanProductParameter.sql

PRINT 'Loading Data Message - Source Location Types';
:r $(path)SourceLocationTypes.sql

PRINT 'Loading Data Message - Facility Services';
:r $(path)FacilityServices.sql

PRINT 'Loading Data Message - Customer Edi Master';
:r $(path)CustomerEdi.sql

PRINT 'Loading Data Message - Source Locations';
:r $(path)SourceLocations.sql

PRINT 'Loading Data Message - Invoice Config';
:r $(path)InvoiceConfig.sql

PRINT 'Loading Data Message - Trade Agreements';
:r $(path)TradeAgreements.sql

PRINT 'Loading Data Message - Billing Config';
:r $(path)BillingConfig.sql


PRINT 'Loading Data Message - COMPLETED';

--PRINT 'Elapsed Time: ' + convert(nvarchar(100),  sysDatetimeoffset() - @Start);

declare @MasterDataCount int =0;

declare @TTDataCount int =( SELECT COUNT(*)
FROM [dbo].[DataMigrationMessages]);

print '******************************** SUMMARY ***************************************'
Print 'Master data Messages: ' + convert(nvarchar(20), @MasterDataCount);
print  'TT data Messages: ' +  convert(nvarchar(20), @TTDataCount);
print 'Total: ' +  convert(nvarchar(20), @MasterDataCount + @TTDataCount);

PRINT '********* Message Count Breakdown ****************';
select MessageType, count(*)
from  [dbo].[DataMigrationMessages]
group by MessageType
union
select MessageType, count(*)
from  [dbo].[DataMigrationMessages]
group by MessageType;



