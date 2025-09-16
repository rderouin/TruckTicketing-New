IF OBJECT_ID(N'dbo.InvoiceExchangeMappings', N'U') IS NOT NULL  
   DROP TABLE dbo.InvoiceExchangeMappings;  
GO

IF OBJECT_ID(N'dbo.InvoiceExchangeMappingsTemp', N'U') IS NOT NULL 
	drop table dbo.InvoiceExchangeMappingsTemp
go

CREATE TABLE [dbo].[InvoiceExchangeMappings](
    [Id] uniqueidentifier not null default newid(),
	CustomerId uniqueidentifier  null,
	[DATAAREAID] [nvarchar](150) NULL,
	[Division]  [nvarchar](150) NULL,
	[CustomerAcct] [nvarchar](150) NULL,
	[Customer] [nvarchar](150) NULL,
	[Platform] [nvarchar](150) not NULL,
	[EdiFieldName] [nvarchar](128) not NULL,
	[Mapping] [nvarchar](500) not NULL
) ON [PRIMARY]
GO

;with maps
as
(
	SELECT [Division], [DATAAREAID], [CustomerID], CustomerAccount, [OrderType], [Customer], [DUNS], [Platform], EdiFieldName, Mapping  

	FROM   
	   (SELECT  [Division], p.[DATAAREAID], a.Id as [CustomerID], p.[CUSTOMERACCOUNT], [OrderType], [Customer], p.[DUNS], [Platform], 
		--pivot columns
		[AFE], [GeneralLedger], [CostCenter], [JobNumber], [AccountingRef], [PONumber], [POLine], [WellIdent], [Contract], [CustomerRef], [PersonnelName], [CostCenterName], [InvoiceNumber], [ItemNumber]
	   FROM [dbo].[EDI_Customer_Additional_Pref] p
	    inner join dbo.AccountMaster a on p.DATAAREAID = a.DATAAREAID and a.[CUSTOMERACCOUNT] = p.CustomerAccount) p  
	UNPIVOT  
	   (Mapping FOR EdiFieldName IN   
		  ([AFE], [GeneralLedger], [CostCenter], [JobNumber], [AccountingRef], [PONumber], [POLine], [WellIdent], [Contract], [CustomerRef], [PersonnelName], [CostCenterName], [InvoiceNumber], [ItemNumber]
	)  
	)AS InvoiceExchangeMappings
)
select newid() as Id, m.*
into dbo.InvoiceExchangeMappingsTemp 
from maps m
where Mapping is not null


/************************ Global Level ************************/
drop table  #Usage;
drop table #maxUsage;
go

select [Platform],  EdiFieldName, Mapping, count(*) cnt
into #Usage
from dbo.InvoiceExchangeMappingsTemp
group by  [Platform],  EdiFieldName, Mapping
-- order by [Platform],  EdiFieldName,  count(*) desc

select [Platform], EdiFieldName, max(cnt) maxUse
into #maxUsage
from #Usage
group by [Platform], EdiFieldName

-- Global Mappings
insert into  [dbo].[InvoiceExchangeMappings] ([Platform], [EdiFieldName], [Mapping])
select u.[Platform], u.EdiFieldName, u.Mapping
from #maxUsage mu
join #Usage u on mu.maxUse = u.cnt 
	and mu.[platform] = u.[platform] 
	and mu.EdiFieldName = u.EdiFieldName

--remove move records from the temp used for global
delete dbo.InvoiceExchangeMappingsTemp
where id in (select t.id
from #maxUsage mu
join #Usage u on mu.maxUse = u.cnt 
	and mu.[platform] = u.[platform] 
	and mu.EdiFieldName = u.EdiFieldName
join dbo.InvoiceExchangeMappingsTemp t 
	on u.[platform] = t.[platform]
	and u.EdiFieldName = t.EdiFieldName
	and u.Mapping = t.Mapping)


/************************ Business Unit  Level ************************/
drop table #Usage;
drop table #maxUsage;
go

select l.Division, [Platform],  EdiFieldName, Mapping, count(*) cnt
into #Usage
from dbo.InvoiceExchangeMappingsTemp t
join dbo.legalEntity l on l.DATAAREAID = t.DATAAREAID
group by l.Division, [Platform],  EdiFieldName, Mapping

select Division, [Platform], EdiFieldName, max(cnt) maxUse
into #maxUsage
from #Usage
group by Division, [Platform], EdiFieldName

insert into  [dbo].[InvoiceExchangeMappings] (Division, [Platform], [EdiFieldName], [Mapping])
select u.Division, u.[Platform], u.EdiFieldName, u.Mapping
from #maxUsage mu
join #Usage u on mu.maxUse = u.cnt 
	and mu.[platform] = u.[platform] 
	and mu.EdiFieldName = u.EdiFieldName
	and mu.Division = u.Division

--remove move records from the temp used for Legal Entity Level
delete dbo.InvoiceExchangeMappingsTemp
where id in (select t.id
from #maxUsage mu
join #Usage u on mu.maxUse = u.cnt 
    and mu.Division = u.Division
	and mu.[platform] = u.[platform] 
	and mu.EdiFieldName = u.EdiFieldName
join dbo.InvoiceExchangeMappingsTemp t 
	on  u.Division = t.Division
	and u.[platform] = t.[platform]
	and u.EdiFieldName = t.EdiFieldName
	and u.Mapping = t.Mapping)	 

/************************ Legal Entity Level ************************/
drop table  #Usage;
drop table #maxUsage;
go

select t.DATAAREAID, l.Division, [Platform],  EdiFieldName, Mapping, count(*) cnt
into #Usage
from dbo.InvoiceExchangeMappingsTemp t
join dbo.legalEntity l on l.DATAAREAID = t.DATAAREAID
group by t.DATAAREAID, l.Division, [Platform],  EdiFieldName, Mapping

select DATAAREAID, Division, [Platform], EdiFieldName, max(cnt) maxUse
into #maxUsage
from #Usage
group by DATAAREAID, Division, [Platform], EdiFieldName

insert into  [dbo].[InvoiceExchangeMappings] (DATAAREAID, Division, [Platform], [EdiFieldName], [Mapping])
select u.DATAAREAID, u.Division, u.[Platform], u.EdiFieldName, u.Mapping
from #maxUsage mu
join #Usage u on mu.maxUse = u.cnt 
	and mu.[platform] = u.[platform] 
	and mu.EdiFieldName = u.EdiFieldName
	and mu.Division = u.Division
	and mu.DATAAREAID = u.DATAAREAID
--remove move records from the temp used for Legal Entity Level
delete dbo.InvoiceExchangeMappingsTemp
where id in (select t.id
from #maxUsage mu
join #Usage u on mu.maxUse = u.cnt 
	and mu.[platform] = u.[platform] 
	and mu.EdiFieldName = u.EdiFieldName
join dbo.InvoiceExchangeMappingsTemp t 
	on  u.Division = t.Division
	and u.DATAAREAID = t.DATAAREAID
	and u.[platform] = t.[platform]
	and u.EdiFieldName = t.EdiFieldName
	and u.Mapping = t.Mapping)	 


	

/************************ Customer Level ************************/

insert into  [dbo].[InvoiceExchangeMappings] (CustomerId, Customer, CustomerAcct, DATAAREAID, Division, [Platform], [EdiFieldName], [Mapping])
select u.CustomerId, u.Customer, u.CustomerAccount, u.DATAAREAID, u.Division, u.[Platform], u.EdiFieldName, u.Mapping
from dbo.InvoiceExchangeMappingsTemp u

select *
from  [dbo].[InvoiceExchangeMappings]
