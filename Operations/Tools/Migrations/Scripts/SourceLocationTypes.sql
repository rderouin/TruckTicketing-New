
;WITH SourceLocationTypeMessages AS
(
		select (select 
			 [Source] = 'D365FO'
			,[SourceId] = t.ID
			,[MessageType] = 'SourceLocationType'
			,[Operation] = 'Create'
			,[CorrelationId] = NEWID()
			,[MessageDate] = SYSDATETIMEOFFSET()			  		
			, t.Id  as [Payload.Id]
			, t.Category as [Payload.Category]
			, t.Country as [Payload.CountryCode]		
			, t.DefaultDeliveryMethod as [Payload.DefaultDeliveryMethod]
			, t.DefaultDownHoleType as [Payload.DefaultDownHoleType]		
			, t.Format1 as [Payload.FormatMask]
			, t.LocationType as [Payload.Name]
			, t.RequiresApiNumber as [Payload.RequiresApiNumber]
			, t.RequiresCtbNumber as [Payload.RequiresCtbNumber]
			, t.RequiresPlsNumber as [Payload.RequiresPlsNumber]
			, t.RequiresWellFileNumber as [Payload.RequiresWellFileNumber]
			, t.[BatteryCodeField] as [Payload.ShortFormCode]		
			, convert(bit, 1) as [Payload.IsActive]
		from dbo.SourceLocationTypes t
		where t.Id = slt.Id
		FOR JSON PATH, INCLUDE_NULL_VALUES
	) AS Message, 'SourceLocationType' AS MessageType, SYSDATETIMEOFFSET() AS GeneratedDate, slt.ID AS EntityId, NULL AS DataAreaId, slt.LocationType AS AxEntityId
	from dbo.SourceLocationTypes slt
)
,Final
as
(
	SELECT SUBSTRING([Message], 2, LEN([Message]) -2) AS [Message], MessageType, GeneratedDate, EntityId, DataAreaId, AxEntityId, 0 AS Processed, null as ProcessedDate, 'enterprise-entity-updates' as TopicName
	FROM SourceLocationTypeMessages ax
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
	VALUES (SOURCE.Message, source.MessageType, SOURCE.GeneratedDate, Source.EntityId, Source.DataAreaId, Source.AxEntityId, 0, null, Source.TopicName);

GO
