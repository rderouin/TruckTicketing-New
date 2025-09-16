
;WITH ProductMessages AS
(
	SELECT (SELECT 'HSInventTableBusinessEvent' AS BusinessEventId
					,p.DATAAREAID AS BusinessEventProductEntity
					,NEWID() AS CorrelationId
					,p.DATAAREAID AS DataAreaId
					,ISNULL((Select
						case when ARESALESDEFAULTORDERSETTINGSOVERRIDDEN = 'Yes'		
							then CONVERT(bit, 1)
							else  CONVERT(bit, 0)
						end as AreaSalesDeafultOrderSettingsOverridden				   
					,case when pc.ISSALESPROCESSINGSTOPPED = 'Yes'		
							then CONVERT(bit, 1)
							else  CONVERT(bit, 0)
						end as SalesStopped
					,pc.SALESWAREHOUSEID as SalesSite
						from [dbo].Product_Site_Specific_Order_Settings pc
						where pc.DATAAREAID = p.DATAAREAID and CONVERT(nvarchar(50), pc.ItemNumber) = p.ITEMNUMBER
						FOR JSON PATH
					), JSON_QUERY('[]')) AS DefaultOrderSettings
					, NEWID() AS EventId
					,SYSDATETIMEOFFSET() AS EventTime
					,SYSDATETIMEOFFSET() AS EventTimeIso8601
					,p.[ITEMNUMBER] AS ItemId
					,p.[SEARCHNAME] AS ItemName
					,p.[ITEMMODELGROUPID] AS ItemType
					,0 AS MajorVersion
					,SYSDATETIMEOFFSET()  AS MessageDate
					,'Product' AS MessageType
					,0 AS MinorVersion
					,'Create' AS Operation 
					,ISNULL((
						select pc.PRODUCTCATEGORYNAME as CategoryId
							  ,pc.PRODUCTCATEGORYHIERARCHYNAME as CategoryHierarchyId
						from dbo.Product_Category_Assignments pc
						where pc.ProductNumber = pr.ITEMNUMBER
						FOR JSON PATH
					), JSON_QUERY('[]')) AS ProductCategories
					,ISNULL((
						select Substance, WasteCode
						from dbo.ProductVariants pv
						where pv.DATAAREAID = pr.DATAAREAID and pv.ItemNumber = pr.ITEMNUMBER
							FOR JSON PATH
					), JSON_QUERY('[]')) AS ProductVariants
					,[PURCHASEPRICE] AS SalesBasePrice
					,0 AS SalesPriceUnit
					,p.[SALESUNITSYMBOL] AS SalesUnitId					
					,'D365FO' AS Source
					,[GUID] AS SourceId
					,p.[SALESSALESTAXITEMGROUPCODE] AS TaxItemGroupId
				FROM [dbo].[Products_Released] p
				WHERE p.[DATAAREAID] = pr.[DATAAREAID] AND p.[ITEMNUMBER] = pr.[ITEMNUMBER]
				FOR JSON PATH, INCLUDE_NULL_VALUES
			) AS [Message], 'ProductEntity' AS MessageType, SYSDATETIMEOFFSET() AS GeneratedDate, pr.[GUID] AS EntityId, pr.DataAreaId, pr.ITEMNUMBER AS AxEntityId, 0 AS Processed
		FROM [dbo].[Products_Released] pr
)
, Final
as
(
	SELECT SUBSTRING([Message], 2, LEN([Message]) -2) AS [Message], MessageType, GeneratedDate, EntityId, DataAreaId, AxEntityId, 0 AS Processed, null as ProcessedDate, 'd365fo-business-events' as TopicName
	FROM ProductMessages ax
	WHERE Message IS NOT NULL and message like '%DefaultOrderSettings%'
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
