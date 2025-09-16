IF OBJECT_ID(N'dbo.Contact_Person', N'U') IS NOT NULL 
	drop table dbo.Contact_Person
go

select [PRIMARY_CONTACT], [CONTACTPERSONID], [CONTACTPERSONNAME], [CONTACTPERSONPARTYNUMBER], [CONTACTPERSONPARTYTYPE], 
		[CONTACTPERSONRESPONSIBLEPERSONNELNUMBER], [ASSOCIATEDPARTYID], [EMPLOYMENTDEPARTMENT], [EMPLOYMENTPROFESSION], 
		[FIRSTNAME], [GENDER], [GOVERNMENTIDENTIFICATIONNUMBER], [INITIALS], [ISIMPORTED], [ISINACTIVE], [LASTNAME], [MARITALSTATUS], 
		[MIDDLENAME], [NOTES], [SEARCHNAME], [DATAAREAID], convert(uniqueidentifier,replace(replace(TRIM([GUID]), '{', ''), '}', '')) as [GUID], RECID
into dbo.Contact_Person
from [dbo].[ZZ_Contact_Person]

go

Alter table dbo.Contact_Person
alter column [GUID] uniqueidentifier not null
go

alter table dbo.contact_person
add primary key ([GUID])
go

CREATE NONCLUSTERED INDEX [NonClusteredIndex-Contact-personid] ON [dbo].[Contact_Person]
(
	[DATAAREAID] ASC,
	[CONTACTPERSONID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [NonClusteredIndex-contact-CompanyPartyNumber] ON [dbo].[Contact_Person]
(
	[DATAAREAID] ASC,
	[ASSOCIATEDPARTYID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [NonClusteredIndex-Contact-PartyNumber] ON [dbo].[Contact_Person]
(
	[DATAAREAID] ASC,
	[CONTACTPERSONPARTYNUMBER] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


IF OBJECT_ID(N'dbo.Contact_Person_Electronic_Address', N'U') IS NOT NULL 
	drop table dbo.Contact_Person_Electronic_Address
go

select	[PARTYNUMBER], [DESCRIPTION], [ISINSTANTMESSAGE], [ISMOBILEPHONE], [ISPRIMARY], [ISPRIVATE], 
		case [Type] 			
			when 'Email' then replace(replace(dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '-com', '.com')
			when 'Phone' then REPLACE(REPLACE(	dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '.', '-')
		end
	 as LOCATOR,		
		[LOCATOREXTENSION], 
		[PURPOSE], [TYPE], [DATAAREAID], convert(uniqueidentifier,replace(replace(TRIM([GUID]), '{', ''), '}', '')) as [GUID]
		,case 
			when [Type] = 'Email' and Locator is not null and Trim(Locator) <> '' then dbo.isValidEmailFormat(replace(replace(dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '-com', '.com')) 
			when [Type] = 'Phone' and Locator is not null and Trim(Locator) <> '' then dbo.isValidPhoneFormat(REPLACE(REPLACE(dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '.', '-')) 
			else null
		 end as IsValidLocator
into dbo.Contact_Person_Electronic_Address
from [dbo].[ZZ_Contact_Person_Electronic_Address]

alter table Contact_Person_Electronic_Address
add SeqId int not null Identity(1,1)  Primary Key 

;With BadPhones
as
(
	select dbo.isValidEmailFormat(replace(replace(dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '-com', '.com')) as IsReallyEamil, 
	replace(replace(dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '-com', '.com') as FixedLocator,
	e.*
	from dbo.Contact_Person_Electronic_Address e
	where e.TYPE = 'Phone'  and e.isvalidlocator = 0
)
update Contact_Person_Electronic_Address
set [Type] = 'Email',
	IsValidLocator = 1,
	LOCATOR = FixedLocator
from Contact_Person_Electronic_Address e
join BadPhones b on e.GUID = b.GUID
where IsReallyEamil = 1

;With BadEmails
as
(
	select dbo.isValidPhoneFormat(REPLACE(REPLACE(	dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '.', '-')) as IsReallyPHone, 
	REPLACE(REPLACE(	dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '.', '-') as FixedLocator
	,e.*
	from dbo.Contact_Person_Electronic_Address e
	where e.TYPE = 'Email'  and e.isvalidlocator = 0
)
update Contact_Person_Electronic_Address
set [Type] = 'Phone',
	IsValidLocator = 1,
	LOCATOR = FixedLocator
from Contact_Person_Electronic_Address e
join BadEmails b on e.GUID = b.GUID
where IsReallyPhone = 1


IF OBJECT_ID(N'dbo.Contact_Person_Postal_Address', N'U') IS NOT NULL 
	drop table dbo.Contact_Person_Postal_Address
go

select  [PARTYNUMBER], [ADDRESS], [CITY], [COUNTRYREGIONID], [COUNTRYREGIONISOCODE], [DESCRIPTION], [ISLOCATIONOWNER], [ISPRIMARY], 
		[ISPRIVATE], [ISPRIVATEPOSTALADDRESS], [ISROLEBUSINESS], [ISROLEDELIVERY], [ROLES], [STATE], [STREET], [STREETNUMBER], 
		[ZIPCODE], [DATAAREAID], convert(uniqueidentifier,replace(replace(TRIM([GUID]), '{', ''), '}', '')) as [GUID]
into  dbo.Contact_Person_Postal_Address
from [dbo].[ZZ_Contact_Person_Postal_Address]



IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL 
	drop table dbo.Customers
go

select  [CUSTOMERACCOUNT], [PARTYNUMBER], [PARTYTYPE], [ORGANIZATIONNAME], [PERSONFIRSTNAME], [PERSONMIDDLENAME], 
		[PERSONLASTNAME], [NAMEALIAS], [CUSTOMERGROUPID], [LANGUAGEID], [LINEOFBUSINESSID], [EMPLOYEERESPONSIBLENUMBER], 
		[SALESDISTRICT], [SALESCURRENCYCODE], [SALESMEMO], [CREDITLIMITISMANDATORY], [CREDITRATING], [CREDITLIMIT], 
		[PAYMENTTERMS], [PAYMENTMETHOD], [PAYMENTCASHDISCOUNT], [DELIVERYTERMS], [DELIVERYMODE], [SALESTAXGROUP], [DEFAULTDIMENSIONDISPLAYVALUE], 
		[AccountStatement], [DISCOUNTPRICEGROUPID], [LineDisc], [CustClassificationId], [BillingType], convert(uniqueidentifier,replace(replace(TRIM([GUID]), '{', ''), '}', '')) as [GUID], [DATAAREAID]
into dbo.Customers
from [dbo].[ZZ_Customers]

alter table dbo.Customers
Alter column GUID uniqueidentifier not null

GO

alter table dbo.Customers
add primary key (GUID)

GO

CREATE NONCLUSTERED INDEX [NonClusteredIndex-CustomerAccount] ON [dbo].[Customers]
(
	[DATAAREAID] ASC,
	[CUSTOMERACCOUNT] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [NonClusteredIndex-PartyNumber] ON [dbo].[Customers]
(
	[PARTYNUMBER] ASC,
	[DATAAREAID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


IF OBJECT_ID(N'dbo.Customers_Electronic_Addresses', N'U') IS NOT NULL 
	drop table dbo.Customers_Electronic_Addresses
go



select	[ASCUSTACCOUNTNUM], [PARTYNUMBER], [DESCRIPTION], [ISINSTANTMESSAGE], [ISMOBILEPHONE], [ISPRIMARY], 
		[ISPRIVATE] 
		,case [Type] 			
			when 'Email' then replace(replace(dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '-com', '.com')
			when 'Phone' then REPLACE(REPLACE(	dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '.', '-')
		end
		as LOCATOR
		, [LOCATOREXTENSION], [PURPOSE], [TYPE], convert(uniqueidentifier,replace(replace(TRIM([GUID]), '{', ''), '}', '')) as [GUID], [DATAAREAID]
		,case 
			when [Type] = 'Email' and Locator is not null and Trim(Locator) <> '' then dbo.isValidEmailFormat(replace(replace(dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '-com', '.com')) 
			when [Type] = 'Phone' and Locator is not null and Trim(Locator) <> '' then dbo.isValidPhoneFormat(REPLACE(REPLACE(dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '.', '-')) 
			else null
		 end as IsValidLocator
into dbo.Customers_Electronic_Addresses
from [dbo].[ZZ_Customers_Electronic_Addresses]


	


IF OBJECT_ID(N'dbo.Customers_operator_code', N'U') IS NOT NULL 
	drop table dbo.Customers_operator_code
go

select [CUSTACCOUNT], [ADDRESSSTATEID], [ADDRESSCOUNTRYREGIONID], [CUSTOPERATORCODE]
into dbo.Customers_operator_code
from [dbo].[ZZ_Customers_operator_code]

IF OBJECT_ID(N'dbo.Customers_Postal_Addresses', N'U') IS NOT NULL 
	drop table dbo.Customers_Postal_Addresses
go

select	[CUSTOMERLEGALENTITYID], [PARTYNUMBER], [CUSTOMERACCOUNTNUMBER], [ADDRESSDESCRIPTION], [ADDRESSLOCATIONROLES], 
		[ADDRESSCOUNTRYREGIONID], [ADDRESSZIPCODE], [ADDRESSSTREET], [ADDRESSCITY], [ADDRESSSTATE], [IsPostalAddress], 
		[ISPRIMARY], [ASCUSTACCOUNTNUM], convert(uniqueidentifier,replace(replace(TRIM([GUID]), '{', ''), '}', '')) as [GUID], [DATAAREAID]
into dbo.Customers_Postal_Addresses
from [dbo].[ZZ_Customers_Postal_Addresses]

IF OBJECT_ID(N'dbo.EDI_Customer_Additional_Pref', N'U') IS NOT NULL 
	drop table dbo.EDI_Customer_Additional_Pref
go

select  l.[Division],
		p.[DATAAREAID], 
		[CustomerId] as CustomerAccount, 
		[OrderType], 
		[Customer], 
		[DUNS], 
		[Platform], 
		convert(nvarchar(500), [AFE]) as [AFE] ,
		convert(nvarchar(500), [GeneralLedger] ) as[GeneralLedger] ,
		convert(nvarchar(500), [CostCenter] ) as [CostCenter] ,
		convert(nvarchar(500), [JobNumber] ) as [JobNumber] ,
		convert(nvarchar(500), [AccountingRef] ) as [AccountingRef] ,
		convert(nvarchar(500), [PONumber] ) as [PONumber] ,
		convert(nvarchar(500), [POLine] ) as [POLine] ,
		convert(nvarchar(500), [WellIdent] ) as [WellIdent] ,
		convert(nvarchar(500), [Contract] ) as [Contract] ,
		convert(nvarchar(500), [CustomerRef] ) as [CustomerRef] ,
		convert(nvarchar(500), [PersonnelName] ) as [PersonnelName] ,
		convert(nvarchar(500), [CostCenterName] ) as [CostCenterName] ,
		convert(nvarchar(500), [InvoiceNumber] ) as [InvoiceNumber] ,
		convert(nvarchar(500), [ItemNumber] ) as [ItemNumber]
into  dbo.EDI_Customer_Additional_Pref
from [dbo].[ZZ_EDI_Customer_Additional_Pref] p
inner join dbo.ZZ_LegalEntity l on l.DATAAREAID = p.DATAAREAID

--clean data to null where its no value
update [dbo].[EDI_Customer_Additional_Pref]		
 set    [Platform] = iif(trim([Platform]) = '', null, trim([Platform] )),
		[AFE] = iif(trim([AFE] ) = '', null, trim([AFE] )),
		[GeneralLedger] = iif(trim([GeneralLedger] ) = '', null, trim([GeneralLedger] )),
		[CostCenter] = iif(trim([CostCenter] ) = '', null, trim([CostCenter] )),
		[JobNumber] = iif(trim([JobNumber] ) = '', null, trim([JobNumber] )),
		[AccountingRef] = iif(trim([AccountingRef] ) = '', null, trim([AccountingRef] )),
		[PONumber] = iif(trim([PONumber] ) = '', null, trim([PONumber] )),
		[POLine] = iif(trim([POLine] ) = '', null, trim([POLine] )),
		[WellIdent] = iif(trim([WellIdent] ) = '', null, trim([WellIdent] )),
		[Contract] = iif(trim([Contract] ) = '', null, trim([Contract] )),
		[CustomerRef] = iif(trim([CustomerRef] ) = '', null, trim([CustomerRef] )),
		[PersonnelName] = iif(trim([PersonnelName] ) = '', null, trim([PersonnelName] )),
		[CostCenterName] = iif(trim([CostCenterName] ) = '', null, trim([CostCenterName] )),
		[InvoiceNumber] = iif(trim([InvoiceNumber] ) = '', null, trim([InvoiceNumber] )),
		[ItemNumber] = iif(trim([ItemNumber] ) = '', null, trim([ItemNumber] ))
from [dbo].[EDI_Customer_Additional_Pref]


IF OBJECT_ID(N'dbo.EDI_Customer_Additional_Pref_Field_Tickets', N'U') IS NOT NULL 
	drop table dbo.EDI_Customer_Additional_Pref_Field_Tickets
go

select  [DATAAREAID], [CustomerID], [OrderType], [Customer], [DUNS], [Platform], [AFE], [GeneralLedger], [CostCenter], [JobNumber], 
		[AccountingRef], [PONumber], [POLine], [WellIdent], [Contract], [CustomerRef], [PersonnelName], [CostCenterName], 
		[ItemNumber], [RecordNum]
into  dbo.EDI_Customer_Additional_Pref_Field_Tickets
from [dbo].[ZZ_EDI_Customer_Additional_Pref_Field_Tickets]


IF OBJECT_ID(N'dbo.EDI_HSEDICustomerConfigData_PROJECTS', N'U') IS NOT NULL 
	drop table dbo.EDI_HSEDICustomerConfigData_PROJECTS
go

select	[PROJID], [ACCOUNT], [ACCOUNTQTY], [AFE], [AFEOWNER], [ALPHA], [APPROVERCODING], 
		[ASSETNUMBER], [ATTNTO], [ATTNTONAME], [BUYERCODE], [BUYERDEPARTMENTDEFAULT], [CATALOGSERIALNO], 
		[CCTYPE], [CHARGETO], [CHARGETONAME], [COMPANY], [COMPANYREP], [CONTRACT], [CONTRACTLINENUMBER], 
		[CORRELATIONID], [COSTCENTER], [COSTCENTERNAME], [EDIZIP4], [FIELDLOCATION], [GLACCOUNT], [JOINTS], 
		[LOCATION], [LOCATIONCOMMENTS], [MAJORCODE], [MATERIALCODE], [MATGRP], [MGDESC], [MINORCODE], 
		[NETWORKACTIVITY], [OBJSUBNA], [OPERATIONCAT], [OPERATORCODING], [OPNUMBER], [ORDERNUMBER], [POAPPROVER], 
		[POLINE], [PONUMBER], [PROFITCENTRE], [PROJECT], [PROJECTPONO], [REPORTCENTRE], [REQUISITIONER], [SERIALNUMBER], 
		[SIGNATORY], [SOLINE], [SONUMBER], [STKCOND], [SUB], [SUBCODE], [SUBFEATURE], [WBS], [WELLFACILITY], [WONUMBER], 
		[WORKORDEREDBY]
into [dbo].[EDI_HSEDICustomerConfigData_PROJECTS]
from [dbo].[ZZ_EDI_HSEDICustomerConfigData_PROJECTS]




IF OBJECT_ID(N'dbo.EDI_HSEDICustomerConfigEntity', N'U') IS NOT NULL 
	drop table dbo.EDI_HSEDICustomerConfigEntity
go


select [EDIFIELD], [ACCOUNTNUM], [HSPRINT], [MANDATORY], [REQUIRED], [SHOW], convert(uniqueidentifier,replace(replace(TRIM([GUID]), '{', ''), '}', '')) as [GUID], [DATAAREAID]
into dbo.EDI_HSEDICustomerConfigEntity
from [dbo].[ZZ_EDI_HSEDICustomerConfigEntity]

/*************************** SCA EDI ************************************************/
IF OBJECT_ID(N'dbo.EDI_HSEDICustomerConfigEntity_SCA', N'U') IS NOT NULL 
	drop table dbo.EDI_HSEDICustomerConfigEntity_SCA
go


;with LastestEDI
as
(
	select SCA, DATAAREAID, max(recid) as latestId
	from dbo.[ZZ_EDI_HSEDICustomerConfigEntity_SCA]
	group by  SCA, DATAAREAID
	--280770
)
select distinct e.*
into [dbo].[EDI_HSEDICustomerConfigEntity_SCA]
from LastestEDI l
join [dbo].[ZZ_EDI_HSEDICustomerConfigEntity_SCA] e on l.SCA = e.SCA and l.latestId = e.RECID and l.DATAAREAID = e.DATAAREAID


--MAKE COLUMNS NULLABLE FOR CLEAN UP PUPROSES
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [ACCOUNT] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [ACCOUNTQTY] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [ACCTCENTRE] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [ACTIVITYDATE] [datetime] NOT NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [ALPHA] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [ATTNTO] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [ATTNTONAME] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [CATALOGSERIALNO] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [CCTYPE] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [CHARGETO] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [CHARGETONAME] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [COMPANY] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [CONTRACT] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [CONTRACTLINENUMBER] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [FIELDLOCATION] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [GLACCOUNT] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [JOINTS] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [LINEITEMDESCRIPTION] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [LOCATION] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [LOCATIONCOMMENTS] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [MATERIALCODE] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [MATGRP] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [MGDESC] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [NETWORKACTIVITY] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [OPNUMBER] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [ORDERNUMBER] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [PROFITCENTER] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [PROJECT] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [PROJID] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [REPORTCENTRE] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SCA] [nvarchar](20) NOT NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SERIALNUMBER] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SERVICEORDERID] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESAFE] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESAFEOWNER] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESAPPROVERCODING] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESASSETNUMBER] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESBUYERCODE] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESCOMPANYREP] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESCOSTCENTER] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESCOSTCENTERNAME] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESMAJORCODE] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESMINORCODE] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESOBJSUBNA] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESOPERATIONCAT] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESOPERATORCODING] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESPOAPPROVER] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESPOLINE] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESPONUMBER] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESPROJECTPONO] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESREQUISITIONER] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESSIGNATORY] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESSONUMBER] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESSUBCODE] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESWBS] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESWONUMBER] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SOLINE] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [STKCOND] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SUB] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SUBFEATURE] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [WELLFACILITY] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [WORKORDEREDBY] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESBUYERSDEPARTMENTDEFAULT] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESEDIZIP4] [nvarchar](100) NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [DATAAREAID] [nvarchar](4) NOT NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [RECVERSION] [int] NOT NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [PARTITION] [bigint] NOT NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [RECID] [bigint] NOT NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SALESID] [nvarchar](100) NOT NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SALESLINEREFRECID] [bigint] NOT NULL
ALTER TABLE [dbo].[EDI_HSEDICustomerConfigEntity_SCA]  ALTER COLUMN [SESEDIFIELDTICKET] [nvarchar](100) NOT NULL
GO

--UNPDATE SCA EDI EXTENDED FIELDS NOT IN USE TO NULL
update [dbo].[EDI_HSEDICustomerConfigEntity_SCA]
set [ACCOUNT] = iif(trim([ACCOUNT] ) = '', null, trim([ACCOUNT])),
	[ACCOUNTQTY] = iif(trim([ACCOUNTQTY] ) = '', null, trim([ACCOUNTQTY] )),
	[ACCTCENTRE] = iif(trim([ACCTCENTRE] ) = '', null, trim([ACCTCENTRE] )),
	[ALPHA] = iif(trim([ALPHA] ) = '', null, trim([ALPHA] )),
	[ATTNTO] = iif(trim([ATTNTO] ) = '', null, trim([ATTNTO] )),
	[ATTNTONAME] = iif(trim([ATTNTONAME] ) = '', null, trim([ATTNTONAME] )),
	[CATALOGSERIALNO] = iif(trim([CATALOGSERIALNO] ) = '', null, trim([CATALOGSERIALNO] )),
	[CCTYPE] = iif(trim([CCTYPE] ) = '', null, trim([CCTYPE] )),
	[CHARGETO] = iif(trim([CHARGETO] ) = '', null, trim([CHARGETO] )),
	[CHARGETONAME] = iif(trim([CHARGETONAME] ) = '', null, trim([CHARGETONAME] )),
	[COMPANY] = iif(trim([COMPANY] ) = '', null, trim([COMPANY] )),
	[CONTRACT] = iif(trim([CONTRACT] ) = '', null, trim([CONTRACT] )),
	[CONTRACTLINENUMBER] = iif(trim([CONTRACTLINENUMBER] ) = '', null, trim([CONTRACTLINENUMBER] )),
	[FIELDLOCATION] = iif(trim([FIELDLOCATION] ) = '', null, trim([FIELDLOCATION] )),
	[GLACCOUNT] = iif(trim([GLACCOUNT] ) = '', null, trim([GLACCOUNT] )),
	[JOINTS] = iif(trim([JOINTS] ) = '', null, trim([JOINTS] )),
	[LINEITEMDESCRIPTION] = iif(trim([LINEITEMDESCRIPTION] ) = '', null, trim([LINEITEMDESCRIPTION] )),
	[LOCATION] = iif(trim([LOCATION] ) = '', null, trim([LOCATION] )),
	[LOCATIONCOMMENTS] = iif(trim([LOCATIONCOMMENTS] ) = '', null, trim([LOCATIONCOMMENTS] )),
	[MATERIALCODE] = iif(trim([MATERIALCODE] ) = '', null, trim([MATERIALCODE] )),
	[MATGRP] = iif(trim([MATGRP] ) = '', null, trim([MATGRP] )),
	[MGDESC] = iif(trim([MGDESC] ) = '', null, trim([MGDESC] )),
	[NETWORKACTIVITY] = iif(trim([NETWORKACTIVITY] ) = '', null, trim([NETWORKACTIVITY] )),
	[OPNUMBER] = iif(trim([OPNUMBER] ) = '', null, trim([OPNUMBER] )),
	[ORDERNUMBER] = iif(trim([ORDERNUMBER] ) = '', null, trim([ORDERNUMBER] )),
	[PROFITCENTER] = iif(trim([PROFITCENTER] ) = '', null, trim([PROFITCENTER] )),
	[PROJECT] = iif(trim([PROJECT] ) = '', null, trim([PROJECT] )),
	[PROJID] = iif(trim([PROJID] ) = '', null, trim([PROJID] )),
	[REPORTCENTRE] = iif(trim([REPORTCENTRE] ) = '', null, trim([REPORTCENTRE] )),
	[SERIALNUMBER] = iif(trim([SERIALNUMBER] ) = '', null, trim([SERIALNUMBER] )),
	[SERVICEORDERID] = iif(trim([SERVICEORDERID] ) = '', null, trim([SERVICEORDERID] )),
	[SESAFE] = iif(trim([SESAFE] ) = '', null, trim([SESAFE] )),
	[SESAFEOWNER] = iif(trim([SESAFEOWNER] ) = '', null, trim([SESAFEOWNER] )),
	[SESAPPROVERCODING] = iif(trim([SESAPPROVERCODING] ) = '', null, trim([SESAPPROVERCODING] )),
	[SESASSETNUMBER] = iif(trim([SESASSETNUMBER] ) = '', null, trim([SESASSETNUMBER] )),
	[SESBUYERCODE] = iif(trim([SESBUYERCODE] ) = '', null, trim([SESBUYERCODE] )),
	[SESCOMPANYREP] = iif(trim([SESCOMPANYREP] ) = '', null, trim([SESCOMPANYREP] )),
	[SESCOSTCENTER] = iif(trim([SESCOSTCENTER] ) = '', null, trim([SESCOSTCENTER] )),
	[SESCOSTCENTERNAME] = iif(trim([SESCOSTCENTERNAME] ) = '', null, trim([SESCOSTCENTERNAME] )),
	[SESMAJORCODE] = iif(trim([SESMAJORCODE] ) = '', null, trim([SESMAJORCODE] )),
	[SESMINORCODE] = iif(trim([SESMINORCODE] ) = '', null, trim([SESMINORCODE] )),
	[SESOBJSUBNA] = iif(trim([SESOBJSUBNA] ) = '', null, trim([SESOBJSUBNA] )),
	[SESOPERATIONCAT] = iif(trim([SESOPERATIONCAT] ) = '', null, trim([SESOPERATIONCAT] )),
	[SESOPERATORCODING] = iif(trim([SESOPERATORCODING] ) = '', null, trim([SESOPERATORCODING] )),
	[SESPOAPPROVER] = iif(trim([SESPOAPPROVER] ) = '', null, trim([SESPOAPPROVER] )),
	[SESPOLINE] = iif(trim([SESPOLINE] ) = '', null, trim([SESPOLINE] )),
	[SESPONUMBER] = iif(trim([SESPONUMBER] ) = '', null, trim([SESPONUMBER] )),
	[SESPROJECTPONO] = iif(trim([SESPROJECTPONO] ) = '', null, trim([SESPROJECTPONO] )),
	[SESREQUISITIONER] = iif(trim([SESREQUISITIONER] ) = '', null, trim([SESREQUISITIONER] )),
	[SESSIGNATORY] = iif(trim([SESSIGNATORY] ) = '', null, trim([SESSIGNATORY] )),
	[SESSONUMBER] = iif(trim([SESSONUMBER] ) = '', null, trim([SESSONUMBER] )),
	[SESSUBCODE] = iif(trim([SESSUBCODE] ) = '', null, trim([SESSUBCODE] )),
	[SESWBS] = iif(trim([SESWBS] ) = '', null, trim([SESWBS] )),
	[SESWONUMBER] = iif(trim([SESWONUMBER] ) = '', null, trim([SESWONUMBER] )),
	[SOLINE] = iif(trim([SOLINE] ) = '', null, trim([SOLINE] )),
	[STKCOND] = iif(trim([STKCOND] ) = '', null, trim([STKCOND] )),
	[SUB] = iif(trim([SUB] ) = '', null, trim([SUB] )),
	[SUBFEATURE] = iif(trim([SUBFEATURE] ) = '', null, trim([SUBFEATURE] )),
	[WELLFACILITY] = iif(trim([WELLFACILITY] ) = '', null, trim([WELLFACILITY] )),
	[WORKORDEREDBY] = iif(trim([WORKORDEREDBY] ) = '', null, trim([WORKORDEREDBY] )),
	[SESBUYERSDEPARTMENTDEFAULT] = iif(trim([SESBUYERSDEPARTMENTDEFAULT]) = '', null, trim([SESBUYERSDEPARTMENTDEFAULT])),
	[SESEDIZIP4] = iif(trim([SESEDIZIP4] ) = '', null, trim([SESEDIZIP4])),
	[SCA] = TRIM([SCA])

GO



IF OBJECT_ID(N'dbo.EDI_HSEDICustomerConfigMasterEntity', N'U') IS NOT NULL 
	drop table dbo.EDI_HSEDICustomerConfigMasterEntity
go

select distinct	[ACCOUNTNUM], [CORRELATIONID], [DEFAULTBUYERDEPARTMENT], [ENABLEEDIFIELDS], [FIELDTICKETACTIVE], [INVOICEACTIVE], [PLATFORMFT], 
		[PLATFORMINVOICE], [SENDATTACHMENTFT], [SENDATTACHMENTINVOICE], [SUMMARYINDICATOR], [DATAAREAID]
into  dbo.EDI_HSEDICustomerConfigMasterEntity
from [dbo].[ZZ_EDI_HSEDICustomerConfigMasterEntity]

IF OBJECT_ID(N'dbo.Non_Billable_Customers', N'U') IS NOT NULL 
	drop table dbo.Non_Billable_Customers
go

select distinct	[CUSTOMERACCOUNT], [PartyNumber], [PARTYTYPE], [ORGANIZATIONNAME], [PERSONFIRSTNAME], [PERSONMIDDLENAME], [PERSONLASTNAME], [NAMEALIAS], 
		[CUSTOMERGROUPID], [LANGUAGEID], [LINEOFBUSINESSID], [EMPLOYEERESPONSIBLENUMBER], [SALESDISTRICT], [SALESCURRENCYCODE], 
		[SALESMEMO], [CREDITLIMITISMANDATORY], [CREDITRATING], [CREDITLIMIT], [PAYMENTTERMS], [PAYMENTMETHOD], [PAYMENTCASHDISCOUNT], 
		[DELIVERYTERMS], [DELIVERYMODE], [SALESTAXGROUP], [DEFAULTDIMENSIONDISPLAYVALUE], [AccountStatement], [DISCOUNTPRICEGROUPID], 
		[LineDisc], [CustClassificationId], convert(uniqueidentifier,replace(replace(TRIM([GUID]), '{', ''), '}', '')) as [GUID], [DATAAREAID]
into dbo.Non_Billable_Customers
from [dbo].[ZZ_Non_Billable_Customers]



IF OBJECT_ID(N'dbo.Non_Billable_Customers_Electronic_Address', N'U') IS NOT NULL 
	drop table dbo.Non_Billable_Customers_Electronic_Address
go

select distinct	[CUSTACCOUNTNUM], [DESCRIPTION], [ISINSTANTMESSAGE], [ISMOBILEPHONE], [ISPRIMARY], [ISPRIVATE] 
		,case [Type] 			
			when 'Email' then replace(replace(dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '-com', '.com')
			when 'Phone' then REPLACE(REPLACE(	dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '.', '-')
		end
		as LOCATOR 
		,[LOCATOREXTENSION], [PURPOSE], [TYPE], convert(uniqueidentifier,replace(replace(TRIM([GUID]), '{', ''), '}', '')) as [GUID], [DATAAREAID]
		,case 
			when [Type] = 'Email' and Locator is not null and Trim(Locator) <> '' then dbo.isValidEmailFormat(replace(replace(dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '-com', '.com')) 
			when [Type] = 'Phone' and Locator is not null and Trim(Locator) <> '' then dbo.isValidPhoneFormat(REPLACE(REPLACE(dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '.', '-')) 
			else null
		 end as IsValidLocator
into dbo.Non_Billable_Customers_Electronic_Address
from [dbo].[ZZ_Non_Billable_Customers_Electronic_Address]



IF OBJECT_ID(N'dbo.Non_Billable_Customers_Postal_Address', N'U') IS NOT NULL 
	drop table dbo.Non_Billable_Customers_Postal_Address
go

select distinct	[CUSTOMERLEGALENTITYID], [CUSTOMERACCOUNTNUMBER], [ADDRESSDESCRIPTION], [ADDRESSLOCATIONROLES], [ADDRESSCOUNTRYREGIONID], 
		[ADDRESSZIPCODE], [ADDRESSSTREET], [ADDRESSCITY], [ADDRESSSTATE], [IsPostalAddress], [ISPRIMARY], [ASCUSTACCOUNTNUM], 
		convert(uniqueidentifier,replace(replace(TRIM([GUID]), '{', ''), '}', '')) as [GUID], [DATAAREAID]
into dbo.Non_Billable_Customers_Postal_Address
from [dbo].[ZZ_Non_Billable_Customers_Postal_Address]


IF OBJECT_ID(N'dbo.Products_Released', N'U') IS NOT NULL 
	drop table dbo.Products_Released
go

select distinct [ITEMNUMBER], [SEARCHNAME], [SEARCHNAME] AS [PRODUCTNAME], [ITEMMODELGROUPID], [PRODUCTGROUPID], [INVENTORYUNITSYMBOL], [PURCHASEUNITSYMBOL], 
		[SALESUNITSYMBOL], [BOMUNITSYMBOL], [PURCHASESALESTAXITEMGROUPCODE], [SALESSALESTAXITEMGROUPCODE], [PRODUCTCOVERAGEGROUPID], 
		[PACKINGMATERIALGROUPID], [NETPRODUCTWEIGHT], [PROJECTCATEGORYID], [ISPURCHASEPRICEAUTOMATICALLYUPDATED], [PURCHASEPRICE], 
		[BATCHNUMBERGROUPCODE], [PRODUCTIONTYPE], [YIELDPERCENTAGE], [SALESSUPPLEMENTARYPRODUCTPRODUCTGROUPID], [DEFAULTLEDGERDIMENSIONDISPLAYVALUE],
		[ProductNumber], convert(uniqueidentifier,replace(replace(TRIM([GUID]), '{', ''), '}', '')) as [GUID], [DATAAREAID]
into  dbo.Products_Released
from  dbo.ZZ_Products_Released

IF OBJECT_ID(N'dbo.Product_Sales_Categories', N'U') IS NOT NULL 
	drop table dbo.Product_Sales_Categories
go

select [GUID] Id, [Sales categories] as CategoryName, [RecID]
into  dbo.Product_Sales_Categories
from[dbo].[ZZ_Product_Sales_Categories]


IF OBJECT_ID(N'dbo.Product_Category_Assignments', N'U') IS NOT NULL 
	drop table dbo.Product_Category_Assignments
go

select a.[GUID] Id, a.[RECID], c.Id as CategoryId --, p.[GUID] as ProductId
, convert(nvarchar(50), a.[PRODUCTNUMBER]) as ProductNumber, 
		[PRODUCTCATEGORYNAME], [PRODUCTCATEGORYHIERARCHYNAME] 
into dbo.Product_Category_Assignments
from[dbo].[ZZ_Product_Category_Assignments] a
join  dbo.Product_Sales_Categories c on PRODUCTCATEGORYNAME = CategoryName
join dbo.Products_Released p on convert(nvarchar(50), a.PRODUCTNUMBER) = p.ITEMNUMBER


IF OBJECT_ID(N'dbo.Product_Site_Specific_Order_Settings', N'U') IS NOT NULL 
	drop table dbo.Product_Site_Specific_Order_Settings
go

select distinct p.[GUID] as ProductId, a.[ITEMNUMBER], a.[OPERATIONALSITEID], a.[SALESWAREHOUSEID], 
		a.[ARESALESDEFAULTORDERSETTINGSOVERRIDDEN], a.[ISSALESPROCESSINGSTOPPED], 
	    a.[DataAreaID], a.[RECID]
into dbo.Product_Site_Specific_Order_Settings
from [dbo].[ZZ_Product_Site_Specific_Order_Settings] a
join dbo.Products_Released p on  convert(nvarchar(50), a.ITEMNUMBER) = p.ITEMNUMBER and a.DataAreaID = p.DATAAREAID

IF OBJECT_ID(N'dbo.Prospect_Electronic_Address', N'U') IS NOT NULL 
	drop table dbo.Prospect_Electronic_Address
go

select	[BUSRELACCOUNT], [DESCRIPTION], [ISINSTANTMESSAGE], [ISMOBILEPHONE], [ISPRIMARY], [ISPRIVATE] 
		,case [Type] 			
			when 'Email' then replace(replace(dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '-com', '.com')
			when 'Phone' then REPLACE(REPLACE(	dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '.', '-')
		end
		as LOCATOR, [LOCATOREXTENSION],[PURPOSE], [TYPE], [DATAAREAID], [GUID]
		,case 
			when [Type] = 'Email' and Locator is not null and Trim(Locator) <> '' then dbo.isValidEmailFormat(replace(replace(dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '-com', '.com')) 
			when [Type] = 'Phone' and Locator is not null and Trim(Locator) <> '' then dbo.isValidPhoneFormat(REPLACE(REPLACE(dbo.RemoveHiddenChars([LOCATOR]), ' ', ''), '.', '-')) 
			else null
		 end as IsValidLocator
into dbo.Prospect_Electronic_Address
from dbo.ZZ_Prospect_Electronic_Address

IF OBJECT_ID(N'dbo.Prospect_Postal_Address', N'U') IS NOT NULL 
	drop table dbo.Prospect_Postal_Address
go
select  [GUID], [CUSTOMERLEGALENTITYID], [BUSRELACCOUNT], [ADDRESSDESCRIPTION], [ADDRESSLOCATIONROLES], [ADDRESSCOUNTRYREGIONID], 
		[ADDRESSZIPCODE], [ADDRESSSTREET], [ADDRESSCITY], [ADDRESSSTATE], [IsPostalAddress], [ISPRIMARY], [DATAAREAID]
into dbo.Prospect_Postal_Address
from dbo.ZZ_Prospect_Postal_Address T



IF OBJECT_ID(N'dbo.Prospects', N'U') IS NOT NULL 
	drop table dbo.Prospects
go

select  [NAME], [PARTYNUMBER], [DATAAREAID], [BLOCKED], [TAXGROUP], [ONETIMERELATION], [CREDITMAX], [CREDITRATING], [MANDATORYCREDITLIMIT],
		[DLVTERM], [COMPANYIDSIRET], [DLVMODE], [FREIGHTZONE], [DESTINATIONCODEID], [INCLTAX], [SALESCALENDARID], [FISCALCODE], [PARTY], 
		[DEFAULTDIMENSION], [COMPANYNAFCODE], [MAINCONTACTWORKER], [SALESDISTRICT], [SEGMENT], [STATUS], [BUSRELACCOUNT], [SUBSEGMENT], 
		[DIRECTMAIL], [MEMO], [COMPANYCHAIN], [BUSRELTYPEID], [OPENTOTIME], [OPENFROMTIME], [IMPORTED], [LINEOFBUSINESSID], [CURRENCY], 
		[CUSTGROUP], [VENDGROUP], [RECVERSION], [RECID], [GUID]
into dbo.Prospects
from [dbo].[ZZ_Prospects]

IF OBJECT_ID(N'dbo.SCA', N'U') IS NOT NULL 
	drop table dbo.SCA
go

select newid() [GUID], T.*
into dbo.SCA
from (select  max([RECID]) [RECID], convert(nvarchar(100), iif(trim([AFE] ) = '', null, trim([AFE]))) as [AFE], 
		[APPLICANTSIGPERSONID], [BILLINGCONTACTPERSONID], [BILLINGCUSTACCOUNT], [CLASSIFICATION], [CUSTREPCONTACTPERSONID], 
		[DISPOSALESTIMATEAMOUNT], [DRILLRIGNO], convert(nvarchar(100), iif(trim([EDICODE] ) = '', null, trim([EDICODE]))) as [EDICODE], [EMAILTONNAGEALERT], 
		[EOX], [ERCBNUMBER], [FACILITY], [FACILITYTYPE], [LOADSUMMARYREPORT], [MIGRATIONRECID], [NOTES], [PCB], 
		convert(nvarchar(100), iif(trim([PO] ) = '', null, trim([PO]))) as [PO], 	[SCA], [SECONDSIGPERSONID], [SIGNATORYCONTACTPERSONID], 
		[SIGNATORYTITLE], [SIGNATUREDATE], [SOURCE], [SOURCEREGION], [STATEMENTFREQUENCY], [SUBSTANCERECID], 
		[SULPHUR], iif(trim([TANKNUMBER] ) = '', null, trim([TANKNUMBER])) as [TANKNUMBER], [THIRDPARTYCOMPANY], [THIRDPARTYCONTACT], [TITLE], [TRUCKINGCOMPANY], [UWIRECID], 
		[WELLCLASSIFICATION], 		[CREATEDDATETIME], [CREATEDBY], [DATAAREAID], [RECVERSION], [CUSTPRICEGROUP], [CUSTLINEDISCCODE], 
		[INACTIVE], [PARTITION], [USEAUTOFILLTAREWEIGHT], [SESISFIELDTICKETEDI], [HAULERPERMITNUMBER] 
from  [dbo].[ZZ_SCA]
group by
        convert(nvarchar(100), iif(trim([AFE] ) = '', null, trim([AFE]))),  [APPLICANTSIGPERSONID], [APPROXIMATEDELIVERYDATE], 
		[BILLINGCONTACTPERSONID], [BILLINGCUSTACCOUNT], [CLASSIFICATION], [CUSTREPCONTACTPERSONID], [DISPOSALESTIMATEAMOUNT], 
		[DRILLRIGNO], convert(nvarchar(100), iif(trim([EDICODE] ) = '', null, trim([EDICODE]))), 
		[EMAILTONNAGEALERT], [EOX], [ERCBNUMBER], [FACILITY], [FACILITYTYPE], [LOADSUMMARYREPORT], 
		[MIGRATIONRECID], [NOTES], [PCB], convert(nvarchar(100), iif(trim([PO] ) = '', null, trim([PO]))), 
		[SCA], [SECONDSIGPERSONID], [SIGNATORYCONTACTPERSONID], [SIGNATORYTITLE], 
		[SIGNATUREDATE], [SOURCE], [SOURCEREGION], [STATEMENTFREQUENCY], [SUBSTANCERECID], [SULPHUR], 
		iif(trim([TANKNUMBER] ) = '', null, trim([TANKNUMBER])), 
		[THIRDPARTYCOMPANY], [THIRDPARTYCONTACT], [TITLE], [TRUCKINGCOMPANY], [UWIRECID], [WELLCLASSIFICATION], 
		[CREATEDDATETIME], [CREATEDBY], [DATAAREAID], [RECVERSION], [CUSTPRICEGROUP], [CUSTLINEDISCCODE], 
		[INACTIVE], [PARTITION], [USEAUTOFILLTAREWEIGHT], 
		[SESISFIELDTICKETEDI], [HAULERPERMITNUMBER]) T


IF OBJECT_ID(N'dbo.Sites', N'U') IS NOT NULL 
	drop table dbo.Sites
go



select distinct	[SITEID], [NAME], [DEFAULTDIMENSION], [TIMEZONE], [ORDERENTRYDEADLINEGROUPID], [DATAAREAID], [RECVERSION], [RECID], 
		[TAXBRANCHREFRECID], [DEFAULTINVENTSTATUSID], [ISRECEIVINGWAREHOUSEOVERRIDEALLOWED], [ADMINEMAIL], [CALENDARID], 
		[FACILITYREGULATORYCODEPIPELINE], [FACILITYREGULATORYCODETERMINALLING], [FACILITYREGULATORYCODETREATING], [FACILITYREGULATORYCODEWASTE], 
		[FACILITYREGULATORYCODEWATER], [HC_STATUS], [INVOICECONTACT], [OVERTONNAGEANALYTICALCONTACT], [PRODUCTIONACCOUNTANTCONTACT], 
		[SCAFORMREPORTDESIGN], [SCALEOPERATORDAILYREPORTDESIGN], [SCALETICKETREPORTDESIGN], [SESFACILITYTYPE], [SESISPIPELINED], 
		[SESISRAIL], [TAXGROUP], [UWI], [PARTITION], [SESTAREWEIGHTSSIGNREQUIRED], [CITY], [PRIMARYADDRESSCOUNTRYREGIONID], 
		[PRIMARYADDRESSDESCRIPTION], [PRIMARYADDRESSSTATEID], [PRIMARYADDRESSSTREET], [PRIMARYADDRESSZIPCODE], convert(uniqueidentifier,replace(replace(TRIM([GUID]), '{', ''), '}', '')) as [GUID]
into dbo.Sites
from dbo.ZZ_Sites


IF OBJECT_ID(N'dbo.TradeAgreement', N'U') IS NOT NULL 
	drop table dbo.TradeAgreement
go


select distinct	[SourceLocation], [SalesQuoteType], [HSCustAccount], [TRADEAGREEMENTJOURNALNUMBER], [LINENUMBER], [CUSTOMERACCOUNTNUMBER], [FIXEDPRICECHARGES], [FROMQUANTITY], [ITEMNUMBER], [PRICE], 
		[PRICEAPPLICABLEFROMDATE], [PRICECURRENCYCODE], [PRICECUSTOMERGROUPCODE], [PRICESITEID], [PRICEWAREHOUSEID], [PRODUCTCOLORID], 
		[PRODUCTCONFIGURATIONID], [PRODUCTSIZEID], [PRODUCTSTYLEID], [QUANTITYUNITSYMBOL], [SALESPRICEQUANTITY], [TOQUANTITY], 
		[WILLDELIVERYDATECONTROLDISREGARDLEADTIME], convert(uniqueidentifier,replace(replace(TRIM([GUID]), '{', ''), '}', '')) as [GUID], [DATAAREAID], [Price Type], [Account Relation], [Account Type]
into dbo.TradeAgreement
from dbo.ZZ_TradeAgreement
 

IF OBJECT_ID(N'dbo.UWI', N'U') IS NOT NULL 
	drop table dbo.UWI
go

select * 
into dbo.UWI
from [dbo].[ZZ_UWI]



IF OBJECT_ID(N'dbo.ProductVariants', N'U') IS NOT NULL 
	drop table dbo.ProductVariants
go

select distinct [ITEMNUMBER], [DATAAREAID], [Substance], [WasteCode], convert(uniqueidentifier,replace(replace(TRIM([PRODUCTGUID]), '{', ''), '}', '')) as [GUID] 
into dbo.ProductVariants
from dbo.ZZ_ProductVariants


IF OBJECT_ID(N'dbo.SpartanProductParameters', N'U') IS NOT NULL 
	drop table dbo.SpartanProductParameters
go

select p.[GUID] as Id, [FLUIDDENSITYMAX], [FLUIDDENSITYMIN], [FLUIDIDENTITY], [LCTNOPERSTATUS], [PRDTNAME], [PRDTNAMEFULL],
				[SHOWDENSITY], [WATERPERCENTMAX], [WATERPERCENTMIN], [RECVERSION], [RECID], [PARTITION], 
				e.DataAreaId, e.GUID as LegalEntityId
into dbo.SpartanProductParameters 
from dbo.ZZ_SpartanProductParameters P
JOIN [dbo].ZZ_LegalEntity e on e.DataAreaId = P.DataAreaId 




IF OBJECT_ID(N'dbo.TankTypes', N'U') IS NOT NULL 
	drop table dbo.TankTypes
go

select distinct [CUTTYPE], [DESCRIPTION], [DONOTSHOWONINVOICE], [OILITEMID], [OILITEMREVERSE], [SOLIDITEMID], [SOLIDITEMREVERSE], 
		[TANKTYPEID], [TOTALITEMID], [TOTALITEMREVERSE], [VOLUMEMINMAX], [VOLUMEMINMAXOIL], 
		[VOLUMEMINMAXPERCENT], [VOLUMEMINMAXSOLIDS], [VOLUMEMINMAXWATER], [WATERITEMID], [WATERITEMREVERSE], 
		[DATAAREAID], [RECVERSION], [RECID], [CLASS], [FACILITYCODETYPE], [OILCREDITMINVOLPAYOUT], 
		[PRORATETANK], [PRODACCOUNTANTREPORT], [PARTITION], [OILSHOWZEROLINE], [SOLIDSHOWZEROLINE], [TOTALSHOWZEROLINE], 
		[WATERSHOWZEROLINE], [GUID]
into dbo.TankTypes
from dbo.ZZ_TankTypes


IF OBJECT_ID(N'dbo.FacilityServiceSpartanParameters', N'U') IS NOT NULL 
	drop table dbo.FacilityServiceSpartanParameters
go

select convert(uniqueidentifier, T.[GUID]) as Id, T.[RECID], T.[FACILITYTANKRECID], T.[ISACTIVE], T.[MAPPED], 
	T.[PRDTNAMEFULL], T.[PRODUCTPARAMETERRECID], T.[PARTITION] 
into dbo.FacilityServiceSpartanParameters
from dbo.ZZ_FacilityServiceSpartanParameters T


IF OBJECT_ID(N'dbo.FacilityMappings', N'U') IS NOT NULL 
	drop table dbo.FacilityMappings
go


select convert(uniqueidentifier, f.[GUID]) as [GUID], f.[FACILITY], f.[FCTNAME], f.[REFCOMPANY], 
	f.[SPARTANACTIVE], f.[OPERATIONTIME], f.[RECVERSION], f.[RECID], f.[PARTITION]
into dbo.FacilityMappings
from dbo.ZZ_FacilityMappings f

IF OBJECT_ID(N'dbo.FacilityTank', N'U') IS NOT NULL 
drop table dbo.FacilityTank
go

select distinct [DESCRIPTION], [FACILITY], [FACILITYTANKNUM], [TANKNUM], [TANKTYPEID], [DATAAREAID],
		[RECVERSION], [RECID], [PARTITION], [GUID]
into dbo.FacilityTank
from dbo.ZZ_FacilityTank


IF OBJECT_ID(N'dbo.LegalEntity', N'U') IS NOT NULL 
drop table dbo.LegalEntity
go

select distinct [DataAreaId], [GUID],
			[LegalEntityName], [BusinessStream], [Country], convert(int, 365) as CreditExpiryThreshold, Division		     
into dbo.LegalEntity
from dbo.ZZ_LegalEntity;