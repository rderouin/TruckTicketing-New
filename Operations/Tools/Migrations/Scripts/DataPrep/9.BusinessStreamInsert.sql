IF OBJECT_ID(N'dbo.BusinessStreams', N'U') IS NOT NULL  
   DROP TABLE dbo.BusinessStreams;  
GO



Create Table dbo.BusinessStreams(
	Id uniqueidentifier not null Primary Key,
	[Name] nvarchar(10) not null)


insert into dbo.BusinessStreams(Id, Name)
Values('78A68BDB-1C88-42DB-92E8-6968E5AB3272',	'CORP')
,('58D35572-33F2-4348-AFDC-B900C4F184D8',	'ES')
,('977865ab-245f-4fc0-ad70-f925c4c5f3b8',	'FM')
,('8459FB2C-93B7-4CEB-9A81-BC705507571B',	'FS')
,('f87f4dd6-6669-4126-a5ec-4aec0f8b4326',	'MI') --this is one being used