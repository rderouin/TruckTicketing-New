IF OBJECT_ID(N'dbo.[LegalEntity]', N'U') IS NOT NULL 
	drop table [dbo].[LegalEntity]
go

CREATE TABLE [dbo].[LegalEntity](
	[DataAreaId] [nvarchar](4) NULL,
	[GUID] [uniqueIdentifier] NULL,
	[LegalEntityName] [nvarchar](255) NULL,
	[BusinessStream] [nvarchar](255) NULL,
	[Country] [nvarchar](4) NULL,
	[CreditExpiryThreshold] [int] NULL,
	[Division] [nvarchar](10) NULL,
	IsCustomerPrimaryContactRequired bit not null,
	ShowCustomersInTruckTicking bit not null
) ON [PRIMARY]
GO

insert into dbo.[LegalEntity]([DataAreaId], [GUID], [LegalEntityName], [BusinessStream], [Country], [CreditExpiryThreshold], Division, IsCustomerPrimaryContactRequired, ShowCustomersInTruckTicking)
VALUES 
('FLIS', '9E5A6769-7DFC-4E65-B467-EC526BD2F964', 'FLIS', 'Secure Energy (Partnership)', 'CA', 365, 'FS', 0, 0),
('FMFN', '5F58D530-A236-43B3-BA6D-2D2677EDA284', 'FMFN', 'Fort McMurray First Nation (Formerly CRE JV)', 'CA', 365, 'ES', 0, 0),
('MAEC', '9AE43A9E-52B0-4BE2-A09B-009AB1214CD2', 'MAEC', 'Secure Energy (Drilling Services) Inc.', 'CA', 365, 'FM', 0, 0),
('MAEU', '0AAB04D2-BF40-44CD-8FE1-0A65B42D1BB8', 'MAEU', 'SECURE Drilling Services USA LLC', 'US', 365, 'FM', 0, 0),
('MEOW', '2516EA83-DD93-4AF8-9C30-A95BAC183C27', 'MEOW', 'Translation Company', 'CA', 365, 'CORP', 0, 0),
('MUEL', 'A09D5227-77F8-476E-B86A-DA100ECBAD6C', 'MUEL', 'Consolidation & Elimination', 'CA', 365, 'CORP', 0, 0),
('NOPC', '588C3223-16B0-4748-80C9-8F35604F4E6E', 'NOPC', 'CAD Functional Currency Non-Operating Entities', 'CA', 365, 'CORP', 0, 0),
('NOPU', '7430339F-47CA-44FB-A2A3-FE8DF5CCD349', 'NOPU', 'USD Functional Currency Non-Operating Entities', 'US', 365, 'CORP', 0, 0),
('SATI', 'FEFAAC14-B4CC-424D-A075-17DC0E17FDA1', 'SATI', 'Secure Alida Terminal Inc. JV', 'CA', 365, 'MI', 1, 1),
('SESC', '343BF745-CF1F-41D6-BA7A-2240266BB8A1', 'SESC', 'Secure Energy Services Inc.', 'CA', 365, 'MI', 1, 1),
('SESU', '2AC7845F-4096-4E04-B1E1-70C39AAE131A', 'SESU', 'Secure Energy USA LLC', 'US', 365, 'MI', 1, 1),
('TESI', 'FC960028-EB22-46C6-AD8D-FC6867F4798D', 'TESI', 'Tervita Environmental Services Inc.', 'US', 365, 'MI', 1, 1);


select *
--into dbo.ZZ_legalentity
from dbo.legalentity



