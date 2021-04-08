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
    public class MilestoneServiceTests : AutoMockContext<MileStoneService>
    {
        private static Fixture Fixture = new Fixture();

        # region GetMileStoneById
        [Test]
        public async Task GetMileStoneById_WhenNotFoundById_ShouldReturnMileStoneNotFoundResponse()
        {
            var milestoneId = Guid.NewGuid();
            MockFor<IMilestoneRepository>().Setup(e => e.GetByIdAsync(milestoneId))
                .ReturnsAsync((Milestone)null);

            var response = await ClassUnderTest.GetMileStoneById(milestoneId);

            response.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.MileStoneNotFound);
        }

        [Test]
        public async Task GetMileStoneById_WhenFoundById_ShouldReturnMappedMileStoneDtoResponse()
        {
            var milestoneId = Guid.NewGuid();
            MilestoneDetailsDto modelAfterMap = new MilestoneDetailsDto();
            MockFor<IMilestoneRepository>().Setup(e => e.GetByIdAsync(milestoneId))
                .ReturnsAsync(new Milestone());
            MockFor<IModelMapper<Milestone, MilestoneDetailsDto>>().Setup(e => e.MapToModel(It.IsAny<Milestone>()))
                .Returns(modelAfterMap);

            var response = await ClassUnderTest.GetMileStoneById(milestoneId);

            response.VerifySuccessResponseWithData(modelAfterMap);
        }

        [Test]
        public async Task GetMileStoneById_WhenExceptionThrown_ShouldReturnInternalErrorResponse()
        {
            var milestoneId = Guid.NewGuid();
            MockFor<IMilestoneRepository>().Setup(e => e.GetByIdAsync(milestoneId))
                .ThrowsAsync(new Exception());

            var response = await ClassUnderTest.GetMileStoneById(milestoneId);

            response.VerifyInternalError();
        }
        #endregion

        #region GetAllMilestonesPaged
        [Test]
        public async Task GetAllMilestonesPaged_WhenRequestedWithPage_ReturnsAllMilestonesPaged()
        {
            var pagedRequest = new PagedRequest()
            {
                Page = 1,
                PageSize = 2
            };
            var calls = 0;
            var pagedItems = MilestonesDbSet.MilestoneBuilder().CreateMany<Milestone>();
            var pagedItemsAfterMap = Fixture.CreateMany<MilestoneDetailsDto>().ToList();
            var pagedMilestones = PagedResult<Milestone>.Paginate(pagedItems, 1, 2, 4, 8);
            MockFor<IMilestoneRepository>().Setup(e => e.GetAllPagedAsync(pagedRequest.Page, pagedRequest.PageSize))
                .ReturnsAsync(pagedMilestones);
            MockFor<IModelMapper<Milestone, MilestoneDetailsDto>>().Setup(e => e.MapToModel(It.IsAny<Milestone>()))
                .Returns(() => pagedItemsAfterMap[calls])
                .Callback(() => calls++);

            var response = await ClassUnderTest.GetAllMilestonesPaged(pagedRequest);

            response.VerifyCorrectPagination(pagedMilestones, pagedItemsAfterMap);
        }
        #endregion

        #region CreateMileStoneAsync

        [Test]
        public async Task CreateIssue_WhenProjectNotFound_ReturnProjectNotFoundResponse()
        {
            var milestonePassed = MilestonesDbSet.MilestoneBuilder().Create<MilestoneDto>();
            MockFor<IProjectRepository>().Setup(e => e.GetByIdAsync(milestonePassed.ProjectId))
                .ReturnsAsync((Project)null);

            var result = await ClassUnderTest.CreateMileStoneAsync(milestonePassed);

            result.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.ProjectNotFound);
        }

        [Test]
        public async Task CreateIssue_WhenMilestoneAddedSuccess_ReturnMappedMilestone()
        {
            var milestonePassed = MilestonesDbSet.MilestoneBuilder().Create<MilestoneDto>();
            var modelAfterMap = new Milestone();
            MockFor<IProjectRepository>().Setup(e => e.GetByIdAsync(milestonePassed.ProjectId))
                .ReturnsAsync(new Project());
            MockFor<IBaseMapper<Milestone, MilestoneDto>>().Setup(e => e.MapToEntity(milestonePassed))
                .Returns(modelAfterMap);
            MockFor<IBaseMapper<Milestone, MilestoneDto>>().Setup(e => e.MapToModel(It.IsAny<Milestone>()))
                .Returns(milestonePassed);
            MockFor<IMilestoneRepository>().Setup(e => e.AddAsync(modelAfterMap))
                .ReturnsAsync(new Milestone());

            var result = await ClassUnderTest.CreateMileStoneAsync(milestonePassed);

            result.VerifySuccessResponseWithData(milestonePassed);
        }


        [Test]
        public async Task CreateIssue_WhenMilestoneAddFailed_ReturnMilestoneCreationFiled()
        {
            var milestonePassed = MilestonesDbSet.MilestoneBuilder().Create<MilestoneDto>();
            var modelAfterMap = new Milestone();
            MockFor<IProjectRepository>().Setup(e => e.GetByIdAsync(milestonePassed.ProjectId))
                .ReturnsAsync(new Project());
            MockFor<IBaseMapper<Milestone, MilestoneDto>>().Setup(e => e.MapToEntity(milestonePassed))
                .Returns(modelAfterMap);
            MockFor<IBaseMapper<Milestone, MilestoneDto>>().Setup(e => e.MapToModel(It.IsAny<Milestone>()))
                .Returns(milestonePassed);
            MockFor<IMilestoneRepository>().Setup(e => e.AddAsync(modelAfterMap))
                .ReturnsAsync((Milestone)null);

            var result = await ClassUnderTest.CreateMileStoneAsync(milestonePassed);

            result.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.MilestoneCreationFiled);
        }

        [Test]
        public async Task CreateIssue_WhenExceptionThrown_ReturnInternalError()
        {
            var milestonePassed = MilestonesDbSet.MilestoneBuilder().Create<MilestoneDto>();
            var modelAfterMap = new Milestone();
            MockFor<IProjectRepository>().Setup(e => e.GetByIdAsync(milestonePassed.ProjectId))
                .ReturnsAsync(new Project());
            MockFor<IBaseMapper<Milestone, MilestoneDto>>().Setup(e => e.MapToEntity(milestonePassed))
                .Returns(modelAfterMap);
            MockFor<IBaseMapper<Milestone, MilestoneDto>>().Setup(e => e.MapToModel(It.IsAny<Milestone>()))
                .Returns(milestonePassed);
            MockFor<IMilestoneRepository>().Setup(e => e.AddAsync(modelAfterMap))
                .ThrowsAsync(new Exception());

            var result = await ClassUnderTest.CreateMileStoneAsync(milestonePassed);

            result.VerifyInternalError();
        }
        #endregion
    }
}