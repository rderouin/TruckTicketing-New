
;WITH FacilityServicesMessages AS
(
	SELECT (SELECT distinct [Source] = 'D365FO'
					,[SourceId] = t.[GUID]
					,[MessageType] = 'FacilityService'
					,[Operation] = 'Create'
					,[CorrelationId] = NEWID()
					,[MessageDate] = SYSDATETIMEOFFSET()
						,[Payload.Id] = t.[GUID]
						,[Payload.Description] = t.[DESCRIPTION]						
						,[Payload.FacilityId] = s.[GUID]
						,[Payload.FacilityServiceNumber] = [FACILITYTANKNUM]
						,[Payload.IsActive] = convert(bit, 1)
						,[Payload.OilItem] = tt.OILITEMID
						,[Payload.ServiceNumber] = convert(int, t.[TankNum])
						,[Payload.ServiceTypeId] = tt.[GUID]
						,[Payload.ServiceTypeName] = tt.[DESCRIPTION]
						,[Payload.SiteId] = t.FACILITY
						,[Payload.SolidsItem] = tt.SolidItemId
						,[Payload.TotalItem] = tt.TotalItemId
						,[Payload.TotalItemProductId] = tp.[GUID]
						,[Payload.WaterItem] = tt.WATERITEMID
						,[Payload.AuthorizedSubstances] = null
						,(SELECT spp.Id as SpartanProductParameterId, 
								fsp.PRDTNAMEFULL as SpartanProductParameterName,
								fsp.PRDTNAMEFULL as SpartanProductParameterDisplay								
							FROM [dbo].[FacilityServiceSpartanParameters] fsp
							JOIN [dbo].[SpartanProductParameters] spp on spp.RECID = fsp.ProductParameterRecId
							WHERE fsp.[FACILITYTANKRECID] = t.[RECID]
							FOR JSON PATH
						) AS [Payload.SpartanProductParameters]											
			FROM [dbo].[FacilityTank] t
			JOIN [dbo].[TankTypes] tt ON t.TankTypeID = tt.tankTypeID and t.DATAAREAID = tt.DATAAREAID
			JOIN [dbo].Products_Released tp on tp.ITEMNUMBER = tt.TOTALITEMID and tp.DATAAREAID = tt.DATAAREAID
			JOIN dbo.Sites s ON s.SITEID = t.Facility and s.DATAAREAID = t.DATAAREAID
			WHERE t.TankTypeID IS NOT NULL AND TRIM(t.TankTypeID) != '' 
			AND t.GUID = ft.GUID			
			FOR JSON PATH, INCLUDE_NULL_VALUES
		) AS [Message], 'FacilityService' AS MessageType, SYSDATETIMEOFFSET() AS GeneratedDate, ft.[GUID] AS EntityId, ft.DATAAREAID, ft.RECID AS AxEntityId
	FROM [dbo].[FacilityTank] ft
	where ft.TankTypeID IS NOT NULL AND TRIM(ft.TankTypeID) != ''
)
,Final
as
(
	SELECT SUBSTRING([Message], 2, LEN([Message]) -2) AS [Message], MessageType, GeneratedDate, EntityId, DataAreaId, AxEntityId, 0 AS Processed, null as ProcessedDate, 'enterprise-entity-updates' as TopicName
	FROM FacilityServicesMessages ax
	WHERE len([Message]) > 0 
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

