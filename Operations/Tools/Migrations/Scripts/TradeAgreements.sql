
;WITH TradeAgreementMessages AS
(
	SELECT (SELECT distinct  'Table' AS AccountCode
					, isnull(trim(t.[PRICECUSTOMERGROUPCODE]), '') AS AccountRelation
					, t.[PRICE] AS Amount
					, 'HSTradeAgreementBusinessEvent' AS BusinessEventId
					, t.DATAAREAID AS BusinessEventLegalEntity
					, NEWID() AS CorrelationId
					, [PRICECURRENCYCODE] AS Currency
					, t.DATAAREAID AS DataAreaId
					, IIF([PRICE] <= 0, 100, 0) AS DiscPercent
					, NEWID() AS EventId
					, SYSDATETIMEOFFSET() AS EventTime
					, SYSDATETIMEOFFSET() AS EventTimeIso8601
					, t.[FIXEDPRICECHARGES] AS FixedAmount
					, t.[PRICEAPPLICABLEFROMDATE] AS FromDate
					, 'Table' AS ItemCode
					, t.[ITEMNUMBER] AS ItemRelation
					, isnull(trim(s.TT_UWI), '')  AS LocationId				
					, SYSDATETIMEOFFSET() AS MessageDate
					, 'TradeAgreement' AS MessageType				
					, 'Create' AS Operation
					, isnull(t.[PRICECUSTOMERGROUPCODE], '') AS PriceGroup
					, 1 AS PriceUnit
					, isnull( t.HSCustAccount, '') as CustomerNumber					
					, isnull(trim(s.TT_UWI), '') as SourceLocation
					, case t.SalesQuoteType					
						when 'BD Agreement' then 'BDAgreement'
						when 'Customer Quote' then 'CustomerQuote'
						when 'Commercial Terms' then 'CommercialTerms'
						when 'Tiered Pricing' then 'TieredPricing'
						when 'Facility Base Rate' then 'FacilityBaseRate'	
						else 'Unknown'
					  end AS SalesQuoteType
					, t.[PRICE] as SalesPriceUnit
					, t.[PRICESITEID] AS SiteId
					, 'D365FO' AS Source
					, t.[GUID] AS SourceId
					, '' as ToDate
					,[QUANTITYUNITSYMBOL] AS UnitId
			FROM [dbo].[TradeAgreement] t			
			JOIN [dbo].[Products_Released] p on t.ITEMNUMBER = p.itemnumber and t.DATAAREAID = p.DATAAREAID
			left join dbo.ValidCustomerAccounts v on t.HSCustAccount = v.CUSTOMERACCOUNT and t.DATAAREAID = v.DATAAREAID
			left outer join dbo.SourceLocations s on t.DATAAREAID = s.DATAAREAID and t.SourceLocation = s.UWI
			WHERE t.[GUID] = ta.[GUID] 
			and (t.HSCustAccount is null or v.CUSTOMERACCOUNT is not null) 
				FOR JSON PATH, INCLUDE_NULL_VALUES
			) AS [Message], 'TradeAgreement' AS MessageType, SYSDATETIMEOFFSET() AS GeneratedDate, ta.[GUID] AS EntityId,  ta.DataAreaId, isnull(ta.HSCustAccount, '') + ' -  ' + isnull(ta.ITEMNUMBER, '') AS AxEntityId
		FROM [dbo].[TradeAgreement] ta
)
,Final
as
(
	SELECT SUBSTRING([Message], 2, LEN([Message]) -2) AS [Message], MessageType, GeneratedDate, EntityId, DataAreaId, AxEntityId, 0 AS Processed, null as ProcessedDate, 'd365fo-business-events' as TopicName
	FROM TradeAgreementMessages ax
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