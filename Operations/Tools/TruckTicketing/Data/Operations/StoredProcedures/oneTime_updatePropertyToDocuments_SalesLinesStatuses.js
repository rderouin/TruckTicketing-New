/**
 * This is a one time stored procedure to update the SalesLine statues to void (given sales Lines)
 * in the Operations container for EntityType = 'SalesLine'.
 * The logic is idempotent and the SP can be executed multiple times.
 *
 * Account Name: zcac-cosmos-<env>-truckticketing
 * Container Name: Operarions
 *
 * @param {*} partitionKey: must be passed - SalesLine|052023 (197 docs), SalesLine|062023 (8 docs)
 * 
 * @param {string} continuationToken - optional, needed when using Powershell script to invoke for large number of documents
 */
function oneTime_updatePropertyToDocuments_SalesLinesStatuses(continuationToken) {
    var propertyName = 'Status';
    var propertyDefaultValue = 'Void';

    addUpdatePropertyToAllDocuments(propertyName, propertyDefaultValue, continuationToken);

    function addUpdatePropertyToAllDocuments(propertyName, propertyDefaultValue, continuationToken) {
        var response = getContext().getResponse();
        var collection = getContext().getCollection();
        var updated = 0;

        if (continuationToken) {
            var token = JSON.parse(continuationToken);

            if (!token.queryContinuationToken) {
                throw new Error('Could not parse continuation token');
            }

            updated = token.updatedSoFar;

            addUpdatePropertyToAllDocumentsImpl(propertyName, propertyDefaultValue, token.queryContinuationToken);
        }
        else {
            addUpdatePropertyToAllDocumentsImpl(propertyName, propertyDefaultValue);
        }

        var ticketLookupData = [
            {
                "Id": "89b3bf9d-aa37-4a49-bf39-54b837ba1188",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|042023",
                "TicketNumber": "FCFST20000027-WT",
                "EffectiveDate": "2023-05-26T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "1473a9c2-d799-47bc-ba11-9b1460053459",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "JANLF10000059-LF",
                "EffectiveDate": "2023-05-02T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "26dbf318-4577-4a24-8a90-a458debcb988",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|062023",
                "TicketNumber": "JCFST063964-SP",
                "EffectiveDate": "2023-05-25T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "d41a833a-3ed1-4cb1-a0a7-9a1f3fe19415",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "BMSWD118702-SP",
                "EffectiveDate": "2023-05-01T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "c3092f56-e5d5-4ed8-9480-d5602df7e60d",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|062023",
                "TicketNumber": "TAFST03651-SP",
                "EffectiveDate": "2023-05-02T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "15492b9c-70a5-4824-938f-9c8506c1a17f",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "BMSWD118863-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "d77adbd4-5c86-4ea6-b06f-5e5ef6ae69cc",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|062023",
                "TicketNumber": "KBSWD075197-SP",
                "EffectiveDate": "2023-06-02T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "187afa02-90f5-49de-aed7-dafdbbd67474",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KIFST229871-SP",
                "EffectiveDate": "2023-05-01T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "f0b467a0-d3e0-45ae-9e23-e2856a14c95e",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|062023",
                "TicketNumber": "LAFST141969-SP",
                "EffectiveDate": "2023-06-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "a8412c39-2629-4680-ad54-c3671a4ff1af",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KIFST229872-SP",
                "EffectiveDate": "2023-05-01T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "4a0361e7-5a37-444e-b0fb-c0b7d57499f7",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|062023",
                "TicketNumber": "GDFST057868-SP",
                "EffectiveDate": "2023-06-05T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "0bb9a476-9af7-4f3d-9c3b-597c54b3cc79",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KWFST79120-SP",
                "EffectiveDate": "2023-05-02T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "55906141-2ce0-420b-ae44-5810ce4de2fe",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KWFST79163-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "0188151f-b28e-46b6-b5c8-300d7c5a90fd",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KWFST79165-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "0111d6c4-d907-4cd3-a9ff-ab9236774fa9",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KWFST79166-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "514ab8a5-e9cc-418c-bb24-a0625a46c6ce",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "SWFST153918-SP",
                "EffectiveDate": "2023-05-04T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "ba56215f-42c0-4c37-80a2-2082df9c0327",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "SWFST20000390-WT",
                "EffectiveDate": "2023-05-18T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "ec864080-dcb4-4c8e-bf8d-6a8ce53588c6",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "BCFST110982-SP",
                "EffectiveDate": "2023-05-04T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "ede61fda-8948-4bca-85de-b6d308869d6c",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "COFST30498-SP",
                "EffectiveDate": "2023-05-06T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "fd528dc2-0147-42a8-acc7-d7bdc7af9555",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "COFST30499-SP",
                "EffectiveDate": "2023-05-06T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "b1b965d9-9304-401e-9b6a-72345acf2948",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "COFST30500-SP",
                "EffectiveDate": "2023-05-06T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "a992b598-16fe-4212-b6a6-e71b4ffa0e1c",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "COFST30501-SP",
                "EffectiveDate": "2023-05-06T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "a9c4eb19-abec-4d38-b45e-274c5d31e7d9",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "GDFST57140-SP",
                "EffectiveDate": "2023-05-06T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "9530685e-58f5-4b33-93d3-b7a52c637c74",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "JCFST063605-SP",
                "EffectiveDate": "2023-05-08T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "15691e2c-c47c-4c53-b51c-124a7d78f079",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KIFST230028-SP",
                "EffectiveDate": "2023-05-04T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "3d5233c7-983d-4324-825f-936b6c1ef7d1",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KIFST230029-SP",
                "EffectiveDate": "2023-05-04T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "51f7f5c6-008a-427b-88ae-ee71a5ccd4c4",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KIFST230030-SP",
                "EffectiveDate": "2023-05-04T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "466ee082-7a1e-4225-b22d-a10292f0c2b2",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KIFST230032-SP",
                "EffectiveDate": "2023-05-04T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "950d005e-0ec9-4480-a7d4-3a07bf8a6796",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TULSO010274-SP",
                "EffectiveDate": "2023-05-09T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "e33ca527-a2bf-441c-9eb6-7304513af474",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03656-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "fc886fd2-8ada-4500-b689-f7c280bc0867",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03655-SP",
                "EffectiveDate": "2023-05-02T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "3a50aab5-b265-4bee-9f90-c00f75fa31f0",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03653-SP",
                "EffectiveDate": "2023-05-02T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "1c65099a-5ff0-4848-9365-b583e0698b61",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03649-SP",
                "EffectiveDate": "2023-05-01T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "d14652cc-94bd-4c91-9d22-839c03280fa7",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03650-SP",
                "EffectiveDate": "2023-05-01T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "46903717-70be-403e-afde-5559872f8653",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03648-SP",
                "EffectiveDate": "2023-05-01T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "15a1c159-5e49-47cb-a5dd-724a4907a824",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00955-SP",
                "EffectiveDate": "2023-05-01T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "81cb2960-f7c3-4571-bcec-390e12c54d07",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03657-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "5c82f6da-fc1f-4659-83ac-a8c3f93a07bf",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00962-SP",
                "EffectiveDate": "2023-05-01T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "1908dc37-0054-4759-8da1-3aad94fe7f10",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03658-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "943edad3-6143-40f4-9676-a30fda8345f5",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03659-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "279e3673-69fb-447a-bf72-12c4ad272ad0",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03672-SP",
                "EffectiveDate": "2023-05-07T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "69612563-3872-4bbb-81f4-732f897c76d0",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03663-SP",
                "EffectiveDate": "2023-05-04T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "1b5b99ec-76f0-47b1-8fb6-030b2f3420e5",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00959-SP",
                "EffectiveDate": "2023-05-01T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "5be9da91-4985-416a-a9a4-0b30972d9ce9",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00965-SP",
                "EffectiveDate": "2023-05-02T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "e6b61803-807b-4a71-aa15-31696051913a",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00966-SP",
                "EffectiveDate": "2023-05-02T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "5bb5d6a1-892e-42af-9f9f-35893ca76ece",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00969-SP",
                "EffectiveDate": "2023-05-02T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "92b02373-16b6-4394-a475-b01d55cd6bc3",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "SWFST154273-SP",
                "EffectiveDate": "2023-05-11T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "94cbde78-ad43-4089-87d5-5533a36e804b",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00980-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "023d76fe-270d-4e5e-8d3a-3b2782ad4b86",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00979-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "1da229d8-e0cc-4e83-a475-b7ba25a49e68",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00985-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "f25ff390-d9a7-4e14-8adb-20f90de3bdee",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00987-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "85effaf4-2575-401b-99f5-e1b04bd64dbd",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03646-SP",
                "EffectiveDate": "2023-05-01T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "b3ce131c-80a0-4bdd-acc1-0ad384636708",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03645-SP",
                "EffectiveDate": "2023-05-01T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "dd9adca2-b3c5-47fe-88da-baf7953f28b0",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00978-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "e63eadc7-beb9-4d91-a3ed-9f50ba44ad04",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00982-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "49fa9a67-001d-40fc-8d5f-93201ddbe462",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00983-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "2a2fb021-ee2a-448d-a5a7-27dcd8ac20f7",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00988-SP",
                "EffectiveDate": "2023-05-03T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "c2cf003c-d15d-42fd-94f8-d653e5154fb3",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03647-SP",
                "EffectiveDate": "2023-05-01T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "38108255-03e5-43e2-abc1-ce8b0c5ef1bf",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00992-SP",
                "EffectiveDate": "2023-05-04T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "2b03d057-b27f-495d-a2f0-6b7fc5a7755f",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "HPFST59972-SP",
                "EffectiveDate": "2023-05-01T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "59e3f0dd-3166-4053-956c-2be610b6c38c",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00997-SP",
                "EffectiveDate": "2023-05-04T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "ba8fbbfd-1f0a-4709-b0fb-25fca098b284",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "ALITM00998-SP",
                "EffectiveDate": "2023-05-04T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "8515c031-afac-468c-ad72-604825cf052f",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "RYFSR095635-SP",
                "EffectiveDate": "2023-05-12T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "f3aadf51-1adc-4ae2-80db-a6397a40cf5f",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03671-SP",
                "EffectiveDate": "2023-05-06T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "22df71be-a848-4a1e-a67e-65e887a8feb3",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03669-SP",
                "EffectiveDate": "2023-05-05T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "84ac12b4-098e-4683-b2cd-4486b53b6a5a",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "TAFST03667-SP",
                "EffectiveDate": "2023-05-05T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "98b8bd0b-0d50-41e1-825f-5d2c8340141a",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "GDFST057405-SP",
                "EffectiveDate": "2023-05-15T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "337bc9c6-f8f3-4044-b001-1f1b093ff081",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "DVFST160783-SP",
                "EffectiveDate": "2023-05-17T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "34973ae7-0f3f-473a-8688-8bb5cd611667",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "GDFST057478-SP",
                "EffectiveDate": "2023-05-17T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "a851b4d1-f111-46db-843e-e55577436619",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "DVFST160782-SP",
                "EffectiveDate": "2023-05-17T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "090003d3-1434-46de-ab09-7c54aee3d487",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KIFST23139-SP",
                "EffectiveDate": "2023-05-21T00:00:00",
                "Status": "Void"
            },
            {
                "Id": "8c49eea4-c722-4e08-bf3e-180f9eb96fd4",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KNEWD275100-SP",
                "EffectiveDate": "2023-05-17T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "7b5fada0-9886-40a1-aba7-c7603e6a553d",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KNEWD275101-SP",
                "EffectiveDate": "2023-05-17T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "cffe9a39-ab23-44f3-bf9e-b8a0f8efef68",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KNEWD275102-SP",
                "EffectiveDate": "2023-05-17T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "e79e1b58-da12-468a-82e3-68558cc65aa9",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KNEWD275103-SP",
                "EffectiveDate": "2023-05-17T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "4034c997-67fd-42af-923c-eea22ab0d00b",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "GDFST057538-SP",
                "EffectiveDate": "2023-05-19T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "5fae2e2d-af31-46fb-ba4c-a371fe8b0925",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "DVFST160878-SP",
                "EffectiveDate": "2023-05-20T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "a5c46512-49b6-4de6-9abf-da95919665a6",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "GDFST057586-SP",
                "EffectiveDate": "2023-05-22T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "15401fa0-01be-4f5e-be85-e0ff4ccd3c37",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "EDFST091395-SP",
                "EffectiveDate": "2023-05-23T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "b400e930-a049-445f-87b3-bd6e8a737734",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "EDFST091396-SP",
                "EffectiveDate": "2023-05-23T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "be1f831e-a250-4d6a-b554-9f995d91d006",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KIFST231237",
                "EffectiveDate": "2023-05-22T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "be1f831e-a250-4d6a-b554-9f995d91d006",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KIFST231237-SP",
                "EffectiveDate": "2023-05-22T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "115396dc-5e1b-4f3d-89cc-be3a1b7af71d",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KIFST231238-SP",
                "EffectiveDate": "2023-05-22T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "584c0502-4a15-47fe-b9b1-1b3182bb5352",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KIFST231240",
                "EffectiveDate": "2023-05-22T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "584c0502-4a15-47fe-b9b1-1b3182bb5352",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KIFST231240-SP",
                "EffectiveDate": "2023-05-22T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "642938a3-dd6d-45e7-9208-6131e043b385",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "GDFST057685-SP",
                "EffectiveDate": "2023-05-27T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "fd1ad088-b9f8-4bbb-9e08-23f7e1a0c566",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "KIFST231239-SP",
                "EffectiveDate": "2023-05-22T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "053719ae-28b0-4686-9966-c3968d34e1ca",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "DVFST161261",
                "EffectiveDate": "2023-05-29T00:00:00",
                "Status": "Invoiced"
            },
            {
                "Id": "053719ae-28b0-4686-9966-c3968d34e1ca",
                "EntityType": "TruckTicket",
                "DocumentType": "TruckTicketEntity|052023",
                "TicketNumber": "DVFST161261-SP",
                "EffectiveDate": "2023-05-29T00:00:00",
                "Status": "Invoiced"
            }
        ];

        function addUpdatePropertyToAllDocumentsImpl(propertyName, propertyDefaultValue, queryContinuationToken) {
            var requestOptions = { continuation: queryContinuationToken };
            var query = `
                SELECT * FROM c WHERE c.EntityType = 'SalesLine' AND
                (
                    (
                        c.Status = 'Preview' AND c.SalesLineNumber in (
                            'JANLF10000079-SL',
                            'EDFST10001113-SL',
                            'RYFSR10000647-SL',
                            'KBSWD10000280-SL',
                            'LAFST10003689-SL',
                            'LAFST10003687-SL',
                            'TAFST10001144-SL',
                            'TAFST10001142-SL',
                            'KIFST10004073-SL',
                            'KIFST10004071-SL',
                            'JCFST10001513-SL',
                            'KIFST10003572-SL',
                            'KIFST10003578-SL',
                            'KIFST10003575-SL',
                            'KIFST10003579-SL',
                            'KIFST10003573-SL',
                            'KIFST10003567-SL',
                            'KIFST10003570-SL',
                            'KIFST10003576-SL',
                            'EDFST10001114-SL',
                            'DVFST10000712-SL',
                            'DVFST10000710-SL',
                            'SWFST10002553-SL',
                            'SWFST10002555-SL',
                            'KNEWD10001862-SL',
                            'KNEWD10001863-SL',
                            'KNEWD10001860-SL',
                            'KNEWD10001861-SL',
                            'DVFST10000400-SL',
                            'DVFST10000350-SL',
                            'DVFST10000398-SL',
                            'TAFST10000268-SL',
                            'TAFST10000277-SL',
                            'TAFST10000170-SL',
                            'TAFST10000226-SL',
                            'HPFST10000112-SL',
                            'TAFST10000266-SL',
                            'TAFST10000275-SL',
                            'TAFST10000168-SL',
                            'TAFST10000224-SL',
                            'TAFST10000269-SL',
                            'TAFST10000271-SL',
                            'TAFST10000160-SL',
                            'TAFST10000163-SL',
                            'TAFST10000161-SL',
                            'TAFST10000158-SL',
                            'TAFST10000139-SL',
                            'TAFST10000137-SL',
                            'SWFST10001634-SL',
                            'TAFST10000117-SL',
                            'TAFST10000087-SL',
                            'TAFST10000114-SL',
                            'TAFST10000093-SL',
                            'TAFST10000085-SL',
                            'TAFST10000091-SL',
                            'TAFST10000112-SL',
                            'TAFST10000115-SL',
                            'TAFST10000088-SL',
                            'TAFST10000090-SL',
                            'TAFST10000059-SL',
                            'TAFST10000062-SL',
                            'TAFST10000074-SL',
                            'TAFST10000080-SL',
                            'TAFST10000083-SL',
                            'TAFST10000060-SL',
                            'TULSO10000331-SL',
                            'TAFST10000081-SL',
                            'TAFST10000078-SL',
                            'TAFST10000057-SL',
                            'TAFST10000072-SL',
                            'TAFST10000069-SL',
                            'TAFST10000071-SL',
                            'TAFST10000075-SL',
                            'TAFST10000077-SL',
                            'KIFST10000642-SL',
                            'KIFST10000639-SL',
                            'KIFST10000645-SL',
                            'KIFST10000646-SL',
                            'KIFST10000640-SL',
                            'KIFST10000637-SL',
                            'KIFST10000643-SL',
                            'JCFST10000331-SL',
                            'COFST10000333-SL',
                            'COFST10000329-SL',
                            'COFST10000332-SL',
                            'COFST10000336-SL',
                            'SWFST10000603-SL',
                            'BCFST10000232-SL',
                            'BMSWD10000209-SL',
                            'BMSWD10000123-SL',
                            'KWFST10000307-SL',
                            'KWFST10000305-SL',
                            'KIFST10000167-SL',
                            'KIFST10000168-SL',
                            'KIFST10000165-SL',
                            'KWFST10000190-SL',
                            'KWFST10000303-SL',
                            'SWFST10002554-SL'
                        )
                    )
                    OR
                    (
                        c.Status = 'Exception' AND c.SalesLineNumber in (
                            'KIFST10000166-SL',
                            'KWFST10000189-SL',
                            'KWFST10000301-SL',
                            'KWFST10000302-SL',
                            'KWFST10000304-SL',
                            'KWFST10000306-SL',
                            'COFST10000327-SL',
                            'COFST10000328-SL',
                            'COFST10000330-SL',
                            'COFST10000331-SL',
                            'COFST10000334-SL',
                            'COFST10000335-SL',
                            'KIFST10000638-SL',
                            'KIFST10000641-SL',
                            'KIFST10000644-SL',
                            'TAFST10000058-SL',
                            'TAFST10000061-SL',
                            'TAFST10000070-SL',
                            'TAFST10000073-SL',
                            'TAFST10000076-SL',
                            'TAFST10000079-SL',
                            'TAFST10000082-SL',
                            'ALITM10000001-SL',
                            'ALITM10000001-SL',
                            'ALITM10000026-SL',
                            'ALITM10000027-SL',
                            'ALITM10000028-SL',
                            'TAFST10000086-SL',
                            'TAFST10000089-SL',
                            'TAFST10000092-SL',
                            'TAFST10000113-SL',
                            'TAFST10000116-SL',
                            'ALITM10000031-SL',
                            'ALITM10000032-SL',
                            'ALITM10000054-SL',
                            'ALITM10000055-SL',
                            'ALITM10000056-SL',
                            'ALITM10000057-SL',
                            'ALITM10000058-SL',
                            'ALITM10000059-SL',
                            'ALITM10000060-SL',
                            'ALITM10000061-SL',
                            'ALITM10000062-SL',
                            'ALITM10000086-SL',
                            'ALITM10000087-SL',
                            'ALITM10000088-SL',
                            'ALITM10000089-SL',
                            'ALITM10000090-SL',
                            'ALITM10000091-SL',
                            'ALITM10000092-SL',
                            'ALITM10000093-SL',
                            'ALITM10000094-SL',
                            'ALITM10000098-SL',
                            'ALITM10000099-SL',
                            'ALITM10000100-SL',
                            'TAFST10000138-SL',
                            'TAFST10000159-SL',
                            'TAFST10000162-SL',
                            'ALITM10000152-SL',
                            'ALITM10000153-SL',
                            'ALITM10000154-SL',
                            'ALITM10000155-SL',
                            'ALITM10000156-SL',
                            'ALITM10000160-SL',
                            'ALITM10000161-SL',
                            'ALITM10000162-SL',
                            'ALITM10000173-SL',
                            'ALITM10000174-SL',
                            'ALITM10000175-SL',
                            'ALITM10000185-SL',
                            'ALITM10000186-SL',
                            'ALITM10000187-SL',
                            'ALITM10000188-SL',
                            'ALITM10000189-SL',
                            'ALITM10000190-SL',
                            'TAFST10000169-SL',
                            'TAFST10000225-SL',
                            'TAFST10000267-SL',
                            'TAFST10000270-SL',
                            'TAFST10000276-SL',
                            'DVFST10000399-SL',
                            'GDFST10000381-SL',
                            'GDFST10000932-SL',
                            'GDFST10001090-SL',
                            'GDFST10001236-SL',
                            'DVFST10000711-SL',
                            'GDFST10001396-SL',
                            'KIFST10003568-SL',
                            'KIFST10003571-SL',
                            'KIFST10003574-SL',
                            'KIFST10003577-SL',
                            'KIFST10003580-SL',
                            'JCFST10001512-SL',
                            'FCFST10000923-SL',
                            'GDFST10001650-SL',
                            'KIFST10004072-SL',
                            'DVFST10001674-SL',
                            'ALITM10000146-SL',
                            'ALITM10000147-SL',
                            'ALITM10000148-SL',
                            'TAFST10001143-SL',
                            'GDFST10002024-SL',
                            'BCFST10000231-SL',
                            'DVFST10000349-SL',
                            'HPFST10000110-SL',
                            'HPFST10000111-SL',
                            'LAFST10003688-SL'
                        )
                    )
                )
            `;

            // console.log(`query: ${query}`);

            var isAccepted = collection.queryDocuments(
                collection.getSelfLink(),
                query,
                requestOptions,
                function (err, feed, responseOptions) {
                    if (err) throw err;

                    if (!feed || !feed.length) {
                        response.setBody('No docs found');
                    }
                    else {
                        feed.forEach(element => {
                            var ticket = ticketLookupData.filter((t) => t.TicketNumber == element.TruckTicketNumber)[0];

                            if (!ticket) {
                                console.log(`Could not find ticket data for ${element.TruckTicketNumber}`);

                                return;
                            }

                            var patchSpec = [
                                { "op": "replace", "path": `/${propertyName}`, "value": propertyDefaultValue },
                                { "op": "replace", "path": `/TruckTicketId`, "value": ticket.Id },
                                { "op": "replace", "path": `/TruckTicketEffectiveDate`, "value": ticket.EffectiveDate }
                            ]

                            // console.log(`${element.TruckTicketNumber}: ${element.TruckTicketId} => ${ticket.Id} ${element.TruckTicketEffectiveDate} => ${ticket.EffectiveDate} `);

                            var isUpdateAccepted = collection.patchDocument(element._self, patchSpec, function (err) {
                                if (err) throw err;
                            });

                            if (!isUpdateAccepted) {
                                throw new Error(`Patch wasn't accepted for ${element._self}`);
                            }

                            updated++;
                        })
                    }

                    if (responseOptions.continuation) {
                        addUpdatePropertyToAllDocumentsImpl(propertyName, propertyDefaultValue, responseOptions.continuation)
                    } else {
                        response.setBody({ count: updated, continuation: null });
                    }
                });

            if (!isAccepted) {
                var sprocToken = JSON.stringify({
                    updatedSoFar: updated,
                    queryContinuationToken: queryContinuationToken
                });

                response.setBody({ count: null, continuation: sprocToken });
            }
        }
    }
}

/*
-- Data verification/validation queries

SELECT * FROM c WHERE c.EntityType = 'SalesLine' AND c.SalesLineNumber in ('KIFST10000166-SL', 'BSFST10003703-SL')
SELECT * FROM c WHERE c.EntityType = 'SalesLine' AND c.Status = 'Void'

SELECT * FROM c WHERE c.DocumentType = 'SalesLine|062023' AND c.EntityType = 'SalesLine' AND c.SalesLineNumber in ()

-- Results from QA (matches the Excel count)
[
    {
        "DocumentType": "SalesLine|052023",
        "Status": "Exception",
        "$1": 104
    },
    {
        "DocumentType": "SalesLine|062023",
        "Status": "Exception",
        "$1": 3
    },
    {
        "DocumentType": "SalesLine|052023",
        "Status": "Preview",
        "$1": 93
    },
    {
        "DocumentType": "SalesLine|062023",
        "Status": "Preview",
        "$1": 5
    }
]

--SELECT c.Id, c.EntityType, c.DocumentType, c.TicketNumber, c.EffectiveDate, c.Status FROM c
--where c.EntityType = 'TruckTicket' AND c.TicketNumber = 'JANLF10000059-LF'

--SELECT c.Id, c.EntityType, c.DocumentType, c.TruckTicketId, c.TruckTicketNumber, c.TruckTicketEffectiveDate, c.Status FROM c WHERE c.EntityType = 'SalesLine'
--AND c.DocumentType = 'SalesLine|052023'
--AND c.Id = 'da0ffa7b-db20-464f-bc88-18c29e94549f'

SELECT c.DocumentType, c.Status, Count(1) FROM c WHERE c.EntityType = 'SalesLine' AND
(
    (
        c.Status = 'Preview' AND c.SalesLineNumber in (
            'JANLF10000079-SL',
            'EDFST10001113-SL',
            'RYFSR10000647-SL',
            'KBSWD10000280-SL',
            'LAFST10003689-SL',
            'LAFST10003687-SL',
            'TAFST10001144-SL',
            'TAFST10001142-SL',
            'KIFST10004073-SL',
            'KIFST10004071-SL',
            'JCFST10001513-SL',
            'KIFST10003572-SL',
            'KIFST10003578-SL',
            'KIFST10003575-SL',
            'KIFST10003579-SL',
            'KIFST10003573-SL',
            'KIFST10003567-SL',
            'KIFST10003570-SL',
            'KIFST10003576-SL',
            'EDFST10001114-SL',
            'DVFST10000712-SL',
            'DVFST10000710-SL',
            'SWFST10002553-SL',
            'SWFST10002555-SL',
            'KNEWD10001862-SL',
            'KNEWD10001863-SL',
            'KNEWD10001860-SL',
            'KNEWD10001861-SL',
            'DVFST10000400-SL',
            'DVFST10000350-SL',
            'DVFST10000398-SL',
            'TAFST10000268-SL',
            'TAFST10000277-SL',
            'TAFST10000170-SL',
            'TAFST10000226-SL',
            'HPFST10000112-SL',
            'TAFST10000266-SL',
            'TAFST10000275-SL',
            'TAFST10000168-SL',
            'TAFST10000224-SL',
            'TAFST10000269-SL',
            'TAFST10000271-SL',
            'TAFST10000160-SL',
            'TAFST10000163-SL',
            'TAFST10000161-SL',
            'TAFST10000158-SL',
            'TAFST10000139-SL',
            'TAFST10000137-SL',
            'SWFST10001634-SL',
            'TAFST10000117-SL',
            'TAFST10000087-SL',
            'TAFST10000114-SL',
            'TAFST10000093-SL',
            'TAFST10000085-SL',
            'TAFST10000091-SL',
            'TAFST10000112-SL',
            'TAFST10000115-SL',
            'TAFST10000088-SL',
            'TAFST10000090-SL',
            'TAFST10000059-SL',
            'TAFST10000062-SL',
            'TAFST10000074-SL',
            'TAFST10000080-SL',
            'TAFST10000083-SL',
            'TAFST10000060-SL',
            'TULSO10000331-SL',
            'TAFST10000081-SL',
            'TAFST10000078-SL',
            'TAFST10000057-SL',
            'TAFST10000072-SL',
            'TAFST10000069-SL',
            'TAFST10000071-SL',
            'TAFST10000075-SL',
            'TAFST10000077-SL',
            'KIFST10000642-SL',
            'KIFST10000639-SL',
            'KIFST10000645-SL',
            'KIFST10000646-SL',
            'KIFST10000640-SL',
            'KIFST10000637-SL',
            'KIFST10000643-SL',
            'JCFST10000331-SL',
            'COFST10000333-SL',
            'COFST10000329-SL',
            'COFST10000332-SL',
            'COFST10000336-SL',
            'SWFST10000603-SL',
            'BCFST10000232-SL',
            'BMSWD10000209-SL',
            'BMSWD10000123-SL',
            'KWFST10000307-SL',
            'KWFST10000305-SL',
            'KIFST10000167-SL',
            'KIFST10000168-SL',
            'KIFST10000165-SL',
            'KWFST10000190-SL',
            'KWFST10000303-SL',
            'SWFST10002554-SL'
        )
    )
    OR
    (
        c.Status = 'Exception' AND c.SalesLineNumber in (
            'KIFST10000166-SL',
            'KWFST10000189-SL',
            'KWFST10000301-SL',
            'KWFST10000302-SL',
            'KWFST10000304-SL',
            'KWFST10000306-SL',
            'COFST10000327-SL',
            'COFST10000328-SL',
            'COFST10000330-SL',
            'COFST10000331-SL',
            'COFST10000334-SL',
            'COFST10000335-SL',
            'KIFST10000638-SL',
            'KIFST10000641-SL',
            'KIFST10000644-SL',
            'TAFST10000058-SL',
            'TAFST10000061-SL',
            'TAFST10000070-SL',
            'TAFST10000073-SL',
            'TAFST10000076-SL',
            'TAFST10000079-SL',
            'TAFST10000082-SL',
            'ALITM10000001-SL',
            'ALITM10000001-SL',
            'ALITM10000026-SL',
            'ALITM10000027-SL',
            'ALITM10000028-SL',
            'TAFST10000086-SL',
            'TAFST10000089-SL',
            'TAFST10000092-SL',
            'TAFST10000113-SL',
            'TAFST10000116-SL',
            'ALITM10000031-SL',
            'ALITM10000032-SL',
            'ALITM10000054-SL',
            'ALITM10000055-SL',
            'ALITM10000056-SL',
            'ALITM10000057-SL',
            'ALITM10000058-SL',
            'ALITM10000059-SL',
            'ALITM10000060-SL',
            'ALITM10000061-SL',
            'ALITM10000062-SL',
            'ALITM10000086-SL',
            'ALITM10000087-SL',
            'ALITM10000088-SL',
            'ALITM10000089-SL',
            'ALITM10000090-SL',
            'ALITM10000091-SL',
            'ALITM10000092-SL',
            'ALITM10000093-SL',
            'ALITM10000094-SL',
            'ALITM10000098-SL',
            'ALITM10000099-SL',
            'ALITM10000100-SL',
            'TAFST10000138-SL',
            'TAFST10000159-SL',
            'TAFST10000162-SL',
            'ALITM10000152-SL',
            'ALITM10000153-SL',
            'ALITM10000154-SL',
            'ALITM10000155-SL',
            'ALITM10000156-SL',
            'ALITM10000160-SL',
            'ALITM10000161-SL',
            'ALITM10000162-SL',
            'ALITM10000173-SL',
            'ALITM10000174-SL',
            'ALITM10000175-SL',
            'ALITM10000185-SL',
            'ALITM10000186-SL',
            'ALITM10000187-SL',
            'ALITM10000188-SL',
            'ALITM10000189-SL',
            'ALITM10000190-SL',
            'TAFST10000169-SL',
            'TAFST10000225-SL',
            'TAFST10000267-SL',
            'TAFST10000270-SL',
            'TAFST10000276-SL',
            'DVFST10000399-SL',
            'GDFST10000381-SL',
            'GDFST10000932-SL',
            'GDFST10001090-SL',
            'GDFST10001236-SL',
            'DVFST10000711-SL',
            'GDFST10001396-SL',
            'KIFST10003568-SL',
            'KIFST10003571-SL',
            'KIFST10003574-SL',
            'KIFST10003577-SL',
            'KIFST10003580-SL',
            'JCFST10001512-SL',
            'FCFST10000923-SL',
            'GDFST10001650-SL',
            'KIFST10004072-SL',
            'DVFST10001674-SL',
            'ALITM10000146-SL',
            'ALITM10000147-SL',
            'ALITM10000148-SL',
            'TAFST10001143-SL',
            'GDFST10002024-SL',
            'BCFST10000231-SL',
            'DVFST10000349-SL',
            'HPFST10000110-SL',
            'HPFST10000111-SL',
            'LAFST10003688-SL'
        )
    )
)
GROUP BY c.DocumentType, c.Status

-- Data from Prod
[
    'ALITM00955-SP',
    'ALITM00959-SP',
    'ALITM00962-SP',
    'ALITM00965-SP',
    'ALITM00966-SP',
    'ALITM00969-SP',
    'ALITM00978-SP',
    'ALITM00979-SP',
    'ALITM00980-SP',
    'ALITM00982-SP',
    'ALITM00983-SP',
    'ALITM00985-SP',
    'ALITM00987-SP',
    'ALITM00988-SP',
    'ALITM00992-SP',
    'ALITM00997-SP',
    'ALITM00998-SP',
    'BCFST110982-SP',
    'BMSWD118702-SP',
    'BMSWD118863-SP',
    'COFST30498-SP',
    'COFST30499-SP',
    'COFST30500-SP',
    'COFST30501-SP',
    'DVFST160782-SP',
    'DVFST160783-SP',
    'DVFST160878-SP',
    'DVFST161261',
    'EDFST091395-SP',
    'EDFST091396-SP',
    'FCFST20000027-WT',
    'GDFST057405-SP',
    'GDFST057478-SP',
    'GDFST057538-SP',
    'GDFST057586-SP',
    'GDFST057685-SP',
    'GDFST057868-SP',
    'GDFST57140-SP',
    'HPFST59972-SP',
    'JANLF10000059-LF',
    'JCFST063605-SP',
    'JCFST063964-SP',
    'KBSWD075197-SP',
    'KIFST229871-SP',
    'KIFST229872-SP',
    'KIFST230028-SP',
    'KIFST230029-SP',
    'KIFST230030-SP',
    'KIFST230032-SP',
    'KIFST231237',
    'KIFST231237-SP',
    'KIFST231238-SP',
    'KIFST231239-SP',
    'KIFST231240',
    'KIFST23139-SP',
    'KNEWD275100-SP',
    'KNEWD275101-SP',
    'KNEWD275102-SP',
    'KNEWD275103-SP',
    'KWFST79120-SP',
    'KWFST79163-SP',
    'KWFST79165-SP',
    'KWFST79166-SP',
    'LAFST141969-SP',
    'RYFSR095635-SP',
    'SWFST153918-SP',
    'SWFST154273-SP',
    'SWFST20000390-WT',
    'TAFST03645-SP',
    'TAFST03646-SP',
    'TAFST03647-SP',
    'TAFST03648-SP',
    'TAFST03649-SP',
    'TAFST03650-SP',
    'TAFST03651-SP',
    'TAFST03653-SP',
    'TAFST03655-SP',
    'TAFST03656-SP',
    'TAFST03657-SP',
    'TAFST03658-SP',
    'TAFST03659-SP',
    'TAFST03663-SP',
    'TAFST03667-SP',
    'TAFST03669-SP',
    'TAFST03671-SP',
    'TAFST03672-SP',
    'TULSO010274-SP'
]
*/
