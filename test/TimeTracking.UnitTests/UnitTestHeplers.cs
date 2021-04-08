using System;
using System.Linq;
using FluentAssertions;
using TimeTracking.Common.Pagination;

namespace TimeTracking.UnitTests
{
    public static class UnitTestHelpers
    {
        public static void EnsurePagedResult<T>(this PagedResult<T> result, int count, int size, int page)
        {
            result.CurrentPage.Should().Be(page);
            result.TotalResults.Should().Be(count);
            result.ResultsPerPage.Should().Be(size);
            result.TotalPages.Should().Be((int)Math.Ceiling((decimal)count / size));
            result.Items.Count().Should().Be(size);
        }
    }
}