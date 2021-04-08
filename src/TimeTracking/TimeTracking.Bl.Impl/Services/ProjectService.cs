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
    public class ProjectService : IProjectService
    {
        private readonly ILogger<ProjectService> _logger;
        private readonly IBaseMapper<Project, ProjectDto> _projectMapper;
        private readonly IModelMapper<Project, ProjectDetailsDto> _projectDetailsMapper;
        private readonly IProjectRepository _projectRepository;

        public ProjectService(ILogger<ProjectService> logger,
            IBaseMapper<Project, ProjectDto> projectMapper,
            IModelMapper<Project, ProjectDetailsDto> projectDetailsMapper,
            IProjectRepository projectRepository)
        {
            _logger = logger;
            _projectMapper = projectMapper;
            _projectDetailsMapper = projectDetailsMapper;
            _projectRepository = projectRepository;
        }


        public async Task<ApiResponse<ProjectDto>> CreateProjectAsync(ProjectDto dto)
        {
            try
            {
                var entityToAdd = _projectMapper.MapToEntity(dto);
                entityToAdd = await _projectRepository.AddAsync(entityToAdd);
                if (entityToAdd != null) return new ApiResponse<ProjectDto>(_projectMapper.MapToModel(entityToAdd));
                _logger.LogWarning("Failed to create entity {0}", JsonConvert.SerializeObject(dto));
                return new ApiResponse<ProjectDto>()
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    ResponseException = new ApiError(ErrorCode.ProjectCreationFailed, ErrorCode.IssueCreationFailed.GetDescription())
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured while adding project entity {0}", JsonConvert.SerializeObject(dto));
                return ApiResponse<ProjectDto>.InternalError();
            }
        }

        public async Task<ApiResponse<ProjectDetailsDto>> GetProjectByIdAsync(Guid projectId)
        {
            try
            {
                var project = await _projectRepository.GetByIdAsync(projectId);
                if (project == null)
                {
                    return new ApiResponse<ProjectDetailsDto>()
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ResponseException = new ApiError(ErrorCode.ProjectNotFound, ErrorCode.ProjectNotFound.GetDescription())
                    };
                }
                else
                {
                    var projectDetailsDto = _projectDetailsMapper.MapToModel(project);
                    return new ApiResponse<ProjectDetailsDto>(projectDetailsDto);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "An error occured while getting project by id {0} ", projectId);
                return ApiResponse<ProjectDetailsDto>.InternalError();
            }
        }

        public async Task<ApiPagedResponse<ProjectDetailsDto>> GetAllProjectPagedAsync(PagedRequest request)
        {
            var pagedList = await _projectRepository.GetAllPagedAsync(request.Page, request.PageSize);
            return new ApiPagedResponse<ProjectDetailsDto>().FromPagedResult(pagedList, _projectDetailsMapper.MapToModel);
        }
    }
}