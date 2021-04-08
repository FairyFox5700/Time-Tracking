#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoMockHelper.Core;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using TimeTracking.Bl.Abstract.Services;
using TimeTracking.Bl.Impl.Helpers;
using TimeTracking.Bl.Impl.Services;
using TimeTracking.Common.Helpers;
using TimeTracking.Common.Mappers;
using TimeTracking.Common.Pagination;
using TimeTracking.Common.Requests;
using TimeTracking.Common.Wrapper;
using TimeTracking.Dal.Abstract.Repositories;
using TimeTracking.Entities;
using TimeTracking.Models;
using TimeTracking.Models.Requests;
using TimeTracking.UnitTests.Data;

namespace TimeTracking.UnitTests.Services
{
    [TestFixture]
    public class WorkLogServiceTests : AutoMockContext<WorkLogService>
    {

        private static Fixture Fixture = new Fixture();
        # region GetWorkLog
        [Test]
        public async Task GetWorkLog_WhenNotFoundById_ShouldReturnWorkLogNotFoundResponse()
        {
            var id = Guid.NewGuid();
            MockFor<IWorklogRepository>().Setup(e => e.GetByIdAsync(id))
                .ReturnsAsync((WorkLog)null);

            var response = await ClassUnderTest.GetWorkLog(id);

            response.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.WorkLogNotFound);
        }

        [Test]
        public async Task GetWorkLog_WhenFoundById_ShouldReturnMappedWorklogDtoResponse()
        {
            var id = Guid.NewGuid();
            WorkLogDto modelAfterMap = new WorkLogDto();
            MockFor<IWorklogRepository>().Setup(e => e.GetByIdAsync(id))
                .ReturnsAsync(new WorkLog());
            MockFor<IBaseMapper<WorkLog, WorkLogDto>>().Setup(e => e.MapToModel(It.IsAny<WorkLog>()))
                .Returns(modelAfterMap);

            var response = await ClassUnderTest.GetWorkLog(id);

            response.VerifySuccessResponseWithData(modelAfterMap);
        }

        [Test]
        public async Task GetWorkLog_WhenExceptionThrown_ShouldReturnInternalErrorResponse()
        {
            var id = Guid.NewGuid();
            MockFor<IWorklogRepository>().Setup(e => e.GetByIdAsync(id))
                .ThrowsAsync(new Exception());

            var response = await ClassUnderTest.GetWorkLog(id);

            response.VerifyInternalError();
        }
        #endregion

        #region GetAllWorkLogsPaged


        [Test]
        public async Task GetAllWorkLogsPaged_GetAllPagedAsync_ReturnsAllWorkLogsPaged()
        {
            var pagedRequest = new PagedRequest()
            {
                Page = 1,
                PageSize = 2
            };
            var calls = 0;
            var pagedWorkLogsItems = WorklogsDbSet.WorkLogBuilder().CreateMany<WorkLog>();
            var pagedWorkLogsItemsAfterMap = Fixture.CreateMany<WorkLogDetailsDto>().ToList();
            var pagedWorkLogs = PagedResult<WorkLog>.Paginate(pagedWorkLogsItems, 1, 2, 4, 8);
            MockFor<IWorklogRepository>().Setup(e => e.GetAllPagedAsync(pagedRequest.Page, pagedRequest.PageSize))
                .ReturnsAsync(pagedWorkLogs);
            MockFor<IModelMapper<WorkLog, WorkLogDetailsDto>>().Setup(e => e.MapToModel(It.IsAny<WorkLog>()))
                .Returns(() => pagedWorkLogsItemsAfterMap[calls])
                .Callback(() => calls++);

            var response = await ClassUnderTest.GetAllWorkLogsPaged(pagedRequest);

            response.StatusCode.Should().Be(200);
            response.Data.Should().NotBeNull();
            response.Data.Count.Should().Be(pagedWorkLogs.Items.Count());
            response.CurrentPage.Should().Be(pagedWorkLogs.CurrentPage);
            response.ResultsPerPage.Should().Be(pagedWorkLogs.ResultsPerPage);
            response.TotalPages.Should().Be(pagedWorkLogs.TotalPages);
            response.TotalResults.Should().Be(pagedWorkLogs.TotalResults);
            response.Data.Should().BeEquivalentTo(pagedWorkLogsItemsAfterMap);
        }


        #endregion

        #region CreateWorkLog

        [Test]
        public async Task CreateIssue_WhenIssueNotFound_ReturnFailedResponse()
        {
            var workLogPassed = WorklogsDbSet.WorkLogBuilder().Create<WorkLogDto>();
            var issueNotFoundResponse = new ApiResponse<IssueDetailsDto>() { IsSuccess = false };
            MockFor<IIssueService>().Setup(x => x.GetIssueByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(issueNotFoundResponse);

            var result = await ClassUnderTest.CreateWorkLog(workLogPassed);

            result.Should().BeEquivalentTo(issueNotFoundResponse.ToFailed<WorkLogDto>());
        }

        [Test]
        public async Task CreateIssue_WhenAddFailed_ReturnWorkLogCreationFailedResponse()
        {
            var workLogPassed = WorklogsDbSet.WorkLogBuilder().Create<WorkLogDto>();
            var mappedWorkLog = WorklogsDbSet.WorkLogBuilder().Create<WorkLog>();
            MockFor<IIssueService>().Setup(x => x.GetIssueByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new ApiResponse<IssueDetailsDto>() { IsSuccess = true });
            MockFor<IBaseMapper<WorkLog, WorkLogDto>>().Setup(e => e.MapToEntity(workLogPassed))
                .Returns(mappedWorkLog);
            var userId = Fixture.Create<Guid>();
            MockFor<IUserProvider>().Setup(e => e.GetUserId())
                .Returns(userId);
            MockFor<IWorklogRepository>().Setup(e => e.AddAsync(mappedWorkLog))
                .ReturnsAsync((WorkLog)null);

            var result = await ClassUnderTest.CreateWorkLog(workLogPassed);

            result.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.WorkLogCreationFailed);
        }

        [Test]
        public async Task CreateIssue_WhenAddSuccess_ReturnMappedResponse()
        {
            var workLogPassed = WorklogsDbSet.WorkLogBuilder().Create<WorkLogDto>();
            var mappedWorkLog = WorklogsDbSet.WorkLogBuilder().Create<WorkLog>();
            MockFor<IIssueService>().Setup(x => x.GetIssueByIdAsync(workLogPassed.IssueId))
                .ReturnsAsync(new ApiResponse<IssueDetailsDto>() { IsSuccess = true });
            MockFor<IBaseMapper<WorkLog, WorkLogDto>>().Setup(e => e.MapToEntity(workLogPassed))
                .Returns(mappedWorkLog);
            MockFor<IBaseMapper<WorkLog, WorkLogDto>>().Setup(e => e.MapToModel(It.IsAny<WorkLog>()))
                .Returns(workLogPassed);
            var userId = Fixture.Create<Guid>();
            MockFor<IUserProvider>().Setup(e => e.GetUserId())
                .Returns(userId);
            MockFor<IWorklogRepository>().Setup(e => e.AddAsync(mappedWorkLog))
                .ReturnsAsync(new WorkLog());

            var result = await ClassUnderTest.CreateWorkLog(workLogPassed);

            mappedWorkLog.IssueId.Should().Be(workLogPassed.IssueId);
            mappedWorkLog.UserId.Should().Be(userId);
            result.VerifySuccessResponseWithData(workLogPassed);
        }


        [Test]
        public async Task CreateIssue_WhenExceptionThrown_ReturnsInternalError()
        {
            var workLogPassed = WorklogsDbSet.WorkLogBuilder().Create<WorkLogDto>();
            var mappedWorkLog = WorklogsDbSet.WorkLogBuilder().Create<WorkLog>();
            MockFor<IIssueService>().Setup(x => x.GetIssueByIdAsync(workLogPassed.IssueId))
                .ReturnsAsync(new ApiResponse<IssueDetailsDto>() { IsSuccess = true });
            MockFor<IBaseMapper<WorkLog, WorkLogDto>>().Setup(e => e.MapToEntity(workLogPassed))
                .Returns(mappedWorkLog);
            MockFor<IBaseMapper<WorkLog, WorkLogDto>>().Setup(e => e.MapToModel(It.IsAny<WorkLog>()))
                .Returns(workLogPassed);
            var userId = Fixture.Create<Guid>();
            MockFor<IUserProvider>().Setup(e => e.GetUserId())
                .Returns(userId);
            MockFor<IWorklogRepository>().Setup(e => e.AddAsync(mappedWorkLog))
                .ThrowsAsync(new Exception());

            var result = await ClassUnderTest.CreateWorkLog(workLogPassed);

            result.VerifyInternalError();
        }
        #endregion

        #region UpdateWorkLogStatus


        [Test]
        public async Task UpdateWorkLogStatus_WhenWorklogNotFound_ReturnWorkLogNotFoundResponse()
        {
            var statusApproved = It.IsAny<bool>();
            var workLogId = It.IsAny<Guid>();
            MockFor<IWorklogRepository>().Setup(x => x.GetByIdWithUserAsync(workLogId))
                .ReturnsAsync((WorkLog)null);

            var result = await ClassUnderTest.UpdateWorkLogStatus(workLogId, statusApproved, default);

            result.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.WorkLogNotFound);
        }

        [Test]
        public async Task UpdateWorkLogStatus_WhenUpdateWorklogSuccess_ReturnsMappedWorklog()
        {
            var statusApproved = It.IsAny<bool>();
            var workLogId = It.IsAny<Guid>();
            var description = It.IsAny<string?>();
            var workLogFound = WorklogsDbSet.WorkLogBuilder().With(x => x.TimeTrackingUser, UsersDbSet.TimeTrackingUserBuilder().Create()).Create<WorkLog>();
            var workLogMapped = WorklogsDbSet.WorkLogBuilder().Create<WorkLogDto>();
            MockFor<IWorklogRepository>().Setup(x => x.GetByIdWithUserAsync(workLogId))
                .ReturnsAsync(workLogFound);
            MockFor<IWorklogRepository>().Setup(x => x.UpdateAsync(workLogFound))
                .ReturnsAsync(workLogFound);
            MockFor<IBaseMapper<WorkLog, WorkLogDto>>().Setup(x => x.MapToModel(workLogFound))
                .Returns(workLogMapped);

            var result = await ClassUnderTest.UpdateWorkLogStatus(workLogId, statusApproved, description);

            if (statusApproved)
            {
                MockFor<IEmailHelper>().Verify(x =>
                    x.SendEmailWithValidationOfWorkLogFailed(workLogFound.TimeTrackingUser.Email, description));
            }
            workLogFound.IsApproved.Should().Be(statusApproved);
            result.VerifySuccessResponseWithData(workLogMapped);
        }

        [Test]
        public async Task UpdateWorkLogStatus_WhenExceptionThrown_ReturnsInternalError()
        {
            var statusApproved = It.IsAny<bool>();
            var workLogId = It.IsAny<Guid>();
            var description = It.IsAny<string?>();
            var workLogFound = WorklogsDbSet.WorkLogBuilder()
                .With(x => x.TimeTrackingUser, UsersDbSet.TimeTrackingUserBuilder().Create()).Create<WorkLog>();
            var workLogMapped = WorklogsDbSet.WorkLogBuilder().Create<WorkLogDto>();
            MockFor<IWorklogRepository>().Setup(x => x.GetByIdWithUserAsync(workLogId))
                .ReturnsAsync(workLogFound);
            MockFor<IWorklogRepository>().Setup(x => x.UpdateAsync(workLogFound))
                .ThrowsAsync(new Exception());

            var result = await ClassUnderTest.UpdateWorkLogStatus(workLogId, statusApproved, description);

            result.VerifyInternalError();
        }

        #endregion

        #region DeleteWorkLog

        [Test]
        public async Task DeleteWorkLog_WhenWorklogNotFound_ReturnWorkLogNotFoundResponse()
        {
            var workLogId = It.IsAny<Guid>();
            MockFor<IWorklogRepository>().Setup(x => x.GetByIdAsync(workLogId))
                .ReturnsAsync((WorkLog)null);

            var response = await ClassUnderTest.DeleteWorkLog(workLogId);

            response.StatusCode.Should().Be(400);
            response.ResponseException.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ResponseException!.ErrorCode.Should().Be(ErrorCode.WorkLogNotFound);
            response.ResponseException.ErrorMessage.Should().NotBeNull(ErrorCode.WorkLogNotFound.GetDescription());
        }

        [Test]
        public async Task DeleteWorkLog_WhenDeleteSuccess_ReturnSuccessStatusCode()
        {
            var workLogId = It.IsAny<Guid>();
            var workLogFound = WorklogsDbSet.WorkLogBuilder().Create();
            MockFor<IWorklogRepository>().Setup(x => x.GetByIdAsync(workLogId))
                .ReturnsAsync(workLogFound);

            var response = await ClassUnderTest.DeleteWorkLog(workLogId);

            MockFor<IWorklogRepository>().Verify(x => x.DeleteAsync(workLogFound));
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(200);
        }

        [Test]
        public async Task DeleteWorkLog_WhenExceptionThrown_ReturnInternalError()
        {
            var workLogId = It.IsAny<Guid>();
            var workLogFound = WorklogsDbSet.WorkLogBuilder().Create();
            MockFor<IWorklogRepository>().Setup(x => x.GetByIdAsync(workLogId))
                .ReturnsAsync(workLogFound);
            MockFor<IWorklogRepository>().Setup(x => x.DeleteAsync(workLogFound))
                .ThrowsAsync(new Exception());

            var response = await ClassUnderTest.DeleteWorkLog(workLogId);

            MockFor<IWorklogRepository>().Verify(x => x.DeleteAsync(workLogFound));
            response.IsSuccess.Should().BeFalse();
            response.ResponseException!.ErrorCode.Should().Be(ErrorCode.InternalError);
            response.ResponseException.ErrorMessage.Should().Be(ErrorCode.InternalError.GetDescription());
            response.StatusCode.Should().Be(500);
        }
        #endregion

        #region GetAllActivitiesForUser

        [Test]
        public async Task GetAllActivitiesForUser_WhenUseNotFound_ReturnWorkLogNotFoundResponse()
        {
            var request = Fixture.Freeze<ActivitiesRequest>();
            var userNotFoundResponse = new ApiResponse<TimeTrackingUserDetailsDto>() { IsSuccess = false };
            MockFor<IUserService>().Setup(x => x.GetUsersById(request.UserId))
                .ReturnsAsync(userNotFoundResponse);

            var response = await ClassUnderTest.GetAllActivitiesForUser(request);

            response.Should().BeEquivalentTo(userNotFoundResponse.ToFailed<UserActivityDto>());
        }

        [Test]
        public async Task GetAllActivitiesForUser_WhenUseFoundAndWorkLogFound_ReturnExpectedUserWorklogs()
        {
            var request = Fixture.Freeze<ActivitiesRequest>();
            var userFoundResponse = new ApiResponse<TimeTrackingUserDetailsDto>()
            { IsSuccess = true, Data = UsersDbSet.TimeTrackingUserBuilder().Create<TimeTrackingUserDetailsDto>() };
            MockFor<IUserService>().Setup(x => x.GetUsersById(request.UserId))
                .ReturnsAsync(userFoundResponse);
            var expectedWorkLogs = WorklogsDbSet.WorkLogBuilder().With(w => w.Issue, IssuesDbSet.IssueBuilder().Create())
                .CreateMany<WorkLog>().ToList();
            var expectedMappedWorkLogs = WorklogsDbSet.WorkLogBuilder().CreateMany<WorkLogDetailsDto>().ToList();
            MockFor<IWorklogRepository>()
                .Setup(e => e.GetActivitiesWithDetailsByUserId(request.UserId, request.ProjectId))
                .ReturnsAsync(expectedWorkLogs);
            var calls = 0;
            MockFor<IModelMapper<WorkLog, WorkLogDetailsDto>>().Setup(e => e.MapToModel(It.IsAny<WorkLog>()))
                .Returns(() => expectedMappedWorkLogs[calls])
                .Callback(() => calls++);
            var userActivityExpected = new UserActivityDto()
            {
                UserId = request.UserId,
                UserName = userFoundResponse.Data.FirstName,
                UserSurname = userFoundResponse.Data.LastName,
                UserEmail = userFoundResponse.Data.Email,
                ProjectName = expectedWorkLogs?.Select(e => e.Issue?.Project?.Name).FirstOrDefault(),
                TotalWorkLogInSeconds = (long?)expectedWorkLogs?.Sum(e => e.TimeSpent.TotalSeconds),
                WorkLogItems = expectedMappedWorkLogs,
            };

            var response = await ClassUnderTest.GetAllActivitiesForUser(request);

            response.Data.Should().BeEquivalentTo(userActivityExpected);
            response.StatusCode.Should().Be(200);
            response.IsSuccess.Should().BeTrue();
        }


        [Test]
        public async Task GetAllActivitiesForUser_WhenUserFoundAndEmptyWorkLogs_ReturnExpectedUserWorklogs()
        {
            var request = Fixture.Freeze<ActivitiesRequest>();
            var userFoundResponse = new ApiResponse<TimeTrackingUserDetailsDto>()
            { IsSuccess = true, Data = UsersDbSet.TimeTrackingUserBuilder().Create<TimeTrackingUserDetailsDto>() };
            MockFor<IUserService>().Setup(x => x.GetUsersById(request.UserId))
                .ReturnsAsync(userFoundResponse);
            MockFor<IWorklogRepository>()
                .Setup(e => e.GetActivitiesWithDetailsByUserId(request.UserId, request.ProjectId))
                .ReturnsAsync((List<WorkLog>)null);
            var userActivityExpected = new UserActivityDto()
            {
                UserId = request.UserId,
                UserName = userFoundResponse.Data.FirstName,
                UserSurname = userFoundResponse.Data.LastName,
                UserEmail = userFoundResponse.Data.Email,
                ProjectName = null,
                TotalWorkLogInSeconds = null,
                WorkLogItems = null,
            };

            var response = await ClassUnderTest.GetAllActivitiesForUser(request);

            response.Data.Should().BeEquivalentTo(userActivityExpected);
            response.StatusCode.Should().Be(200);
            response.IsSuccess.Should().BeTrue();
        }

        [Test]
        public async Task GetAllActivitiesForUser_WhenExceptionThrown_ReturnInternalError()
        {
            var request = Fixture.Freeze<ActivitiesRequest>();
            var userFoundResponse = new ApiResponse<TimeTrackingUserDetailsDto>()
            { IsSuccess = true, Data = UsersDbSet.TimeTrackingUserBuilder().Create<TimeTrackingUserDetailsDto>() };
            MockFor<IUserService>().Setup(x => x.GetUsersById(request.UserId))
                .ReturnsAsync(userFoundResponse);
            MockFor<IWorklogRepository>()
                .Setup(e => e.GetActivitiesWithDetailsByUserId(request.UserId, request.ProjectId))
                .ThrowsAsync(new Exception());

            var response = await ClassUnderTest.GetAllActivitiesForUser(request);

            response.VerifyInternalError();
        }
        #endregion

        #region UpdateWorkLog
        [Test]
        public async Task UpdateWorkLog_WhenNotFound_ShouldReturnWorkLogNotFound()
        {
            var request = Fixture.Create<WorkLogUpdateRequest>();
            MockFor<IWorklogRepository>().Setup(x => x.GetByIdAsync(request.WorkLogId))
                .ReturnsAsync((WorkLog)null);

            var response = await ClassUnderTest.UpdateWorkLog(request);

            response.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.WorkLogNotFound);
        }

        [Test]
        public async Task UpdateWorkLog_WhenExceptionThrown_ShouldReturnInternalError()
        {
            var request = Fixture.Create<WorkLogUpdateRequest>();
            var worklog = new WorkLog();
            MockFor<IWorklogRepository>().Setup(x => x.GetByIdAsync(request.WorkLogId))
                .ReturnsAsync(worklog);

            var response = await ClassUnderTest.UpdateWorkLog(request);

            worklog.Description = request.Description;
            worklog.ActivityType = request.ActivityType;
            worklog.StartDate = request.StartDate;
            worklog.TimeSpent = request.TimeSpent;
            MockFor<IWorklogRepository>().Verify(w => w.UpdateAsync(worklog));
            response.VerifyInternalError();

        }

        [Test]
        public async Task UpdateWorkLog_WhenFound_ShouldUpdateWorkLog()
        {
            var request = Fixture.Create<WorkLogUpdateRequest>();
            MockFor<IWorklogRepository>().Setup(x => x.GetByIdAsync(request.WorkLogId))
                .ThrowsAsync(new Exception());

            var response = await ClassUnderTest.UpdateWorkLog(request);

            response.VerifyInternalError();

        }
        #endregion

    }
}