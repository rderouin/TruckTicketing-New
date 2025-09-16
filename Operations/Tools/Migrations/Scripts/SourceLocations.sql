
;WITH SourceLocationMessage AS
(	
	SELECT (SELECT [Source] = 'D365FO'
					,[SourceId] = a.ID
					,[MessageType] = 'SourceLocation'
					,[Operation] = 'Create'
					,[CorrelationId] = NEWID()
					,[MessageDate] = SYSDATETIMEOFFSET() 				  
						,[Payload.Id] = a.ID
						,[Payload.ApiNumber] = a.APINUMBER
						,[Payload.AssociatedSourceLocationFormattedIdentifier] = null
						,[Payload.AssociatedSourceLocationId] = null
						,[Payload.AssociatedSourceLocationIdentifier] = null
						,[Payload.ContractOperatorId] = co.ID
						,[Payload.ContractOperatorProductionAccountContactId] = cto.[GUID]
						,[Payload.CountryCode] = TRIM(SUBSTRING(ISNULL([COUNTRYREGIONID], '  '), 1, 2))						
						,[Payload.CtbNumber] = a.CTBNUMBER
						,[Payload.DeliveryMethod] = CASE a.TRUCKEDORPIPELINED 
													WHEN 0 THEN 'Undefined'
													WHEN 1 THEN 'Trucked'
													WHEN 2 THEN 'Pipeline'
													ELSE 'Undefined'
													END						
						,[Payload.DownHoleType] = CASE a.TRUCKEDORPIPELINED 
													WHEN 0 THEN 'Undefined'
													WHEN 1 THEN 'Pit'
													WHEN 2 THEN 'Well'
													ELSE 'Undefined'
												END						
						,[Payload.FieldName] = a.UWIFIELDNAME
						,[Payload.FormattedIdentifier] = case 
															when st.BatteryCodeField = 'BT' then  SUBSTRING(a.UWI, 5, len(a.uwi) -7)
															else a.TT_UWI
														 end
						,[Payload.GeneratorAccountNumber] = gen.CUSTOMERACCOUNT			  
						,[Payload.GeneratorId] = gen.ID
						,[Payload.GeneratorName] = gen.ORGANIZATIONNAME
						,[Payload.GeneratorProductionAccountContactId] = cto.[GUID] 
						,[Payload.GeneratorProductionAccountContactName] = cto.CONTACTPERSONNAME
						,[Payload.GeneratorStartDate] = a.CREATEDDATETIME
						,[Payload.Identifier] = a.UWIALIAS
						,[Payload.IsActive] = convert(bit, 1)
						,[Payload.IsDeleted] = convert(bit, 0)
						,[Payload.IsUnique] = convert(bit, 0)
						,[Payload.PlsNumber] = a.NDICLOCATION
						,[Payload.ProvinceOrState] = a.PROVINCEORSTATE
						,[Payload.FormattedIdentifierPattern] = st.Format1
						,[Payload.SourceLocationCode] = IIF( a.COUNTRYREGIONID = 'CAN', a.SOBATTERYCODE, null)
						,[Payload.SourceLocationName] = IIF( a.COUNTRYREGIONID = 'USA', a.TT_UWI, null)
						,[Payload.SourceLocationTypeCategory] = CASE a.locationtype 
																WHEN 0 THEN 'Undefined'
																WHEN 1 THEN 'Well' 
																WHEN 2 THEN 'Surface' 
																ELSE 'Undefined'
																END
						,[Payload.SourceLocationTypeId] = a.SOURCELOCATIONTYPEID
						,[Payload.SourceLocationTypeName] = a.SOURCELOCATIONTYPE						
						,[Payload.WellFileNumber] = a.WELLFILENUMBER
						,[Payload.SourceLocationVerified] = convert(bit, 1)										
			FROM [dbo].[SourceLocations] AS a
			join dbo.SourceLocationTypes st on st.id = a.SOURCELOCATIONTYPEID
			-- doing left joins, so that we can coalesce above. if customer not found in customers then look in nonbillablecustomers		   
			LEFT JOIN [dbo].[AccountMaster] gen ON a.[OWNER] = gen.CUSTOMERACCOUNT AND a.DATAAREAID = gen.DATAAREAID
			LEFT JOIN [dbo].Contact_Person cto ON a.CONTACTPERSONOWNER = cto.RECID AND a.DATAAREAID = cto.DATAAREAID  --GeneratorId, GeneratorName
			LEFT JOIN [dbo].[AccountMaster] co ON a.CONTRACTOPERATED = co.CUSTOMERACCOUNT AND a.DATAAREAID = co.DATAAREAID   --ContractOperatorId
			LEFT JOIN [dbo].Contact_Person coc ON a.CONTRACTOPERATEDCONTACT = coc.CONTACTPERSONID AND a.DATAAREAID = coc.DATAAREAID --ContractOperatorProductionAccountContactId
			
			WHERE a.ID = sl.ID
			and gen.CUSTOMERACCOUNT is not null 		
			FOR JSON PATH, INCLUDE_NULL_VALUES
		) AS Message, 'SourceLocation' AS MessageType, SYSDATETIMEOFFSET() AS GeneratedDate, sl.[GUID] AS EntityId, sl.DATAAREAID, sl.UWI AS AxEntityId
	FROM [dbo].[SourceLocations] AS sl
)
,Final
as
(
	SELECT SUBSTRING([Message], 2, LEN([Message]) -2) AS [Message], MessageType, GeneratedDate, EntityId, DataAreaId, AxEntityId, 0 AS Processed, null as ProcessedDate, 'enterprise-entity-updates' as TopicName
	FROM SourceLocationMessage ax
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
