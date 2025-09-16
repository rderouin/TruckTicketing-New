--select *
--into [TruckTicketDataMigration].dbo.[ZZ_SubstancesOG]
--from [AX_SECURE_PRD_REPLICA].[dbo].[ECORESSIZE]
--where Recid in (select distinct SUBSTANCERECID
--from SCA)

create table dbo.SubstanceOldNewMap (
	Id int identity(1,1) not null Primary key,
	SubstanceId uniqueidentifier not null,
	RecId bigint not null
)



;with Test
as
(
	select c.Id, o.RECID, c.WasteCode New_WasteCode, o.SESERCBNUMBER OG_WasteCode, c.Substance New_Name,  o.name OG_Name
	from dbo.SubstancesAndWasteCodes c
	  join ZZ_SubstancesOG o
		on  CHARINDEX( c.WasteCode , o.SESERCBNUMBER) >= 1
	where o.RECID not in (select x.RecId from dbo.SubstanceOldNewMap x)
	
		--and c.WasteCode = 'SOILRO'
		Order By  c.WasteCode, id

)
insert into  dbo.SubstanceOldNewMap(SubstanceId, RecId)
select t.Id, RECID
from test t
Order by t.New_WasteCode

select *
from dbo.SubstancesAndWasteCodes

'513A0079-2C0A-4983-B492-025ED3669BFB'

insert into  dbo.SubstanceOldNewMap(SubstanceId, RecId)
values ('513A0079-2C0A-4983-B492-025ED3669BFB',	5637146330


)


select *
from dbo.SubstancesAndWasteCodes
order by WasteCode

select *
from  ZZ_SubstancesOG z
where z.RECID in (

select distinct substanceRecid
from dbo.sca
where SUBSTANCERECID not in (select recid
from  dbo.SubstanceOldNewMap))



select '(''' + convert(nvarchar(50), substanceid) + ''', ' + convert(nvarchar(30), recid) + '),'
from dbo.SubstanceOldNewMap