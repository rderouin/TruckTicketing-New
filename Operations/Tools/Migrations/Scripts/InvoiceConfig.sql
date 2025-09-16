
declare @Splits Table ([Name] nvarchar(20));

insert into @splits
values('Facility')
, ('SourceLocation');

;WITH InvoiceConfigMessage AS
(
	select (select 
				 'D365FO' as [Source]
				, ic.[Id] as [SourceId]
				, 'InvoiceConfiguration' as [MessageType]
				, 'Create' as [Operation]
				, NEWID() as [CorrelationId]
				, SYSDATETIMEOFFSET() as [MessageDate]
					, ic.[Id] as [Payload.Id]
					, convert(bit, ic.[AllFacilities]) as [Payload.AllFacilities]					
					, convert(bit, ic.[AllServiceTypes]) as [Payload.AllServiceTypes]
					, convert(bit, ic.[AllSourceLocations]) as [Payload.AllSourceLocations]
					, convert(bit, ic.[AllSubstances]) as [Payload.AllSubstances]
					, convert(bit, ic.[AllWellClassifications]) as [Payload.AllWellClassifications]
					, convert(bit, ic.[BusinessUnitId]) as [Payload.BusinessUnitId]
					, convert(bit, ic.[CatchAll]) as [Payload.CatchAll]					
					, ic.[CustomerId] as [Payload.CustomerId]					
					, ISNULL(b.guid, '00000000-0000-0000-0000-000000000000') as [Payload.CustomerLegalEntityId]
					, ic.[CustomerName] as [Payload.CustomerName]
					, ic.[Description] as [Payload.Description]
					, convert(bit, ic.[IncludeExternalDocumentAttachment]) as [Payload.IncludeExternalDocumentAttachment]
					, convert(bit, ic.[IncludeInternalDocumentAttachment]) as [Payload.IncludeInternalDocumentAttachment]
					, ic.[InvoiceNumber] as [Payload.InvoiceNumber]
					, convert(bit, ic.[IsSplitByFacility]) as [Payload.IsSplitByFacility]
					, convert(bit, ic.[IsSplitByServiceType]) as [Payload.IsSplitByServiceType]
					, convert(bit, ic.[IsSplitBySourceLocation]) as [Payload.IsSplitBySourceLocation]
					, convert(bit, ic.[IsSplitBySubstance]) as [Payload.IsSplitBySubstance]
					, convert(bit, ic.[IsSplitByWellClassification]) as [Payload.IsSplitByWellClassification]
					, ic.[Name] as [Payload.Name]
					, ic.[PermutationsHash] as [Payload.PermutationsHash]					
					, ic.[Facilities] as [Payload.Facilities]
					, ic.[FacilityCode] as [Payload.FacilityCode]
					, ic.[Permutations] as [Payload.Permutations]
					, ic.[ServiceTypes] as [Payload.ServiceTypes]
					, ic.[ServiceTypesName] as [Payload.ServiceTypesName]
					, ic.[SourceLocationIdentifier] as [Payload.SourceLocationIdentifier]
					, ic.[SourceLocations] as [Payload.SourceLocations]
					, ic.[SplitEdiFieldDefinitions] as [Payload.SplitEdiFieldDefinitions]
					, ic.[Substances] as [Payload.Substances]
					, ic.[SubstancesName] as [Payload.SubstancesName]
					, ic.[WellClassifications] as [Payload.WellClassifications]
					, ic.[CUSTOMERACCOUNT]  as [Payload.CUSTOMERACCOUNT]
					, json_query((select	
							CONCAT('["',STRING_AGG(convert(nvarchar(50), x.[Name]), '","'),'"]') 
							from @Splits x)) as [Payload.SplittingCategories]
	from  InvoiceConfigs ic
		join dbo.ValidCustomerAccounts vc on ic.customerId = vc.CustomerId
	    left join dbo.LegalEntity b on ic.CustomerLegalEntityId = b.LegalEntityName
 	where ic.Id = sd.Id and b.Division = 'MI' --added MI filter as a safety precation
	for json path, INCLUDE_NULL_VALUES)
	AS [Message], 'InvoiceConfig' AS MessageType, SYSDATETIMEOFFSET() AS GeneratedDate, sd.Id AS EntityId, sd.CustomerLegalEntityId, sd.CUSTOMERACCOUNT AS AxEntityId, 0 AS Processed
		FROM [dbo].InvoiceConfigs sd
		
)
,Final
as
(
	SELECT SUBSTRING([Message], 2, LEN([Message]) -2) AS [Message], MessageType, GeneratedDate, EntityId, CustomerLegalEntityId, AxEntityId, 0 AS Processed, null as ProcessedDate, 'enterprise-entity-updates' as TopicName
	FROM InvoiceConfigMessage ax
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
	VALUES (SOURCE.Message, source.MessageType, SOURCE.GeneratedDate, Source.EntityId, Source.CustomerLegalEntityId, Source.AxEntityId, 0, null, Source.TopicName);

GO
