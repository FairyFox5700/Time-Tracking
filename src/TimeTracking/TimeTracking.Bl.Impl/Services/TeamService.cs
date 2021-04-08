using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

namespace TimeTracking.Bl.Impl.Services
{
    public class TeamService : ITeamService
    {
        private readonly ILogger<TeamService> _logger;
        private readonly IBaseMapper<Team, TeamDto> _teamMapper;
        private readonly IModelMapper<Team, TeamDetailsDto> _teamDetailsMapper;
        private readonly ITeamRepository _teamRepository;
        private readonly IProjectRepository _projectRepository;

        public TeamService(ILogger<TeamService> logger,
            IBaseMapper<Team, TeamDto> teamMapper,
            IModelMapper<Team, TeamDetailsDto> teamDetailsMapper,
            ITeamRepository teamRepository,
            IProjectRepository projectRepository)
        {
            _logger = logger;
            _teamMapper = teamMapper;
            _teamDetailsMapper = teamDetailsMapper;
            _teamRepository = teamRepository;
            _projectRepository = projectRepository;
        }
        public async Task<ApiResponse<TeamDto>> CreateTeamAsync(TeamDto dto)
        {
            try
            {
                var projectFound = await _projectRepository.GetByIdAsync(dto.ProjectId);
                if (projectFound == null)
                {
                    _logger.LogWarning("Failed to found project by id {0}", dto.ProjectId);
                    return new ApiResponse<TeamDto>()
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ResponseException = new ApiError(ErrorCode.ProjectNotFound, ErrorCode.ProjectNotFound.GetDescription())
                    };
                }
                var entityToAdd = _teamMapper.MapToEntity(dto);
                entityToAdd = await _teamRepository.AddAsync(entityToAdd);
                if (entityToAdd != null) return new ApiResponse<TeamDto>(dto);
                _logger.LogWarning("Failed to create entity {0}", JsonConvert.SerializeObject(dto));
                return new ApiResponse<TeamDto>()
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    ResponseException = new ApiError(ErrorCode.TeamCreationFailed, ErrorCode.TeamCreationFailed.GetDescription())
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An error occured while creating new team {0} ", JsonConvert.SerializeObject(dto));
                return ApiResponse<TeamDto>.InternalError();
            }
        }


        public async Task<ApiResponse<TeamDetailsDto>> GetTeamById(Guid teamId)
        {
            try
            {
                var teamFounded = await _teamRepository.GetByIdWithDetails(teamId);
                if (teamFounded == null)
                {
                    _logger.LogWarning("Failed to found team by id {0}", teamId);
                    return new ApiResponse<TeamDetailsDto>()
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ResponseException = new ApiError(ErrorCode.TeamNotFound, ErrorCode.TeamNotFound.GetDescription())
                    };
                }
                return new ApiResponse<TeamDetailsDto>(_teamDetailsMapper.MapToModel(teamFounded));
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "An error occured while getting team by id {0} ", teamId);
                return ApiResponse<TeamDetailsDto>.InternalError();
            }
        }

        public async Task<ApiPagedResponse<TeamDetailsDto>> GetAllTeamAsync(PagedRequest request)
        {
            var listOfRecords = await _teamRepository.GetAllPagedAsync(request.Page, request.PageSize);
            return new ApiPagedResponse<TeamDetailsDto>().FromPagedResult(listOfRecords, _teamDetailsMapper.MapToModel);
        }
    }

}