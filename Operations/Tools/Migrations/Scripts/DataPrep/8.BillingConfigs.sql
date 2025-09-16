
IF OBJECT_ID(N'dbo.EdiFieldDefinitions', N'U') IS NOT NULL  
   DROP TABLE dbo.EdiFieldDefinitions;  
GO

IF OBJECT_ID(N'dbo.InvoiceConfigs', N'U') IS NOT NULL  
   DROP TABLE dbo.InvoiceConfigs;  
GO

IF OBJECT_ID(N'dbo.BillingConfigsRaw', N'U') IS NOT NULL  
   DROP TABLE dbo.BillingConfigsRaw;  
GO

IF OBJECT_ID(N'dbo.BillingConfigs', N'U') IS NOT NULL  
   DROP TABLE dbo.BillingConfigs;  
GO

IF OBJECT_ID(N'dbo.BillingConfigSourceLocations', N'U') IS NOT NULL  
   DROP TABLE dbo.BillingConfigSourceLocations;  
GO

IF OBJECT_ID(N'dbo.BillingConfigMatchCriteria', N'U') IS NOT NULL  
   DROP TABLE dbo.BillingConfigMatchCriteria;  
GO

IF OBJECT_ID(N'dbo.BillingConfigFacilities', N'U') IS NOT NULL  
   DROP TABLE dbo.BillingConfigFacilities;  
GO

IF OBJECT_ID(N'dbo.BillingConfigSCAs', N'U') IS NOT NULL  
   DROP TABLE dbo.BillingConfigSCAs;  
GO

IF OBJECT_ID(N'dbo.BillingConfigEdi', N'U') IS NOT NULL  
   DROP TABLE dbo.BillingConfigEdi;  
GO

IF OBJECT_ID(N'dbo.BillingConfigSigners', N'U') IS NOT NULL  
   DROP TABLE dbo.BillingConfigSigners;  
GO

IF OBJECT_ID(N'dbo.ContactFunctions', N'U') IS NOT NULL  
   DROP TABLE dbo.ContactFunctions;  
GO

CREATE TABLE [dbo].[EdiFieldDefinitions](
	[Id] [uniqueidentifier] NULL,
	[EdiField] [nvarchar](128) NULL
) ON [PRIMARY]

GO

--THESE KEYS MUCH MATCH TT EDI DEFINITION IDS THAT ARE SEEDED INTO THE DB
  INSERT INTO dbo.EdiFieldDefinitions([EdiField], Id)
  VALUES('AccountQty', 'b3b14df2-d415-40ae-a8d6-0dcfe23d0d41'),
  ('AFE', '968b07b4-3cdd-4d91-891c-c7112ce90dfc'),
  ('AFEOwner', 'fcbfc899-b2e9-45f5-b648-62072bd0a0be'),
  ('Alpha', 'cc76eba5-60a3-44d9-91cf-cdac230e6d7b'),
  ('ApproverCoding', '3fc04ffe-1512-44bc-abac-e1a65111e151'),
  ('AssetNumber', '25e3c093-286d-4f4e-90f3-369ee6fd7fbd'),
  ('AttnTo', '0b43a12a-a84e-4cde-b105-0c21b1a88e95'),
  ('AttnToName', 'a426f156-d5a5-4571-83b5-a7eb2b70a176'),
  ('BuyerCode', '1a1bae70-c465-427d-beaa-8fcf765fde2a'),
  ('BuyerDepartmentDefault', '8301f6b8-eacc-4b6e-9d00-012f1b714dd5'),
  ('CatalogSerialNo', 'f9e40e2d-1c6b-4338-8e74-968748912060'),
  ('CCType', 'de56ef74-8ed3-42f9-b656-c76e35fe6021'),
  ('ChargeTo', '8aa37e7a-71a4-4cb5-9735-6e14cd9f855a'),
  ('ChargeToName', '9e089905-0cf0-4850-b830-22ac212e2fca'),
  ('Company', 'a464f3de-3858-4873-b589-ba5c81b94805'),
  ('CompanyRep', 'fc0a9d2d-aad1-42f8-8681-5f7b4ca0ba17'),
  ('Contract', '719bd4b0-ba48-4073-ba8f-364906b765a7'),
  ('ContractLineNumber', '9b566674-f02d-4abc-923d-8f3654797218'),
  ('CostCenter', 'ef8d2fdc-12c8-432b-a152-dc7a27ccf0d8'),
  ('CostCenterName', '535ab2f6-6873-40b7-ab3b-6fcb2d2ec8c9'),
  ('Zip4', '86da08f1-6d14-40da-a909-9b93ee76dd19'),
  ('FieldLocation', 'bb431a7d-0945-412b-ad26-746d85ab0a25'),
  ('GLAccount', 'afb5cf9b-d841-477d-a460-0a50ec20a937'),
  ('Joints', '0879eb6b-2192-438a-9e03-7a8fa3a3b4f0'),
  ('Location', '51518489-4dc7-49d7-90d7-516f26c69337'),
  ('LocationComments', '2c73da14-cb1a-4757-98ff-b4475d40d18d'),
  ('MajorCode', '4a160984-d8bb-4769-85ac-6d7de79ac0a9'),
  ('MaterialCode', 'c820a7b5-a248-4cf0-bb11-beb5d57a18fd'),
  ('MatGrp', '4020aa19-26f8-460d-8cac-aedc6c70f637'),
  ('MGDesc', '116abc6a-12e2-4a1b-8153-9a6fc6ad0688'),
  ('MinorCode', '5c45dc1f-430d-4b20-80b9-b62a1398b7dd'),
  ('NetworkActivity', 'ba0963ec-b25f-487d-800f-830f42356f79'),
  ('OBJSubna', 'f3957ded-169f-4b4a-9b5e-da63253d862f'),
  ('OperationCat', 'e4f6a92f-41cc-49e0-88c9-2b21beddd591'),
  ('OperatorCoding', '5fed3bd3-b662-493d-9378-6fc8e25a3247'),
  ('OPNumber', '908a99c0-90b6-4a56-8da9-5948f34e5490'),
  ('OrderNumber', 'cca80f34-197c-4be3-86d8-119132392833'),
  ('POApprover', '3cb3b51d-5774-4c00-95bf-ad49ac307d54'),
  ('POLine', '9ee4c60b-1677-4a90-8738-8e66bd90bdd3'),
  ('PONumber', '1df9a6ce-02bc-42fb-a93d-2ab2cdbfb8ea'),
  ('ProfitCentre', '41d286cb-a795-4e36-83ba-c95fc6bb118c'),
  ('Project', 'a1226336-7fde-4e97-bf9b-b067639d7668'),
  ('ProjectPONo', '00f803f4-dd07-423c-b5b2-cd6281b2d605'),
  ('ProjId', 'fa78ecc2-c2dc-41ea-86ba-74557329fc8a'),
  ('ReportCentre', 'f812b23e-85ef-4f01-ad71-2e7b77ad8cb3'),
  ('Requisitioner', '082d53d8-ce62-434d-9a96-28f51ab7ff75'),
  ('SerialNumber', '5a87a25f-9927-4d27-9670-800350b3bd1b'),
  ('Signatory', '72863c47-d62c-4b26-93d6-75e400553c54'),
  ('SOLine', '058813c4-6d19-4262-bf39-74ee6f3227d8'),
  ('SONumber', 'b8fcf7d2-6738-4fae-9c10-fcbdbba027f1'),
  ('StkCond', 'd34771b9-7680-4320-b5ca-a0e0d3b7f8ce'),
  ('Sub', 'd6fdd1b3-1510-4656-a03d-d05616f619d2'),
  ('SubCode', 'f232bbba-474d-4ff5-926f-be8b45d6b1b1'),
  ('SubFeature', 'b376a7a7-e35b-4772-977d-6f9ecad5497e'),
  ('WBS', '5ed91ed7-222a-4400-96ac-84f932f6e161'),
  ('WellFacility', 'b913bfae-bd48-4c9f-ba7b-0ac43894567b'),
  ('WONumber', 'a93106fa-b20d-4fbc-ad59-81ec7a6033e5'),
  ('WorkOrderBy', '75473423-a88d-4f6d-a84e-2ede027fe9e5');

 GO

;WITH InvoiceConfigsCte
as
(
	select
		newid() as Id
		, 1 as AllFacilities
		, 1 as AllServiceTypes
		, 1 as AllSourceLocations
		, 1 as AllSubstances
		, 1 as AllWellClassifications
		, null as BusinessUnitId
		, 1 as CatchAll
		, sysdatetimeoffset() as CreatedAt
		, 'System' as CreatedBy
		, '00000000-0000-0000--0000-000000000000' as CreatedById
		, c.[GUID] as CustomerId
		, c.[DATAAREAID] as CustomerLegalEntityId
		, c.[ORGANIZATIONNAME] as CustomerName
		, 'Default' as [Description]
		, 'InvoiceConfiguration' as DocumentType
		, 'InvoiceConfiguration' as EntityType
		, 1 as IncludeExternalDocumentAttachment
		, 0 as IncludeInternalDocumentAttachment
		, '' as InvoiceNumber
		, 1 as IsSplitByFacility
		, 0 as IsSplitByServiceType
		, 1 as IsSplitBySourceLocation
		, 0 as IsSplitBySubstance
		, 0 as IsSplitByWellClassification
		, '(Default)' as [Name]
		, '' as PermutationsHash
		, sysdatetimeoffset() as UpdatedAt
		, 'System' as UpdatedBy
		, '00000000-0000-0000--0000-000000000000' as UpdatedById
		, null as Facilities
		, null as FacilityCode
		, '' as Permutations
		, null as ServiceTypes
		, null as ServiceTypesName
		, null as SourceLocationIdentifier
		, null as SourceLocations
		, null as SplitEdiFieldDefinitions		
		, null as Substances
		, null as SubstancesName
		, null as WellClassifications
		, c.CUSTOMERACCOUNT
	from dbo.customers c
	where c.DataAreaId in ('SESC' , 'SESU')
)
select *
into dbo.InvoiceConfigs 
from InvoiceConfigsCte
 

;WITH AllCustomers 
as
(
	select [CUSTOMERACCOUNT], [PARTYTYPE], [ORGANIZATIONNAME], [PERSONFIRSTNAME], [PERSONMIDDLENAME], [PERSONLASTNAME], [NAMEALIAS], [CUSTOMERGROUPID], [LANGUAGEID], [LINEOFBUSINESSID], [EMPLOYEERESPONSIBLENUMBER], [SALESDISTRICT], [SALESCURRENCYCODE], [SALESMEMO],  [GUID], [DATAAREAID]
	from [dbo].[Customers]
	union
	select [CUSTOMERACCOUNT], [PARTYTYPE], [ORGANIZATIONNAME], [PERSONFIRSTNAME], [PERSONMIDDLENAME], [PERSONLASTNAME], [NAMEALIAS], [CUSTOMERGROUPID], [LANGUAGEID], [LINEOFBUSINESSID], [EMPLOYEERESPONSIBLENUMBER], [SALESDISTRICT], [SALESCURRENCYCODE], [SALESMEMO],  [GUID], [DATAAREAID]
	from [dbo].[Non_Billable_Customers]
)
,uniqueSCA
as
( 
   --111,325 - for active uwi in last 6 mo
   select distinct s.recid, s.dataareaid, sca
   from DBO.SCA s
   inner join [dbo].[SourceLocations] l on l.RECID = s.UWIRECID
   where inactive = 0 
)
, UniqueSCAWithEDi as (
--111,325 outer join
--105,000 inner join
	select distinct s.*, 
    e.[ACCOUNT], e.[ACCOUNTQTY], e.[ACCTCENTRE], e.[ACTIVITYDATE], e.[ALPHA], e.[ATTNTO], e.[ATTNTONAME], e.[CATALOGSERIALNO], e.[CCTYPE],
	e.[CHARGETO], e.[CHARGETONAME], e.[COMPANY], e.[CONTRACT], e.[CONTRACTLINENUMBER], e.[FIELDLOCATION], e.[GLACCOUNT], e.[JOINTS], 
	e.[LINEITEMDESCRIPTION], e.[LOCATION], e.[LOCATIONCOMMENTS], e.[MATERIALCODE], e.[MATGRP], e.[MGDESC], e.[NETWORKACTIVITY], e.[OPNUMBER], 
	e.[ORDERNUMBER], e.[PROFITCENTER], e.[PROJECT], e.[PROJID], e.[REPORTCENTRE], e.[SERIALNUMBER], e.[SERVICEORDERID], e.[SESAFE], e.[SESAFEOWNER], 
	e.[SESAPPROVERCODING], e.[SESASSETNUMBER], e.[SESBUYERCODE], e.[SESCOMPANYREP], e.[SESCOSTCENTER], e.[SESCOSTCENTERNAME], e.[SESMAJORCODE], e.[SESMINORCODE], 
	e.[SESOBJSUBNA], e.[SESOPERATIONCAT], e.[SESOPERATORCODING], e.[SESPOAPPROVER], e.[SESPOLINE], e.[SESPONUMBER], e.[SESPROJECTPONO], e.[SESREQUISITIONER], 
	e.[SESSIGNATORY], e.[SESSONUMBER], e.[SESSUBCODE], e.[SESWBS], e.[SESWONUMBER], e.[SOLINE], e.[STKCOND], e.[SUB], e.[SUBFEATURE], e.[WELLFACILITY], 
	e.[WORKORDEREDBY], e.[SESBUYERSDEPARTMENTDEFAULT], e.[SESEDIZIP4], e.[RECID] EDI_RECID, e.[SALESID], 
	e.[SALESLINEREFRECID], e.[SESEDIFIELDTICKET]
	from uniqueSCA u 
	inner join dbo.sca s on s.sca = u.sca and s.dataareaid = u.dataareaid and s.recid = u.recid
	inner join [dbo].[EDI_HSEDICustomerConfigEntity_SCA] e on s.sca = e.sca and s.dataareaid = e.dataareaid
)
,FlatSCA
as
(
 --missing customers and uwis reduced to 108,038 missing customers expecting 111,325
	select distinct s.*, 
	--customer
	c.[GUID] as CustomerId, c.[OrganizationName] CustomerAccountName, 
	
	--Generator
	l.[OWNER] as GeneratorOwnerNumber, g.[GUID] as GeneratorId, g.[OrganizationName] GeneratorAccountName, l.Id as SourceLocationId,
	
	tt.FACILITYCODETYPE, tt.TANKTYPEID

	from UniqueSCAWithEDi s
		inner join AllCustomers c on s.[BILLINGCUSTACCOUNT] = c.[CustomerAccount] and s.dataareaid = c.dataareaid
		inner join dbo.sourcelocations l on s.uwirecid = l.recid and s.dataareaid = l.dataareaid
		left outer join AllCustomers g on l.[Owner] = g.[CustomerAccount] and s.dataareaid = g.dataareaid
		left outer join dbo.FacilityTank ft on s.[TANKNUMBER] = ft.[FacilityTANKNUM] and ft.[DATAAREAID] = s.[DATAAREAID]
		left outer join dbo.TankTypes tt on ft.[TANKTYPEID] = tt.[TANKTYPEID] and ft.[DATAAREAID] = tt.[DATAAREAID]
)
, BillingConfigEdiSetsTemp
as
(
	select 
		--customer
		s.[BILLINGCONTACTPERSONID], s.[BILLINGCUSTACCOUNT], s.[DATAAREAID], 
	
		--features
		s.[SESISFIELDTICKETEDI], [STATEMENTFREQUENCY],
	
		--signers
		s.[SIGNATORYCONTACTPERSONID] ,  s.[SECONDSIGPERSONID], 			
		     
		--EXTENED EDI FIELDS
		s.[AFE], s.[EDICODE], s.[PO],  
		[ACCOUNT], [ACCOUNTQTY], [ACCTCENTRE], [ALPHA], [ATTNTO], [ATTNTONAME], [CATALOGSERIALNO], [CCTYPE], [CHARGETO], 
		[CHARGETONAME], [COMPANY], [CONTRACT], [CONTRACTLINENUMBER], [FIELDLOCATION], [GLACCOUNT], [JOINTS], [LINEITEMDESCRIPTION], 
		[LOCATION], [LOCATIONCOMMENTS], [MATERIALCODE], [MATGRP], [MGDESC], [NETWORKACTIVITY], [OPNUMBER], [ORDERNUMBER], 
		[PROFITCENTER], [PROJECT], [PROJID], [REPORTCENTRE], [SERIALNUMBER], [SERVICEORDERID], [SESAFE], [SESAFEOWNER], 
		[SESAPPROVERCODING], [SESASSETNUMBER], [SESBUYERCODE], [SESCOMPANYREP], [SESCOSTCENTER], [SESCOSTCENTERNAME], [SESMAJORCODE], 
		[SESMINORCODE], [SESOBJSUBNA], [SESOPERATIONCAT], [SESOPERATORCODING], [SESPOAPPROVER], [SESPOLINE], [SESPONUMBER], 
		[SESPROJECTPONO], [SESREQUISITIONER], [SESSIGNATORY], [SESSONUMBER], [SESSUBCODE], [SESWBS], [SESWONUMBER], [SOLINE], 
		[STKCOND], [SUB], [SUBFEATURE], [WELLFACILITY], [WORKORDEREDBY], [SESBUYERSDEPARTMENTDEFAULT], [SESEDIZIP4], [SESEDIFIELDTICKET]
			
	from FlatSCA s
		left outer join dbo.FacilityTank ft on s.[TANKNUMBER] = ft.[FacilityTANKNUM] and ft.[DATAAREAID] = s.[DATAAREAID]
		left outer join dbo.TankTypes tt on ft.[TANKTYPEID] = tt.[TANKTYPEID] and ft.[DATAAREAID] = tt.[DATAAREAID]

	where inactive = 0 and [BILLINGCUSTACCOUNT] is not null and trim([BILLINGCUSTACCOUNT]) <> ''
	group by  
		-- customer
		s.[BILLINGCONTACTPERSONID], s.[BILLINGCUSTACCOUNT],  s.[DATAAREAID],  
	
		--features
		s.[SESISFIELDTICKETEDI], [STATEMENTFREQUENCY],
			  
		--signers
		s.[SIGNATORYCONTACTPERSONID] ,  s.[SECONDSIGPERSONID],

		--edi fields
		s.[AFE], s.[EDICODE], s.[PO],	          
		[ACCOUNT], [ACCOUNTQTY], [ACCTCENTRE], [ALPHA], [ATTNTO], [ATTNTONAME], [CATALOGSERIALNO], [CCTYPE], [CHARGETO], 
		[CHARGETONAME], [COMPANY], [CONTRACT], [CONTRACTLINENUMBER], [FIELDLOCATION], [GLACCOUNT], [JOINTS], [LINEITEMDESCRIPTION], 
		[LOCATION], [LOCATIONCOMMENTS], [MATERIALCODE], [MATGRP], [MGDESC], [NETWORKACTIVITY], [OPNUMBER], [ORDERNUMBER], 
		[PROFITCENTER], [PROJECT], [PROJID], [REPORTCENTRE], [SERIALNUMBER], [SERVICEORDERID], [SESAFE], [SESAFEOWNER], 
		[SESAPPROVERCODING], [SESASSETNUMBER], [SESBUYERCODE], [SESCOMPANYREP], [SESCOSTCENTER], [SESCOSTCENTERNAME], [SESMAJORCODE], 
		[SESMINORCODE], [SESOBJSUBNA], [SESOPERATIONCAT], [SESOPERATORCODING], [SESPOAPPROVER], [SESPOLINE], [SESPONUMBER], 
		[SESPROJECTPONO], [SESREQUISITIONER], [SESSIGNATORY], [SESSONUMBER], [SESSUBCODE], [SESWBS], [SESWONUMBER], [SOLINE], 
		[STKCOND], [SUB], [SUBFEATURE], [WELLFACILITY], [WORKORDEREDBY], [SESBUYERSDEPARTMENTDEFAULT], [SESEDIZIP4], [SESEDIFIELDTICKET]
)
, BillingConfigEdiSets
as
(
	select newid() BCGroupID, b.*
	from BillingConfigEdiSetsTemp b
)
, final
as
(
	select distinct 
	--Top level Geneneral info
	s.[GUID] ScaId, d.BCGroupId, s.[RECID] SCA_RECID, s.[SCA], s.[DATAAREAID], s.[INACTIVE], s.[MIGRATIONRECID], s.[TITLE], s.notes,

	--customer info
    s.[BILLINGCONTACTPERSONID], s.[BILLINGCUSTACCOUNT], s.CustomerId, s.CustomerAccountName,
	
	
	--Generator
	s.GeneratorOwnerNumber, s.GeneratorId, s.GeneratorAccountName, 

	--Source Location
	s.[UWIRECID], s.SourceLocationId,

	--Facility
    s.[FACILITY], s.[FACILITYTYPE],
	
	--Match Criteria
    s.[TANKNUMBER], s.[WELLCLASSIFICATION], s.[FACILITYCODETYPE], iif(s.[FACILITYCODETYPE] is null, null, s.[SUBSTANCERECID]) as [SUBSTANCERECID], s.TankTypeID,

	--Signers
    s.[SIGNATORYCONTACTPERSONID] as PrimarySignerContactPersonId,  s.[SECONDSIGPERSONID] as SecondarySignerContactPersonId, 
	
	--Features 
	s.[SESISFIELDTICKETEDI], s.[STATEMENTFREQUENCY], 
	  
	--EDI Columns
	s.[AFE], s.[EDICODE], s.[PO],
	s.[ACCOUNT], s.[ACCOUNTQTY], s.[ACCTCENTRE], s.[ACTIVITYDATE], s.[ALPHA], s.[ATTNTO], s.[ATTNTONAME], s.[CATALOGSERIALNO], s.[CCTYPE], s.[CHARGETO], 
	s.[CHARGETONAME], s.[COMPANY], s.[CONTRACT], s.[CONTRACTLINENUMBER], s.[FIELDLOCATION], s.[GLACCOUNT], s.[JOINTS], s.[LINEITEMDESCRIPTION], s.[LOCATION], 
	s.[LOCATIONCOMMENTS], s.[MATERIALCODE], s.[MATGRP], s.[MGDESC], s.[NETWORKACTIVITY], s.[OPNUMBER], s.[ORDERNUMBER], s.[PROFITCENTER], s.[PROJECT], s.[PROJID], 
	s.[REPORTCENTRE], s.[SERIALNUMBER], s.[SERVICEORDERID], s.[SESAFE], s.[SESAFEOWNER], s.[SESAPPROVERCODING], s.[SESASSETNUMBER], s.[SESBUYERCODE], 
	s.[SESCOMPANYREP], s.[SESCOSTCENTER], s.[SESCOSTCENTERNAME], s.[SESMAJORCODE], s.[SESMINORCODE], s.[SESOBJSUBNA], s.[SESOPERATIONCAT], s.[SESOPERATORCODING], 
	s.[SESPOAPPROVER], s.[SESPOLINE], s.[SESPONUMBER], s.[SESPROJECTPONO], s.[SESREQUISITIONER], s.[SESSIGNATORY], s.[SESSONUMBER], s.[SESSUBCODE], s.[SESWBS], s.[SESWONUMBER], 
	s.[SOLINE], s.[STKCOND], s.[SUB], s.[SUBFEATURE], s.[WELLFACILITY], s.[WORKORDEREDBY], s.[SESBUYERSDEPARTMENTDEFAULT], s.[SESEDIZIP4], 
	s.[RECID] EdiRecId, s.[SALESID], s.[SALESLINEREFRECID], s.[SESEDIFIELDTICKET]

	FROM FlatSCA s		
		left outer join BillingConfigEdiSets d 	on
			d.[BILLINGCUSTACCOUNT] = s.[BILLINGCUSTACCOUNT] 
			and d.[BILLINGCONTACTPERSONID] = s.[BILLINGCONTACTPERSONID]
			and d.[STATEMENTFREQUENCY] = s.[STATEMENTFREQUENCY]
			and d.[DATAAREAID] = s.[DATAAREAID]
			and (d.[AFE] = s.[AFE] or (d.[AFE] IS NULL AND s.[AFE] IS NULL))
			and (d.[EDICODE] = s.[EDICODE]  or (d.[EDICODE] IS NULL AND s.[EDICODE] IS NULL))
			and (d.[PO] = s.[PO]  or (d.[PO] IS NULL AND s.[PO] IS NULL))
			and (d.[Account] = s.[Account] or (d.[Account] IS NULL AND s.[Account] IS NULL))
			and (d.[ATTNTO] = s.[ATTNTO] or (d.[ATTNTO] IS NULL AND s.[ATTNTO] IS NULL))
			and (d.[CATALOGSERIALNO] = s.[CATALOGSERIALNO] or (d.[CATALOGSERIALNO] IS NULL AND s.[CATALOGSERIALNO] IS NULL))
			and (d.[CCTYPE] = s.[CCTYPE] or (d.[CCTYPE] IS NULL AND s.[CCTYPE] IS NULL))
			and (d.[COMPANY] = s.[COMPANY] or (d.[COMPANY] IS NULL AND s.[COMPANY] IS NULL))
			and (d.[CONTRACT] = s.[CONTRACT] or (d.[CONTRACT] IS NULL AND s.[CONTRACT] IS NULL))
			and (d.[FIELDLOCATION] = s.[FIELDLOCATION] or (d.[FIELDLOCATION] IS NULL AND s.[FIELDLOCATION] IS NULL))
			and (d.[GLACCOUNT] = s.[GLACCOUNT] or (d.[GLACCOUNT] IS NULL AND s.[GLACCOUNT] IS NULL))
			and (d.[SESAPPROVERCODING] = s.[SESAPPROVERCODING] or (d.[SESAPPROVERCODING] IS NULL AND s.[SESAPPROVERCODING] IS NULL))
			and (d.[SESCOSTCENTER] = s.[SESCOSTCENTER] or (d.[SESCOSTCENTER] IS NULL AND s.[SESCOSTCENTER] IS NULL))
			and (d.[SESMAJORCODE] = s.[SESMAJORCODE] or (d.[SESMAJORCODE] IS NULL AND s.[SESMAJORCODE] IS NULL))
			and (d.[SESMINORCODE] = s.[SESMINORCODE] or (d.[SESMINORCODE] IS NULL AND s.[SESMINORCODE] IS NULL))
			and (d.[SESOBJSUBNA] = s.[SESOBJSUBNA] or (d.[SESOBJSUBNA] IS NULL AND s.[SESOBJSUBNA] IS NULL))
			and (d.[SESPOLINE] = s.[SESPOLINE] or (d.[SESPOLINE] IS NULL AND s.[SESPOLINE] IS NULL))
			and (d.[SESREQUISITIONER] = s.[SESREQUISITIONER] or (d.[SESREQUISITIONER] IS NULL AND s.[SESREQUISITIONER] IS NULL))
			and (d.[SESSIGNATORY] = s.[SESSIGNATORY] or (d.[SESSIGNATORY] IS NULL AND s.[SESSIGNATORY] IS NULL))
			and (d.[SESWONUMBER] = s.[SESWONUMBER] or (d.[SESWONUMBER] IS NULL AND s.[SESWONUMBER] IS NULL))
			and (d.[SUB] = s.[SUB] or (d.[SUB] IS NULL AND s.[SUB] IS NULL))
			and (d.[SESBUYERSDEPARTMENTDEFAULT] = s.[SESBUYERSDEPARTMENTDEFAULT] or (d.[SESBUYERSDEPARTMENTDEFAULT] IS NULL AND s.[SESBUYERSDEPARTMENTDEFAULT] IS NULL))
			and (d.[ACCOUNTQTY] = s.[ACCOUNTQTY] or (d.[ACCOUNTQTY] IS NULL AND s.[ACCOUNTQTY] IS NULL))
			and (d.[ALPHA] = s.[ALPHA] or (d.[ALPHA] is null and s.[ALPHA] is null))
			and (d.[ATTNTONAME] = s.[ATTNTONAME] or (d.[ATTNTONAME] is null and s.[ATTNTONAME] is null) )
			and (d.[CHARGETO] = s.[CHARGETO] or (d.[CHARGETO] is null and s.[CHARGETO] is null) )
			and (d.[CHARGETONAME] = s.[CHARGETONAME] or (d.[CHARGETONAME] is null and s.[CHARGETONAME] is null) )
			and (d.[CONTRACTLINENUMBER] = s.[CONTRACTLINENUMBER] or (d.[CONTRACTLINENUMBER] is null and s.[CONTRACTLINENUMBER] is null) )
			and (d.[JOINTS] = s.[JOINTS] or (d.[JOINTS] is null and s.[JOINTS] is null) )
			and (d.[LINEITEMDESCRIPTION] = s.[LINEITEMDESCRIPTION] or (d.[LINEITEMDESCRIPTION] is null and s.[LINEITEMDESCRIPTION] is null) )
			and (d.[LOCATION] = s.[LOCATION] or (d.[LOCATION] is null and s.[LOCATION] is null) )
			and (d.[LOCATIONCOMMENTS] = s.[LOCATIONCOMMENTS] or (d.[LOCATIONCOMMENTS] is null and s.[LOCATIONCOMMENTS] is null) )
			and (d.[MATERIALCODE] = s.[MATERIALCODE] or (d.[MATERIALCODE] is null and s.[MATERIALCODE] is null) )
			and (d.[MATGRP] = s.[MATGRP] or (d.[MATGRP] is null and s.[MATGRP] is null) )
			and (d.[MGDESC] = s.[MGDESC] or (d.[MGDESC] is null and s.[MGDESC] is null) )
			and (d.[NETWORKACTIVITY] = s.[NETWORKACTIVITY] or (d.[NETWORKACTIVITY] is null and s.[NETWORKACTIVITY] is null) )
			and (d.[OPNUMBER] = s.[OPNUMBER] or (d.[OPNUMBER] is null and s.[OPNUMBER] is null) )
			and (d.[ORDERNUMBER] = s.[ORDERNUMBER] or (d.[ORDERNUMBER] is null and s.[ORDERNUMBER] is null) )
			and (d.[PROFITCENTER] = s.[PROFITCENTER] or (d.[PROFITCENTER] is null and s.[PROFITCENTER] is null) )
			and (d.[PROJECT] = s.[PROJECT] or (d.[PROJECT] is null and s.[PROJECT] is null) )
			and (d.[PROJID] = s.[PROJID] or (d.[PROJID] is null and s.[PROJID] is null) )
			and (d.[REPORTCENTRE] = s.[REPORTCENTRE] or (d.[REPORTCENTRE] is null and s.[REPORTCENTRE] is null) )
			and (d.[SERIALNUMBER] = s.[SERIALNUMBER] or (d.[SERIALNUMBER] is null and s.[SERIALNUMBER] is null) )
			and (d.[SERVICEORDERID] = s.[SERVICEORDERID] or (d.[SERVICEORDERID] is null and s.[SERVICEORDERID] is null) )
			and (d.[SESAFE] = s.[SESAFE] or (d.[SESAFE] is null and s.[SESAFE] is null) )
			and (d.[SESAFEOWNER] = s.[SESAFEOWNER] or (d.[SESAFEOWNER] is null and s.[SESAFEOWNER] is null) )
			and (d.[SESASSETNUMBER] = s.[SESASSETNUMBER] or (d.[SESASSETNUMBER] is null and s.[SESASSETNUMBER] is null) )
			and (d.[SESBUYERCODE] = s.[SESBUYERCODE] or (d.[SESBUYERCODE] is null and s.[SESBUYERCODE] is null) )
			and (d.[SESCOMPANYREP] = s.[SESCOMPANYREP] or (d.[SESCOMPANYREP] is null and s.[SESCOMPANYREP] is null) )
			and (d.[SESCOSTCENTERNAME] = s.[SESCOSTCENTERNAME] or (d.[SESCOSTCENTERNAME] is null and s.[SESCOSTCENTERNAME] is null) )
			and (d.[SESOPERATIONCAT] = s.[SESOPERATIONCAT] or (d.[SESOPERATIONCAT] is null and s.[SESOPERATIONCAT] is null) )
			and (d.[SESOPERATORCODING] = s.[SESOPERATORCODING] or (d.[SESOPERATORCODING] is null and s.[SESOPERATORCODING] is null) )
			and (d.[SESPOAPPROVER] = s.[SESPOAPPROVER] or (d.[SESPOAPPROVER] is null and s.[SESPOAPPROVER] is null) )
			and (d.[SESPONUMBER] = s.[SESPONUMBER] or (d.[SESPONUMBER] is null and s.[SESPONUMBER] is null) )
			and (d.[SESPROJECTPONO] = s.[SESPROJECTPONO] or (d.[SESPROJECTPONO] is null and s.[SESPROJECTPONO] is null) )
			and (d.[SESSONUMBER] = s.[SESSONUMBER] or (d.[SESSONUMBER] is null and s.[SESSONUMBER] is null) )
			and (d.[SESSUBCODE] = s.[SESSUBCODE] or (d.[SESSUBCODE] is null and s.[SESSUBCODE] is null) )
			and (d.[SESWBS] = s.[SESWBS] or (d.[SESWBS] is null and s.[SESWBS] is null) )
			and (d.[SOLINE] = s.[SOLINE] or (d.[SOLINE] is null and s.[SOLINE] is null) )
			and (d.[STKCOND] = s.[STKCOND] or (d.[STKCOND] is null and s.[STKCOND] is null) )
			and (d.[SUBFEATURE] = s.[SUBFEATURE] or (d.[SUBFEATURE] is null and s.[SUBFEATURE] is null) )
			and (d.[WELLFACILITY] = s.[WELLFACILITY] or (d.[WELLFACILITY] is null and s.[WELLFACILITY] is null))
			and (d.[WORKORDEREDBY] = s.[WORKORDEREDBY] or (d.[WORKORDEREDBY] is null and s.[WORKORDEREDBY] is null))
			and (d.[SESEDIZIP4] = s.[SESEDIZIP4] or (d.[SESEDIZIP4] is null and s.[SESEDIZIP4] is null))
			and (d.[SESEDIFIELDTICKET] = s.[SESEDIFIELDTICKET] or (d.[SESEDIFIELDTICKET] is null and s.[SESEDIFIELDTICKET] is null))			
			and (d.[SIGNATORYCONTACTPERSONID] = s.[SIGNATORYCONTACTPERSONID] or (d.[SIGNATORYCONTACTPERSONID] is null and s.[SIGNATORYCONTACTPERSONID] is null))
			and (d.[SECONDSIGPERSONID] = s.[SECONDSIGPERSONID] or (d.[SECONDSIGPERSONID] is null and s.[SECONDSIGPERSONID] is null))
	where d.BCGroupId is not null
)
select f.*, s.GUID as FacilityId
into dbo.BillingConfigsRaw
from final f
join dbo.Sites s on s.SITEID = f.FACILITY  and s.DATAAREAID = f.DATAAREAID

;with BillingConfigTemp
as
(
	select distinct  s.BCGroupId as Id, s.customerId, s.CustomerAccountName,  s.[BILLINGCUSTACCOUNT],  s.[DATAAREAID],  
		s.[SESISFIELDTICKETEDI], s.[STATEMENTFREQUENCY], s.[BILLINGCONTACTPERSONID], 
		s.GeneratorAccountName, s.GeneratorOwnerNumber, s.GeneratorId
	from  dbo.BillingConfigsRaw s
)
select t.*,  
	c.[GUID] as BillingContactId, c.[CONTACTPERSONPARTYNUMBER] as BillingContactPartyNumber, c.[CONTACTPERSONNAME] BillingContactFullName,  
	c.[FIRSTNAME] BillingContactFirstName, c.[MIDDLENAME] BillingContactMiddleName, c.[LASTNAME] BillingContactLastName, c.[SEARCHNAME] BillingContactSearchName
	,e.locator as PrimaryEmail, p.locator as PrimaryPhone, pa.[ADDRESS] BillingContactAddress
into dbo.BillingConfigs 
from BillingConfigTemp t
inner join contact_person c on c.[CONTACTPERSONID] =  t.[BILLINGCONTACTPERSONID]
inner join  [dbo].[Contact_Person_Electronic_Address] e 
		on e.PARTYNUMBER = c.[CONTACTPERSONPARTYNUMBER]  and e.[type] = 'Email' 
			and e.[LOCATOR] is not null and e.isprimary = 'Yes' 
inner join  [dbo].[Contact_Person_Electronic_Address] p 
		on p.PARTYNUMBER = c.[CONTACTPERSONPARTYNUMBER]  and p.[type] = 'Phone' 
			and p.[LOCATOR] is not null and p.isprimary = 'Yes' 
inner join [dbo].[Contact_Person_Postal_Address] pa 
	on  pa.PARTYNUMBER = c.[CONTACTPERSONPARTYNUMBER] and 
	pa.[ADDRESS] is not null and pa.isprimary = 1




;with uniqueSourceLocationsRefs
as
(
	select distinct s.BCGroupId as BillingConfigId, s.SourceLocationId, s.[UWIRECID], s.DATAAREAID, s.CustomerId, s.[BILLINGCUSTACCOUNT], s.GeneratorOwnerNumber, s.GeneratorId, s.GeneratorAccountName 
	from  dbo.BillingConfigsRaw s
)
select newID() as Id, s.*
into dbo.BillingConfigSourceLocations
from  uniqueSourceLocationsRefs s


;with uniqueMatchCriteriaRefs
as
(
	select distinct s.BCGroupId as BillingConfigId, s.CustomerId, s.[BILLINGCUSTACCOUNT], 
		s.DATAAREAID, s.[WELLCLASSIFICATION], s.[FACILITYCODETYPE], sonm.[SubstanceId], swc.[SubstanceName],
		tt.[DESCRIPTION] ServiceTypeName, tt.TANKTYPEID as ServiceType, tt.[GUID] as ServiceTypeId
	from  dbo.BillingConfigsRaw s
	join [dbo].[SubstanceOldNewMap] sonm on s.SUBSTANCERECID = sonm.RecId
	join [dbo].[SubstancesAndWasteCodes] swc on sonm.[SubstanceId] = swc.[Id]
	left join dbo.TankTypes tt on s.TANKTYPEID = tt.TANKTYPEID and s.DATAAREAID = tt.DATAAREAID

)
select newID() as Id,  s.*
into dbo.BillingConfigMatchCriteria
from  uniqueMatchCriteriaRefs s


;with uniqueFacilityRefs
as
(
	select distinct s.BCGroupId as BillingConfigId, s.CustomerId, s.[BILLINGCUSTACCOUNT],  s.[FACILITY], s.DATAAREAID, s.FacilityId
	from  dbo.BillingConfigsRaw s
)
select newID() as Id,  BillingConfigId, s.[FACILITY], s.DATAAREAID, s.FacilityId
into dbo.BillingConfigFacilities
from  uniqueFacilityRefs s

;with uniqueScaRefs
as
(
	select distinct s.BCGroupId as BillingConfigId,s.CustomerId, s.[BILLINGCUSTACCOUNT], s.DATAAREAID, s.SCA  
	from  dbo.BillingConfigsRaw s
)
select newID() as Id, BillingConfigId, s.SCA, s.DATAAREAID
into dbo.BillingConfigSCAs
from  uniqueScaRefs s


;With EDI
as
(
	SELECT distinct BillingConfigId, CustomerId, DATAAREAID, [BILLINGCUSTACCOUNT],  EdiField, EdiFieldValue
	from (select 	
		S. BCGroupId AS BillingConfigId,	
		s.CustomerId, 
		s.DATAAREAID, 
		s.[BILLINGCUSTACCOUNT], 		
		 s.[AFE], 
		s.[EDICODE], 
		s.[PO],
		s.[ACCOUNT], 
		s.[ACCOUNTQTY], 
		s.[ACCTCENTRE], 
		convert(nvarchar(100), s.[ACTIVITYDATE]) [ACTIVITYDATE], 
		s.[ALPHA], 
		s.[ATTNTO], 
		s.[ATTNTONAME], 
		s.[CATALOGSERIALNO], s.[CCTYPE], s.[CHARGETO], 
		s.[CHARGETONAME], s.[COMPANY], s.[CONTRACT], s.[CONTRACTLINENUMBER], s.[FIELDLOCATION], s.[GLACCOUNT], s.[JOINTS], s.[LINEITEMDESCRIPTION], s.[LOCATION], 
		s.[LOCATIONCOMMENTS], s.[MATERIALCODE], s.[MATGRP], s.[MGDESC], s.[NETWORKACTIVITY], s.[OPNUMBER], s.[ORDERNUMBER], s.[PROFITCENTER], s.[PROJECT], s.[PROJID], 
		s.[REPORTCENTRE], s.[SERIALNUMBER], s.[SERVICEORDERID], s.[SESAFE], s.[SESAFEOWNER], s.[SESAPPROVERCODING], s.[SESASSETNUMBER], s.[SESBUYERCODE], 
		s.[SESCOMPANYREP], s.[SESCOSTCENTER], s.[SESCOSTCENTERNAME], s.[SESMAJORCODE], s.[SESMINORCODE], s.[SESOBJSUBNA], s.[SESOPERATIONCAT], s.[SESOPERATORCODING], 
		s.[SESPOAPPROVER], s.[SESPOLINE], s.[SESPONUMBER], s.[SESPROJECTPONO], s.[SESREQUISITIONER], s.[SESSIGNATORY], s.[SESSONUMBER], s.[SESSUBCODE], s.[SESWBS], s.[SESWONUMBER], 
		s.[SOLINE], s.[STKCOND], s.[SUB], s.[SUBFEATURE], s.[WELLFACILITY], s.[WORKORDEREDBY], s.[SESBUYERSDEPARTMENTDEFAULT], s.[SESEDIZIP4]
	
	from  dbo.BillingConfigsRaw s) P  
	UNPIVOT  
	   (EdiFieldValue FOR EdiField IN   
		  (AFE, EDICODE, PO,
		ACCOUNT, ACCOUNTQTY, ACCTCENTRE, ACTIVITYDATE, ALPHA, ATTNTO, ATTNTONAME, CATALOGSERIALNO, CCTYPE, CHARGETO, 
		CHARGETONAME, COMPANY, CONTRACT, CONTRACTLINENUMBER, FIELDLOCATION, GLACCOUNT, JOINTS, LINEITEMDESCRIPTION, LOCATION, 
		LOCATIONCOMMENTS, MATERIALCODE, MATGRP, MGDESC, NETWORKACTIVITY, OPNUMBER, ORDERNUMBER, PROFITCENTER, PROJECT, PROJID, 
		REPORTCENTRE, SERIALNUMBER, SERVICEORDERID, SESAFE, SESAFEOWNER, SESAPPROVERCODING, SESASSETNUMBER, SESBUYERCODE, 
		SESCOMPANYREP, SESCOSTCENTER, SESCOSTCENTERNAME, SESMAJORCODE, SESMINORCODE, SESOBJSUBNA, SESOPERATIONCAT, SESOPERATORCODING, 
		SESPOAPPROVER, SESPOLINE, SESPONUMBER, SESPROJECTPONO, SESREQUISITIONER, SESSIGNATORY, SESSONUMBER, SESSUBCODE, SESWBS, SESWONUMBER, 
		SOLINE, STKCOND, SUB, SUBFEATURE, WELLFACILITY, WORKORDEREDBY, SESBUYERSDEPARTMENTDEFAULT, SESEDIZIP4)  
	)AS unpvt

)
select newid() as Id, d.Id as EdiDefinitionId, e.*
into dbo.BillingConfigEdi
from edi e
join dbo.EdiFieldDefinitions d on e.EdiField = d.EdiField


;With Signers
as
(
	SELECT distinct BillingConfigId, CustomerId, DATAAREAID, [BILLINGCUSTACCOUNT], SignOrder, ContactPersonId
	from (select 	
		S. BCGroupId AS BillingConfigId,
		s.CustomerId, 
		s.DATAAREAID, 
		s.[BILLINGCUSTACCOUNT], 		
		trim(s.PrimarySignerContactPersonId) [Primary],  trim(s.SecondarySignerContactPersonId) as [Secondary]
	from  dbo.BillingConfigsRaw s) P  
	UNPIVOT  
	   (ContactPersonId FOR SignOrder IN   
		  ([Primary], [Secondary])  
	)AS unpvt

)
select newid() as Id, s.*, c.GUID as ContactId, c.[CONTACTPERSONNAME], [ASSOCIATEDPARTYID], [FIRSTNAME], [MIDDLENAME], [LASTNAME], [SEARCHNAME] 
		,cpe.LOCATOR as PrimaryEmail, cpp.LOCATOR as PrimaryPhone, cpa.[ADDRESS] as PrimaryAddress
into dbo.BillingConfigSigners
from Signers s
	left join [dbo].[Contact_Person] c on c.[CONTACTPERSONID] = s.ContactPersonId and c.DATAAREAID = s.DATAAREAID
	left join [dbo].[Contact_Person_Electronic_Address] cpe on cpe.PartyNumber = c.[CONTACTPERSONPARTYNUMBER] 
		and cpe.[TYPE] = 'Email' and cpe.ISPRIMARY = 'Yes' and cpe.purpose = 'Business' 
	left join [dbo].[Contact_Person_Electronic_Address] cpp on cpp.PartyNumber = c.[CONTACTPERSONPARTYNUMBER] 
		and cpp.[TYPE] = 'Phone' and cpp.ISPRIMARY = 'Yes' and cpp.purpose = 'Business' 
	left join [dbo].[Contact_Person_Postal_Address] cpa on cpa.PartyNumber = c.[CONTACTPERSONPARTYNUMBER] 
		and cpa.ISPRIMARY = 1
where trim(s.ContactPersonId) != '';


;With ContactFuncs
as
(
--generator contact
	select distinct cto.CONTACTPERSONID, cto.DATAAREAID, cto.[GUID] as ContactId,  'GeneratorRepresentative' as FunctionName
	from SourceLocations a
		JOIN [dbo].Contact_Person cto 
			ON a.CONTACTPERSONOWNER = cto.RECID AND a.DATAAREAID = cto.DATAAREAID  --GeneratorId, GeneratorName

	union

	--production accountant
	select distinct cto.CONTACTPERSONID, cto.DATAAREAID,  cto.[GUID] as ContactId, 'ProductionAccountant' as FunctionName
	from SourceLocations a
		JOIN [dbo].Contact_Person cto 
		ON a.CONTRACTOPERATEDCONTACT = cto.CONTACTPERSONID AND a.DATAAREAID = cto.DATAAREAID --ContractOperatorProductionAccountContactId
	
	union	
	select distinct s.ContactPersonId, s.DATAAREAID, ContactId, 'FieldSignatoryContact' as FunctionName
	from BillingConfigSigners s
	union
	select distinct b.BILLINGCONTACTPERSONID, b.DATAAREAID, b.BillingContactId, 'BillingContact' as FunctionName
	from dbo.BillingConfigs b 
	union
	select distinct b.CONTACTPERSONID, b.[DATAAREAID], b.[GUID], 'BillingContact' as FunctionName
	from [dbo].Contact_Person b 
	where b.PRIMARY_CONTACT = 1
	union
	select distinct b.CONTACTPERSONID, b.[DATAAREAID], b.[GUID], 'General' as FunctionName
	from [dbo].Contact_Person b 
	union
	select distinct b.BUSRELACCOUNT, b.[DATAAREAID], b.Id, 'General' as FunctionName
	from [dbo].ProspectContacts b 
)
select distinct CONTACTPERSONID as ContactPersionId, DATAAREAID as DataAreaId, ContactId, FunctionName
into  dbo.ContactFunctions
from ContactFuncs
where contactid is not null


