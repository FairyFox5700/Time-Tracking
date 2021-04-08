using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace TimeTracking.Common.Pagination
{
    public class PagedResult<T> : PagedDataBase
    {
        public IEnumerable<T> Items { get; }

        public bool IsEmpty => Items == null || !Items.Any();
        public bool IsNotEmpty => !IsEmpty;

        public PagedResult()
        {
            Items = Enumerable.Empty<T>();
        }

        [JsonConstructor]
        protected PagedResult(IEnumerable<T> items,
            int currentPage, int resultsPerPage,
            int totalPages, long totalResults) :
            base(currentPage, resultsPerPage, totalPages, totalResults)
        {
            Items = items;
        }

        public static PagedResult<T> Paginate(IEnumerable<T> items,
            int currentPage, int resultsPerPage,
            int totalPages, long totalResults)
            => new PagedResult<T>(items, currentPage, resultsPerPage, totalPages, totalResults);

        public static PagedResult<T> PaginateFromBase(PagedDataBase result, IEnumerable<T> items)
            => new PagedResult<T>(items, result.CurrentPage, result.ResultsPerPage,
                result.TotalPages, result.TotalResults);

        public static PagedResult<T> Empty => new PagedResult<T>();
    }
}