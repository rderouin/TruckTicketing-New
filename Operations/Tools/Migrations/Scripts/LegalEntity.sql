;WITH LegalMessages AS
(
	SELECT (SELECT 	
					  'D365FO' as [Source]
					, le.guid as [SourceId]
					, 'LegalEntityMessage' as [MessageType]
					, 'Create' as [Operation]
					, NEWID() as [CorrelationId]
					, SYSDATETIMEOFFSET() as [MessageDate]
					, le.GUID as [Payload.Id]
					, b.Id as [Payload.BusinessStreamId]					
					, [Country] AS [Payload.CountryCode]	
					, 365 AS [Payload.CreditExpiryThreshold]
					, [DataAreaId] AS [Payload.Code]
					, le.BusinessStream as [Payload.Name]
					, IsCustomerPrimaryContactRequired as [Payload.IsCustomerPrimaryContactRequired]
					, l.ShowCustomersInTruckTicking as [Payload.ShowCustomersInTruckTicking]
				FROM [dbo].[LegalEntity] l
				join dbo.BusinessStreams b on l.Division = b.Name
				WHERE l.DataAreaId = le.DataAreaId
				FOR JSON PATH, INCLUDE_NULL_VALUES
			) AS [Message], 'LegalEntityMessage' AS MessageType, SYSDATETIMEOFFSET() AS GeneratedDate, le.[GUID] AS EntityId, le.DataAreaId AS AxEntityId, 0 AS Processed
		FROM [dbo].[LegalEntity] le
)
,Final
as
(
	SELECT SUBSTRING([Message], 2, LEN([Message]) -2) AS [Message], MessageType, GeneratedDate, EntityId, AxEntityId as DataAreaId, AxEntityId, 0 AS Processed, null as ProcessedDate, 'enterprise-entity-updates' as TopicName
	FROM LegalMessages ax
	WHERE Message IS NOT NULL 
)
MERGE [dbo].[DataMigrationMessages] AS TARGET 
USING Final AS SOURCE
ON (TARGET.EntityId = SOURCE.EntityId AND TARGET.MessageType = SOURCE.MessageType)
WHEN MATCHED THEN UPDATE SET 
		TARGET.Message = Source.Message,
		TARGET.Processed = 0,
		TARGET.ProcessedDate = null,
		TARGET.GeneratedDate = Source.GeneratedDate
WHEN NOT MATCHED THEN 
	INSERT ([Message], MessageType, GeneratedDate, EntityId, DataAreaId, [AxEntityId], Processed, [ProcessedDate], TopicName)
	VALUES (SOURCE.Message, source.MessageType, SOURCE.GeneratedDate, Source.EntityId, Source.DataAreaId, Source.AxEntityId, 0, null, Source.TopicName);

GO


