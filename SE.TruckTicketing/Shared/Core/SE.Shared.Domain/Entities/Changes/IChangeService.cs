using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using SE.Shared.Common.Changes;

namespace SE.Shared.Domain.Entities.Changes;

public interface IChangeService
{
    List<FieldChange> CompareObjects(Type type, JObject source, JObject target, ChangeConfiguration config);
}
