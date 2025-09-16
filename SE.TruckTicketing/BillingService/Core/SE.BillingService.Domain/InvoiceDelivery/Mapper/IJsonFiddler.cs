using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace SE.BillingService.Domain.InvoiceDelivery.Mapper;

public interface IJsonFiddler
{
    IEnumerable<JValue> ReadValue(JObject source, string path);

    bool WriteValue(JObject target, string path, object value, IDictionary<string, int?> placementHint, ISet<string> dynamicIndexNames);
}
