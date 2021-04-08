using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TimeTracking.Bl.Abstract.Services;
using TimeTracking.Common.Helpers;
using TimeTracking.Common.Mappers;
using TimeTracking.Common.Pagination;
using TimeTracking.Common.Requests;
using TimeTracking.Common.Wrapper;
using TimeTracking.Dal.Abstract.Repositories;
using TimeTracking.Entities;
using TimeTracking.Models;
using TimeTracking.Models.Requests;

namespace TimeTracking.Bl.Impl.Services
{
    public class IssueService : IIssueService
    {
        private readonly ILogger<IssueService> _logger;
        private readonly IIssueRepository _issueRepository;
        private readonly IUserService _userService;
        private readonly IMileStoneService _mileStoneService;
        private readonly IProjectService _projectService;
        private readonly ISystemClock _systemClock;
        private readonly IBaseMapper<Issue, IssueDto> _issueMapper;
        private readonly IModelMapper<Issue, IssueDetailsDto> _issueDetailsMapper;

        public IssueService(ILogger<IssueService> logger,
            IIssueRepository issueRepository,
            IUserService userService,
            IMileStoneService mileStoneService,
            IProjectService projectService,
            ISystemClock systemClock,
            IBaseMapper<Issue, IssueDto> issueMapper,
            IModelMapper<Issue, IssueDetailsDto> issueDetailsMapper)
        {
            _logger = logger;
            _issueRepository = issueRepository;
            _userService = userService;
            _mileStoneService = mileStoneService;
            _projectService = projectService;
            _systemClock = systemClock;
            _issueMapper = issueMapper;
            _issueDetailsMapper = issueDetailsMapper;
        }

        public async Task<ApiResponse<IssueDto>> CreateIssue(IssueDto dto)
        {
            try
            {
                var entityToAdd = _issueMapper.MapToEntity(dto);
                if (dto.MilestoneId.HasValue)
                {
                    var mileStoneFoundResponse = await _mileStoneService.GetMileStoneById(dto.MilestoneId.Value);
                    if (!mileStoneFoundResponse.IsSuccess)
                    {
                        return mileStoneFoundResponse.ToFailed<IssueDto>();
                    }
                }

                var projectFindResponse = await _projectService.GetProjectByIdAsync(dto.ProjectId);
                if (!projectFindResponse.IsSuccess)
                {
                    return projectFindResponse.ToFailed<IssueDto>();
                }
                entityToAdd.ProjectId = dto.ProjectId;
                entityToAdd.OpenedAt = _systemClock.UtcNow;
                entityToAdd.Status = Status.Open;
                entityToAdd = await _issueRepository.AddAsync(entityToAdd);
                if (entityToAdd != null)
                {
                    return new ApiResponse<IssueDto>(dto);
                }
                _logger.LogWarning("Failed to create entity {0}", JsonConvert.SerializeObject(dto));
                return new ApiResponse<IssueDto>()
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    ResponseException = new ApiError(ErrorCode.IssueCreationFailed, ErrorCode.IssueCreationFailed.GetDescription())
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An error occured while creating new issue {0} ", JsonConvert.SerializeObject(dto));
                return ApiResponse<IssueDto>.InternalError();
            }
        }

        public async Task<ApiResponse<IssueDto>> AssignIssueToUser(AssignIssueToUserRequest request)
        {
            try
            {
                var entityFound = await _issueRepository.GetByIdAsync(request.IssueId);
                if (entityFound == null)
                {
                    _logger.LogWarning("Failed to found issue by id {0}", request.IssueId);
                    return new ApiResponse<IssueDto>()
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ResponseException = new ApiError(ErrorCode.IssueNotFound, ErrorCode.IssueNotFound.GetDescription())
                    };
                }

                var userFoundResponse = await _userService.GetUsersById(request.UserId);
                if (!userFoundResponse.IsSuccess)
                {
                    return userFoundResponse.ToFailed<IssueDto>();
                }

                entityFound.AssignedToUserId = request.UserId;
                entityFound.UpdatedAt = _systemClock.UtcNow;
                await _issueRepository.UpdateAsync(entityFound);
                return new ApiResponse<IssueDto>(_issueMapper.MapToModel(entityFound));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An error occured while assigning user {0} for issue {1} ", request.UserId, request.IssueId);
                return ApiResponse<IssueDto>.InternalError();
            }
        }


        public async Task<ApiResponse<IssueDto>> ChangeIssueStatus(Status status, Guid issueId)
        {
            try
            {
                var entityFound = await _issueRepository.GetByIdAsync(issueId);
                if (entityFound == null)
                {
                    _logger.LogWarning("Failed to found issue by id {0}", issueId);
                    return new ApiResponse<IssueDto>()
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ResponseException = new ApiError(ErrorCode.IssueNotFound, ErrorCode.IssueNotFound.GetDescription())
                    };
                }

                entityFound = ChangeEntityByStatus(status, entityFound);
                await _issueRepository.UpdateAsync(entityFound);
                return new ApiResponse<IssueDto>(_issueMapper.MapToModel(entityFound));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An error occured while updating status to {0} for issue {1} ", status, issueId);
                return ApiResponse<IssueDto>.InternalError();
            }
        }


        private Issue ChangeEntityByStatus(Status status, Issue issue)
        {
            switch (status)
            {
                case Status.Closed:
                    issue.ClosedAt = _systemClock.UtcNow;
                    break;
                case Status.Open:
                    issue.OpenedAt = _systemClock.UtcNow;
                    break;
            }
            issue.Status = status;
            issue.UpdatedAt = _systemClock.UtcNow;
            return issue;
        }

        public async Task<ApiResponse<IssueDetailsDto>> GetIssueByIdAsync(Guid issueId)
        {
            try
            {
                var issueWithDetails = await _issueRepository.GetIssueWithDetails(issueId);
                if (issueWithDetails == null)
                {
                    return new ApiResponse<IssueDetailsDto>()
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ResponseException = new ApiError(ErrorCode.IssueNotFound, ErrorCode.IssueNotFound.GetDescription())
                    };
                }
                else
                {
                    var issueDetailed = _issueDetailsMapper.MapToModel(issueWithDetails);
                    issueDetailed.TotalRemainingTimeInSeconds = (long)(issueWithDetails.ClosedAt - issueWithDetails.OpenedAt).TotalSeconds;
                    issueDetailed.TotalSpentTimeInSeconds = issueWithDetails.WorkLogs.Sum(e => e.TimeSpent.Seconds);
                    return new ApiResponse<IssueDetailsDto>(issueDetailed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An error occured while getting issue by id {0} ", issueId);
                return ApiResponse<IssueDetailsDto>.InternalError();
            }
        }

        public async Task<ApiPagedResponse<IssueDetailsDto>> GetAllIssuesAsync(PagedRequest request)
        {
            var response = new ApiPagedResponse<IssueDetailsDto>();
            var pagedList = await _issueRepository.GetAllIssueWithDetails(request.Page, request.PageSize);
            return response.FromPagedResult(pagedList, _issueDetailsMapper.MapToModel);
        }
    }
}