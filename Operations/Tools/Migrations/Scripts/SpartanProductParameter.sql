
;WITH SpartanProductParameterMessage AS
(
	SELECT (SELECT [Source] = 'D365FO'
					,[SourceId] = p.Id
					,[MessageType] = 'SpartanProductParameter'
					,[Operation] = 'Create'
					,[CorrelationId] = NEWID()
					,[MessageDate] = SYSDATETIMEOFFSET()
						,[Payload.Id] = p.Id
						,[Payload.CreatedAt] = SYSDATETIMEOFFSET()
						,[Payload.CreatedBy] = 'System'
						,[Payload.CreatedById] = '00000000-0000-0000-0000-000000000000'						
						,[Payload.FluidIdentity] = [FLUIDIDENTITY]
						,[Payload.IsActive] = convert(bit, 1)
						,[Payload.IsDeleted] = convert(bit, 0)
						,[Payload.LegalEntity] = p.DataAreaId
						,[Payload.LegalEntityId] = p.LegalEntityId
						,[Payload.LocationOperatingStatus] = 
							case 
								when [LCTNOPERSTATUS] is null or  trim([LCTNOPERSTATUS]) = '' then 'Blank'
								else trim([LCTNOPERSTATUS])
							end
						,[Payload.MaxFluidDensity] = [FLUIDDENSITYMAX]
						,[Payload.MaxWaterPercentage] = [WATERPERCENTMAX]
						,[Payload.MinFluidDensity] = [FLUIDDENSITYMIN]
						,[Payload.MinWaterPercentage] = [WaterPercentMin]
						,[Payload.ProductName] = [PRDTNAME]
						,[Payload.ShowDensity] = [SHOWDENSITY]
						,[Payload.UpdatedAt] = SYSDATETIMEOFFSET()
						,[Payload.UpdatedBy] = 'System'
						,[Payload.UpdatedById] = '00000000-0000-0000-0000-000000000000'						
				FROM [dbo].[SpartanProductParameters] p				
				WHERE p.[RECID] NOT IN (5637144576, 5637144577, 5637144594, 5637144595)
				AND p.Id = sp.Id
				ORDER BY p.[FLUIDDENSITYMAX]
				FOR JSON PATH, INCLUDE_NULL_VALUES
			) AS Message, 'SpartanProductParameter' AS MessageType, SYSDATETIMEOFFSET() AS GeneratedDate, sp.Id as EntityId, sp.DataAreaId, sp.RECID AS AxEntityId
	FROM [dbo].[SpartanProductParameters] sp
	WHERE sp.[RECID] NOT IN (5637144576, 5637144577, 5637144594, 5637144595)
)
,Final
as
(
	SELECT SUBSTRING([Message], 2, LEN([Message]) -2) AS [Message], MessageType, GeneratedDate, EntityId, DataAreaId, AxEntityId, 0 AS Processed, null as ProcessedDate, 'enterprise-entity-updates' as TopicName
	FROM SpartanProductParameterMessage ax
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
