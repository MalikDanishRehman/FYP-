using System.Collections.Generic;

namespace AI_Driven_Water_Supply.Application.Common
{
    public sealed class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = new List<T>();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public bool HasNextPage { get; init; }
    }
}
