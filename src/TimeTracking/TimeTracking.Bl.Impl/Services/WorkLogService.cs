using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TimeTracking.Bl.Abstract.Services;
using TimeTracking.Bl.Impl.Helpers;
using TimeTracking.Common.Abstract;
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
    public class WorkLogService : IWorkLogService
    {
        private readonly ILogger<WorkLogService> _logger;
        private readonly IBaseMapper<WorkLog, WorkLogDto> _worklogMapper;
        private readonly IUserProvider _userProvider;
        private readonly IModelMapper<WorkLog, WorkLogDetailsDto> _worklogDetailsMapper;
        private readonly IWorklogRepository _worklogRepository;
        private readonly IIssueService _issueService;
        private readonly IEmailHelper _emailHelper;
        private readonly IUserService _userService;

        public WorkLogService(ILogger<WorkLogService> logger,
            IBaseMapper<WorkLog, WorkLogDto> worklogMapper,
            IUserProvider userProvider,
            IUserService userService,
            IWorklogRepository worklogRepository,
            IIssueService issueService,
            IEmailHelper emailHelper,
            IModelMapper<WorkLog, WorkLogDetailsDto> worklogDetailsMapper)
        {
            _logger = logger;
            _worklogMapper = worklogMapper;
            _userProvider = userProvider;
            _worklogRepository = worklogRepository;
            _issueService = issueService;
            _emailHelper = emailHelper;
            _userService = userService;
            _worklogDetailsMapper = worklogDetailsMapper;
        }

        public async Task<ApiResponse<WorkLogDto>> CreateWorkLog(WorkLogDto dto)
        {
            try
            {
                var issueFoundResponse = await _issueService.GetIssueByIdAsync(dto.IssueId);
                if (!issueFoundResponse.IsSuccess)
                {
                    return issueFoundResponse.ToFailed<WorkLogDto>();

                }
                var entityToAdd = _worklogMapper.MapToEntity(dto);
                entityToAdd.IssueId = dto.IssueId;
                entityToAdd.UserId = _userProvider.GetUserId();
                entityToAdd = await _worklogRepository.AddAsync(entityToAdd);
                if (entityToAdd != null) return new ApiResponse<WorkLogDto>(dto);
                _logger.LogWarning("Failed to create entity {0}", JsonConvert.SerializeObject(dto));
                return new ApiResponse<WorkLogDto>()
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    ResponseException = new ApiError(ErrorCode.WorkLogCreationFailed, ErrorCode.WorkLogCreationFailed.GetDescription())
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An error occured while creating new worklog {0} ", JsonConvert.SerializeObject(dto));
                return ApiResponse<WorkLogDto>.InternalError();
            }
        }

        public async Task<ApiResponse<UserActivityDto>> GetAllActivitiesForUser(ActivitiesRequest request)
        {
            try
            {
                var userFoundResponse = await _userService.GetUsersById(request.UserId);
                if (!userFoundResponse.IsSuccess)
                    return userFoundResponse.ToFailed<UserActivityDto>();

                var workLogActivities = (await _worklogRepository.GetActivitiesWithDetailsByUserId(request.UserId, request.ProjectId));
                var userActivity = new UserActivityDto()
                {
                    UserId = request.UserId,
                    UserName = userFoundResponse.Data.FirstName,
                    UserSurname = userFoundResponse.Data.LastName,
                    UserEmail = userFoundResponse.Data.Email,
                    ProjectName = workLogActivities?.Select(e => e.Issue?.Project?.Name).FirstOrDefault(),
                    TotalWorkLogInSeconds = (long?)workLogActivities?.Sum(e => e.TimeSpent.TotalSeconds),
                    WorkLogItems = workLogActivities?.Select(e => _worklogDetailsMapper.MapToModel(e)).ToList(),
                };
                return new ApiResponse<UserActivityDto>(userActivity);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "An error occured while fetching workLog activities for user {0} ", request.UserId);
                return ApiResponse<UserActivityDto>.InternalError();
            }
        }

        public async Task<ApiPagedResponse<WorkLogDetailsDto>> GetAllWorkLogsPaged(PagedRequest pagedRequest)
        {
            var listOfRecords = await _worklogRepository.GetAllPagedAsync(pagedRequest.Page, pagedRequest.PageSize);
            return new ApiPagedResponse<WorkLogDetailsDto>().FromPagedResult(listOfRecords, _worklogDetailsMapper.MapToModel);
        }



        public async Task<ApiResponse<WorkLogDto>> UpdateWorkLog(WorkLogUpdateRequest workLogUpdateRequest)
        {
            try
            {
                var workLog = await _worklogRepository.GetByIdAsync(workLogUpdateRequest.WorkLogId);
                if (workLog == null)
                {
                    return new ApiResponse<WorkLogDto>()
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ResponseException = new ApiError(ErrorCode.WorkLogNotFound, ErrorCode.WorkLogNotFound.GetDescription())
                    };
                }
                workLog.Description = workLogUpdateRequest.Description;
                workLog.ActivityType = workLogUpdateRequest.ActivityType;
                workLog.StartDate = workLogUpdateRequest.StartDate;
                workLog.TimeSpent = workLogUpdateRequest.TimeSpent;
                workLog = await _worklogRepository.UpdateAsync(workLog);
                return new ApiResponse<WorkLogDto>(_worklogMapper.MapToModel(workLog));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured while updating workLog");
                return ApiResponse<WorkLogDto>.InternalError();
            }
        }
        public async Task<ApiResponse<WorkLogDto>> GetWorkLog(Guid workLogId)
        {
            try
            {
                var workLog = await _worklogRepository.GetByIdAsync(workLogId);
                if (workLog == null)
                {
                    return new ApiResponse<WorkLogDto>()
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ResponseException = new ApiError(ErrorCode.WorkLogNotFound, ErrorCode.WorkLogNotFound.GetDescription())
                    };
                }
                var workLogDto = _worklogMapper.MapToModel(workLog);
                return new ApiResponse<WorkLogDto>(workLogDto);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "An error occured while getting worklog by id {0} ", workLogId);
                return ApiResponse<WorkLogDto>.InternalError();
            }
        }


        public async Task<ApiResponse<WorkLogDto>> UpdateWorkLogStatus(Guid workLogId, bool isApproved, string description = null)
        {
            try
            {
                var workLog = await _worklogRepository.GetByIdWithUserAsync(workLogId);
                if (workLog == null)
                {
                    return new ApiResponse<WorkLogDto>()
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ResponseException = new ApiError(ErrorCode.WorkLogNotFound, ErrorCode.WorkLogNotFound.GetDescription())
                    };
                }

                if (!isApproved)
                {
                    await _emailHelper.SendEmailWithValidationOfWorkLogFailed(workLog.TimeTrackingUser.Email,
                        description);
                }
                workLog.IsApproved = isApproved;
                workLog = await _worklogRepository.UpdateAsync(workLog);
                return new ApiResponse<WorkLogDto>(_worklogMapper.MapToModel(workLog));
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "An error occured while updating workLog {0] status to {1} ", workLogId, isApproved);
                return ApiResponse<WorkLogDto>.InternalError();
            }
        }
        public async Task<ApiResponse> DeleteWorkLog(Guid workLogId)
        {
            try
            {
                var workLogFound = await _worklogRepository.GetByIdAsync(workLogId);
                if (workLogFound == null)
                {
                    _logger.LogWarning("Worklog by id {0} was not found in database", workLogId);
                    return new ApiResponse()
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ResponseException = new ApiError(ErrorCode.WorkLogNotFound, ErrorCode.WorkLogNotFound.GetDescription())
                    };
                }
                await _worklogRepository.DeleteAsync(workLogFound);
                return ApiResponse.Success();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "An error occured while deleting workLog {0] ", workLogId);
                return ApiResponse<WorkLogDto>.InternalError();
            }
        }
    }
}