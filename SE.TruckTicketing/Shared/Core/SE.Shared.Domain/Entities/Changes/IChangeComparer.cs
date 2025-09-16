using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace SE.Shared.Domain.Entities.Changes;

public interface IChangeComparer
{
    List<FieldChange> Compare(JToken source, JToken target);
}
