using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly ITeamRepository _teamRepository;
        private readonly IUserRepository _userRepository;
        private readonly IModelMapper<TimeTrackingUser, TimeTrackingUserDetailsDto> _userDetailsMapper;
        private readonly IBaseMapper<TimeTrackingUser, TimeTrackingUserDto> _userMapper;

        public UserService(ILogger<UserService> logger,
            ITeamRepository teamRepository,
            IUserRepository userRepository,
            IBaseMapper<TimeTrackingUser, TimeTrackingUserDto> userMapper,
            IModelMapper<TimeTrackingUser, TimeTrackingUserDetailsDto> userDetailsMapper)
        {
            _logger = logger;
            _teamRepository = teamRepository;
            _userRepository = userRepository;
            _userMapper = userMapper;
            _userDetailsMapper = userDetailsMapper;
        }


        public async Task<ApiResponse<TimeTrackingUserDto>> AddUserToTeam(AssignUserToTeamRequest request)
        {
            try
            {
                var teamFounded = await _teamRepository.GetByIdAsync(request.TeamId);
                if (teamFounded == null)
                {
                    _logger.LogWarning("Failed to found team by id {0}", request.TeamId);
                    return new ApiResponse<TimeTrackingUserDto>()
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ResponseException = new ApiError(ErrorCode.TeamNotFound, ErrorCode.TeamNotFound.GetDescription())
                    };
                }
                var userFound = await _userRepository.GetByIdAsync(request.UserId);
                if (userFound == null)
                {
                    _logger.LogWarning("Failed to find user by id {0}", request.UserId);
                    return new ApiResponse<TimeTrackingUserDto>()
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ResponseException = new ApiError(ErrorCode.UserNotFound, ErrorCode.UserNotFound.GetDescription())
                    };
                }

                userFound.TeamId = request.TeamId;
                userFound = await _userRepository.UpdateAsync(userFound);
                return new ApiResponse<TimeTrackingUserDto>(_userMapper.MapToModel(userFound));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An error occured while assigning user {0] to team team {1} ", request.UserId, request.TeamId);
                return ApiResponse<TimeTrackingUserDto>.InternalError();
            }
        }
        public async Task<ApiPagedResponse<TimeTrackingUserDetailsDto>> GetAllUsers(PagedRequest request)
        {
            var listOfRecords = await _userRepository.GetAllPagedAsync(request.Page, request.PageSize);
            return new ApiPagedResponse<TimeTrackingUserDetailsDto>().FromPagedResult(listOfRecords,
                _userDetailsMapper.MapToModel);
        }

        public async Task<ApiResponse<TimeTrackingUserDetailsDto>> GetUsersById(Guid userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<TimeTrackingUserDetailsDto>(new ApiError()
                    {
                        ErrorCode = ErrorCode.UserNotFound,
                        ErrorMessage = ErrorCode.UserNotFound.GetDescription(),
                    }, statusCode: 400);
                }

                return new ApiResponse<TimeTrackingUserDetailsDto>(_userDetailsMapper.MapToModel(user));
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "An error occured while getting user by id {0} ", userId);
                return ApiResponse<TimeTrackingUserDetailsDto>.InternalError();
            }
        }
    }
}