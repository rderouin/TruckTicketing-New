

;WITH lfFacCodes AS
(
	SELECT tt.DESCRIPTION, tt.TANKTYPEID, tt.FACILITYCODETYPE
	FROM tanktypes tt
	WHERE FACILITYCODETYPE = 0 AND (CHARINDEX('Cl',  dbo.RemoveHiddenChars(TANKTYPEID)) >= 1 
	OR CHARINDEX('LF',  dbo.RemoveHiddenChars(TANKTYPEID)) >= 1) 
	AND TANKTYPEID NOT IN ('Misc LF', 'RMLFee')
)
,ItemFacCodes AS
(
	SELECT tt.TANKTYPEID
	FROM tanktypes tt
	WHERE FACILITYCODETYPE = 0 AND tt.TANKTYPEID NOT IN (SELECT lf.TANKTYPEID FROM lfFacCodes lf)
)
,BillingConfigMessage AS
(
	select (
		  select 
			  'D365FO' as [Source]
			, c.Id as [SourceId]
			, 'BillingConfiguration' as [MessageType]
			, 'Create' as [Operation]
			, NEWID() as [CorrelationId]
			, SYSDATETIMEOFFSET() as [MessageDate]
				, c.Id as [Payload.Id]
				, convert(bit, 1) as [Payload.BillingConfigurationEnabled]
				, c.BillingContactAddress as [Payload.BillingContactAddress]
				, c.BillingContactId as [Payload.BillingContactId]
				, c.BillingContactFullName as [Payload.BillingContactName]
				, c.customerId as [Payload.BillingCustomerAccountId]
				, c.CustomerAccountName as [Payload.BillingCustomerName]				
				, c.GeneratorId as [Payload.CustomerGeneratorId]
				, c.GeneratorAccountName as [Payload.CustomerGeneratorName]
				, 'Billing Config' as [Payload.Description]
				, convert(bit, 0) as [Payload.EmailDeliveryEnabled]
				, null as [Payload.EndDate]		
				, convert(bit, c.SESISFIELDTICKETEDI) as [Payload.FieldTicketsUploadEnabled]
				, null as [Payload.GeneratorRepresentativeId]
				, convert(bit, 1) as [Payload.IncludeExternalAttachmentInLC]
				, convert(bit, 0) as [Payload.IncludeForAutomation]
				, convert(bit, 0) as [Payload.IncludeInternalAttachmentInLC]
				, convert(bit, 0) as [Payload.IncludeSaleTaxInformation]
				, ic.Id as [Payload.InvoiceConfigurationId]
				, convert(bit, 0) as [Payload.IsDefaultConfiguration]
				,  convert(bit, 1) as [Payload.IsEdiValid]
				, convert(bit, 0) as [Payload.IsSignatureRequired]
				, convert(bit, 1) as [Payload.IsValid]
				, null as [Payload.LastComment]
				, case [STATEMENTFREQUENCY]
					when 0 then 'Undefined'
					when 4 then 'Undefined'
					when 1 then 'Daily'
					when 2 then 'Weekly'
					when 3 then 'Monthly'
					end as [Payload.LoadConfirmationFrequency]
				, iif([STATEMENTFREQUENCY] > 0 and [STATEMENTFREQUENCY] < 4, convert(bit, 1), convert(bit, 0)) as [Payload.LoadConfirmationsEnabled]
				, 'Billing Config' as [Payload.Name]
				, '' as [Payload.RigNumber]				
				, SYSDATETIMEOFFSET() as [Payload.StartDate]
				, '' as [Payload.EmailDeliveryContacts]
				, json_query((select	
								CONCAT('["',STRING_AGG(convert(nvarchar(50), x.FacilityId), '","'),'"]') 
								from dbo.BillingConfigFacilities x 
								where x.BillingConfigId = c.Id	
				)) as [Payload.Facilities]
				, (select  m.Id as	[Id]
					, null as [EndDate]			
					, convert(bit, 1) as [IsEnabled]
					, m.ServiceTypeName as [ServiceType]
					, m.ServiceTypeId as [ServiceTypeId]
					, iif(m.ServiceTypeId  is not null, 'Any', 'NotSet') as [ServiceTypeValueState]
					, null as [SourceIdentifier]
					, null as [SourceLocationId]
					, 'NotSet' as [SourceLocationValueState]
					, null as [StartDate]
					, (case
						WHEN m.FACILITYCODETYPE =  1 THEN 'Pipeline'
						WHEN m.FACILITYCODETYPE = 2 THEN 'Terminalling'
						WHEN m.FACILITYCODETYPE = 3 THEN 'Treating'
						WHEN m.FACILITYCODETYPE = 4 THEN 'Waste'
						WHEN m.FACILITYCODETYPE = 5 THEN 'Water'
						WHEN m.FACILITYCODETYPE = 0 AND m.ServiceType is not null and m.ServiceType in (SELECT l.TANKTYPEID FROM lfFacCodes l) THEN 'Landfill'
						WHEN m.FACILITYCODETYPE = 0 AND m.ServiceType is not null and m.ServiceType in (SELECT l.TANKTYPEID FROM ItemFacCodes l) THEN null		
						end)as [Stream]
					,  iif(m.SubstanceId is not null, 'Any', 'NotSet') as [StreamValueState]
					, m.SubstanceId as [SubstanceId]
					, m.SubstanceName as [SubstanceName]
					,  iif(m.[SubstanceId] is not null, 'Any', 'NotSet') as [SubstanceValueState]
					, (case  m.[WELLCLASSIFICATION] 
						when 0 then ''
						when 1 then 'Production'
						when 2 then 'Drilling'
						when 3 then 'Completions'
						when 4 then 'Remediation'
						when 5 then 'Industrial'
						when 6 then 'Oilfield'			
						end) as [WellClassification]
					, iif(m.[WELLCLASSIFICATION] > 0, 'Any', 'NotSet')  as [WellClassificationState]
					from dbo.BillingConfigMatchCriteria m						
					where m.BillingConfigId = c.Id	
					for json path
				) as [Payload.MatchCriteria]
				,(select s.Id as [Id]
						,s.ContactId  as [AccountContactId]
						,c.customerId as [AccountId]
						,s.PrimaryAddress as [Address]
						,s.PrimaryEmail as [Email]
						,s.FIRSTNAME as [FirstName]
						,convert(bit, 1) as [IsAuthorized]
						,s.LASTNAME as [LastName]
						,s.PrimaryPhone as [PhoneNumber]
					from dbo.BillingConfigSigners s
					where c.Id = s.BillingConfigId
					for json path
				)
				as [Payload.Signatories]
				,(select e.Id as [Id]
						,e.EdiDefinitionId as [EDIFieldDefinitionId]
						,e.EdiField as [EDIFieldName]
						,e.EdiFieldValue as [EDIFieldValueContent]
					from dbo.BillingConfigEdi e 
					where c.Id = e.BillingConfigId
					for json path
				) as [Payload.EDIValueData]
	from [dbo].[BillingConfigs] c
	join dbo.ValidCustomerAccounts vc on c.customerId = vc.CustomerId
	left outer join dbo.invoiceConfigs ic on c.customerId = ic.CustomerId
	where c.Id = sd.Id
	for json path)
	AS [Message], 'BillingConfig' AS MessageType, SYSDATETIMEOFFSET() AS GeneratedDate, sd.Id AS EntityId, sd.DataAreaId, sd.BILLINGCUSTACCOUNT AS AxEntityId
		FROM [dbo].[BillingConfigs] sd
)
,Final
as
(
	SELECT SUBSTRING([Message], 2, LEN([Message]) -2) AS [Message], MessageType, GeneratedDate, EntityId, DataAreaId, AxEntityId, 0 AS Processed, null as ProcessedDate, 'enterprise-entity-updates' as TopicName
	FROM BillingConfigMessage
	WHERE Message IS NOT NULL 
)
MERGE [dbo].[DataMigrationMessages] AS TARGET USING Final AS SOURCE
ON (TARGET.EntityId = SOURCE.EntityId AND TARGET.MessageType = SOURCE.MessageType)
WHEN MATCHED THEN UPDATE SET 
		TARGET.Message = Source.Message,
		TARGET.Processed = 0,
		TARGET.ProcessedDate = null,
		TARGET.GeneratedDate = Source.GeneratedDate
WHEN NOT MATCHED THEN 
	INSERT ([Message], MessageType, GeneratedDate, EntityId, DataAreaId, [AxEntityId], Processed, [ProcessedDate], TopicName)
	VALUES (SOURCE.Message, source.MessageType, SOURCE.GeneratedDate, Source.EntityId, Source.DataAreaId, Source.EntityId, 0, null, Source.TopicName);


-- This is a fix for dups being created due to bad data in accounts/contacts
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

GO


/*
declare @BCId uniqueidentifier = (select top(1) Id from dbo.BillingConfigs)

select *
from dbo.BillingConfigs
where id = @BCId

select *
from dbo.BillingConfigSourceLocations
where BillingConfigId = @BCId

select *
from dbo.BillingConfigMatchCriteria
where BillingConfigId = @BCId
select *

from dbo.BillingConfigFacilities
where BillingConfigId =@BCId

select *
from dbo.BillingConfigSCAs
where BillingConfigId = @BCId
*/
