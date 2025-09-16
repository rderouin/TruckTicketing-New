
IF OBJECT_ID(N'dbo.AccountMaster', N'U') IS NOT NULL  
   DROP TABLE dbo.AccountMaster;  
GO

IF OBJECT_ID(N'dbo.ProspectContactsTemp', N'U') IS NOT NULL  
   DROP TABLE dbo.ProspectContactsTemp;  
GO

IF OBJECT_ID(N'dbo.ProspectContactEmailPhones', N'U') IS NOT NULL  
   DROP TABLE dbo.ProspectContactEmailPhones;  
GO

IF OBJECT_ID(N'dbo.ProspectContacts', N'U') IS NOT NULL  
   DROP TABLE dbo.ProspectContacts;  
GO

IF OBJECT_ID(N'dbo.ValidCustomerAccounts', N'U') IS NOT NULL  
   DROP TABLE dbo.ValidCustomerAccounts;  
GO

IF OBJECT_ID(N'dbo.ValidContacts', N'U') IS NOT NULL  
   DROP TABLE dbo.ValidContacts;  
GO




--Creating Contacts from prospect company level contact info
select newid() ContactId, T.*
    INTO DBO.ProspectContactsTemp
	from (select distinct e.BUSRELACCOUNT, 
					iif(e.[description] is not null and trim(e.[DESCRIPTION]) != '', trim(e.[DESCRIPTION]) , trim(c.[name])) as ContactName,
					e.DATAAREAID   
	from [dbo].[Prospects] c	
	join [dbo].[Prospect_Electronic_Address] e on c.DATAAREAID = e.DATAAREAID and c.BUSRELACCOUNT = e.BUSRELACCOUNT and e.IsValidLocator = 1
	GROUP BY e.BUSRELACCOUNT, 
					iif(e.[description] is not null and trim(e.[DESCRIPTION]) != '', trim(e.[DESCRIPTION]) , trim(c.[name])),
					e.DATAAREAID
	) T


select NEWID() AS Id, t.ContactId, t.ContactName, e.* 
into dbo.ProspectContactEmailPhones
from [dbo].[Prospects] c 
join [dbo].[Prospect_Electronic_Address] e on c.DATAAREAID = e.DATAAREAID and c.BUSRELACCOUNT = e.BUSRELACCOUNT and e.IsValidLocator = 1
join ProspectContactsTemp t on t.BUSRELACCOUNT = c.BUSRELACCOUNT and t.DATAAREAID = c.DATAAREAID 
	and t.ContactName = iif(e.[description] is not null and trim(e.[DESCRIPTION]) != '', trim(e.[DESCRIPTION]) , trim(c.[name])) 	


select DISTINCT r.ContactId as Id
    ,r.BUSRELACCOUNT, r.DATAAREAID, 
	r.ContactName
	,iif(CHARINDEX(' ', r.ContactName) >=1,  SUBSTRING(r.ContactName, 1, CHARINDEX(' ', r.ContactName) - 1), r.ContactName) AS Firstname
	,iif(CHARINDEX(' ', r.ContactName) >=1, 
		SUBSTRING(r.ContactName, CHARINDEX(' ', r.ContactName) + 1, LEN(r.ContactName) - CHARINDEX(' ', r.ContactName)), 
		r.ContactName) AS Lastname
	,convert(bit, 0) as Primary_Contact
into dbo.ProspectContacts
from dbo.ProspectContactEmailPhones r

update dbo.ProspectContactEmailPhones 
set ISPRIMARY = 0;

update dbo.ProspectContactEmailPhones 
set ISPRIMARY = 1
where Id in (select MAX(e.id)
from  dbo.ProspectContactEmailPhones e
group by ContactId, e.TYPE)


update dbo.ProspectContacts
set Primary_Contact = 1
where Id in (select MAX(e.id)
from  dbo.ProspectContacts e
group by e.BUSRELACCOUNT)

-- Creates Table with IDs of only Customers that meet the information validation 
;With CoWithGoodContacts
as
(
	select p.ASSOCIATEDPARTYID, p.DATAAREAID, c.Guid as CustomerId
	from [dbo].[Contact_Person] p
	join dbo.Customers c on p.ASSOCIATEDPARTYID = c.PARTYNUMBER  and c.DATAAREAID = p.DATAAREAID
	left join dbo.Contact_Person_Electronic_Address e on e.DATAAREAID = p.DATAAREAID and e.PARTYNUMBER = p.CONTACTPERSONPARTYNUMBER 
		and e.TYPE = 'Email' and e.ISPRIMARY = 'Yes'
	left join dbo.Contact_Person_Electronic_Address pp on pp.DATAAREAID = p.DATAAREAID and pp.PARTYNUMBER = p.CONTACTPERSONPARTYNUMBER 
		and pp.TYPE = 'Phone' and pp.ISPRIMARY = 'Yes'
	left join dbo.Contact_Person_Postal_Address a on a.PARTYNUMBER = p.CONTACTPERSONPARTYNUMBER and a.DATAAREAID = p.DATAAREAID
		and a.ISPRIMARY = 1
	where p.PRIMARY_CONTACT = 1
	and p.FIRSTNAME is not null and trim(p.FIRSTNAME) != ''
	and p.LASTNAME is not null and trim(p.LASTNAME) != ''
	and e.LOCATOR is not null and trim(e.locator) != ''
	and pp.LOCATOR is not null and trim(pp.locator) != ''
	and a.ADDRESS is not null and trim(a.ADDRESS) != ''
	and a.CITY is not null and trim(a.CITY) != ''
	and a.STATE is not null and trim(a.STATE) != ''
	and a.ZIPCODE is not null and trim(a.ZIPCODE) != ''
	and a.COUNTRYREGIONID is not null and trim(a.COUNTRYREGIONID) != ''
)
, GoodCustWithAddr
as
(
	select c.GUID as CustomerId, c.DATAAREAID, c.PARTYNUMBER,  c.CUSTOMERACCOUNT
	FROM [dbo].[Customers] c  
	left join dbo.Customers_Postal_Addresses ca on ca.CUSTOMERACCOUNTNUMBER = c.CUSTOMERACCOUNT and ca.DATAAREAID = c.DATAAREAID
		and ca.ISPRIMARY = 'Yes'
	where ca.ADDRESSSTREET is not null and trim(ca.ADDRESSSTREET) != ''
		and ca.ADDRESSCITY is not null and trim(ca.ADDRESSCITY) != ''
		and ca.ADDRESSSTATE is not null and trim(ca.ADDRESSSTATE) != ''
		and ca.ADDRESSZIPCODE is not null and trim(ca.ADDRESSZIPCODE) != ''
		and ca.ADDRESSCOUNTRYREGIONID is not null and trim(ca.ADDRESSCOUNTRYREGIONID) != ''
)
select c.*
into  dbo.ValidCustomerAccounts
from GoodCustWithAddr c
join CoWithGoodContacts cc on c.CustomerId = cc.CustomerId;


select p.CONTACTPERSONID, p.DATAAREAID, p.Guid as ContactId, c.GUID as CustomerId
	into dbo.ValidContacts
	from [dbo].[Contact_Person] p
	join dbo.Customers c on p.ASSOCIATEDPARTYID = c.PARTYNUMBER  and c.DATAAREAID = p.DATAAREAID
	left join dbo.Contact_Person_Electronic_Address e on e.DATAAREAID = p.DATAAREAID and e.PARTYNUMBER = p.CONTACTPERSONPARTYNUMBER 
		and e.TYPE = 'Email' and e.ISPRIMARY = 'Yes'
	left join dbo.Contact_Person_Electronic_Address pp on pp.DATAAREAID = p.DATAAREAID and pp.PARTYNUMBER = p.CONTACTPERSONPARTYNUMBER 
		and pp.TYPE = 'Phone' and pp.ISPRIMARY = 'Yes'
	left join dbo.Contact_Person_Postal_Address a on a.PARTYNUMBER = p.CONTACTPERSONPARTYNUMBER and a.DATAAREAID = p.DATAAREAID
		and a.ISPRIMARY = 1
	where
	    e.LOCATOR is not null and trim(e.locator) != ''
	and pp.LOCATOR is not null and trim(pp.locator) != ''
	and a.ADDRESS is not null and trim(a.ADDRESS) != ''
	and a.CITY is not null and trim(a.CITY) != ''
	and a.STATE is not null and trim(a.STATE) != ''
	and a.ZIPCODE is not null and trim(a.ZIPCODE) != ''
	and a.COUNTRYREGIONID is not null and trim(a.COUNTRYREGIONID) != ''
	and p.FIRSTNAME is not null and trim(p.FIRSTNAME) != ''
	and p.LASTNAME is not null and trim(p.LASTNAME) != ''



;With CoWithGoodContacts
as
(
	select p.ASSOCIATEDPARTYID, p.DATAAREAID, c.Guid as CustomerId
	from [dbo].[Contact_Person] p
	join dbo.Non_Billable_Customers c on p.ASSOCIATEDPARTYID = c.PARTYNUMBER  and c.DATAAREAID = p.DATAAREAID
	left join dbo.Contact_Person_Electronic_Address e on e.DATAAREAID = p.DATAAREAID and e.PARTYNUMBER = p.CONTACTPERSONPARTYNUMBER 
		and e.TYPE = 'Email' and e.ISPRIMARY = 'Yes'
	left join dbo.Contact_Person_Electronic_Address pp on pp.DATAAREAID = p.DATAAREAID and pp.PARTYNUMBER = p.CONTACTPERSONPARTYNUMBER 
		and pp.TYPE = 'Phone' and pp.ISPRIMARY = 'Yes'
	left join dbo.Contact_Person_Postal_Address a on a.PARTYNUMBER = p.CONTACTPERSONPARTYNUMBER and a.DATAAREAID = p.DATAAREAID
		and a.ISPRIMARY = 1
	where p.PRIMARY_CONTACT = 1
	and e.LOCATOR is not null and trim(e.locator) != ''
	and pp.LOCATOR is not null and trim(pp.locator) != ''
	and a.ADDRESS is not null and trim(a.ADDRESS) != ''
	and a.CITY is not null and trim(a.CITY) != ''
	and a.STATE is not null and trim(a.STATE) != ''
	and a.ZIPCODE is not null and trim(a.ZIPCODE) != ''
	and a.COUNTRYREGIONID is not null and trim(a.COUNTRYREGIONID) != ''
)
, GoodCustWithAddr
as
(
	select c.GUID as CustomerId, c.DATAAREAID, c.PARTYNUMBER,  c.CUSTOMERACCOUNT
	FROM [dbo].Non_Billable_Customers c  
	left join dbo.Non_Billable_Customers_Postal_Address ca on ca.CUSTOMERACCOUNTNUMBER = c.CUSTOMERACCOUNT and ca.DATAAREAID = c.DATAAREAID
		and ca.ISPRIMARY = 'Yes'
	where ca.ADDRESSSTREET is not null and trim(ca.ADDRESSSTREET) != ''
		and ca.ADDRESSCITY is not null and trim(ca.ADDRESSCITY) != ''
		and ca.ADDRESSSTATE is not null and trim(ca.ADDRESSSTATE) != ''
		and ca.ADDRESSZIPCODE is not null and trim(ca.ADDRESSZIPCODE) != ''
		and ca.ADDRESSCOUNTRYREGIONID is not null and trim(ca.ADDRESSCOUNTRYREGIONID) != ''
)
insert into dbo.ValidCustomerAccounts(CustomerId, DATAAREAID, PARTYNUMBER,  CUSTOMERACCOUNT)
select c.CustomerId, c.DATAAREAID, c.PARTYNUMBER,  c.CUSTOMERACCOUNT
from GoodCustWithAddr c
join CoWithGoodContacts cc on c.CustomerId = cc.CustomerId

GO

--Add all non mi accounts as valid
insert into dbo.ValidCustomerAccounts(CustomerId, DATAAREAID, PARTYNUMBER,  CUSTOMERACCOUNT)
select c.GUID, c.DATAAREAID, c.PARTYNUMBER,  c.CUSTOMERACCOUNT
from dbo.customers c 
join LegalEntity l on l.DataAreaId = c.DATAAREAID
where l.Division <> 'MI'

--Add Prospects as valid
insert into dbo.ValidCustomerAccounts(CustomerId, DATAAREAID, PARTYNUMBER,  CUSTOMERACCOUNT)
select c.GUID, c.DATAAREAID, c.PARTYNUMBER,  c.BUSRELACCOUNT
from dbo.Prospects c 


--*********************** account master table ****************************

CREATE TABLE [dbo].[AccountMaster](
    [TTAccountID] [int] IDENTITY(1000000,1) NOT NULL,
	[ID] uniqueidentifier not null,
	[SourceTable] [nvarchar](100),
	[DATAAREAID] [nvarchar](4) NULL,
	[CUSTOMERACCOUNT] [nvarchar](20) NULL,
	[PARTYNUMBER] [nchar](255) NULL,
	[PARTYTYPE] [varchar](12) NULL,
	[ORGANIZATIONNAME] [nvarchar](100) NULL,	
	[NAMEALIAS] [nvarchar](20) NULL,
	[CUSTOMERGROUPID] [nvarchar](255) NULL,
	IsActiveCustomer bit null,
	IsGenerator bit null,
	IsTruckingCo bit Null,
	Is3rdParty bit null,
	[DUNS] nvarchar(100),
	FIELDTICKETACTIVE nvarchar(100),
	INVOICEACTIVE nvarchar(100),
	PrimaryContactId uniqueidentifier,
	[CONTACTPERSONNAME] nvarchar(100),
	PrimaryEmail nvarchar(100),
	PrimaryPhone nvarchar(100),
	CustClassificationId nvarchar(100),
	CREDITRATING nvarchar(100),
	CREDITLIMIT nvarchar(100)
) ON [PRIMARY]
GO

-----------------ACCOUNT MASTER---------------------------

;With Accounts
as
(
	
	SELECT distinct c.[GUID] ID
		  ,'CUSTOMERS' as SourceTable
		  ,c.DATAAREAID
		  ,trim(c.CUSTOMERACCOUNT) [CUSTOMERACCOUNT]
		  ,c.PARTYNUMBER
		  ,PARTYTYPE
		  ,ORGANIZATIONNAME	 
		  ,NAMEALIAS
		  ,CUSTOMERGROUPID	 
		  , convert(bit, 1) [IsActiveCustomer]
		  , convert(bit, (select top(1) iif(count(*) >= 1, 1, 0) from dbo.uwi u where u.[owner] = c.CUSTOMERACCOUNT and u.DATAAREAID = c.DATAAREAID)) as [IsGenerator]
		  , convert(bit, 0)[IsTruckingCo]
		  ,	convert(bit, 0)[Is3rdParty]
		  , (select top(1) DUNS 
				from [dbo].[EDI_Customer_Additional_Pref] p 
				where  c.DATAAREAID = p.DATAAREAID and c.CUSTOMERACCOUNT = p.CustomerAccount
		  ) as DUNS
		  , m.FIELDTICKETACTIVE
		  , m.INVOICEACTIVE
		  , cp.[GUID] as PrimaryContactId
		  , cp.[CONTACTPERSONNAME] as PrimaryContactName
		  ,Isnull(trim((
				select top (1) value
				from string_split(trim(cpe.LOCATOR), ',')
			 )), trim(cpe.LOCATOR)) as PrimaryEmail
		  , trim(cpp.Locator) as PrimaryPhone
		  , c.CustClassificationId
		  , c.CREDITRATING
		  , c.CREDITLIMIT 
		FROM [dbo].[Customers] c    
		join dbo.ValidCustomerAccounts va on c.GUID = va.CustomerId
		left join [dbo].[EDI_HSEDICustomerConfigMasterEntity] m on c.CUSTOMERACCOUNT = m.ACCOUNTNUM and c.DATAAREAID = m.DATAAREAID
		left join [dbo].[Contact_Person] cp on cp.AssociatedPartyId = c.PartyNumber and cp.DATAAREAID = c.DATAAREAID and cp.Primary_Contact = 1
		left join [dbo].[Contact_Person_Electronic_Address] cpe on cpe.PartyNumber = cp.[CONTACTPERSONPARTYNUMBER] 
			and cpe.[TYPE] = 'Email' and cpe.ISPRIMARY = 'Yes' and cpe.purpose = 'Business' 
		left join [dbo].[Contact_Person_Electronic_Address] cpp on cpp.PartyNumber = cp.[CONTACTPERSONPARTYNUMBER] 
			and cpp.[TYPE] = 'Phone' and cpp.ISPRIMARY = 'Yes' and cpp.purpose = 'Business' 
	where c.[GUID] is not null
	Union
	SELECT b.[GUID] ID
		  ,'NONBILLABLECUSTOMERS' as SourceTable
		  ,b.DATAAREAID
		  ,trim(CUSTOMERACCOUNT) [CUSTOMERACCOUNT]
		  ,NULL
		  ,NULL
		  ,ORGANIZATIONNAME	 
		  ,NAMEALIAS
		  ,CUSTOMERGROUPID	
		  , convert(bit, 0) [IsActiveCustomer] 
		  , convert(bit, (select top(1) iif(count(*) >= 1, 1, 0) from dbo.uwi u where u.[owner] = b.CUSTOMERACCOUNT and u.DATAAREAID = b.DATAAREAID)) as [IsGenerator]
		  , convert(bit, 0)[IsTruckingCo]
		  ,	convert(bit, 0)[Is3rdParty]
		  , NULL as DUNS
		  , NULL as FIELDTICKETACTIVE
		  , NULL AS INVOICEACTIVE
		  , cp.[GUID] as PrimaryContactId
		  , cp.CONTACTPERSONNAME as PrimaryContactName
		  , Isnull(trim((
				select top (1) value
				from string_split(trim(cpe.LOCATOR), ',')
			 )), trim(cpe.LOCATOR)) as PrimaryEmail
		  , trim(cpp.Locator) as PrimaryPhone
		  , b.CustClassificationId as CustClassificationId
		  , b.CREDITRATING
		  , b.CREDITLIMIT 
	FROM  [dbo].[Non_Billable_Customers] b
		left join [dbo].[Contact_Person] cp on cp.AssociatedPartyId = b.PartyNumber and cp.DATAAREAID = b.DATAAREAID and cp.Primary_Contact = 1
		left join [dbo].[Contact_Person_Electronic_Address] cpe on cpe.PartyNumber = cp.[CONTACTPERSONPARTYNUMBER] 
			and cpe.[TYPE] = 'Email' and cpe.ISPRIMARY = 'Yes' and cpe.purpose = 'Business' 
		left join [dbo].[Contact_Person_Electronic_Address] cpp on cpp.PartyNumber = cp.[CONTACTPERSONPARTYNUMBER] 
			and cpp.[TYPE] = 'Phone' and cpp.ISPRIMARY = 'Yes' and cpe.purpose = 'Business' 
	where  b.[Guid] is not null and b.[GUID] not in (select c.[GUID] from [dbo].[Customers] c where c.[GUID] = b.[guid])
	UNION
	SELECT distinct c.[GUID] ID
		  ,'PROSPECTS' as SourceTable
		  ,c.DATAAREAID
		  ,trim(c.BUSRELACCOUNT) CustomerNumber
		  ,trim(c.PARTYNUMBER) PartyNumber
		  , NULL as PartyType
		  ,c.[NAME]	 
		  ,NULL as NAMEALIAS
		  ,c.CUSTGROUP
		  , convert(bit, 0) as IsActiveCustomer
		  , convert(bit, 0) as IsGenerator
		  , iif(c.busreltypeid = 'Trucking Company', 1 , 0 ) as IsTruckingCo	 
		  , iif(c.busreltypeid = '3rd Party Analytical', 1 , 0 ) as Is3rdParty
		  , NULL as DUNS
		  , NULL as FIELDTICKETACTIVE
		  , NULL AS INVOICEACTIVE
		  , cp.Id as PrimaryContactId
		  , cp.ContactName as PrimaryContactName
		  , Isnull(trim((
				select top (1) value
				from string_split(trim(cpe.LOCATOR), ',')
			 )), trim(cpe.LOCATOR)) as PrimaryEmail
		  , trim(cpp.Locator) as PrimaryPhone	
		  , null as CustClassificationId
		  , null as CREDITRATING
		  , null as CREDITLIMIT 
	FROM [dbo].[Prospects] c	
		left join [dbo].ProspectContacts cp on cp.BUSRELACCOUNT = c.BUSRELACCOUNT and cp.DATAAREAID = c.DATAAREAID and cp.Primary_Contact = 1
		left join [dbo].ProspectContactEmailPhones cpe on cpe.ContactId = cp.Id 
			and cpe.[TYPE] = 'Email' and cpe.ISPRIMARY = 1
		left join [dbo].ProspectContactEmailPhones cpp on cpp.ContactId = cp.Id 
			and cpp.[TYPE] = 'Phone' and cpp.ISPRIMARY = 1
	where c.[Guid] is not null
)
INSERT INTO [dbo].[AccountMaster] ([ID], [SourceTable], [DATAAREAID], [CUSTOMERACCOUNT], [PARTYNUMBER], 
[PARTYTYPE], [ORGANIZATIONNAME], [NAMEALIAS], [CUSTOMERGROUPID], [IsActiveCustomer], [IsGenerator], [IsTruckingCo], 
[Is3rdParty], [DUNS], FIELDTICKETACTIVE, INVOICEACTIVE, PrimaryContactId, [CONTACTPERSONNAME], PrimaryEmail, PrimaryPhone, CustClassificationId, CREDITRATING, CREDITLIMIT) 
select *
from Accounts a
where a.ID is not null
--Need to remove this from data or provide legal entity refs
and a.DATAAREAID not in ('nesi', 'sosu', 'osrv', 'dsmn');


alter table dbo.AccountMaster
add primary key (ID)

CREATE NONCLUSTERED INDEX [NonClusteredIndex-AccountMaster-CustomerAccount] ON [dbo].[AccountMaster]
(
	[DATAAREAID] ASC,
	[CUSTOMERACCOUNT] ASC,
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [NonClusteredIndex-AccountMaster-PartyNumber] ON [dbo].[AccountMaster]
(
	[DATAAREAID] ASC,
	[PARTYNUMBER] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


