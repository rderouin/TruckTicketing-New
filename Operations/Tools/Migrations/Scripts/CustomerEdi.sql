
;WITH EdiCustomerConfigMessages AS
(
			SELECT (SELECT  
					 'D365FO' as [Source]
					, convert(uniqueidentifier, c.Id) as [SourceId]
					, 'EDIFieldDefinition' as [MessageType]
					, 'Create' as [Operation]
					, NEWID() as [CorrelationId]
					, SYSDATETIMEOFFSET() as [MessageDate]
						, convert(uniqueidentifier, e.[GUID]) as [Payload.Id]
						, SYSDATETIMEOFFSET() as [Payload.CreatedAt]
						, 'System' as [Payload.CreatedBy]
						, '00000000-0000-0000-0000-000000000000' as [Payload.CreatedById]
						, convert(uniqueidentifier, c.Id) as [Payload.CustomerId]
						, '' as [Payload.DefaultValue] --Need to findout if we can get this from the old data somewhere		
						, d.Id as [Payload.EDIFieldLookupId]
						, d.EDIFIELD as [Payload.EDIFieldName]		
						, case e.HSPRINT
							when 'No' then convert(bit, 0)
							when 'Yes' then convert(bit, 1)
							end as [Payload.IsPrinted]
						, case e.MANDATORY
							when 'No' then convert(bit, 0)
							when 'Yes' then convert(bit, 1)
							end as [Payload.IsRequired]
						, convert(bit, 0) as [Payload.IsValidated]
						, sysdatetimeoffset() as [Payload.UpdatedAt]
						, 'System' as [Payload.UpdatedBy]
						, e.DataAreaId as [Payload.LegalEntity]
						, '00000000-0000-0000-0000-000000000000' as [Payload.UpdatedById]
						, null as [Payload.ValidationErrorMessage]
						, null as [Payload.ValidationPattern]
						, null as [Payload.ValidationPatternId]
						, null as [Payload.ValidationRequired]
		from  [dbo].[EDI_HSEDICustomerConfigEntity] e
		inner join dbo.EdiFieldDefinitions d on   case 	e.EDIFIELD
													when 'Account'				then 'AccountQty'
													when 'BuyerDepartment'		then 'BuyerDepartmentDefault'
													when 'CatalogSerialNum'		then 'CatalogSerialNo'
													when 'Obj'					then 'OBJSubna'
													when 'OperationCategory'	then 'OperationCat'
													when 'ProjectPONumber'		then 'ProjectPONo'
													when 'WorkOrderedBy'		then 'WorkOrderBy'
													else e.EDIFIELD
												  end 	= trim(d.EdiField)
		inner join dbo.AccountMaster c on c.CUSTOMERACCOUNT = e.ACCOUNTNUM and c.DATAAREAID = e.DATAAREAID
		where o.EDIFIELD =e.EDIFIELD and o.ACCOUNTNUM = e.ACCOUNTNUM and o.DATAAREAID = e.DATAAREAID
		FOR JSON PATH
	) AS [MESSAGE], 'EDIFieldDefinition' AS MessageType, SYSDATETIMEOFFSET() AS GeneratedDate, o.[GUID] AS EntityId, o.DataAreaId, o.EDIFIELD AS AxEntityId
	FROM [dbo].[EDI_HSEDICustomerConfigEntity] o 
)
, Final
as
(
	SELECT SUBSTRING([Message], 2, LEN([Message]) -2) AS [Message], MessageType, GeneratedDate, EntityId, DataAreaId, AxEntityId, 0 AS Processed, null as ProcessedDate, 'enterprise-entity-updates' as TopicName
	FROM EdiCustomerConfigMessages
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

GO
