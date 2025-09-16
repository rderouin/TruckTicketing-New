declare @AccountTypes Table (
	AccountType nvarchar(30),
	TypeKey nvarchar(1)
);

insert into @AccountTypes 
VALUES	('Customer', 'C'),
		('Generator', 'G'),
		('TruckingCompany', 'T'),
		('ThirdPartyAnalytical', 'A')
		
;WITH AccountAddreses 
as
(
	select convert(bit, iif(ca.IsPrimary = 'Yes', convert(bit, 1), convert(bit, 0))) as isPrimaryAddress
		,convert(bit, 0) as isDeleted
		,'undefined' as addressType
		,ca.[ADDRESSSTREET] as street
		,ca.[ADDRESSCITY] as City
		,ca.[ADDRESSZIPCODE]  as zipCode
		,substring(ca.[ADDRESSCOUNTRYREGIONID], 1, 2)  as country
		,ca.[ADDRESSSTATE]  as province
		,ca.[ADDRESSSTREET] + ', ' + ca.[ADDRESSCITY] + ', ' + ca.[ADDRESSSTATE] + ', ' +  + ca.[ADDRESSZIPCODE] + ', ' + substring(ca.[ADDRESSCOUNTRYREGIONID], 1, 2)  as display
		,ca.[GUID] as id
		,ca.PARTYNUMBER 
		,null as BUSRELACCOUNT
		,ca.DATAAREAID
	from [dbo].[Customers_Postal_Addresses] ca
	union
	select  convert(bit, iif(pa.IsPrimary = 'Yes', convert(bit, 1), convert(bit, 0))) as isPrimaryAddress
		,convert(bit, 0) as isDeleted
		,'undefined' as addressType
		,pa.[ADDRESSSTREET] as street
		,pa.[ADDRESSCITY] as City
		,pa.[ADDRESSZIPCODE]  as zipCode
		,substring(pa.[ADDRESSCOUNTRYREGIONID], 1, 2)  as country
		,pa.[ADDRESSSTATE]  as province
		,pa.[ADDRESSSTREET] + ', ' + pa.[ADDRESSCITY] + ', ' + pa.[ADDRESSSTATE] + ', ' +  + pa.[ADDRESSZIPCODE] + ', ' + substring(pa.[ADDRESSCOUNTRYREGIONID], 1, 2)  as display
		,pa.[GUID] as id
		,null as PARTYNUMBER
		,pa.BUSRELACCOUNT
		,pa.DATAAREAID
	from [dbo].[Prospect_Postal_Address] pa
	
)
,AllContacts
as
(
		select	convert(bit, cp.Primary_Contact) as isPrimaryAccountContact
			, convert(bit, 0) as isDeleted
			, convert(bit, 1) as isActive
			, cp.[FIRSTNAME]  as [name]				
			, cp.[LASTNAME] as lastName
			, cpa.[ADDRESS] + ', ' + cpa.[CITY] + ', ' + cpa.[STATE] + ', ' +  cpa.[ZIPCODE] + ', ' + substring(cpa.[COUNTRYREGIONID], 1, 2) as address
			, trim(cpe.locator) as email
			, trim(cpp.locator) as phoneNumber
			, null as jobTitle
			, null as contact
			, cpa.[ADDRESS] as [ADDRESSSTREET] 			
			, cpa.[CITY]    as [ADDRESSCITY]
			, cpa.[ZIPCODE] as [ADDRESSZIPCODE]
			, substring(cpa.[COUNTRYREGIONID], 1, 2) as [ADDRESSCOUNTRYREGIONID]
			, cpa.[STATE] as [ADDRESSSTATE]
			, '00000000-0000-0000-0000-000000000000' as accountContactAddressId
			, 'none' as signatoryType
			, cp.[CONTACTPERSONNAME] as displayName
			, cp.[GUID] as id
			, cp.[ASSOCIATEDPARTYID]
			, null as [BUSRELACCOUNT]
			, cp.[DATAAREAID]
			,iif(cpa.[ADDRESS] is not null and trim(cpa.[ADDRESS]) <> ''
			and cpa.[CITY] is not null and trim(cpa.[CITY]) <> ''
			and cpa.[ZIPCODE] is not null and trim(cpa.[ZIPCODE]) <> ''
			and cpa.[COUNTRYREGIONID] is not null and trim(cpa.[COUNTRYREGIONID]) <> ''
			and cpa.[STATE] is not null and trim(cpa.[STATE]) <> '', 1, 0) as IsValidAddress
		from dbo.contact_person cp		 
			left join [dbo].[Contact_Person_Electronic_Address] cpe on cpe.PartyNumber = cp.[CONTACTPERSONPARTYNUMBER] 
				and cpe.[TYPE] = 'Email' and cpe.ISPRIMARY = 'Yes' and cpe.purpose = 'Business' 
			left join [dbo].[Contact_Person_Electronic_Address] cpp on cpp.PartyNumber = cp.[CONTACTPERSONPARTYNUMBER] 
				and cpp.[TYPE] = 'Phone' and cpp.ISPRIMARY = 'Yes' and cpp.purpose = 'Business' 
			left join [dbo].[Contact_Person_Postal_Address] cpa on  cpa.PartyNumber = cp.[CONTACTPERSONPARTYNUMBER] 
					and cpa.ISPRIMARY = 1	
		where cpe.IsValidLocator = 1 and  cp.ISINACTIVE <> 'Yes' 
		union
		select 	convert(bit, pc.Primary_Contact) as isPrimaryAccountContact
			, convert(bit, 0) as isDeleted
			, convert(bit, 1) as isActive
			, pc.[FIRSTNAME]  as [name]				
			, pc.[LASTNAME] as lastName
			, pca.[ADDRESSSTREET] + ', ' + pca.[ADDRESSCITY] + ', ' + pca.[ADDRESSSTATE] + ', ' +  + pca.[ADDRESSZIPCODE] + ', ' + substring(pca.[ADDRESSCOUNTRYREGIONID], 1, 2)  as address
			, trim(pce.Locator) as email
			, trim(pcp.Locator) as phoneNumber
			, null as jobTitle
			, null as contact
			, pca.[ADDRESSSTREET] 		
			, pca.[ADDRESSCITY] 
			, pca.[ADDRESSZIPCODE] 
			, substring(pca.[ADDRESSCOUNTRYREGIONID], 1, 2)  as [ADDRESSCOUNTRYREGIONID]
			, pca.[ADDRESSSTATE]
			, '00000000-0000-0000-0000-000000000000' as accountContactAddressId
			, 'none' as signatoryType
			, pc.[ContactName] as displayName
			, pc.Id as id
			,null as [ASSOCIATEDPARTYID]
			,pc.[BUSRELACCOUNT]
			,pc.[DATAAREAID]
			,iif(pca.[ADDRESSSTREET] is not null and trim(pca.[ADDRESSSTREET]) <> ''
				and pca.[ADDRESSCITY] is not null and trim(pca.[ADDRESSCITY]) <> ''
				and pca.[ADDRESSZIPCODE] is not null and trim(pca.[ADDRESSZIPCODE]) <> ''
				and pca.[ADDRESSCOUNTRYREGIONID] is not null and trim(pca.[ADDRESSCOUNTRYREGIONID]) <> ''
				and pca.[ADDRESSSTATE] is not null and trim(pca.[ADDRESSSTATE]) <> '', 1, 0) as IsValidAddress
			from ProspectContacts pc
			left outer join [dbo].[ProspectContactEmailPhones] pce 
				on pce.[ContactId] = pc.Id and pce.[Type] = 'Email' and pce.IsPrimary = 1
			left outer join [dbo].[ProspectContactEmailPhones] pcp 
				on pcp.[ContactId] = pc.Id and pcp.[Type] = 'Phone'  and pcp.IsPrimary = 1
			left outer join [dbo].[Prospect_Postal_Address] pca 
				on pca.IsPrimary = 'Yes' and pca.DATAAREAID = pc.DATAAREAID and pc.[BUSRELACCOUNT] = pca.[BUSRELACCOUNT]		
      
)
,AccountMessages AS
(
	SELECT(select distinct 
				  'D365FO' as [Source]
				, a.ID as [SourceId]
				, 'Account' as [MessageType]
				, 'Create' as [Operation]
				, NEWID() as [CorrelationId]
				, SYSDATETIMEOFFSET() as [MessageDate]
					, a.ID as [Payload.Id]
					, a.[ORGANIZATIONNAME] as [Payload.Name]
					, a.[NAMEALIAS] as [Payload.NickName]
					, a.[ORGANIZATIONNAME] as [Payload.Display]
					, 'A' + convert(nvarchar(30), a.TTAccountID) as [Payload.AccountNumber]
					, a.[CUSTOMERACCOUNT] as [Payload.CustomerNumber]
					, 'undefined' as [Payload.BillingType]
					, case 
						when a.IsActiveCustomer = 1 then 'open'
						when a.IsGenerator = 1 then 'Open'
						when a.IsTRuckingCo = 1 then 'Open'
						when a.Is3rdParty = 1 then 'Open'
						end as [Payload.AccountStatus]
					, case trim(a.CustClassificationId)
						when 'Red' then 'Red'
						when 'Yellow' then 'Yellow'
						else 'Undefined'
					  end as [Payload.WatchListStatus]										
					,case 
						when trim(c.PAYMENTTERMS) in ('N90', 'N45', 'N30', 'N10', 'N0', 'N60') then 'Approved'
						else 'Undefined'
					end as [Payload.CreditStatus]					
					, CAST(a.CREDITLIMIT as float) as [Payload.CreditLimit]
					,  iif([dbo].[isValidEmailFormat](a.PrimaryEmail) = 1,  a.PrimaryContactId, null) as [Payload.AccountPrimaryContactId]
					, a.[CONTACTPERSONNAME] as [Payload.AccountPrimaryContactName]
					, a.PrimaryPhone as [Payload.AccountPrimaryContactPhoneNumber]
					, a.PrimaryEmail as [Payload.AccountPrimaryContactEmail]					
					, ISNULL(b.guid, '00000000-0000-0000-0000-000000000000') as [Payload.LegalEntityId]
					, a.DataAreaId as [Payload.LegalEntity]
					,json_query((select	
							CONCAT('["',STRING_AGG(x.AccountType, '","'),'"]') 
						from @AccountTypes x 
						where (x.TypeKey = iif( a.IsActiveCustomer = 1, 'C', '')
								or  x.TypeKey = iif( a.IsGenerator = 1, 'G', '')
								or  x.TypeKey = iif( a.IsTRuckingCo = 1, 'T', '')
								or  x.TypeKey = iif( a.Is3rdParty = 1, 'A', ''))		
						))  as [Payload.AccountTypes]
					, json_query(case when b.Division = 'MI'
						  then isnull((select 
							 iif([dbo].[isValidEmailFormat](a.PrimaryEmail) = 1, isPrimaryAccountContact, convert(bit, 0)) as [IsPrimaryAccountContact]
							, isDeleted as [IsDeleted]
							, isActive as [IsActive]
							, name as [Name]
							, lastName as [LastName]
							, address as [Address]
							, email as [Email]
							, phoneNumber as [PhoneNumber]
							, jobTitle as [JobTitle]
							, contact as [Contact]
						    , json_query(iif(pca.IsValidAddress = 1, Isnull(replace(replace((
								select    pca.[ADDRESSSTREET] as  [Street]
										, pca.[ADDRESSCITY] as [City]
										, pca.[ADDRESSZIPCODE] as [ZipCode]
										, SUBSTRING(pca.[ADDRESSCOUNTRYREGIONID], 1, 2) as [Country]
										, pca.[ADDRESSSTATE] as [Province]
										, pca. accountContactAddressId as [Id]																
								FOR JSON PATH
							), '[', '' ), ']', ''), null), null)) as AccountContactAddress							
							, signatoryType as [SignatoryType]
							, displayName as [DisplayName]
							, id as [Id]							
							, json_query(isnull(
									(select CONCAT('["',STRING_AGG(x.FunctionName, '","'),'"]') 
									from dbo.ContactFunctions x 
								    where x.ContactId = pca.id
							      ), '[]')) as ContactFunctions
						from AllContacts pca
						left join dbo.ValidContacts vc on vc.ContactId = pca.id and vc.CustomerId = a.ID
						where (pca.[ASSOCIATEDPARTYID] = a.PartyNumber or pca.[BUSRELACCOUNT] = a.[CUSTOMERACCOUNT]) and pca.[DATAAREAID] = a.[DATAAREAID]
						and (vc.ContactId is not null OR (vc.ContactId is NULL and a.SourceTable = 'PROSPECTS' and pca.email is not null and [dbo].[isValidEmailFormat](pca.email) = 1))						
						for JSON PATH
					), '[]')
					else
						'[]'
					end ) as [Payload.Contacts]
					,Json_query(isnull(( select 
							  isPrimaryAddress as [IsPrimaryAddress]
							, isDeleted as [IsDeleted]
							, addressType as [AddressType]
							, street as [Street]
							, City as [City]
							, zipCode as [ZipCode]
							, SUBSTRING(country, 1,2) as [Country] 
							, province as [Province]
							, display as [Display]
							, id as [Id]
						from AccountAddreses ca
						where (ca.PartyNumber = a.PartyNumber  or ca.[BUSRELACCOUNT] = a.[CUSTOMERACCOUNT] ) and ca.DATAAREAID = a.DATAAREAID
							and zipCode is not null and trim(zipcode) != '' 
							and street is not null and trim(street) != '' 
							and City is not null and trim(City) != '' 
							and province is not null and trim(province) != '' 
							and [Country] is not null and trim([Country]) != '' 						
						for JSON PATH
					), JSON_QUERY('[]'))) as [Payload.AccountAddresses]
					, null as [Payload.BillingTransferRecipientId]
					, null as [Payload.BillingTransferRecipientName]
					, convert(bit, 0) as [Payload.EnableNewTruckingCompany]
					, convert(bit, 0) as [Payload.EnableNewThirdPartyAnalytical]
					, convert(bit, 0) as [Payload.HasPriceBook]
					, null as [Payload.LastTransactionDate]
					, iif(a.FIELDTICKETACTIVE='Yes' OR a.INVOICEACTIVE = 'Yes', convert(bit, 1),convert(bit, 0)) as [Payload.IsElectronicBillingEnabled]
					, null as [Payload.MailingRecipientName]
					, JSON_QUERY('[]') as [Payload.Attachments]
					, IIF(a.CustClassificationId = 'Red', convert(bit, 1), convert(bit, 0))  as [Payload.DisplayRedFlagMessage]
					, a.duns as [Payload.DunsNumber]
					, null as [Payload.GstNumber]
					, null as [Payload.OperatorLicenseCode]
					, convert(bit, 1) as [Payload.IncludeExternalDocumentAttachmentInLC]
					, convert(bit, 0) as [Payload.IncludeInternalDocumentAttachmentInLC]
					, convert(bit, 1) as [Payload.IsAccountActive]			
		from dbo.AccountMaster a
		left join dbo.Customers c on a.Id = c.guid
		left join dbo.LegalEntity b on a.dataareaid = b.LegalEntityName
		where a.Id = sd.Id  and not (a.IsActiveCustomer = 0 and IsGenerator = 0 and IsTruckingCo =0 and Is3rdParty = 0)
		for JSON PATH, INCLUDE_NULL_VALUES
	)
	AS [Message], 'AccountEntity' AS MessageType, SYSDATETIMEOFFSET() AS GeneratedDate, sd.Id AS EntityId, sd.DataAreaId, sd.[CUSTOMERACCOUNT] AS AxEntityId
		FROM [dbo].AccountMaster sd
)
,Final
as
(
	SELECT SUBSTRING([Message], 2, LEN([Message]) -2) AS [Message], MessageType, GeneratedDate, EntityId, DataAreaId, AxEntityId, 0 AS Processed, null as ProcessedDate, 'enterprise-entity-updates' as TopicName
	FROM AccountMessages ax
	WHERE Message IS NOT NULL 
)
MERGE [dbo].[DataMigrationMessages] AS TARGET 
	USING Final AS SOURCE
on (
	TARGET.EntityId = SOURCE.EntityId 
	AND Target.MessageType = SOURCE.MessageType
)
WHEN MATCHED THEN UPDATE SET 
		TARGET.Message = Source.Message,
		Target.Processed = 0,
		Target.ProcessedDate = null,
		Target.GeneratedDate = Source.GeneratedDate

when not matched then 
	INSERT ([Message], MessageType, GeneratedDate, EntityId, DataAreaId, [AxEntityId], Processed, [ProcessedDate], TopicName)
	values(SOURCE.Message, source.MessageType, SOURCE.GeneratedDate, Source.EntityId, Source.DataAreaId, Source.AxEntityId, 0, null, Source.TopicName);

GO

