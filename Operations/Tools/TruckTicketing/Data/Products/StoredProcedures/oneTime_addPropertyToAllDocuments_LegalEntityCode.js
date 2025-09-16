/**
 * This is a one time stored procedure to populate the LegalEntityCode
 * in the Products container by using a lookup using LegalEntityId as a key.
 * The logic is idempotent and the SP can be executed multiple times.
 *
 * Account Name: zcac-cosmos-<env>-truckticketing
 * Container Name: Products
 * 
 * @param {*} partitionKey: must be passed - Products
 * 
 * @param {string} envName - should be devint|qa|uat|prod
 * @param {string} continuationToken - optional, needed when using Powershell script to invoke for large number of documents
 */
function oneTime_addPropertyToAllDocuments_LegalEntityCode(
  envName,
  continuationToken
) {
  var propertyName = "LegalEntityCode";
  var lookupPropertyName = "LegalEntityId";

  validateEnvName(envName);

  var lookupPropertyMap = getPropertyLookupMap(envName);

  addPropertyUsingMapToAllDocuments(
    propertyName,
    lookupPropertyName,
    lookupPropertyMap,
    continuationToken
  );

  function getPropertyLookupMap(envName) {
    var data = [];

    data["prod"] = [
      {
        Id: "81d044c6-62ce-4e2c-9133-8d0241788bb9",
        Code: "FLIS",
        Name: "Secure Energy (Partnership)",
      },
      {
        Id: "d152f132-3803-4b20-b6b0-c49dca56dcd8",
        Code: "MAEC",
        Name: "Secure Energy (Drilling Services) Inc.",
      },
      {
        Id: "c0de05a8-cc7b-4c77-b9ea-12723ea7b6f0",
        Code: "MAEU",
        Name: "SECURE Drilling Services USA LLC",
      },
      {
        Id: "a90768e8-b6ef-4457-bf07-2a2b2a037824",
        Code: "SESC",
        Name: "Secure Energy Services Inc.",
      },
      {
        Id: "97c7e667-dbc3-4874-b388-6bb43ad10ea6",
        Code: "SESU",
        Name: "Secure Energy USA LLC",
      },
    ];

    data["uat"] = data["prod"];

    data["qa"] = data["prod"];

    data["devint"] = [
      {
        Id: "81daf38c-88fc-4350-baff-87fdc14b66f4",
        Code: "CKR2",
        Name: "FEB 24 TEST",
      },
      {
        Id: "1b645f4b-a1bd-4c39-bb12-f7d1a942311e",
        Code: "CKRR",
        Name: "CRANDELL LE",
      },
      {
        Id: "9e5a6769-7dfc-4e65-b467-ec526bd2f964",
        Code: "FLIS",
        Name: "Secure Energy (Partnership)",
      },
      {
        Id: "5f58d530-a236-43b3-ba6d-2d2677eda284",
        Code: "FMFN",
        Name: "Fort McMurray First Nation (Formerly CRE JV)",
      },
      {
        Id: "9ae43a9e-52b0-4be2-a09b-009ab1214cd2",
        Code: "MAEC",
        Name: "Secure Energy (Drilling Services) Inc.",
      },
      {
        Id: "40d71b4e-fb4d-4ffa-a5a1-e41ccb2722d2",
        Code: "MAEC",
        Name: "SECURE ENERGY (DRILLING SERVICES) INC.",
      },
      {
        Id: "0aab04d2-bf40-44cd-8fe1-0a65b42d1bb8",
        Code: "MAEU",
        Name: "SECURE Drilling Services USA LLC",
      },
      {
        Id: "2516ea83-dd93-4af8-9c30-a95bac183c27",
        Code: "MEOW",
        Name: "Translation Company",
      },
      {
        Id: "a09d5227-77f8-476e-b86a-da100ecbad6c",
        Code: "MUEL",
        Name: "Consolidation & Elimination",
      },
      {
        Id: "588c3223-16b0-4748-80c9-8f35604f4e6e",
        Code: "NOPC",
        Name: "CAD Functional Currency Non-Operating Entities",
      },
      {
        Id: "7430339f-47ca-44fb-a2a3-fe8df5ccd349",
        Code: "NOPU",
        Name: "USD Functional Currency Non-Operating Entities",
      },
      {
        Id: "fefaac14-b4cc-424d-a075-17dc0e17fda1",
        Code: "SATI",
        Name: "Secure Alida Terminal Inc. JV",
      },
      {
        Id: "343bf745-cf1f-41d6-ba7a-2240266bb8a1",
        Code: "SESC",
        Name: "Secure Energy Services Inc.",
      },
      {
        Id: "2ac7845f-4096-4e04-b1e1-70c39aae131a",
        Code: "SESU",
        Name: "Secure Energy USA LLC",
      },
      {
        Id: "fc960028-eb22-46c6-ad8d-fc6867f4798d",
        Code: "TESI",
        Name: "Tervita Environmental Services Inc.",
      },
      {
        Id: "991034d5-1be5-45da-97a2-8fa526b5edef",
        Code: "TST3",
        Name: "TEST JM 0208",
      },
    ];

    var propertyLookupMap = data[envName].reduce((acc, cv) => {
      acc[cv.Id] = cv.Code;
      return acc;
    }, {});

    return propertyLookupMap;
  }

  function addPropertyUsingMapToAllDocuments(
    propertyName,
    lookupPropertyName,
    lookupPropertyMap,
    continuationToken
  ) {
    var response = getContext().getResponse();
    var collection = getContext().getCollection();
    var updated = 0;

    if (continuationToken) {
      var token = JSON.parse(continuationToken);

      if (!token.queryContinuationToken) {
        throw new Error("Could not parse continuation token");
      }

      updated = token.updatedSoFar;

      addPropertyUsingMapToAllDocumentsImpl(
        propertyName,
        lookupPropertyName,
        lookupPropertyMap,
        token.queryContinuationToken
      );
    } else {
      addPropertyUsingMapToAllDocumentsImpl(
        propertyName,
        lookupPropertyName,
        lookupPropertyMap
      );
    }

    function addPropertyUsingMapToAllDocumentsImpl(
      propertyName,
      lookupPropertyName,
      lookupPropertyMap,
      queryContinuationToken
    ) {
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
            response.setBody("No docs found");
          } else {
            feed.forEach((element) => {
              var lookupPropertyValue = element[lookupPropertyName];
              var propertyValue = lookupPropertyMap[lookupPropertyValue];
              element[propertyName] = propertyValue;

              //console.log(`lookupPropertyName: ${lookupPropertyName}`);
              //console.log(`lookupPropertyValue: ${lookupPropertyValue}`);
              //console.log(`Adding ${propertyName}: ${propertyValue}`);
              //console.log(`Adding ${propertyName}: ${propertyValue}`);

              collection.replaceDocument(
                element._self,
                element,
                function (err) {
                  if (err) throw err;
                }
              );

              updated++;
            });
          }

          if (responseOptions.continuation) {
            addPropertyUsingMapToAllDocumentsImpl(
              propertyName,
              lookupPropertyName,
              lookupPropertyMap,
              responseOptions.continuation
            );
          } else {
            response.setBody({ count: updated, continuation: null });
          }
        }
      );

      if (!isAccepted) {
        var sprocToken = JSON.stringify({
          updatedSoFar: updated,
          queryContinuationToken: queryContinuationToken
        });

        response.setBody({ count: null, continuation: sprocToken });
      }
    }
  }

  function validateEnvName(envName) {
    var supportedEnvNames = ["devint", "qa", "uat", "prod"];

    if (!envName) {
      throw new Error(`${envName} must be populated`);
    }

    if (!supportedEnvNames.includes(envName)) {
      throw new Error(
        `${envName} must be one of ${supportedEnvNames.join("|")}`
      );
    }
  }
}
