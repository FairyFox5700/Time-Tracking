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
    public class MileStoneService : IMileStoneService
    {
        private readonly ILogger<Milestone> _logger;
        private readonly IBaseMapper<Milestone, MilestoneDto> _milestoneMapper;
        private readonly IModelMapper<Milestone, MilestoneDetailsDto> _milestoneDetailsMapper;
        private readonly IMilestoneRepository _milestoneRepository;
        private readonly IUserProvider _userProvider;
        private readonly IProjectRepository _projectRepository;

        public MileStoneService(ILogger<Milestone> logger,
            IBaseMapper<Milestone, MilestoneDto> milestoneMapper,
            IModelMapper<Milestone, MilestoneDetailsDto> milestoneDetailsMapper,
            IMilestoneRepository milestoneRepository,
            IUserProvider userProvider,
            IProjectRepository projectRepository)
        {
            _logger = logger;
            _milestoneMapper = milestoneMapper;
            _milestoneDetailsMapper = milestoneDetailsMapper;
            _milestoneRepository = milestoneRepository;
            _userProvider = userProvider;
            _projectRepository = projectRepository;
        }

        public async Task<ApiResponse<MilestoneDto>> CreateMileStoneAsync(MilestoneDto dto)
        {
            try
            {
                var projectFound = await _projectRepository.GetByIdAsync(dto.ProjectId);
                if (projectFound == null)
                {
                    _logger.LogWarning("Failed to found project by id {0}", dto.ProjectId);
                    return new ApiResponse<MilestoneDto>()
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ResponseException = new ApiError(ErrorCode.ProjectNotFound, ErrorCode.ProjectNotFound.GetDescription())
                    };
                }
                var entityToAdd = _milestoneMapper.MapToEntity(dto);
                entityToAdd.CreatedByUserId = _userProvider.GetUserId();
                entityToAdd = await _milestoneRepository.AddAsync(entityToAdd);
                if (entityToAdd != null) return new ApiResponse<MilestoneDto>(_milestoneMapper.MapToModel(entityToAdd));
                _logger.LogWarning("Failed to create milestone entity {0}", JsonConvert.SerializeObject(dto));
                return new ApiResponse<MilestoneDto>()
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    ResponseException = new ApiError(ErrorCode.MilestoneCreationFiled, ErrorCode.MilestoneCreationFiled.GetDescription())
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured while creating milestone {0}", JsonConvert.SerializeObject(dto));
                return ApiResponse<MilestoneDto>.InternalError();
            }
        }

        public async Task<ApiResponse<MilestoneDetailsDto>> GetMileStoneById(Guid mileStoneId)
        {
            try
            {
                var milestone = await _milestoneRepository.GetByIdAsync(mileStoneId);
                if (milestone == null)
                {
                    return new ApiResponse<MilestoneDetailsDto>()
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ResponseException = new ApiError(ErrorCode.MileStoneNotFound, ErrorCode.MileStoneNotFound.GetDescription())
                    };
                }
                else
                {
                    var milestoneDetailsDto = _milestoneDetailsMapper.MapToModel(milestone);
                    return new ApiResponse<MilestoneDetailsDto>(milestoneDetailsDto);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "An error occured while getting milestone by id {0} ", mileStoneId);
                return ApiResponse<MilestoneDetailsDto>.InternalError();
            }
        }

        public async Task<ApiPagedResponse<MilestoneDetailsDto>> GetAllMilestonesPaged(PagedRequest request)
        {
            var pagedList = await _milestoneRepository.GetAllPagedAsync(request.Page, request.PageSize);
            return new ApiPagedResponse<MilestoneDetailsDto>().FromPagedResult(pagedList, _milestoneDetailsMapper.MapToModel);
        }
    }
}