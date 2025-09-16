/****** Script for SelectTopNRows command from SSMS  ******/


alter table [dbo].[ZZ_Geoscout_Wells]
add SeqPkId int identity(1,1) not null Primary Key 

alter table [dbo].[ZZ_Geoscout_Wells]
add CleanUWI nvarchar(50)  null  


update [dbo].[ZZ_Geoscout_Wells]
set CleanUWI = trim(GSL_UWI)

SELECT TOP (1000) *
  FROM [TruckTicketDataMigration ].[dbo].[UWI]
  where countryregionid = 'CAN' and SOBATTERYCODE like '%WI%'




  --Location is Well, 
  select count(*)
  FROM [TruckTicketDataMigration ].[dbo].[UWI]
  where countryregionid = 'CAN' and SOBATTERYCODE like '%WI%'


  
  --Location is Well, 
  select count(*)
  FROM [TruckTicketDataMigration ].[dbo].[UWI]
  where countryregionid = 'CAN' and SOBATTERYCODE like '%WI%'



  --Wells or Batteries
  SELECT *
  FROM [TruckTicketDataMigration ].[dbo].[UWI]
  where countryregionid = 'CAN' 
	  and  SOBATTERYCODE is not null 
	  and len(SOBATTERYCODE) > 2 
	  and  SUBSTRING( SOBATTERYCODE, 3, len(SOBATTERYCODE)-3) like 'BT%'


	  --BULK MATCH ON bt CHAR CODE DIGIT 3-4
SELECT t.LocationType, 
		s.SOBATTERYCODE, 
		SUBSTRING( SOBATTERYCODE, 3, 2) as MatchedON , 
		s.LOCATIONTYPE, 
		case s.locationtype 
			when 1 then 'downhole' 
			when 2 then 'surface' 
		end as LocationTypeName, 
		s.UWIFORMATTYPE,
		s.*
  FROM [TruckTicketDataMigration ].[dbo].[UWI] s
  inner join [dbo].[SourceLocationTypes] t on t.BatteryCodeField = SUBSTRING( SOBATTERYCODE, 3, 2) 
  where countryregionid = 'CAN' 
	  and  SOBATTERYCODE is not null 
	  and len(SOBATTERYCODE) >= 4 	




	  
	  SELECT t.LocationType, 
		s.SOBATTERYCODE, 
		SUBSTRING( SOBATTERYCODE, 3, 2) as MatchedON , 
		s.LOCATIONTYPE, 
		case s.locationtype 
			when 1 then 'downhole' 
			when 2 then 'surface' 
		end as LocationTypeName, 
		s.UWIFORMATTYPE,
		s.*
  FROM [TruckTicketDataMigration ].[dbo].[UWI] s
  inner join [dbo].[SourceLocationTypes] t on t.BatteryCodeField = SUBSTRING( SOBATTERYCODE, 3, 2) 
  where TRIM(countryregionid) = '' 
	  and  SOBATTERYCODE is not null 
	  AND 	 DATAAREAID = 'SESC'
	  and len(SOBATTERYCODE) >= 4 	


	  --USA

	  select 
		s.SOBATTERYCODE, 
		SUBSTRING( SOBATTERYCODE, 3, 2) as MatchedON , 
		s.LOCATIONTYPE, 
		S.UWI,
		case s.locationtype 
			when 1 then 'downhole' 
			when 2 then 'surface' 
		end as LocationTypeName, 
		s.UWIFORMATTYPE,
		s.*
FROM [TruckTicketDataMigration ].[dbo].[UWI] s  
  where (TRIM(countryregionid) = 'USA'
  OR TRIM(countryregionid) = '') AND   DATAAREAID = 'SESU'





select COUNT( COUNTRYREGIONID)
FROM [TruckTicketDataMigration ].[dbo].[UWI] s  
  where  	 DATAAREAID = 'SESU'
  AND TRIM(countryregionid) = '' 


SELECT s.UWI, s.SOBATTERYCODE, s.DATAAREAID,  s.*
  FROM [TruckTicketDataMigration ].[dbo].[UWI] s
  where countryregionid = 'USA'  
   and  (SOBATTERYCODE is  null 
   or len(SOBATTERYCODE) < 4) 	


--get license number  1,495 
select a.[COUNTRYREGIONID], a.SALESID, a.DataAreaId, a.[CONTRACTOPERATED], a.[OWNER], a.uwi, a.uwialias, a.SOBATTERYCODE, a.UWIFieldName, a.[NDICLOCATION], NOTES, '' as LicenseNumber	
FROM [dbo].[SourceLocations] a
where a.NOTES like '%license%' or a.NOTES like '%licence%' 


--get provinceorstate
select a.SALESID, a.DataAreaId, a.[CONTRACTOPERATED], a.[OWNER], a.uwi, a.uwialias, a.SOBATTERYCODE, a.UWIFieldName, a.[NDICLOCATION], NOTES, 
a.[COUNTRYREGIONID], '' as [PROVINCEORSTATESTRING]  
from [dbo].[SourceLocations] a
where a.[PROVINCEORSTATE] is null



  select *
  from [dbo].[UWI] s

  select *
  from [dbo].[SourceLocationTypes] t


  select c.*
  from [dbo].[Non_Billable_Customers] c

  
  select c.*
  from [dbo].[Prospects] c


--'TradeAgreementEntity', 'EDIDefinitionEntity'
 update dbo.DataMigrationMessages
 set Processed = 1, ProcessedDate = SYSDATETIME()
 where  MessageType in ('LegalEntity', 'ProductEntity', 'FacilityEntity')  -- Processed = 1
 

SELECT m.[Message], m.[MessageType], m.[GeneratedDate], m.[EntityId], m.[AxEntityId], m.[Processed], m.[ID] 
                             FROM 
                             ( 
                                SELECT m.*, ROW_NUMBER() OVER(PARTITION BY m.[MessageType] ORDER BY m.[Id] ASC) rn
                                FROM [TruckTicketDataMigration].[dbo].[DataMigrationMessages] m 
                             ) m 
                             WHERE m.rn <= 50 
                             AND m.[Processed] = 0                       
                             ORDER BY m.[Id] ASC 


  SELECT top(2000) m.[Message], m.[MessageType], m.[GeneratedDate], m.[EntityId], m.[AxEntityId], m.[Processed], m.[ID], TopicName 
                        FROM [TruckTicketDataMigration].[dbo].[DataMigrationMessages] m 
                        WHERE  m.[Processed] = 0                       
                        ORDER BY m.[Id] ASC;


						SELECT m.[Message], m.[MessageType], m.[GeneratedDate], m.[EntityId], m.[AxEntityId], m.[Processed], m.[ID], TopicName  
                             FROM 
                             ( 
                                SELECT m.*, ROW_NUMBER() OVER(PARTITION BY m.[MessageType] ORDER BY m.[Id] ASC) rn
                                FROM [TruckTicketDataMigration].[dbo].[DataMigrationMessages] m 
                             ) m 
                             WHERE m.rn <= 10 
                             AND m.[Processed] = 0                       
                             ORDER BY m.[Id] ASC
							 


							 select *
							 from [dbo].[DataMigrationMessages]
							 where messagetype = 'ServiceType'
							 and message like '%DefaultOrderSettings%'



						delete [dbo].[DataMigrationMessages] 						
						where MessageType = 'SourceLocation'


						

						update [dbo].[DataMigrationMessages]
						set Processed = 0, ProcessedDate = null
							 where MessageType ='SpartanProductParameter'



						SELECT top(100) m.[Message], m.[MessageType], m.[GeneratedDate], m.[EntityId], m.[AxEntityId], m.[Processed], m.[ID], m.TopicName 
							
                        FROM [TruckTicketDataMigration].[dbo].[DataMigrationMessages] m 
                        WHERE  m.[Processed] = 0 --and messageType = 'FacilityService' -- not in ('TradeAgreementEntity', 'EDIDefinitionEntity')

                        ORDER BY m.[Id] ASC;



						select *
						from [dbo].[DataMigrationMessages] 						
						where MessageType = 'EDIFieldDefinition'


					

						select s.UWI, s.TT_UWI, st.Format1, st.Country TypeCountry, s.COUNTRYREGIONID as SLCountry
							,st.LocationType, st.[BatteryCodeField], s.SOBATTERYCODE
							, iif(s.uwi like st.[RegExValidator], 'Match', 'No Match') as IsMatch
							,st.[RegExValidator]
						from SourceLocations s 
						join dbo.SourceLocationTypes st on st.Id = s.SOURCELOCATIONTYPEID
						where ---BatteryCodeField not in ('BT', 'WI') 
						--and
						Format1 is not null
						order by st.BatteryCodeField, st.LocationType
		
		
		select distinct s.TT_UWI, s.SOURCELOCATIONTYPEID
		from dbo.SourceLocations s

--t.BatteryCodeField = SUBSTRING(s.SOBATTERYCODE, 3, 2) 	and
		
SELECT s.GUID, s.UWI, s.TT_UWI, t.Format1, t.Country TypeCountry, s.COUNTRYREGIONID as SLCountry
	,t.LocationType, t.[BatteryCodeField], s.SOBATTERYCODE
FROM [dbo].[SourceLocations] s
join dbo.ZZ_Geoscout_Wells w on s.UWIALIAS = w.GSL_UWI
JOIN [dbo].[SourceLocationTypes] t on  
	t.Country =  SUBSTRING(s.countryregionid, 1, 2)  and t.BatteryCodeField = 'WI' and  s.TT_UWI like t.RegExValidator
WHERE s.countryregionid = 'CAN' 

AND s.SOBATTERYCODE IS NOT NULL 
AND len(s.SOBATTERYCODE) >= 4 
and SUBSTRING(s.SOBATTERYCODE, 3, 2) in ('WI')
AND s.SOURCELOCATIONTYPEID IS NULL


--17961
select count(*)
from dbo.SourceLocations s
join dbo.ZZ_Geoscout_Wells w on s.UWIALIAS = w.GSL_UWI

select distinct s.tt_uwi
from dbo.ZZ_UWI s 


;with Wells
as
(
	select s.GUID as SourceLocationId, s.UWI, s.TT_UWI, s.SOBATTERYCODE 
	from dbo.SourceLocations s 
	where SUBSTRING(s.SOBATTERYCODE, 3, 2) in ('WI')
	and  s.countryregionid = 'CAN'  
	and s.UWIALIAS in (select GSL_UWI from dbo.ZZ_Geoscout_Wells where cleanUwi = UWIALIAS)
)
--update dbo.SourceLocations
--set SOURCELOCATIONTYPEID = t.Id
select *
from dbo.SourceLocations s
join Wells  w on s.GUID = w.SourceLocationId
join SourceLocationTypes t on  t.BatterycodeField = 'WI' and w.TT_UWI like t.RegExValidator



;with Wells
as
(
	select s.GUID as SourceLocationId, s.UWI, s.TT_UWI 
	from dbo.SourceLocations s 
	where SUBSTRING(s.SOBATTERYCODE, 3, 2) in ('BT')
	and  s.countryregionid = 'CAN' 
    and s.UWIALIAS in (select GSL_UWI from dbo.ZZ_Geoscout_Wells where cleanUwi = UWIALIAS)
)
update dbo.SourceLocations
set SOURCELOCATIONTYPEID = t.Id
--select *
from dbo.SourceLocations s
join Wells  w on s.GUID = w.SourceLocationId
join SourceLocationTypes t on  t.BatterycodeField = 'WI' and w.TT_UWI like t.RegExValidator



;with Wells
as
(
	select s.GUID as SourceLocationId, s.UWI, s.TT_UWI 
	from dbo.SourceLocations s 
	where SUBSTRING(s.SOBATTERYCODE, 3, 2) in ('BT')
	and  s.countryregionid = 'CAN' 
	and s.UWIALIAS not in (select GSL_UWI from dbo.ZZ_Geoscout_Wells where cleanUwi = UWIALIAS)
)
update dbo.SourceLocations
set SOURCELOCATIONTYPEID = t.Id
--select *
from dbo.SourceLocations s
join Wells  w on s.GUID = w.SourceLocationId
join SourceLocationTypes t on  t.BatterycodeField = 'BT' and w.TT_UWI like t.RegExValidator



update dbo.DataMigrationMessages
set Processed = 0, ProcessedDate = null
where Processed = 1 and MessageType = 'ProductEntity'



select *
from dbo.DataMigrationMessages
where messageType = 'ServiceType'

 select *
from [dbo].[DataMigrationMessages]							
where entityId = '9421DAAA-27B9-41A7-8123-34489F51E839'


 select *
from [dbo].[DataMigrationMessages]							
where entityId = '6536E672-243F-4D7F-9C4F-1C451EE59DE3'


select distinct GUID
from dbo.Products_Released


;with dups 
as
(
	select EntityId
	from [dbo].[DataMigrationMessages]	
	where MessageType = 'BillingConfig'
	group by EntityId
	having count(*) > 1
)
delete [dbo].[DataMigrationMessages] 
where EntityId in (select EntityID from dups)



select *
into dbo.DataMigrationMessageSIT
from dbo.DataMigrationMessages




						update [dbo].DataMigrationMessageSIT
						set Processed = 0, ProcessedDate = null
							 where MessageType ='AccountEntity'





select  Id
from dbo.AccountMaster a 
where a.PARTYNUMBER = '100000457'

select 
*
from dbo.DataMigrationMessageSIT
where EntityId = 'D3658858-BD94-4461-800B-5FDB9A154353'



select *
from Contact_Person c
join 
where ISINACTIVE = 'Yes' and c.




select e.*
from dbo.Contact_Person_Electronic_Address e
where e.TYPE = 'Email'  and e.isvalidlocator = 0 


	Update Contact_Person_Electronic_Address
	set IsValidLocator = dbo.isValidPhoneFormat(LOCATOR)
	where type = 'PHone'

	alter table Contact_Person_Electronic_Address
	add IsValidLocator bit null
	add SeqId int not null Identity(1,1)  Primary Key 


	
With BadPhones
as
(
	select dbo.isValidEmailFormat(e.LOCATOR) as IsReallyEamil, e.*
	from dbo.Contact_Person_Electronic_Address e
	where e.TYPE = 'Phone'  and e.isvalidlocator = 0
)
update Contact_Person_Electronic_Address
set [Type] = 'Email'
from Contact_Person_Electronic_Address e
join BadPhones b on e.GUID = b.GUID
where IsReallyEamil = 1



With BadPhones
as
(
	select dbo.isValidPhoneFormat(e.LOCATOR) as IsReallyPHone, e.*
	from dbo.Contact_Person_Electronic_Address e
	where e.TYPE = 'Email'  and e.isvalidlocator = 0
)
update Contact_Person_Electronic_Address
set [Type] = 'Phone'
from Contact_Person_Electronic_Address e
join BadPhones b on e.GUID = b.GUID
where IsReallyPhone = 1



	select dbo.isValidEmailFormat(trim(e.LOCATOR)) as IsReallyEamil, e.*
	from dbo.Contact_Person_Electronic_Address e
	where e.TYPE = 'Email'  and e.isvalidlocator = 0


	Update Contact_Person_Electronic_Address
	set IsValidLocator = dbo.isValidPhoneFormat(LOCATOR)
	where type = 'PHone' and isvalidlocator = 0



	select  isvalidlocator, count(*)
	from Contact_Person_Electronic_Address e
	group by e.IsValidLocator


	select Contact_Person_Electronic_Address
	where 



	select dbo.isValidEmailFormat(trim(e.LOCATOR)) as IsReallyEamil, dbo.isValidPhoneFormat(trim(e.LOCATOR)) as isRealyPhone , e.*
	from dbo.Contact_Person_Electronic_Address e
	where e.TYPE = 'Email'  


;with badONes
as
(
	select  e.isvalidlocator, e.LOCATOR, e.[TYPE], 
		dbo.isValidEmailFormat(trim(e.LOCATOR)) as IsEmail,
		 dbo.isValidPhoneFormat(trim(e.LOCATOR)) as isPhone
	from Contact_Person_Electronic_Address e
	where isvalidlocator = 0
)
select *
from badONes
where (type='Email' and isPhone = 1)
	or (type='Phone' and isEmail = 1)

select dbo.isValidEmailFormat('ap.cnd.@silverfox.net')


select e.DATAAREAID, e.PARTYNUMBER, ISPRIMARY, TYPE, count(*)
from Contact_Person_Electronic_Address e
where e.isvalidLocator = 0 --and e.isprimary = 'Yes'
group by e.DATAAREAID, e.PARTYNUMBER, ISPRIMARY, TYPE
having count(*) > 1

select *
from [dbo].[Customers_Electronic_Addresses] a
where a.IsValidLocator = 0 --and a.isprimary = 1


select *
from dbo.Non_Billable_Customers_Electronic_Address a
where a.IsValidLocator = 0 --and a.isprimary = 1

select *
from dbo.Prospect_Electronic_Address a
where a.IsValidLocator = 0 -- and a.isprimary = 1


select *
from dbo.AccountMaster a
where a.IsActiveCustomer = 1


select *
from dbo.InvoiceConfigs

select *
from dbo.ValidCustomerAccounts

select *
from dbo.ValidContacts


select *
from dbo.InvoiceConfigs


select EntityId, convert(nvarchar(50), EntityId), * --, count(*)
from dbo.DataMigrationMessages
where MessageType = 'BillingConfig' and CHARINDEX(convert(nvarchar(50), EntityId),    [Message]) > 1
group by EntityId
having count(*) > 0

delete [dbo].[DataMigrationMessages] 
where MessageType = 'BillingConfig'


select v.*
from dbo.ValidCustomerAccounts v
join dbo.customers a on v.CustomerId = a.guid
where a.PAYMENTTERMS in ('N90', 'N45', 'N30', 'N10', 'N0', 'N60') and
v.CUSTOMERACCOUNT in (
select AxEntityId
from [dbo].[DataMigrationMessages] 
where MessageType = 'BillingConfig'

)

select *
from dbo.BillingConfigs c


select *
from AccountMaster a
where  a.IsActiveCustomer = 0 and IsGenerator = 0 and IsTruckingCo =0 and Is3rdParty = 0

where id = '762D3292-F474-4EC5-BB5D-0D6570D74B59'

select *
from [dbo].[MissingLicenses]

select *
from AccountMaster a
where SourceTable = 'NONBILLABLECUSTOMERS' and IsGenerator = 0
and  CUSTOMERACCOUNT not in (select OWNER
from dbo.UWI w
where w.DATAAREAID = a.DATAAREAID)





select *
from dbo.missingLicenses m
where uwialias is  not null
join uwi u on u.NOTES like '%' + m.LICENSENUMBER + '%'


select *
from dbo.DataMigrationMessages
where MessageType = 'FacilityEntity'



select *
from dbo.DataMigrationMessages
where entityId = '4e283dc1-7506-4016-b4a6-7cf4df21d212'



update dbo.DataMigrationMessages
set  Processed = 0
--select *
from dbo.DataMigrationMessages
where charindex('4e283dc1-7506-4016-b4a6-7cf4df21d212', message) > 1 and MessageType = 'EDIFieldDefinition'


update dbo.DataMigrationMessages
set  Processed = 0, ProcessedDate = null
where  MessageType like 'Account%'


select distinct b.EdiField
from [EDI_HSEDICustomerConfigEntity] b
where b.EdiField  not in (select distinct edifield from dbo.EdiFieldDefinitions)



select *
into  dbo.DataMigrationMessagesSIT
from dbo.DataMigrationMessages d


update dbo.DataMigrationMessagesSIT
set  Processed = 0, ProcessedDate = null
where  MessageType = 'TradeAgreement'

update dbo.DataMigrationMessages
set  Processed = 0, ProcessedDate = null
where Processed = 1 MessageType = 'LegalEntityMessage'



select *
--into dbo.SitMsgBkp
from  dbo.DataMigrationMessagesSIT
where  MessageType = 'TradeAgreement'


update dbo.DataMigrationMessagesSIT
set message = d.message
--select *
from  dbo.DataMigrationMessagesSIT s 
join dbo.DataMigrationMessages d  on d.EntityId = s.EntityId
where  s.MessageType = 'TradeAgreement'

select *
from dbo.accountmaster c


where c.SourceTable = 'CUSTOMERS'


select * --distinct c.DATAAREAID, l.Division
from Customers c
join dbo.LegalEntity l on l.DataAreaId = c.DATAAREAID
where division <> 'MI'



select *
from  dbo.DataMigrationMessages
where   EntityId = '2629342A-73E1-4B5A-8431-71C3639F9B44' 
and MessageType like '%Account%' and (Message like '%"ThirdPartyAnalytical"%' or  Message like '%"TruckingCompany"%')


select *
from validcontacts

select *
from  dbo.DataMigrationMessages
where  MessageType like '%Legal%'