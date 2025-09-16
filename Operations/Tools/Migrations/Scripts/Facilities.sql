
;WITH FacilityMessages AS
(
	SELECT 	(SELECT  'HSInventSiteBusinessEvent' AS BusinessEventId 
					,NEWID() AS CorrelationId 
					,SUBSTRING(TRIM(ISNULL([PRIMARYADDRESSCOUNTRYREGIONID], 'CA')), 1,2) AS Country 
					,TRIM([DATAAREAID]) AS DataAreaId 
					,NEWID() AS EventId 
					,SYSDATETIMEOFFSET() AS EventTime 
					,s.ADMINEMAIL AS HSAdminEmail 
					,[CALENDARID] AS HSCaLENdarId 
					,[FACILITYREGULATORYCODEPIPELINE] AS HSFacilityRegulatoryCodePipeline 
					,[FACILITYREGULATORYCODETERMINALLING] AS HSFacilityRegulatoryCodeTerminalling 
					,[FACILITYREGULATORYCODETREATING] AS HSFacilityRegulatoryCodeTreating 
					,[FACILITYREGULATORYCODEWASTE] AS HSFacilityRegulatoryCodeWASte 
					,[FACILITYREGULATORYCODEWATER] AS HSFacilityRegulatoryCodeWater 
					,case 
						when CHARINDEX('LF', s.SiteID) > 1  then 'Lf'
						when CHARINDEX('FST', s.SiteID) > 1  then 'Fst'
						when CHARINDEX('CAV', s.SiteID) > 1  then 'Cavern'
						when CHARINDEX('SWD', s.SiteID) > 1  then 'Swd'
					 end AS FacilityType 
					,[INVOICECONTACT] AS HSInvoiceContact 						
					,s.OVERTONNAGEANALYTICALCONTACT AS HSOvertonnageAnalyticalContract 
					,s.PRODUCTIONACCOUNTANTCONTACT AS HSProductionAccountantContact 
					,s.TAXGROUP AS HSTaxGroup 
					,s.UWI AS HSUWI 
					,convert(bit, 1) AS IsActive 
					,0 AS MajorVersion 
					,SYSDATETIMEOFFSET() AS MessageDate 
					,'Facility' AS MessageType 
					,0 AS MinorVersion 
					,'Create' AS Operation 
					,TRIM(SUBSTRING(ISNULL([PRIMARYADDRESSSTATEID], '  '),1,2)) AS Province 
					,[SITEID] AS SiteId 
					,case 
						when name is null or TRIM([NAME]) = '' then [SITEID]
						else TRIM([NAME])
					 end AS SiteName
					,s.[UWI] AS FacilityLocationCode 
					,'D365FO' AS Source 					
					,[GUID] AS SourceId 	
				FROM [dbo].[Sites] s 
				WHERE s.siteid = sd.siteid and s.dataareaid = sd.dataareaid
				FOR JSON PATH, INCLUDE_NULL_VALUES
			) AS [Message], 'FacilityEntity' AS MessageType, SYSDATETIMEOFFSET() AS GeneratedDate, sd.[GUID] AS EntityId, sd.DataAreaId, SiteId AS AxEntityId
		FROM [dbo].[Sites] sd
		
)
,Final
as
(
	SELECT SUBSTRING([Message], 2, LEN([Message]) -2) AS [Message], MessageType, GeneratedDate, EntityId, DataAreaId, AxEntityId, 0 AS Processed, null as ProcessedDate, 'd365fo-business-events' as TopicName
	FROM FacilityMessages ax
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




