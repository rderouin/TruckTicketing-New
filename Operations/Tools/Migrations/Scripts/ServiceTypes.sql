
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
,ServiceTypeMessage AS
(	
	SELECT (
	
	SELECT   [Source] = 'D365FO'
					,[SourceId] = tt.[GUID]
					,[MessageType] = 'ServiceType'
					,[Operation] = 'Create'
					,[CorrelationId] = NEWID() 
					,[MessageDate] = SYSDATETIMEOFFSET()
						,[Payload.Id] = tt.[GUID]						
						,[Payload.Description] = tt.[DESCRIPTION]
						,[Payload.CountryCode] = TRIM(SUBSTRING(ISNULL(l.Country, '  '), 1, 2))
						,[Payload.CountryCodeString] = CASE l.Country 
														WHEN NULL THEN 'Undefined'
														WHEN 'CA' THEN 'Trucked'
														WHEN 'US' THEN 'Pipeline'
														ELSE 'Undefined'
														END 					  
						,[Payload.LegalEntityId] = l.[GUID]
						,[Payload.LegalEntityCode] = tt.DATAAREAID
						,[Payload.ServiceTypeId] = tt.TANKTYPEID
						,[Payload.Name] = tt.[DESCRIPTION]
						,[Payload.Hash] = 'Needs Fixed in TT'						
						,[Payload.Class] = case  substring( tt.TOTALITEMID , 1, 3) 
												when '702' then 'Class1'
												when '701' then 'Class2'
												else 'Class1'
											end
						,[Payload.TotalItemId] = isnull(tp.[GUID], '00000000-0000-0000-0000-000000000000')  
						,[Payload.TotalItemName] = tp.ProductName
						,[Payload.TotalThresholdType] = 'Undefined'
						,[Payload.TotalEnableCombinedValidation] = convert(bit, 0)
						,[Payload.TotalShowZeroDollarLine] = tt.TOTALSHOWZEROLINE 
						,[Payload.TotalFixedUnit] = 'Undefined' 
						,[Payload.TotalMinValue] = 0 
						,[Payload.TotalMaxValue] = 100 
						,[Payload.OilItemId] =  isnull(op.[GUID], '00000000-0000-0000-0000-000000000000') 
						,[Payload.OilItemName] = op.ProductName
						,[Payload.OilItemReverse] = tt.OILITEMREVERSE
						,[Payload.OilShowZeroDollarLine] = tt.OILSHOWZEROLINE
						,[Payload.OilEnableValidation] = IIF(tt.VOLUMEMINMAX = 1 AND tt.[VOLUMEMINMAXPERCENT] > 0 AND tt.VOLUMEMINMAXOIL = 1, 1 , 0)
						,[Payload.OilCreditMinVolume] = tt.OILCREDITMINVOLPAYOUT 
						,[Payload.OilThresholdType] = IIF(tt.VOLUMEMINMAX = 1 AND tt.[VOLUMEMINMAXPERCENT] > 0 AND tt.VOLUMEMINMAXOIL = 1, 'Percentage', 'Undefined')
						,[Payload.OilFixedUnit] = 'Undefined'
						,[Payload.OilMinValue] = IIF(tt.VOLUMEMINMAX = 0 AND tt.[VOLUMEMINMAXPERCENT] > 0 AND tt.VOLUMEMINMAXOIL = 1, tt.[VOLUMEMINMAXPERCENT], 0)
						,[Payload.OilMaxValue] = IIF(tt.VOLUMEMINMAX = 1 AND tt.[VOLUMEMINMAXPERCENT] > 0 AND tt.VOLUMEMINMAXOIL = 1, tt.[VOLUMEMINMAXPERCENT], 100)					  
						,[Payload.WaterItemId] = isnull( wp.[GUID], '00000000-0000-0000-0000-000000000000') 
						,[Payload.WaterItemName] = wp.ProductName
						,[Payload.WaterShowZeroDollarLine] = tt.WATERSHOWZEROLINE
						,[Payload.WaterMinPricingPercentage] = 0
						,[Payload.WaterMinValue] = IIF(tt.VOLUMEMINMAX = 0 AND tt.[VOLUMEMINMAXPERCENT] > 0 AND tt.VOLUMEMINMAXWATER = 1, tt.[VOLUMEMINMAXPERCENT], 0)
						,[Payload.WaterThresholdType] = IIF(tt.VOLUMEMINMAX = 1 AND tt.[VOLUMEMINMAXPERCENT] > 0 AND tt.VOLUMEMINMAXWATER = 1, 'Percentage', 'Undefined')
						,[Payload.WaterFixedUnit] = 'Undefined'
						,[Payload.WaterMaxValue] = IIF(tt.VOLUMEMINMAX = 1 AND tt.[VOLUMEMINMAXPERCENT] > 0 AND tt.VOLUMEMINMAXWATER = 1, tt.[VOLUMEMINMAXPERCENT], 100)
						,[Payload.WaterEnableValidation] = IIF(tt.VOLUMEMINMAX = 1 AND tt.[VOLUMEMINMAXPERCENT] > 0 AND tt.VOLUMEMINMAXWATER = 1, 1 , 0)
						,[Payload.SolidItemId] = isnull(sp.[GUID], '00000000-0000-0000-0000-000000000000') 
						,[Payload.SolidItemName] = sp.ProductName
						,[Payload.SolidMinPricingPercentage] = 0
						,[Payload.SolidShowZeroDollarLine] = tt.SOLIDSHOWZEROLINE
						,[Payload.SolidMinValue] = IIF(tt.VOLUMEMINMAX = 0 AND tt.[VOLUMEMINMAXPERCENT] > 0 AND tt.VOLUMEMINMAXSOLIDS = 1, tt.[VOLUMEMINMAXPERCENT], 0)
						,[Payload.SolidThresholdType] = IIF(tt.VOLUMEMINMAX = 1 AND tt.[VOLUMEMINMAXPERCENT] > 0 AND tt.VOLUMEMINMAXSOLIDS = 1, 'Percentage', 'Undefined')
						,[Payload.SolidFixedUnit] = 'Undefined'
						,[Payload.SolidMaxValue] = IIF(tt.VOLUMEMINMAX = 1 AND tt.[VOLUMEMINMAXPERCENT] > 0 AND tt.VOLUMEMINMAXSOLIDS = 1, tt.[VOLUMEMINMAXPERCENT], 100)
						,[Payload.SolidEnableValidation] = IIF(tt.VOLUMEMINMAX = 1 AND tt.[VOLUMEMINMAXPERCENT] > 0 AND tt.VOLUMEMINMAXSOLIDS = 1, 1 , 0)
						,[Payload.ReportAsCutType] = CASE tt.CUTTYPE
														WHEN 0 THEN 'AsPerCutsEntered'
														WHEN 1 THEN 'Water' 
														WHEN 2 THEN 'Oil' 
														WHEN 3 THEN 'Solids' 
														WHEN 4 THEN 'Service' 
														ELSE 'Undefined'
													 END    
						,[Payload.Stream] = CASE 
												WHEN FACILITYCODETYPE =  1 THEN 'Pipeline'
												WHEN FACILITYCODETYPE = 2 THEN 'Terminalling'
												WHEN FACILITYCODETYPE = 3 THEN 'Treating'
												WHEN FACILITYCODETYPE = 4 THEN 'Waste'
												WHEN FACILITYCODETYPE = 5 THEN 'Water'
												WHEN FACILITYCODETYPE = 0 AND tt.TANKTYPEID in (SELECT l.TANKTYPEID FROM lfFacCodes l) THEN 'Landfill'
												WHEN FACILITYCODETYPE = 0 AND tt.TANKTYPEID in (SELECT l.TANKTYPEID FROM ItemFacCodes l) THEN 'Landfill'
												ELSE 'Undefined'
											END
						,[Payload.ProrateService] = tt.PRORATETANK
						,[Payload.ProductionAccountantReport] = tt.PRODACCOUNTANTREPORT
						,[Payload.IncludesWater] = tt.VOLUMEMINMAXWATER
						,[Payload.IncludesOil] = tt.VOLUMEMINMAXOIL
						,[Payload.IncludesSolids] = tt.VOLUMEMINMAXSOLIDS
						,[Payload.IsActive] = convert(bit, 1)
						,[Payload.SearchableId] = ''										  
			FROM [dbo].[TankTypes] tt
			JOIN LegalEntity l ON tt.DATAAREAID = l.DataAreaId
			LEFT JOIN [dbo].[Products_Released] tp ON tp.ITEMNUMBER = tt.TOTALITEMID AND tp.DATAAREAID = tt.DATAAREAID
			LEFT JOIN [dbo].[Products_Released] op ON op.ITEMNUMBER = tt.SOLIDITEMID AND op.DATAAREAID = tt.DATAAREAID
			LEFT JOIN [dbo].[Products_Released] wp ON wp.ITEMNUMBER = tt.WATERITEMID AND wp.DATAAREAID = tt.DATAAREAID
			LEFT JOIN [dbo].[Products_Released] sp ON sp.ITEMNUMBER = tt.SOLIDITEMID AND sp.DATAAREAID = tt.DATAAREAID
			WHERE tt.[GUID] NOT IN ('5665E887-B616-422C-9E49-D0806F347053',
									'FE4C1099-E88F-4E48-BC2B-F9BB77436B74',
									'EBABD1FC-C1C6-47D7-AABF-73D4DF1B797B',
									'71919B52-CEC1-4AA1-8967-A123FBEF0DB5',
									'C28F6C90-EF7F-4755-BA91-77A93D24224E',
									'82660F34-A674-4A98-BFA8-C6A07F04186F')
			AND tt.[GUID] = tts.[GUID]			
			FOR JSON PATH, INCLUDE_NULL_VALUES
		) AS Message, 'ServiceType' AS MessageType, SYSDATETIMEOFFSET() AS GeneratedDate, tts.[GUID] AS EntityId, tts.DATAAREAID, tts.TANKTYPEID AS AxEntityId
	FROM [dbo].[TankTypes] AS tts	
)
,Final
as
(
	SELECT SUBSTRING([Message], 2, LEN([Message]) -2) AS [Message], MessageType, GeneratedDate, EntityId, DataAreaId, AxEntityId, 0 AS Processed, null as ProcessedDate, 'enterprise-entity-updates' as TopicName
	FROM ServiceTypeMessage ax
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
