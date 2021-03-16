﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TimeTracking.Bl.Abstract.Services;
using TimeTracking.Common.Requests;
using TimeTracking.Common.Wrapper;
using TimeTracking.Models;
using TimeTracking.Models.Requests;

namespace TimeTracking.WebApi.Controllers
{
    [ApiController]
    [Route("issue")]
    public class IssueController : ControllerBase
    {
        private readonly IIssueService _issueService;

        public IssueController(IIssueService issueService)
        {
            _issueService = issueService;
        }
        
        
        /// <summary>
        /// Assigns issue to user 
        /// </summary>
        /// <param name="request">AssignIssueToUserRequest</param>
        /// <returns></returns>
        [HttpPost]
        [Route("assign-to-user")]
        public async Task<ApiResponse> AssignIssueToUser([FromQuery]AssignIssueToUserRequest request)
        {
            return await _issueService.AssignIssueToUser(request);
        }
        
        /// <summary>
        /// Change status of issue by id 
        /// </summary>
        /// <param name="request">ChangeIssueStatusRequest</param>
        /// <returns></returns>
        [HttpPost]
        [Route("change-status")]
        public async Task<ApiResponse> ChangeIssueStatus([FromQuery]ChangeIssueStatusRequest request)
        {
            return await _issueService.ChangeIssueStatus(request.Status,request.IssueId);
        }
        
        /// <summary>
        /// Assigns issue to user by id
        /// </summary>
        /// <param name="issueId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        public async Task<ApiResponse<IssueDetailsDto>> AssignIssueToUser([FromRoute]Guid issueId)
        {
            return await _issueService.GetIssueByIdAsync(issueId);
        }
        
        /// <summary>
        /// Creates issue by request
        /// </summary>
        /// <param name="issueDto"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("create-issue")]
        public async Task<ApiResponse<IssueDto>> CreateIssue([FromBody]IssueDto issueDto)
        {
            return await _issueService.CreateIssue(issueDto);
        }
        
        /// <summary>
        /// Returns paged response with all issues
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ApiPagedResponse<IssueDetailsDto>> GetAllIssuesAsync([FromRoute] PagedRequest request)
        {
            return await _issueService.GetAllIssuesAsync(request);
        }
        
    }
}