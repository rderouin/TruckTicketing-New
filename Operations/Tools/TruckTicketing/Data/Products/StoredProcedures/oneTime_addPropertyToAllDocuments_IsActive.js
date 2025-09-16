/**
 * This is a one time stored procedure to add the IsActive property to documents
 * in the Operations container for DocumentType = 'Products'.
 * The logic is idempotent and the SP can be executed multiple times.
 *
 * Account Name: zcac-cosmos-<env>-truckticketing
 * Container Name: Products
 *
 * @param {*} partitionKey: must be passed - Products
 * 
 * @param {string} continuationToken - optional, needed when using Powershell script to invoke for large number of documents
 */
function oneTime_addPropertyToAllDocuments_IsActive(continuationToken) {
    var propertyName = 'IsActive';
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
            var query = `SELECT * FROM root r WHERE NOT IS_DEFINED(r.${propertyName})`;

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
