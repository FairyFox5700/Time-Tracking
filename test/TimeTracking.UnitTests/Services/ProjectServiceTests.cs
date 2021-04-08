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
    public class ProjectServiceTests : AutoMockContext<ProjectService>
    {
        private static Fixture Fixture = new Fixture();

        #region GetProjectByIdAsync
        [Test]
        public async Task GetProjectByIdAsync_WhenNotFoundById_ShouldReturnProjectNotFoundResponse()
        {
            var id = Guid.NewGuid();
            MockFor<IProjectRepository>().Setup(e => e.GetByIdAsync(id))
                .ReturnsAsync((Project)null);

            var response = await ClassUnderTest.GetProjectByIdAsync(id);

            response.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.ProjectNotFound);
        }

        [Test]
        public async Task GetProjectByIdAsync_WhenFoundById_ShouldReturnMappedProjectDtoResponse()
        {
            var id = Guid.NewGuid();
            ProjectDetailsDto modelAfterMap = new ProjectDetailsDto();
            MockFor<IProjectRepository>().Setup(e => e.GetByIdAsync(id))
                .ReturnsAsync(new Project());
            MockFor<IModelMapper<Project, ProjectDetailsDto>>().Setup(e => e.MapToModel(It.IsAny<Project>()))
                .Returns(modelAfterMap);

            var response = await ClassUnderTest.GetProjectByIdAsync(id);

            response.VerifySuccessResponseWithData(modelAfterMap);
        }

        [Test]
        public async Task GetProjectByIdAsync_WhenExceptionThrown_ShouldReturnInternalErrorResponse()
        {
            var id = Guid.NewGuid();
            MockFor<IProjectRepository>().Setup(e => e.GetByIdAsync(id))
                .ThrowsAsync(new Exception());

            var response = await ClassUnderTest.GetProjectByIdAsync(id);

            response.VerifyInternalError();
        }
        #endregion


        #region GetAllProjectPagedAsync
        [Test]
        public async Task GetAllProjectPagedAsync_WhenRequestedWithPage_ReturnsAllProjectsPaged()
        {
            var pagedRequest = new PagedRequest()
            {
                Page = 1,
                PageSize = 2
            };
            var calls = 0;
            var pagedItems = ProjectsDbSet.ProjectBuilder().CreateMany<Project>();
            var pagedItemsAfterMap = Fixture.CreateMany<ProjectDetailsDto>().ToList();
            var pagedProjects = PagedResult<Project>.Paginate(pagedItems, 1, 2, 4, 8);
            MockFor<IProjectRepository>().Setup(e => e.GetAllPagedAsync(pagedRequest.Page, pagedRequest.PageSize))
                .ReturnsAsync(pagedProjects);
            MockFor<IModelMapper<Project, ProjectDetailsDto>>().Setup(e => e.MapToModel(It.IsAny<Project>()))
                .Returns(() => pagedItemsAfterMap[calls])
                .Callback(() => calls++);

            var response = await ClassUnderTest.GetAllProjectPagedAsync(pagedRequest);

            response.VerifyCorrectPagination(pagedProjects, pagedItemsAfterMap);
        }
        #endregion

        #region CreateProjectAsync

        [Test]
        public async Task CreateIssue_WhenAddFailed_ReturnProjectCreationFailedResponse()
        {
            var projectPassed = ProjectsDbSet.ProjectBuilder().Create<ProjectDto>();
            Project modelAfterMap = new Project();
            MockFor<IBaseMapper<Project, ProjectDto>>().Setup(e => e.MapToEntity(It.IsAny<ProjectDto>()))
                .Returns(modelAfterMap);
            MockFor<IProjectRepository>().Setup(x => x.AddAsync(modelAfterMap))
                .ReturnsAsync((Project)null);

            var result = await ClassUnderTest.CreateProjectAsync(projectPassed);

            result.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.ProjectCreationFailed);
        }

        [Test]
        public async Task CreateIssue_WhenAddSuccess_ReturnProjectMappedResponse()
        {
            var projectPassed = ProjectsDbSet.ProjectBuilder().Create<ProjectDto>();
            Project modelAfterMap = new Project();
            MockFor<IBaseMapper<Project, ProjectDto>>().Setup(e => e.MapToEntity(It.IsAny<ProjectDto>()))
                .Returns(modelAfterMap);
            MockFor<IBaseMapper<Project, ProjectDto>>().Setup(e => e.MapToModel(It.IsAny<Project>()))
                .Returns(projectPassed);
            MockFor<IProjectRepository>().Setup(x => x.AddAsync(modelAfterMap))
                .ReturnsAsync(new Project());

            var result = await ClassUnderTest.CreateProjectAsync(projectPassed);

            result.VerifySuccessResponseWithData(projectPassed);
        }

        [Test]
        public async Task CreateIssue_WhenExceptionThrows_ReturnInternalError()
        {
            var projectPassed = ProjectsDbSet.ProjectBuilder().Create<ProjectDto>();
            Project modelAfterMap = new Project();
            MockFor<IBaseMapper<Project, ProjectDto>>().Setup(e => e.MapToEntity(It.IsAny<ProjectDto>()))
                .Returns(modelAfterMap);
            MockFor<IBaseMapper<Project, ProjectDto>>().Setup(e => e.MapToModel(It.IsAny<Project>()))
                .Returns(projectPassed);
            MockFor<IProjectRepository>().Setup(x => x.AddAsync(modelAfterMap))
                .ThrowsAsync(new Exception());

            var result = await ClassUnderTest.CreateProjectAsync(projectPassed);

            result.VerifyInternalError();
        }
        #endregion

    }
}