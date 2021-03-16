﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TimeTracking.Bl.Abstract.Services;
using TimeTracking.Common.Wrapper;
using TimeTracking.Models;

namespace TimeTracking.WebApi.Controllers
{
    [ApiController]
    [Route("api/team")]
    public class TeamController : ControllerBase
    {
        private readonly ITeamService _teamService;

        
        /// <summary>
        /// Team controller
        /// </summary>
        /// <param name="teamService"></param>
        public TeamController(ITeamService teamService )
        {
            _teamService = teamService;
        }
        
        /// <summary>
        /// Get team by id
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{teamId}")]
        public async Task<ApiResponse<TeamDetailsDto>> GetTeamById([FromRoute]Guid teamId)
        {
            return await _teamService.GetTeamById(teamId);
        }

        /// <summary>
        /// Creates team by request
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("create")]
        public async Task<ApiResponse<TeamDto>> CreateTeamAsync([FromBody]TeamDto dto)
        {
            return await _teamService.CreateTeamAsync(dto);
        }
    }
}