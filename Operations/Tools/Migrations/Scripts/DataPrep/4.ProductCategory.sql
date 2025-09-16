IF OBJECT_ID(N'dbo.ProductItemCategories', N'U')  IS NOT NULL  
   drop table dbo.ProductItemCategories;
GO

IF OBJECT_ID(N'dbo.ProductCategories', N'U') IS NOT NULL  
   drop table dbo.ProductCategories;
GO


IF NOT EXISTS(SELECT *
          FROM   INFORMATION_SCHEMA.COLUMNS
          WHERE  TABLE_NAME = 'ProductCategoriesImport'
                 AND COLUMN_NAME = 'ID') 
begin
	ALTER TABLE [dbo].[ProductCategoriesImport] 
	ADD ID UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()	
end
GO

select newid() ID, c.Category
into dbo.ProductCategories
from (select distinct i.Category
from dbo.ProductCategoriesImport i) C

SELECT T.DATAAREAID, T.ItemName, T.ItemNumber, T.Item_Unit, 
	T.Category, x.SiteID Facility, pc.ID CategoryId, 'Sales' as CategoryHierarchyId
into dbo.ProductItemCategories
FROM [dbo].[ProductCategoriesImport] T
inner join dbo.ProductCategories pc on pc.Category = t.Category
left JOIN
(
	select value SiteID, ID
	from [ProductCategoriesImport] 
	cross apply	string_split(Facility, ',')
	where value is not null
) X on x.id = t.ID;

GO




