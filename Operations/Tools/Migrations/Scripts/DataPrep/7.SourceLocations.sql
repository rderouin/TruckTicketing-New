
DROP TABLE IF EXISTS [dbo].[SourceLocations];

--create copy of uwi table for our processing

SELECT a.*
INTO [dbo].[SourceLocations] 
FROM [dbo].[uwi] a

ALTER TABLE [dbo].[SourceLocations]
ADD SOURCELOCATIONTYPEID UNIQUEIDENTIFIER;

ALTER TABLE [dbo].[SourceLocations]
ADD SOURCELOCATIONTYPE NVARCHAR(50);

ALTER TABLE [dbo].[SourceLocations]
ADD ID UNIQUEIDENTIFIER NULL;

ALTER TABLE [dbo].[SourceLocations]
ADD PROVINCEORSTATE NVARCHAR(50);

ALTER TABLE [dbo].[SourceLocations]
ADD PROVINCEORSTATESTRING NVARCHAR(50);

ALTER TABLE [dbo].[SourceLocations]
ADD LICENSENUMBER NVARCHAR(50) NULL;

ALTER TABLE [dbo].[SourceLocations]
ADD APINUMBER NVARCHAR(50);

ALTER TABLE [dbo].[SourceLocations]
ADD CTBNUMBER NVARCHAR(50);

ALTER TABLE [dbo].[SourceLocations]
ADD WELLFILENUMBERBAK NVARCHAR(50);

ALTER TABLE [dbo].[SourceLocations]
ALTER column WELLFILENUMBER nvarchar(30) NULL

GO

--set guid values on the newly added column
UPDATE [dbo].[SourceLocations] SET ID = [GUID];

ALTER TABLE [dbo].[SourceLocations]
alter column ID UNIQUEIDENTIFIER NOT NULL;
GO
ALTER TABLE [dbo].[SourceLocations]
ADD PRIMARY KEY(ID);
GO

/****** Object:  Index [NonClusteredIndex-20230201-102849]    Script Date: 2/1/2023 10:58:53 AM ******/
CREATE NONCLUSTERED INDEX [NonClusteredIndex-Sourcelocation-owner] ON [dbo].[SourceLocations]
(
	[DATAAREAID] ASC,
	[OWNER] ASC,
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


SET ANSI_PADDING ON
GO

/****** Object:  Index [NonClusteredIndex-20230201-102715]    Script Date: 2/1/2023 10:58:36 AM ******/
CREATE NONCLUSTERED INDEX [NonClusteredIndex-sourcelocation-uwi] ON [dbo].[SourceLocations]
(
	[DATAAREAID] ASC,
	[UWI] ASC,
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO



--Set Wells with matching formats
;with Wells
as
(
	select s.GUID as SourceLocationId, s.UWI, s.TT_UWI, s.SOBATTERYCODE 
	from dbo.SourceLocations s 
	where SUBSTRING(s.SOBATTERYCODE, 3, 2) in ('WI')
	and  s.countryregionid = 'CAN'  
	and s.UWIALIAS in (select GSL_UWI from dbo.ZZ_Geoscout_Wells where cleanUwi = UWIALIAS)
)
update dbo.SourceLocations
set SOURCELOCATIONTYPEID = t.Id
--select *
from dbo.SourceLocations s
join Wells  w on s.GUID = w.SourceLocationId
join SourceLocationTypes t on  t.BatterycodeField = 'WI' and w.TT_UWI like t.RegExValidator



--set wells that are marked as BT with matching format
;with Wells
as
(
	select s.GUID as SourceLocationId, s.UWI, s.TT_UWI 
	from dbo.SourceLocations s 
	where SUBSTRING(s.SOBATTERYCODE, 3, 2) in ('BT')
	and  s.countryregionid = 'CAN' 
    and s.UWIALIAS in (select GSL_UWI from dbo.ZZ_Geoscout_Wells where cleanUwi = UWIALIAS)
)
update dbo.SourceLocations
set SOURCELOCATIONTYPEID = t.Id
--select *
from dbo.SourceLocations s
join Wells  w on s.GUID = w.SourceLocationId
join SourceLocationTypes t on  t.BatterycodeField = 'WI' and w.TT_UWI like t.RegExValidator

--set Batteries that are BT and not in the Geoscout data
;with Wells
as
(
	select s.GUID as SourceLocationId, s.UWI, s.TT_UWI 
	from dbo.SourceLocations s 
	where SUBSTRING(s.SOBATTERYCODE, 3, 2) in ('BT')
	and  s.countryregionid = 'CAN' 
	and s.UWIALIAS not in (select GSL_UWI from dbo.ZZ_Geoscout_Wells where cleanUwi = UWIALIAS)
)
update dbo.SourceLocations
set SOURCELOCATIONTYPEID = t.Id
--select *
from dbo.SourceLocations s
join Wells  w on s.GUID = w.SourceLocationId
join SourceLocationTypes t on  t.BatterycodeField = 'BT' and w.TT_UWI like t.RegExValidator



--Set remaining canada source location types based on Battery code digit 3-4

--this will pickup UWI that Match the format but are not in geo scout or they would have been pick up by one 
--of the previous queries
--UPDATE [dbo].[SourceLocations]
--SET SOURCELOCATIONTYPEID = t.Id, 
--	SOURCELOCATIONTYPE =  t.locationtype 	
--SELECT s.ID, t.Id as SourceLocationtypeId, t.LocationType, t.LocationType, t.BatteryCodeField, s.TT_UWI	
--FROM [dbo].[SourceLocations] s
--JOIN [dbo].[SourceLocationTypes] t on t.BatteryCodeField = SUBSTRING(s.SOBATTERYCODE, 3, 2) 
--	and s.TT_UWI like t.RegExValidator
--WHERE s.countryregionid = 'CAN' 
--AND s.SOBATTERYCODE IS NOT NULL 
--AND len(s.SOBATTERYCODE) >= 4
--AND s.SOURCELOCATIONTYPEID IS NULL
--and t.id is not null

----------------------"MISC" CA-----------------------------------
--1124
--UPDATE [dbo].[SourceLocations]
--SET SOURCELOCATIONTYPEID = t.Id, 
--	SOURCELOCATIONTYPE = t.LocationType
----SELECT s.ID, t.Id as SourceLocationtypeId, t.LocationType
--FROM [dbo].[SourceLocations] s
--JOIN [SourceLocationTypes] t on [dbo].[RemoveHiddenChars](t.LocationType) = 'Miscellaneous' AND t.Country = 'CA'
--AND s.COUNTRYREGIONID = 'CAN'
--AND s.SOURCELOCATIONTYPEID IS NULL --update records we havent included in updates above


	  	   	   	  	 
--------------------"CTB", "RTB" or "Battery" UWI's--------------------
--278
UPDATE [dbo].[SourceLocations]
SET SOURCELOCATIONTYPEID = t.Id, 
	SOURCELOCATIONTYPE = t.LocationType
--SELECT s.ID, t.Id as SourceLocationtypeId, t.LocationType	
FROM [dbo].[SourceLocations] s
JOIN [SourceLocationTypes] t on [dbo].[RemoveHiddenChars](t.LocationType) = 'CTB/RTB' AND t.Country = 'US'
WHERE TRIM(UWI) LIKE '%ctb%' OR TRIM(UWI) LIKE '%rtb%' OR TRIM(UWI) LIKE '%battery%'
AND (s.COUNTRYREGIONID = 'USA' OR s.DATAAREAID = 'SESU') 
AND s.SOURCELOCATIONTYPEID IS NULL

--------------------"Compressor Station" , "Comp Station" or "Comp"--------------------
--37
UPDATE [dbo].[SourceLocations]
SET SOURCELOCATIONTYPEID = t.Id, 
	SOURCELOCATIONTYPE = t.LocationType
--SELECT s.ID, t.Id as SourceLocationtypeId, t.LocationType	
FROM [dbo].[SourceLocations] s
JOIN [SourceLocationTypes] t on [dbo].[RemoveHiddenChars](t.LocationType) = 'Compressor Station' AND t.Country = 'US'
WHERE TRIM(UWI) LIKE '%comp%'
AND (s.COUNTRYREGIONID = 'USA' OR s.DATAAREAID = 'SESU') 
AND s.SOURCELOCATIONTYPEID IS NULL

--------------------"Gather"--------------------
--6
UPDATE [dbo].[SourceLocations]
SET SOURCELOCATIONTYPEID = t.Id, 
	SOURCELOCATIONTYPE = t.LocationType
--SELECT s.ID, t.Id as SourceLocationtypeId, t.LocationType, salesid	
FROM [dbo].[SourceLocations] s
JOIN [SourceLocationTypes] t on [dbo].[RemoveHiddenChars](t.LocationType) = 'Gas Gathering System' AND t.Country = 'US'
WHERE TRIM(UWI) LIKE '%gathering%'
AND (s.COUNTRYREGIONID = 'USA' OR s.DATAAREAID = 'SESU') 
AND s.SOURCELOCATIONTYPEID IS NULL

--------------------"Plant"--------------------
--15
UPDATE [dbo].[SourceLocations]
SET SOURCELOCATIONTYPEID = t.Id, 
	SOURCELOCATIONTYPE = t.LocationType
--SELECT s.ID, t.Id as SourceLocationtypeId, t.LocationType
FROM [dbo].[SourceLocations] s
JOIN [SourceLocationTypes] t on [dbo].[RemoveHiddenChars](t.LocationType) = 'Gas Plant' AND t.Country = 'US'
WHERE TRIM(UWI) LIKE '%plant%'
AND (s.COUNTRYREGIONID = 'USA' OR s.DATAAREAID = 'SESU') 
AND s.SOURCELOCATIONTYPEID IS NULL

--------------------"Pad"--------------------
--238
UPDATE [dbo].[SourceLocations]
SET SOURCELOCATIONTYPEID = t.Id, 
	SOURCELOCATIONTYPE = t.LocationType
--SELECT s.ID, t.Id as SourceLocationtypeId, t.LocationType
FROM [dbo].[SourceLocations] s
JOIN [SourceLocationTypes] t on [dbo].[RemoveHiddenChars](t.LocationType) = 'Pad' AND t.Country = 'US'
WHERE TRIM(UWI) LIKE '%pad%'
AND (s.COUNTRYREGIONID = 'USA' OR s.DATAAREAID = 'SESU') 
AND s.SOURCELOCATIONTYPEID IS NULL

--------------------"Pipeline"--------------------
--7
UPDATE [dbo].[SourceLocations]
SET SOURCELOCATIONTYPEID = t.Id, 
	SOURCELOCATIONTYPE = t.LocationType
--SELECT s.ID, t.Id as SourceLocationtypeId, t.LocationType
FROM [dbo].[SourceLocations] s
JOIN [SourceLocationTypes] t on [dbo].[RemoveHiddenChars](t.LocationType) = 'Pipeline' AND t.Country = 'US'
WHERE TRIM(UWI) LIKE '%pipeline%'
AND (s.COUNTRYREGIONID = 'USA' OR s.DATAAREAID = 'SESU') 
--AND SALESID NOT IN ('CSO175735', 'CSO178046', 'CSO175876') --per Cassie these are "Gas Gathering System" source location type
AND s.SOURCELOCATIONTYPEID IS NULL

--------------------"Refinery"--------------------------------
--0
--UPDATE [dbo].[SourceLocations]
--SET SOURCELOCATIONTYPEID = t.Id, 
--	SOURCELOCATIONTYPE = t.LocationType
----SELECT s.ID, t.Id as SourceLocationtypeId, t.LocationType
--FROM [dbo].[SourceLocations] s
--JOIN [SourceLocationTypes] t on [dbo].[RemoveHiddenChars](t.LocationType) = 'Refinery' AND t.Country = 'US'
--WHERE TRIM(UWI) LIKE '%refinery%'
--AND (s.COUNTRYREGIONID = 'USA' OR s.DATAAREAID = 'SESU') 
--AND s.SOURCELOCATIONTYPEID IS NULL

------------------"Terminal" or "Term"--------------------------------
--8
UPDATE [dbo].[SourceLocations]
SET SOURCELOCATIONTYPEID = t.Id, 
	SOURCELOCATIONTYPE = t.LocationType
--SELECT s.ID, t.Id as SourceLocationtypeId, t.LocationType
FROM [dbo].[SourceLocations] s
JOIN [SourceLocationTypes] t on [dbo].[RemoveHiddenChars](t.LocationType) = 'Terminal/Station' AND t.Country = 'US'
WHERE TRIM(UWI) LIKE '%terminal%' OR TRIM(UWI) LIKE '%term%'
AND (s.COUNTRYREGIONID = 'USA' OR s.DATAAREAID = 'SESU') 
AND s.SOURCELOCATIONTYPEID IS NULL

----------------------"SWD"-----------------------------------
--94
UPDATE [dbo].[SourceLocations]
SET SOURCELOCATIONTYPEID = t.Id, 
	SOURCELOCATIONTYPE = t.LocationType
--SELECT s.ID, t.Id as SourceLocationtypeId, t.LocationType
FROM [dbo].[SourceLocations] s
JOIN [SourceLocationTypes] t on [dbo].[RemoveHiddenChars](t.LocationType) = 'Salt Water Disposal' AND t.Country = 'US'
WHERE TRIM(UWI) LIKE '%SWD%'
AND (s.COUNTRYREGIONID = 'USA' OR s.DATAAREAID = 'SESU') 
--AND SALESID NOT IN ('CSO175044') --per Cassie "Gas Gathering System" source location type
AND s.SOURCELOCATIONTYPEID IS NULL

----------------------"WELL"-----------------------------------
--26
UPDATE [dbo].[SourceLocations]
SET SOURCELOCATIONTYPEID = t.Id, 
	SOURCELOCATIONTYPE = t.LocationType
--SELECT s.ID, t.Id as SourceLocationtypeId, t.LocationType
FROM [dbo].[SourceLocations] s
JOIN [SourceLocationTypes] t on [dbo].[RemoveHiddenChars](t.LocationType) = 'Well' AND t.Country = 'US'
WHERE TRIM(UWI) LIKE '%WELL%'
AND (s.COUNTRYREGIONID = 'USA' OR s.DATAAREAID = 'SESU') 
AND s.SOURCELOCATIONTYPEID IS NULL




----------------------"OTHER" US-----------------------------------
--3225
UPDATE [dbo].[SourceLocations]
SET SOURCELOCATIONTYPEID = t.Id, 
	SOURCELOCATIONTYPE = t.LocationType
--SELECT s.ID, t.Id as SourceLocationtypeId, t.LocationType
FROM [dbo].[SourceLocations] s
JOIN [SourceLocationTypes] t on [dbo].[RemoveHiddenChars](t.LocationType) = 'Other' AND t.Country = 'US'
AND s.COUNTRYREGIONID = 'USA'
AND s.SOURCELOCATIONTYPEID IS NULL --update records we havent included in updates above





--------------------------UPDATE CA PROVINCE-------------------------
UPDATE [dbo].[SourceLocations] 
SET ProvinceOrState = SUBSTRING(dbo.RemoveHiddenChars(SOBATTERYCODE), 1, 2), 
	ProvinceOrStateString = 
		CASE SUBSTRING(SOBATTERYCODE, 1, 2)
			WHEN 'AB' THEN 'Alberta'
			WHEN 'BC' THEN 'British Columbia'
			WHEN 'MB' THEN 'Manitoba'
			WHEN 'SK' THEN 'Saskatchewan'
		END
FROM [dbo].[SourceLocations] a
WHERE a.COUNTRYREGIONID = 'CAN' 
AND SOBATTERYCODE is not null
AND TRIM(SOBATTERYCODE) != '' 
AND SUBSTRING(dbo.RemoveHiddenChars(SOBATTERYCODE), 1, 2) IN ('AB', 'BC'  , 'MB' , 'SK')
															 
-------------------------UPDATE US STATE-------------------------										 
UPDATE [dbo].[SourceLocations]							  
SET ProvinceOrState =
		CASE   substring(dbo.ReplaceNonNumericChars(SOBATTERYCODE), 1, 2)
			WHEN '33' THEN 'ND'
			WHEN '25' THEN 'MT'
	    END,
	ProvinceOrStateString =
		CASE substring(dbo.ReplaceNonNumericChars(SOBATTERYCODE), 1, 2)
			WHEN '33' THEN 'North Dakota'
			WHEN '25' THEN 'Montana'
		END,
	APINumber = dbo.ReplaceNonNumericChars(SOBATTERYCODE) 
FROM [dbo].[SourceLocations] a
WHERE a.COUNTRYREGIONID = 'USA' 
AND SOBATTERYCODE is not null
AND TRIM(SOBATTERYCODE)!= '' 
AND dbo.ReplaceNonNumericChars(SOBATTERYCODE) LIKE '25%' OR dbo.ReplaceNonNumericChars(SOBATTERYCODE) LIKE '33%' AND LEN(TRIM(SOBATTERYCODE)) >= 10

-------------------------EXTRACT CTB NUMBER-------------------------
UPDATE [dbo].[SourceLocations]
SET CTBNumber = dbo.RemoveHiddenChars(a.WellFileNumber),
	WELLFILENUMBERBAK = WELLFILENUMBER,
	WELLFILENUMBER = NULL
FROM [dbo].[SourceLocations] a
WHERE a.COUNTRYREGIONID = 'USA' AND a.ProvinceOrState = 'ND' AND LEN(dbo.RemoveHiddenChars(a.WellFileNumber)) = 6

-------------------------REMOVE BAD WELL FILE NUMBERS - BACK UP TO WELLFILENUMBERBAK-------------------------
UPDATE [dbo].[SourceLocations]
SET WELLFILENUMBERBAK = WELLFILENUMBER,
	WELLFILENUMBER = NULL
FROM [dbo].[SourceLocations] a
WHERE a.COUNTRYREGIONID = 'USA' AND ((a.ProvinceOrState = 'MT' AND LEN(dbo.RemoveHiddenChars(a.WellFileNumber)) = 8)
	OR (a.ProvinceOrState = 'ND' AND LEN(dbo.RemoveHiddenChars(a.WellFileNumber)) = 5))

---------------------------MAP US ND STATE-------------------------
;WITH uwiTemp AS
(
	SELECT   a.[NDICLOCATION]
			,dbo.CleanNumericWithDashes(a.[NDICLOCATION]) AS cleanNDIC
			,CONVERT(INT, dbo.ReplaceNonNumericChars(SUBSTRING(dbo.CleanNumericWithDashes(a.NDICLOCATION), CHARINDEX('-',  dbo.CleanNumericWithDashes(a.NDICLOCATION))+1, CHARINDEX('-',  dbo.CleanNumericWithDashes(a.NDICLOCATION), CHARINDEX('-',  dbo.CleanNumericWithDashes(a.NDICLOCATION))+1) - CHARINDEX('-',  dbo.CleanNumericWithDashes(a.NDICLOCATION))-1))) as MiddleSegment 
			,a.DataAreaId, a.[CONTRACTOPERATED], a.[OWNER], a.uwi, a.uwialias, a.SOBATTERYCODE, a.UWIFieldName
	FROM [dbo].[SourceLocations] a
	WHERE a.COUNTRYREGIONID = 'USA' AND a.ProvinceOrState IS NULL 
		AND dbo.CleanNumericWithDashes(a.[NDICLOCATION]) IS NOT NULL 
		AND dbo.CleanNumericWithDashes(a.[NDICLOCATION]) <> ''
		AND dbo.CountOccurrencesOfString(a.[NDICLOCATION], '-') = 2
)
UPDATE [dbo].[SourceLocations] 
SET	[PROVINCEORSTATE] = iif(middleSegment <= 164 and middleSegment >= 129, 'ND', NULL),
	[PROVINCEORSTATESTRING] = iif(middleSegment <= 164 and middleSegment >= 129, 'North Dakota', NULL)
FROM uwiTemp u
JOIN [dbo].[SourceLocations] a ON u.DATAAREAID = a.DATAAREAID AND u.UWI = a.UWI   -- u.OWNER = a.OWNER and u.CONTRACTOPERATED = a.CONTRACTOPERATED 
WHERE middleSegment <= 164 AND middleSegment >= 129	

-------------------------UPDATE MISSING PROVINCES/STATES-------------------------
--1371
UPDATE [dbo].[SourceLocations] 
SET [PROVINCEORSTATE] = a.[PROVINCEORSTATE]
FROM [dbo].[MissingStatesAndProvinces] a 
JOIN [dbo].[SourceLocations] b ON b.UWI = a.UWI and b.DATAAREAID = a.DATAAREAID
WHERE b.[PROVINCEORSTATE] IS NULL -- only set when column on record is null

-------------------------UPDATE LICENSES-------------------------
--1477
UPDATE [dbo].[SourceLocations] 
SET [LICENSENUMBER] = a.[LICENSENUMBER]
FROM [dbo].[MissingLicenses] a 
JOIN [dbo].[SourceLocations] b ON  b.UWI = a.UWI and b.DATAAREAID = a.DATAAREAID
WHERE b.[LICENSENUMBER] IS NULL -- only set when column on record is null

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 
