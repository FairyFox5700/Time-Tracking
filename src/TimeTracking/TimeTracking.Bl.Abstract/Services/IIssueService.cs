using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TimeTracking.Common.Requests;
using TimeTracking.Common.Wrapper;
using TimeTracking.Entities;
using TimeTracking.Models;
using TimeTracking.Models.Requests;

namespace TimeTracking.Bl.Abstract.Services
{
    public interface IIssueService
    {
        Task<ApiResponse<IssueDto>> CreateIssue(IssueDto dto);
        Task<ApiResponse<IssueDto>> AssignIssueToUser(AssignIssueToUserRequest request);
        Task<ApiResponse<IssueDto>> ChangeIssueStatus(Status status, Guid issueId);
        Task<ApiResponse<IssueDetailsDto>> GetIssueByIdAsync(Guid issueId);
        Task<ApiPagedResponse<IssueDetailsDto>> GetAllIssuesAsync(PagedRequest request);
    }
}