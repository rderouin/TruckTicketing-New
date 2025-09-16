
;WITH BusinessStreamMessages AS
(

	select (select 
		  'D365FO' as [Source]
		, Id as [SourceId]
		, 'BusinessStream' as [MessageType]
		, 'Create' as [Operation]
		, NEWID() as [CorrelationId]
		, SYSDATETIMEOFFSET() as [MessageDate]
		, Id as [Payload.Id]
		, Name as [Payload.Name]	
	from dbo.BusinessStreams s
	where s.Id = b.Id
	for json path, INCLUDE_NULL_VALUES
	) AS [Message], 'BusinessStream' AS MessageType, SYSDATETIMEOFFSET() AS GeneratedDate, b.Id AS EntityId, null as DataAreaId,  b.Name AS AxEntityId
		FROM dbo.BusinessStreams b
)
,Final AS
(
	SELECT SUBSTRING([Message], 2, LEN([Message]) -2) AS [Message], MessageType, GeneratedDate, EntityId, DataAreaId, AxEntityId, 0 AS Processed, null as ProcessedDate, 'enterprise-entity-updates' as TopicName
	FROM BusinessStreamMessages ax
	WHERE Message IS NOT NULL 
)
MERGE [dbo].[DataMigrationMessages] AS TARGET 
	USING Final AS SOURCE
ON (
	TARGET.EntityId = SOURCE.EntityId 
	AND Target.MessageType = SOURCE.MessageType
)
WHEN MATCHED THEN UPDATE SET 
	TARGET.Message = Source.Message,
	Target.Processed = 0,
	Target.ProcessedDate = null,
	Target.GeneratedDate = Source.GeneratedDate

WHEN NOT MATCHED THEN 
	INSERT ([Message], MessageType, GeneratedDate, EntityId, DataAreaId, [AxEntityId], Processed, [ProcessedDate], TopicName)
	VALUES (SOURCE.Message, source.MessageType, SOURCE.GeneratedDate, Source.EntityId, Source.DataAreaId, Source.AxEntityId, 0, null, Source.TopicName);

	GO
