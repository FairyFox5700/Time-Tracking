using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using TimeTracking.Common.Helpers;
using TimeTracking.Common.Pagination;
using TimeTracking.Common.Wrapper;
using TimeTracking.Models;

namespace TimeTracking.UnitTests
{
    public static class ResponseVerificationHelper
    {

        public static void VerifyInternalError<T>(this ApiResponse<T> response)
        {
            response.StatusCode.Should().Be(500);
            response.ResponseException.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ResponseException!.ErrorCode.Should().Be(ErrorCode.InternalError);
            response.ResponseException.ErrorMessage.Should().NotBeNull(ErrorCode.InternalError.GetDescription());
        }

        public static void VerifyNotSuccessResponseWithErrorCodeAndMessage<T>(this ApiResponse<T> response, ErrorCode errorCode,
            int statusCode = 400)
        {
            response.StatusCode.Should().Be(statusCode);
            response.ResponseException.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ResponseException!.ErrorCode.Should().Be(errorCode);
            response.ResponseException.ErrorMessage.Should().NotBeNull(errorCode.GetDescription());
        }

        public static void VerifySuccessResponse<T>(this ApiResponse<T> response, int statusCode = 200)
        {
            response.StatusCode.Should().Be(statusCode);
            response.ResponseException.Should().BeNull();
            response.IsSuccess.Should().BeTrue();
        }

        public static void VerifySuccessResponseWithData<T>(this ApiResponse<T> response, T data, int statusCode = 200)
        {
            response.StatusCode.Should().Be(statusCode);
            response.ResponseException.Should().BeNull();
            response.IsSuccess.Should().BeTrue();
            response.Data.Should().Be(data);
        }


        public static void VerifyCorrectPagination<T, TDto>(this ApiPagedResponse<TDto> response, PagedResult<T> expectedResult, IEnumerable<TDto> mappedResult)
            where TDto : class, new()
        {
            response.StatusCode.Should().Be(200);
            response.Data.Should().NotBeNull();
            response.Data.Count.Should().Be(expectedResult.Items.Count());
            response.CurrentPage.Should().Be(expectedResult.CurrentPage);
            response.ResultsPerPage.Should().Be(expectedResult.ResultsPerPage);
            response.TotalPages.Should().Be(expectedResult.TotalPages);
            response.TotalResults.Should().Be(expectedResult.TotalResults);
            response.Data.Should().BeEquivalentTo(mappedResult);
        }
    }
}