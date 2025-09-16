/**
 * This is a one time stored procedure to add the CanPriceBeRefreshed
 * in the Operations container for DocumentType = 'SalesLine|MM2023' and ... (as per query).
 * The logic is idempotent and the SP can be executed multiple times.
 *
 * Account Name: zcac-cosmos-<env>-truckticketing
 * Container Name: Operations
 *
 * @param {*} partitionKey: must be passed - SalesLine|052023, SalesLine|062023, SalesLine|072023, SalesLine|082023, SalesLine|092023 (One at a time)
 * 
 * @param {string} continuationToken - optional, needed when using Powershell script to invoke for large number of documents
 */
function oneTime_addPropertyToDocuments_CanPriceBeRefreshed(continuationToken) {
    var propertyName = 'CanPriceBeRefreshed';
    var propertyDefaultValue = true;

    addPropertyToAllDocuments(propertyName, propertyDefaultValue, continuationToken);

    function addPropertyToAllDocuments(propertyName, propertyDefaultValue, continuationToken) {
        var response = getContext().getResponse();
        var collection = getContext().getCollection();
        var updated = 0;

        if (continuationToken) {
            var token = JSON.parse(continuationToken);

            if (!token.queryContinuationToken) {
                throw new Error('Could not parse continuation token');
            }
            
            updated = token.updatedSoFar;

            addPropertyToAllDocumentsImpl(propertyName, propertyDefaultValue, token.queryContinuationToken);
        }
        else {
            addPropertyToAllDocumentsImpl(propertyName, propertyDefaultValue);
        }

        function addPropertyToAllDocumentsImpl(propertyName, propertyDefaultValue, queryContinuationToken) {
            var requestOptions = { continuation: queryContinuationToken };
            var query = `
                SELECT * FROM c
                WHERE c.EntityType = 'SalesLine'
                    AND c.Status IN ('Approved','Preview','SentToFo')
                    -- AND c.CreatedAt < '2023-09-27T21:00:00'
                    AND (
                        (c.Rate != 0 AND c.CutType = 'Total')
                        OR
                        (c.CutType != 'Total' )
                    )
                    AND NOT IS_DEFINED(c.${propertyName})
                    -- AND c.TruckTicketNumber = 'DCFST10001574-WT'
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
                            element[propertyName] = propertyDefaultValue;

                            collection.replaceDocument(element._self, element, function (err) {
                                if (err) throw err;
                            });

                            updated++;
                        })
                    }

                    if (responseOptions.continuation) {
                        addPropertyToAllDocumentsImpl(propertyName, propertyDefaultValue, responseOptions.continuation)
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
