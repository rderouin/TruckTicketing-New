/**
 * This is a one time stored procedure to update the LoadConfirmationFrequency
 * in the Billing container for DocumentType = 'BillingConfiguration' and FieldTicketDeliveryMethod = 1.
 * The logic is idempotent and the SP can be executed multiple times.
 *
 * Account Name: zcac-cosmos-<env>-truckticketing
 * Container Name: Billing
 *
 * @param {*} partitionKey: must be passed - BillingConfiguration
 * 
 * @param {string} continuationToken - optional, needed when using Powershell script to invoke for large number of documents
 */
function oneTime_updatePropertyToDocuments_LoadConfirmationFrequency(continuationToken) {
    var propertyName = 'LoadConfirmationFrequency';
    var propertyDefaultValue = 'TicketByTicket';

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

        function addUpdatePropertyToAllDocumentsImpl(propertyName, propertyDefaultValue, queryContinuationToken) {
            var requestOptions = { continuation: queryContinuationToken };
            var query = `
                SELECT * FROM c WHERE c.DocumentType = 'BillingConfiguration' AND c.FieldTicketDeliveryMethod = 1
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
