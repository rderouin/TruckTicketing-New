// ExecQuery STORED PROCEDURE
function ExecQuery(query) { 
    var collection = getContext().getCollection();
    console.log("starting proc");  
    console.log(query);

    // Query documents and take 1st item.
    var isAccepted = collection.queryDocuments(
        collection.getSelfLink(), query,
    function (err, feed, options) {
        if (err) throw err;

        // Check the feed and if empty, set the body to 'no docs found', 
        // else take 1st element from feed
        if (!feed || !feed.length) {
            var response = getContext().getResponse();
            response.setBody('[]');
        }
        else {
            var response = getContext().getResponse();
            var body = feed;
            response.setBody(JSON.stringify(body));
        }
    });

    if (!isAccepted) throw new Error('The query was not accepted by the server.');
}