using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoMockHelper.Core;
using Moq;
using NUnit.Framework;
using TimeTracking.Bl.Impl.Services;
using TimeTracking.Common.Mappers;
using TimeTracking.Common.Pagination;
using TimeTracking.Common.Requests;
using TimeTracking.Common.Wrapper;
using TimeTracking.Dal.Abstract.Repositories;
using TimeTracking.Entities;
using TimeTracking.Models;
using TimeTracking.UnitTests.Data;

namespace TimeTracking.UnitTests.Services
{

    [TestFixture]
    public class TeamServiceTests : AutoMockContext<TeamService>
    {
        private static Fixture Fixture = new Fixture();
        # region GetTeamById
        [Test]
        public async Task GetTeamById_WhenNotFoundById_ShouldReturnProjectNotFoundResponse()
        {
            var id = Guid.NewGuid();
            MockFor<ITeamRepository>().Setup(e => e.GetByIdWithDetails(id))
                .ReturnsAsync((Team)null);

            var response = await ClassUnderTest.GetTeamById(id);

            response.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.TeamNotFound);
        }

        [Test]
        public async Task GetTeamById_WhenFoundById_ShouldReturnMappedProjectDtoResponse()
        {
            var id = Guid.NewGuid();
            TeamDetailsDto modelAfterMap = new TeamDetailsDto();
            MockFor<ITeamRepository>().Setup(e => e.GetByIdWithDetails(id))
                .ReturnsAsync(new Team());
            MockFor<IModelMapper<Team, TeamDetailsDto>>().Setup(e => e.MapToModel(It.IsAny<Team>()))
                .Returns(modelAfterMap);

            var response = await ClassUnderTest.GetTeamById(id);

            response.VerifySuccessResponseWithData(modelAfterMap);
        }

        [Test]
        public async Task GetTeamById_WhenExceptionThrown_ShouldReturnInternalErrorResponse()
        {
            var id = Guid.NewGuid();
            MockFor<ITeamRepository>().Setup(e => e.GetByIdWithDetails(id))
                .ThrowsAsync(new Exception());

            var response = await ClassUnderTest.GetTeamById(id);

            response.VerifyInternalError();
        }
        # endregion

        #region GetAllTeamAsync
        [Test]
        public async Task GetAllTeamAsync_WhenRequestedWithPage_ReturnsAllTeamsPaged()
        {
            var pagedRequest = new PagedRequest()
            {
                Page = 1,
                PageSize = 2
            };
            var calls = 0;
            var pagedItems = TeamsDbSet.TeamBuilder().CreateMany<Team>();
            var pagedItemsAfterMap = Fixture.CreateMany<TeamDetailsDto>().ToList();
            var pagedTeams = PagedResult<Team>.Paginate(pagedItems, 1, 2, 4, 8);
            MockFor<ITeamRepository>().Setup(e => e.GetAllPagedAsync(pagedRequest.Page, pagedRequest.PageSize))
                .ReturnsAsync(pagedTeams);
            MockFor<IModelMapper<Team, TeamDetailsDto>>().Setup(e => e.MapToModel(It.IsAny<Team>()))
                .Returns(() => pagedItemsAfterMap[calls])
                .Callback(() => calls++);

            var response = await ClassUnderTest.GetAllTeamAsync(pagedRequest);

            response.VerifyCorrectPagination(pagedTeams, pagedItemsAfterMap);
        }
        #endregion


        #region CreateTeamAsync

        [Test]
        public async Task CreateTeamAsync_WhenProjectNotFound_ReturnProjectNotFoundResponse()
        {
            var teamPassed = TeamsDbSet.TeamBuilder().Create<TeamDto>();
            MockFor<IProjectRepository>().Setup(e => e.GetByIdAsync(teamPassed.ProjectId))
                .ReturnsAsync((Project)null);

            var result = await ClassUnderTest.CreateTeamAsync(teamPassed);

            result.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.ProjectNotFound);
        }

        [Test]
        public async Task CreateTeamAsync_WhenAddFailed_ReturnTeamCreationFailedResponse()
        {
            var teamPassed = TeamsDbSet.TeamBuilder().Create<TeamDto>();
            MockFor<IProjectRepository>().Setup(e => e.GetByIdAsync(teamPassed.ProjectId))
                .ReturnsAsync(new Project());
            Team modelAfterMap = new Team();
            MockFor<IBaseMapper<Team, TeamDto>>().Setup(e => e.MapToEntity(It.IsAny<TeamDto>()))
                .Returns(modelAfterMap);
            MockFor<ITeamRepository>().Setup(e => e.AddAsync(modelAfterMap))
                .ReturnsAsync((Team)null);

            var result = await ClassUnderTest.CreateTeamAsync(teamPassed);

            result.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.TeamCreationFailed);
        }

        [Test]
        public async Task CreateTeamAsync_WhenAddSuccess_ReturnTeamMappedResponse()
        {
            var teamPassed = TeamsDbSet.TeamBuilder().Create<TeamDto>();
            MockFor<IProjectRepository>().Setup(e => e.GetByIdAsync(teamPassed.ProjectId))
                .ReturnsAsync(new Project());
            Team modelAfterMap = new Team();
            MockFor<IBaseMapper<Team, TeamDto>>().Setup(e => e.MapToEntity(It.IsAny<TeamDto>()))
                .Returns(modelAfterMap);
            MockFor<IBaseMapper<Team, TeamDto>>().Setup(e => e.MapToModel(It.IsAny<Team>()))
                .Returns(teamPassed);
            MockFor<ITeamRepository>().Setup(e => e.AddAsync(modelAfterMap))
                .ReturnsAsync(new Team());

            var result = await ClassUnderTest.CreateTeamAsync(teamPassed);

            result.VerifySuccessResponseWithData(teamPassed);
        }

        [Test]
        public async Task CreateTeamAsync_WhenExceptionThrown_ReturnInternalError()
        {
            var teamPassed = TeamsDbSet.TeamBuilder().Create<TeamDto>();
            MockFor<IProjectRepository>().Setup(e => e.GetByIdAsync(teamPassed.ProjectId))
                .ReturnsAsync(new Project());
            Team modelAfterMap = new Team();
            MockFor<IBaseMapper<Team, TeamDto>>().Setup(e => e.MapToEntity(It.IsAny<TeamDto>()))
                .Returns(modelAfterMap);
            MockFor<IBaseMapper<Team, TeamDto>>().Setup(e => e.MapToModel(It.IsAny<Team>()))
                .Returns(teamPassed);
            MockFor<ITeamRepository>().Setup(e => e.AddAsync(modelAfterMap))
                .ThrowsAsync(new Exception());

            var result = await ClassUnderTest.CreateTeamAsync(teamPassed);

            result.VerifyInternalError();
        }
        #endregion
    }
}