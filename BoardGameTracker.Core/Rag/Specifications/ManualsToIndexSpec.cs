using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.Rag.Specifications;

public sealed class ManualsToIndexSpec : Specification<Manual>
{
    public ManualsToIndexSpec()
    {
        Query.Where(m =>
            m.IndexStatus == ManualIndexStatus.Pending ||
            m.IndexStatus == ManualIndexStatus.Failed ||
            m.IndexStatus == ManualIndexStatus.Indexing);
    }
}
