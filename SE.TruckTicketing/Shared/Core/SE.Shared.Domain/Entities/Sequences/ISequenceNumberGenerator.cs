using System.Collections.Generic;

namespace SE.Shared.Domain.Entities.Sequences;

public interface ISequenceNumberGenerator
{
    IAsyncEnumerable<string> GenerateSequenceNumbers(string sequenceType, string prefix, int count, string infix = null, string suffix = null);
}
