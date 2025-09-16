;
DROP TABLE IF EXISTS [dbo].[SourceLocationTypes];

CREATE TABLE [SourceLocationTypes] (
	Id UNIQUEIDENTIFIER NOT NULL,
	LocationType NVARCHAR(50) NULL,
	BatteryCodeField NVARCHAR(10) NULL,
	Format1 NVARCHAR(50) NULL,
	[RegExValidator] NVARCHAR(255) NULL,
	Country NVARCHAR(50) NULL,
	UWIFormatType NVARCHAR(10) NULL,
	Category NVARCHAR(10) NULL,
	DefaultDeliveryMethod NVARCHAR(10) NULL,
	DefaultDownHoleType NVARCHAR(10) NULL,
	IsActive BIT NULL,
	Name NVARCHAR(50) NULL,
	RequiresApiNumber BIT NULL,
	RequiresCtbNumber BIT NULL,
	RequiresPlsNumber BIT NULL,
	RequiresWellFileNumber BIT NULL,
	ShortFormCode NVARCHAR(50) NULL
)
GO

--ALTER TABLE [dbo].[SourceLocationTypes] ADD CONSTRAINT [DF_SourceLocationTypes_Id] DEFAULT (newid()) FOR [Id]
--GO																	 200-A-100-L/094-A-04-01

INSERT INTO [dbo].[SourceLocationTypes]
([Id], [LocationType], [BatteryCodeField], [Format1], [RegExValidator], [Country], [Category], [DefaultDeliveryMethod], [DefaultDownHoleType], [IsActive], [RequiresApiNumber], [RequiresCtbNumber], [RequiresPlsNumber], [RequiresWellFileNumber])
VALUES
 ('072C10AC-60AE-49FA-BBE8-4DF9C0C276D6' ,'Battery-DLS'					,'BT'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('072C10AC-60AE-49FA-BBE8-4DF9C0C276D7' ,'Battery-NTS'					,'BT'	,'##-##-###-##w#'				, '[A-Z0-9]-[0-9][0-9][0-9]-[A-Z]/[0-9][0-9][0-9]-[A-Z]-[0-9][0-9]-[0-9][0-9]'							,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('24558E7C-42F6-4B79-81F4-8AB45B7221C5' ,'Booster Station'				,'BS'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('94FECAC1-5ED4-4643-9254-04C1A30FBEF7' ,'Compressor Station'			,'CS'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('603C8770-D1F1-4684-8A38-62A552300A7B' ,'Custom Treating Facility'	,'CT'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('706C5F0E-8E9F-4ED2-B469-F7FD515EEA35' ,'Dehydrator'					,'DH'   ,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('3524CCFF-4606-4911-8E07-01D75C887CAE' ,'Gas Gathering System'		,'GS'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('51168C39-EF8F-4B77-B224-ADE00EA9D18E' ,'Gas Plant'					,'GP'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('ECFDD856-3831-4F91-B38E-CAA461996B26' ,'Gathering LSD'				,'GL'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('464222D1-99DD-44C7-A3EA-9D56B8B55C6E' ,'Injection Facility'			,'IF'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('C5EE3A59-E24C-4892-B33F-5CEC9A2E3C36' ,'Landfill Facility'			,'LF'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('B4C6ED1A-E174-44CF-9523-89EB575B0EC7' ,'LNG Plant'					,'LN'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('394D814E-0131-4D56-BAD9-5951A6B6F310' ,'Metering Station'			,'MS'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('8C2AE703-3CA1-4C95-96DC-A94CA116B2FC' ,'Miscellaneous'				,'MC'	,null							, ''																									,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('10CA0E1C-AA9D-4600-9F3C-6937BDC96ABE' ,'Misc. Sale'					,'ML'	,null							, ''																									,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('34C004AE-100B-4039-B072-CEC7E836E1D3' ,'Oil Sands Processing Plant'	,'OS'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('A626166B-B794-4835-86B3-20DBE4C2CC0E' ,'Pipeline'					,'PL'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('2330498A-99A6-4158-A56B-C3987C981E48' ,'Refinery'					,'RF'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('DE7C0362-65EF-4995-B9AB-625468B1473A' ,'Tank Terminal'				,'TM'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('FFC2CE6C-127D-44FD-88AA-E6016213783E' ,'UWI-DLS'						,'WI'	,'#*#/##-##-###-##W#/##'		, '[0-9][A-Z0-9][0-9]/[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]W[0-9]/[0-9][0-9]'				,'CA'	,'Well'		,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('865f0441-b091-43d7-a0e8-bd5d3e4613ff' ,'UWI-NTS'						,'WI'   ,'##*/@-###-@/###-@-##/##'		, '[0-9][0-9][A-Z0-9]/[A-Z]-[0-9][0-9][0-9]-[A-Z]/[0-9][0-9][0-9]-[A-Z]-[0-9][0-9]/[0-9][0-9]'			,'CA'	,'Well'		,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('965EDB6D-7344-4EA8-9CBC-0853F382FD0D' ,'Waste Plant'					,'WP'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('B80D5998-4848-4253-BC95-24D37D164343' ,'Water Source'				,'WS'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('323CC63C-55D5-4AE0-9034-3A40A8AD5929' ,'Fresh/Formation Water Source','WT'	,'##-##-###-##w#'				, '[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9]w[0-9]'												,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('512181D3-0248-4CDD-ADB0-2F3E59E52262' ,'Waste Location'				,'WL'	,null							, ''																									,'CA'	,'Surface'	,'Undefined'	,'Undefined'	,1	,0	,0	,0	,0)
,('28C9736F-EAA8-4881-BB74-780E0085B1C6' ,'CTB/RTB'						,null	,null							, ''																									,'US'	,'Surface'	,'Trucked'		,'Well'			,1	,0	,0	,0	,0)
,('26A96FCB-2E09-4C2F-B290-766672DE249D' ,'Compressor Station'			,null	,null							, ''																									,'US'	,'Surface'	,'Trucked'		,'Pit'			,1	,0	,0	,0	,0)
,('B61AD4A0-D748-40A0-9E5C-9EDA94D177AA' ,'Gas Gathering System'		,null	,null							, ''																									,'US'	,'Surface'	,'Trucked'		,'Pit'			,1	,0	,0	,0	,0)
,('20BA1C6D-64DB-4FF1-8797-40E82F94BC7D' ,'Gas Plant'					,null	,null							, ''																									,'US'	,'Surface'	,'Trucked'		,'Pit'			,1	,0	,0	,0	,0)
,('44725510-E0F7-415A-ADFB-35D8553B4DC0' ,'Other'						,null	,null							, ''																									,'US'	,'Surface'	,'Trucked'		,'Pit'			,1	,0	,0	,0	,0)
,('AF430D1D-9C4C-4E34-B1ED-7C43636F4AAF' ,'Pad'							,null	,null							, ''																									,'US'	,'Surface'	,'Trucked'		,'Well'			,1	,0	,0	,0	,0)
,('81167D60-16A3-49AC-A435-C2C1AD21A9D9' ,'Pipeline'					,null	,null							, ''																									,'US'	,'Surface'	,'Trucked'		,'Pit'			,1	,0	,0	,0	,0)
,('9937EA74-C060-4AA1-87FA-4F21CDA06F58' ,'Refinery'					,null	,null							, ''																									,'US'	,'Surface'	,'Trucked'		,'Pit'			,1	,0	,0	,0	,0)
,('5EBA57BD-FEA9-4C46-A394-53031436F555' ,'Terminal/Station'			,null	,null							, ''																									,'US'	,'Surface'	,'Trucked'		,'Pit'			,1	,0	,0	,0	,0)
,('A78C6BA9-5CC8-4391-88B2-1595879BFF7F' ,'Salt Water Disposal'			,null	,null							, ''																									,'US'	,'Surface'	,'Trucked'		,'Well'			,1	,1	,0	,0	,0)
,('403F800D-327B-4340-8283-88049A3A33E7' ,'Well'						,null	,null							, ''																									,'US'	,'Well'		,'Trucked'		,'Well'			,1	,1	,0	,0	,0)										   
