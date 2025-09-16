USE [TruckTicketDataMigration]
GO

---- CREATE FUNCTIONS ----
CREATE OR ALTER FUNCTION dbo.ReplaceNonNumericChars (@string VARCHAR(5000))
RETURNS VARCHAR(1000)
AS
BEGIN
	SET @string = REPLACE(@string , ' ,' , '.')
	SET @string = (SELECT SUBSTRING(@string , v.number , 1)
	FROM master..spt_values v
	WHERE v.type = 'P'
	AND v.number BETWEEN 1 AND LEN(@string)
	AND (SUBSTRING(@string , v.number , 1) LIKE '[0-9]'
	OR SUBSTRING(@string , v.number , 1) LIKE '[.]')
	ORDER BY v.number
	FOR	XML PATH(''))
	RETURN @string
END
GO

CREATE OR ALTER FUNCTION dbo.CleanNumericWithDashes (@string VARCHAR(5000))
RETURNS VARCHAR(1000)
AS
BEGIN
	SET @string = REPLACE(@string , ' ,' , '.')
	SET @string = (SELECT SUBSTRING(@string , v.number , 1)
	FROM master..spt_values v
	WHERE v.type = 'P'
	AND v.number BETWEEN 1 AND LEN(@string)
	AND (SUBSTRING(@string , v.number , 1) LIKE '[0-9]'
	OR SUBSTRING(@string , v.number , 1) LIKE '[-]')
	ORDER BY v.number
	FOR
	XML PATH(''))
	RETURN @string
END
GO

CREATE OR ALTER FUNCTION dbo.RemoveHiddenChars (@string VARCHAR(5000))
RETURNS VARCHAR(1000)
AS
BEGIN
	return LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(@string , CHAR(10) ,
	CHAR(32)) ,CHAR(13) , CHAR(32)) ,CHAR(160) , CHAR(32)) ,CHAR(9) ,CHAR(32))))
END
GO

CREATE OR ALTER FUNCTION dbo.CountOccurrencesOfString(@searchString nvarchar(max) , @searchTerm nvarchar(max))
RETURNS INT
AS
BEGIN
	return (LEN(@searchString)-LEN(REPLACE(@searchString ,@searchTerm ,'')))/LEN(@searchTerm)
END
GO



--Enables the necessary tooling to use regex this only need run once but puting here to keep in 
--to disable it, set the bits to 0 and run the statements below

--EXEC sp_configure 'Ole Automation Procedures';  --this line shows you the current values

--sp_configure 'show advanced options', 1;
--GO
--RECONFIGURE;
--GO
--sp_configure 'Ole Automation Procedures', 1;
--GO
--RECONFIGURE;
--GO
---------------------------------------------------------------------------------

CREATE OR ALTER FUNCTION dbo.IsValidPattern
(
    @Target varchar(100),
	@pattern varchar(4000)
)
RETURNS bit
AS
BEGIN     
    DECLARE @Result bit
    DECLARE @objRegexExp INT
    EXEC sp_OACreate 'VBScript.RegExp', @objRegexExp OUT
    EXEC sp_OASetProperty @objRegexExp, 'Pattern', @pattern
    EXEC sp_OASetProperty @objRegexExp, 'IgnoreCase', 1
    EXEC sp_OASetProperty @objRegexExp, 'MultiLine', 0
    EXEC sp_OASetProperty @objRegexExp, 'Global', false
    EXEC sp_OASetProperty @objRegexExp, 'CultureInvariant', true
    EXEC sp_OAMethod @objRegexExp, 'Test', @Result OUT, @Target
    EXEC sp_OADestroy @objRegexExp
    RETURN @Result
END

go


CREATE OR ALTER FUNCTION dbo.isValidEmailFormat
(
    @Target varchar(100)
)
RETURNS bit
AS
BEGIN
    DECLARE @pattern varchar(4000)
	SET @Target = trim(@Target)
	SET @pattern = '^((([a-z]|\d|[!#\$%&''''\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&''''\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-||_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+([a-z]+|\d|-|\.{0,1}|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])?([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))'
    
    RETURN dbo.IsValidPattern(@Target, @pattern)
END

go


CREATE OR ALTER FUNCTION dbo.isValidPhoneFormat
(
    @Target varchar(100)
)
RETURNS bit
AS
BEGIN
    DECLARE @pattern varchar(4000)
	DECLARE @result bit = 0;
  
	SET @pattern = '(1\s?)?(\d{3}|\(\d{3}\))[\s\-]?\d{3}[\s\-]?\d{4}'	
    SET @result = dbo.IsValidPattern(@Target, @pattern)

	RETURN @result;

END

go




---- DROP TABLES IF THEY EXIST ----
IF OBJECT_ID(N'dbo.DataMigrationMessages', N'U') IS NOT NULL  
   DROP TABLE dbo.DataMigrationMessages;  
GO

IF OBJECT_ID(N'dbo.MissingStatesAndProvinces', N'U') IS NOT NULL  
   DROP TABLE dbo.MissingStatesAndProvinces;  
GO

IF OBJECT_ID(N'dbo.MissingLicenses', N'U') IS NOT NULL  
   DROP TABLE dbo.MissingLicenses;  
GO

IF OBJECT_ID(N'dbo.AccountMaster', N'U') IS NOT NULL  
   DROP TABLE dbo.AccountMaster;  
GO

---- CREATE TABLES ----
CREATE TABLE dbo.[DataMigrationMessages] (
	Id INT IDENTITY(1,1) NOT NULL Primary Key,
	[Message] NVARCHAR(MAX) NULL ,
	MessageType NVARCHAR(50) NULL ,
	GeneratedDate DATETIMEOFFSET(7) NULL ,
	EntityId uniqueidentifier NULL ,
	DataAreaId nvarchar(10) NULL,
	AxEntityId NVARCHAR(50) NULL ,
	Processed BIT NULL ,
	ProcessedDate DATETIMEOFFSET(7) NULL,
	TopicName nvarchar(50) not null
)
GO


CREATE TABLE [dbo].[MissingLicenses](
	ID INT IDENTITY(1,1) NOT NULL,	
	UWI NVARCHAR(50) NOT NULL,
	SALESID NVARCHAR(50) NOT NULL,
	LICENSENUMBER NVARCHAR(50) NOT NULL,
	UWIALIAS NVARCHAR(25) NULL,
	DATAAREAID NVARCHAR(4) NULL,
	UWI_UWIALIAS_DATAAREAID NVARCHAR(250) NULL,
)
GO

CREATE TABLE [dbo].[MissingStatesAndProvinces](
	ID INT IDENTITY(1,1) NOT NULL,
	SALESID NVARCHAR(50) NOT NULL,	
	UWI NVARCHAR(50) NOT NULL,
	COUNTRYREGIONID NVARCHAR(50) NULL,
	PROVINCEORSTATE NVARCHAR(50) NOT NULL,
	UWIALIAS NVARCHAR(25) NULL,
	DATAAREAID NVARCHAR(4) NULL,
	UWI_UWIALIAS_DATAAREAID NVARCHAR(250) NULL,
)
GO

CREATE TABLE [dbo].[AccountMaster](
    [TTAccountID] [int] IDENTITY(1000000,1) NOT NULL Primary Key,
	[ID] [uniqueidentifier] not null,
	[SourceTable] [nvarchar](100),
	[DATAAREAID] [nvarchar](4) NULL,
	[CUSTOMERACCOUNT] [nvarchar](20) NULL,
	[PARTYNUMBER] [nchar](255) NULL,
	[PARTYTYPE] [varchar](12) NULL,
	[ORGANIZATIONNAME] [nvarchar](100) NULL,	
	[NAMEALIAS] [nvarchar](20) NULL,
	[CUSTOMERGROUPID] [nvarchar](255) NULL,
	IsActiveCustomer bit null,
	IsGenerator bit null,
	IsTruckingCo bit Null,
	Is3rdParty bit null	
) ON [PRIMARY]
GO

--CREATE NONCLUSTERED INDEX AccountMasterNonClusteredIndex
--ON [dbo].[AccountMaster] ([SourceTable],[DATAAREAID],[CUSTOMERACCOUNT])
--INCLUDE ([ID],[ORGANIZATIONNAME])
--GO