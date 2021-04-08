using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoMockHelper.Core;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Moq;
using NUnit.Framework;
using TimeTracking.Bl.Abstract.Services;
using TimeTracking.Bl.Impl.Services;
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
    public class IssueServiceTests : AutoMockContext<IssueService>
    {
        private static Fixture Fixture = new Fixture();

        # region GetIssueByIdAsync
        [Test]
        public async Task GetIssueByIdAsync_WhenNotFoundById_ShouldReturnMileStoneNotFoundResponse()
        {
            var id = Guid.NewGuid();
            MockFor<IIssueRepository>().Setup(e => e.GetIssueWithDetails(id))
                .ReturnsAsync((Issue)null);

            var response = await ClassUnderTest.GetIssueByIdAsync(id);

            response.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.IssueNotFound);
        }

        [Test]
        public async Task GetMileStoneById_WhenFoundById_ShouldReturnMappedMileStoneDtoResponse()
        {
            var id = Guid.NewGuid();
            IssueDetailsDto modelAfterMap = new IssueDetailsDto();
            Issue issueFound = IssuesDbSet.IssueBuilder().With(w => w.WorkLogs, WorklogsDbSet.WorkLogBuilder().CreateMany(4).ToList).Create();
            MockFor<IIssueRepository>().Setup(e => e.GetIssueWithDetails(id))
                .ReturnsAsync(issueFound);
            MockFor<IModelMapper<Issue, IssueDetailsDto>>().Setup(e => e.MapToModel(It.IsAny<Issue>()))
                .Returns(modelAfterMap);

            var response = await ClassUnderTest.GetIssueByIdAsync(id);
            modelAfterMap.TotalRemainingTimeInSeconds = (long)(issueFound.ClosedAt - issueFound.OpenedAt).TotalSeconds;
            modelAfterMap.TotalSpentTimeInSeconds = issueFound.WorkLogs.Sum(e => e.TimeSpent.Seconds);
            response.VerifySuccessResponseWithData(modelAfterMap);

        }

        [Test]
        public async Task GetIssueById_WhenExceptionThrown_ShouldReturnInternalErrorResponse()
        {
            var id = Guid.NewGuid();
            MockFor<IIssueRepository>().Setup(e => e.GetIssueWithDetails(id))
                .ThrowsAsync(new Exception());

            var response = await ClassUnderTest.GetIssueByIdAsync(id);

            response.VerifyInternalError();
        }
        #endregion

        #region CreateIssue

        [Test]
        public async Task CreateIssue_WhenIssueModelPassedWithMilestoneIdButFailedToFoundMilestone_ReturnFailedResponse()
        {
            IssueDto issuePassed = IssuesDbSet.IssueBuilder().Create<IssueDto>();
            var issueMapped = new Issue();
            var foundedMilestone = new ApiResponse<MilestoneDetailsDto>() { IsSuccess = false };
            MockFor<IMileStoneService>().Setup(x => x.GetMileStoneById(It.IsAny<Guid>()))
                .ReturnsAsync(foundedMilestone);
            MockFor<IBaseMapper<Issue, IssueDto>>().Setup(e => e.MapToEntity(issuePassed))
                .Returns(issueMapped);
            var result = await ClassUnderTest.CreateIssue(issuePassed);

            result.Should().BeEquivalentTo(foundedMilestone.ToFailed<IssueDto>());
        }

        [Test]
        public async Task CreateIssue_WhenIssueModelPassedAndProjectNotFound_ShouldReturnProjectNotFound()
        {
            IssueDto issuePassed = IssuesDbSet.IssueBuilder().Create<IssueDto>();
            var foundedMilestone = new ApiResponse<MilestoneDetailsDto>() { IsSuccess = true };
            var projectNotFoundResponse = new ApiResponse<ProjectDetailsDto>() { IsSuccess = false };
            var issueMapped = new Issue();
            MockFor<IMileStoneService>().Setup(x => x.GetMileStoneById(It.IsAny<Guid>()))
                .ReturnsAsync(foundedMilestone);
            MockFor<IBaseMapper<Issue, IssueDto>>().Setup(e => e.MapToEntity(issuePassed))
                .Returns(issueMapped);
            MockFor<IProjectService>().Setup(e => e.GetProjectByIdAsync(issuePassed.ProjectId))
                .ReturnsAsync(projectNotFoundResponse);

            var result = await ClassUnderTest.CreateIssue(issuePassed);

            result.Should().BeEquivalentTo(projectNotFoundResponse.ToFailed<IssueDto>());
        }

        [Test]
        public async Task CreateIssue_WhenIssueModelPassedAndProjectFoundButAddFailed_ShouldReturnProjectAddFailedResponse()
        {
            IssueDto issuePassed = IssuesDbSet.IssueBuilder().Create<IssueDto>();
            var issueMapped = new Issue();
            MockFor<IMileStoneService>().Setup(x => x.GetMileStoneById(It.IsAny<Guid>()))
                .ReturnsAsync(new ApiResponse<MilestoneDetailsDto>() { IsSuccess = true });
            MockFor<IBaseMapper<Issue, IssueDto>>().Setup(e => e.MapToEntity(issuePassed))
                .Returns(issueMapped);
            MockFor<IProjectService>().Setup(e => e.GetProjectByIdAsync(issuePassed.ProjectId))
                .ReturnsAsync(new ApiResponse<ProjectDetailsDto>() { IsSuccess = true });
            var mockedTime = Fixture.Freeze<DateTimeOffset>();
            MockFor<ISystemClock>().SetupGet(e => e.UtcNow)
                .Returns(mockedTime);
            MockFor<IIssueRepository>().Setup(e => e.AddAsync(issueMapped))
                .ReturnsAsync((Issue)null);

            var result = await ClassUnderTest.CreateIssue(issuePassed);

            issueMapped.ProjectId.Should().Be(issueMapped.ProjectId);
            issueMapped.OpenedAt.Should().Be(mockedTime);
            issueMapped.Status.Should().Be(Status.Open);
            result.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.IssueCreationFailed);
        }

        [Test]
        public async Task CreateIssue_WhenIssueModelPassedAndProjectFoundAndAddSuccess_ShouldReturnAddedIssueDto()
        {
            IssueDto issuePassed = IssuesDbSet.IssueBuilder().Create<IssueDto>();
            var issueMapped = new Issue();
            MockFor<IMileStoneService>().Setup(x => x.GetMileStoneById(It.IsAny<Guid>()))
                .ReturnsAsync(new ApiResponse<MilestoneDetailsDto>() { IsSuccess = true });
            MockFor<IBaseMapper<Issue, IssueDto>>().Setup(e => e.MapToEntity(issuePassed))
                .Returns(issueMapped);
            MockFor<IProjectService>().Setup(e => e.GetProjectByIdAsync(issuePassed.ProjectId))
                .ReturnsAsync(new ApiResponse<ProjectDetailsDto>() { IsSuccess = true });
            var mockedTime = Fixture.Freeze<DateTimeOffset>();
            MockFor<ISystemClock>().SetupGet(e => e.UtcNow)
                .Returns(mockedTime);
            MockFor<IIssueRepository>().Setup(e => e.AddAsync(issueMapped))
                .ReturnsAsync(new Issue());

            var result = await ClassUnderTest.CreateIssue(issuePassed);

            issueMapped.ProjectId.Should().Be(issueMapped.ProjectId);
            issueMapped.OpenedAt.Should().Be(mockedTime);
            issueMapped.Status.Should().Be(Status.Open);
            result.VerifySuccessResponseWithData(issuePassed);
        }


        [Test]
        public async Task CreateIssue_WhenExceptionThrown_ShouldReturnInternalError()
        {
            IssueDto issuePassed = IssuesDbSet.IssueBuilder().Create<IssueDto>();
            var issueMapped = new Issue();
            MockFor<IMileStoneService>().Setup(x => x.GetMileStoneById(It.IsAny<Guid>()))
                .ReturnsAsync(new ApiResponse<MilestoneDetailsDto>() { IsSuccess = true });
            MockFor<IBaseMapper<Issue, IssueDto>>().Setup(e => e.MapToEntity(issuePassed))
                .Returns(issueMapped);
            MockFor<IProjectService>().Setup(e => e.GetProjectByIdAsync(issuePassed.ProjectId))
                .ReturnsAsync(new ApiResponse<ProjectDetailsDto>() { IsSuccess = true });
            var mockedTime = Fixture.Freeze<DateTimeOffset>();
            MockFor<ISystemClock>().SetupGet(e => e.UtcNow)
                .Returns(mockedTime);
            MockFor<IIssueRepository>().Setup(e => e.AddAsync(issueMapped))
                .ThrowsAsync(new Exception());

            var result = await ClassUnderTest.CreateIssue(issuePassed);

            result.VerifyInternalError();
        }
        #endregion


        #region GetAllIssuesAsync
        [Test]
        public async Task GetAllIssuesAsync_WhenRequestedWithPage_ReturnsAllIssuesPaged()
        {
            var pagedRequest = new PagedRequest()
            {
                Page = 1,
                PageSize = 2
            };
            var calls = 0;
            var pagedItems = IssuesDbSet.IssueBuilder().CreateMany<Issue>();
            var pagedItemsAfterMap = Fixture.CreateMany<IssueDetailsDto>().ToList();
            var pagedIssues = PagedResult<Issue>.Paginate(pagedItems, 1, 2, 4, 8);
            MockFor<IIssueRepository>().Setup(e => e.GetAllIssueWithDetails(pagedRequest.Page, pagedRequest.PageSize))
                .ReturnsAsync(pagedIssues);
            MockFor<IModelMapper<Issue, IssueDetailsDto>>().Setup(e => e.MapToModel(It.IsAny<Issue>()))
                .Returns(() => pagedItemsAfterMap[calls])
                .Callback(() => calls++);

            var response = await ClassUnderTest.GetAllIssuesAsync(pagedRequest);

            response.VerifyCorrectPagination(pagedIssues, pagedItemsAfterMap);
        }
        #endregion

        #region  ChangeIssueStatus


        [TestCase(Status.Closed)]
        [TestCase(Status.Closed)]
        public async Task ChangeIssueStatus_WhenNotFound_ReturnsIssueNotFoundResponse(Status status)
        {
            var id = Guid.NewGuid();
            MockFor<IIssueRepository>().Setup(e => e.GetByIdAsync(id))
                .ReturnsAsync((Issue)null);

            var response = await ClassUnderTest.ChangeIssueStatus(status, id);

            response.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.IssueNotFound);
        }

        [TestCase(Status.Closed)]
        [TestCase(Status.Open)]
        public async Task ChangeIssueStatus_WhenIssueNotFound_ReturnsIssueNotFoundResponse(Status status)
        {
            var id = Guid.NewGuid();
            var issueFound = IssuesDbSet.IssueBuilder().Create<Issue>();
            var modelAfterMap = new IssueDto();
            MockFor<IIssueRepository>().Setup(e => e.GetByIdAsync(id))
                                .ReturnsAsync(issueFound);
            MockFor<IIssueRepository>().Setup(e => e.UpdateAsync(issueFound))
                .ReturnsAsync(issueFound);
            MockFor<IBaseMapper<Issue, IssueDto>>().Setup(e => e.MapToModel(It.IsAny<Issue>()))
                .Returns(modelAfterMap);
            var mockedTime = Fixture.Freeze<DateTimeOffset>();
            MockFor<ISystemClock>().SetupGet(e => e.UtcNow)
                .Returns(mockedTime);

            var response = await ClassUnderTest.ChangeIssueStatus(status, id);

            if (status == Status.Closed)
            {
                issueFound.ClosedAt.Should().Be(mockedTime);
            }
            if (status == Status.Open)
            {
                issueFound.OpenedAt.Should().Be(mockedTime);
            }
            issueFound.UpdatedAt.Should().Be(mockedTime);
            issueFound.Status.Should().Be(status);
            response.VerifySuccessResponseWithData(modelAfterMap);
        }


        [TestCase(Status.Closed)]
        [TestCase(Status.Open)]
        public async Task ChangeIssueStatus_WhenExceptionThrows_ReturnsInternalErrorResponse(Status status)
        {
            var id = Guid.NewGuid();
            var issueFound = IssuesDbSet.IssueBuilder().Create<Issue>();
            var modelAfterMap = new IssueDetailsDto();
            MockFor<IIssueRepository>().Setup(e => e.GetByIdAsync(id))
                .ReturnsAsync(issueFound);
            MockFor<IIssueRepository>().Setup(e => e.UpdateAsync(issueFound))
                .ThrowsAsync(new Exception());

            var response = await ClassUnderTest.ChangeIssueStatus(status, id);

            response.VerifyInternalError();
        }

        #endregion

        #region AssignIssueToUser

        [Test]
        public async Task AssignIssueToUser_WhenIssueNotFound_ReturnsIssueNotFoundResponse()
        {
            var request = Fixture.Freeze<AssignIssueToUserRequest>();
            var id = Guid.NewGuid();
            MockFor<IIssueRepository>().Setup(e => e.GetByIdAsync(request.IssueId))
                .ReturnsAsync((Issue)null);

            var response = await ClassUnderTest.AssignIssueToUser(request);

            response.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.IssueNotFound);
        }

        [Test]
        public async Task AssignIssueToUser_WhenUserNotFound_ReturnsUserNotFoundResponse()
        {
            var request = Fixture.Freeze<AssignIssueToUserRequest>();
            var issueFound = IssuesDbSet.IssueBuilder().Create<Issue>();
            var userNotFoundResponse = new ApiResponse<TimeTrackingUserDetailsDto>()
            {
                IsSuccess = false,
            };
            MockFor<IIssueRepository>().Setup(e => e.GetByIdAsync(request.IssueId))
                .ReturnsAsync(issueFound);
            MockFor<IUserService>().Setup(e => e.GetUsersById(request.UserId))
                .ReturnsAsync(userNotFoundResponse);

            var response = await ClassUnderTest.AssignIssueToUser(request);

            response.Should().BeEquivalentTo(userNotFoundResponse.ToFailed<IssueDto>());
        }


        [Test]
        public async Task AssignIssueToUser_WhenUserAndIssueFoundById_ReturnsMappedModelAfterUpdate()
        {
            var request = Fixture.Freeze<AssignIssueToUserRequest>();
            var modelAfterMap = new IssueDto();
            var issueFound = IssuesDbSet.IssueBuilder().Create<Issue>();
            MockFor<IIssueRepository>().Setup(e => e.GetByIdAsync(request.IssueId))
                .ReturnsAsync(issueFound);
            MockFor<IIssueRepository>().Setup(e => e.UpdateAsync(issueFound))
                .ReturnsAsync(issueFound);
            MockFor<IUserService>().Setup(e => e.GetUsersById(request.UserId))
                .ReturnsAsync(new ApiResponse<TimeTrackingUserDetailsDto>() { IsSuccess = true });
            var mockedTime = Fixture.Freeze<DateTimeOffset>();
            MockFor<ISystemClock>().SetupGet(e => e.UtcNow)
                .Returns(mockedTime);
            MockFor<IBaseMapper<Issue, IssueDto>>().Setup(e => e.MapToModel(It.IsAny<Issue>()))
                .Returns(modelAfterMap);

            var response = await ClassUnderTest.AssignIssueToUser(request);


            issueFound.AssignedToUserId.Should().Be(request.UserId);
            issueFound.UpdatedAt.Should().Be(mockedTime);
            response.VerifySuccessResponseWithData(modelAfterMap);
        }


        [Test]
        public async Task AssignIssueToUser_WhenExceptionThrows_ReturnsInternalErrorResponse()
        {
            var request = Fixture.Freeze<AssignIssueToUserRequest>();
            var issueFound = IssuesDbSet.IssueBuilder().Create<Issue>();
            MockFor<IIssueRepository>().Setup(e => e.GetByIdAsync(request.IssueId))
                .ReturnsAsync(new Issue());
            MockFor<IIssueRepository>().Setup(e => e.UpdateAsync(issueFound))
                .ThrowsAsync(new Exception());

            var response = await ClassUnderTest.AssignIssueToUser(request);

            response.VerifyInternalError();
        }

        #endregion

    }
}