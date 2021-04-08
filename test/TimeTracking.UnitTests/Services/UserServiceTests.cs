using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoMockHelper.Core;
using FluentAssertions;
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
using TimeTracking.Models.Requests;
using TimeTracking.UnitTests.Data;

namespace TimeTracking.UnitTests.Services
{
    [TestFixture]
    public class UserServiceTests : AutoMockContext<UserService>
    {

        private static Fixture Fixture = new Fixture();

        # region GetUsersById
        [Test]
        public async Task GetUsersById_WhenNotFoundById_ShouldReturnUserNotFoundResponse()
        {
            var id = Guid.NewGuid();
            MockFor<IUserRepository>().Setup(e => e.GetByIdAsync(id))
                .ReturnsAsync((TimeTrackingUser)null);

            var response = await ClassUnderTest.GetUsersById(id);

            response.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.UserNotFound);
        }

        [Test]
        public async Task GetUsersById_WhenFoundById_ShouldReturnMappedUserDtoResponse()
        {
            var id = Guid.NewGuid();
            TimeTrackingUserDetailsDto modelAfterMap = new TimeTrackingUserDetailsDto();
            MockFor<IUserRepository>().Setup(e => e.GetByIdAsync(id))
                .ReturnsAsync(new TimeTrackingUser());
            MockFor<IModelMapper<TimeTrackingUser, TimeTrackingUserDetailsDto>>().Setup(e => e.MapToModel(It.IsAny<TimeTrackingUser>()))
                .Returns(modelAfterMap);

            var response = await ClassUnderTest.GetUsersById(id);

            response.VerifySuccessResponseWithData(modelAfterMap);
        }

        [Test]
        public async Task GetUsersById_WhenExceptionThrown_ShouldReturnInternalErrorResponse()
        {
            var id = Guid.NewGuid();
            MockFor<IUserRepository>().Setup(e => e.GetByIdAsync(id))
                .ThrowsAsync(new Exception());

            var response = await ClassUnderTest.GetUsersById(id);

            response.VerifyInternalError();
        }
        # endregion

        #region GetAllUsers

        [Test]
        public async Task GetAllUsers_WhenRequestedWithPage_ReturnsAllUsersPaged()
        {
            var pagedRequest = new PagedRequest()
            {
                Page = 1,
                PageSize = 2
            };
            var calls = 0;
            var pagedItems = UsersDbSet.TimeTrackingUserBuilder().CreateMany<TimeTrackingUser>();
            var pagedItemsAfterMap = Fixture.CreateMany<TimeTrackingUserDetailsDto>().ToList();
            var pagedUsers = PagedResult<TimeTrackingUser>.Paginate(pagedItems, 1, 2, 4, 8);
            MockFor<IUserRepository>().Setup(e => e.GetAllPagedAsync(pagedRequest.Page, pagedRequest.PageSize))
                .ReturnsAsync(pagedUsers);
            MockFor<IModelMapper<TimeTrackingUser, TimeTrackingUserDetailsDto>>().Setup(e => e.MapToModel(It.IsAny<TimeTrackingUser>()))
                .Returns(() => pagedItemsAfterMap[calls])
                .Callback(() => calls++);

            var response = await ClassUnderTest.GetAllUsers(pagedRequest);

            response.VerifyCorrectPagination(pagedUsers, pagedItemsAfterMap);
        }

        #endregion

        #region AddUserToTeam

        [Test]
        public async Task AddUserToTeam_WhenTeamNotFound_ReturnTeamNotFoundResponse()
        {
            var request = Fixture.Freeze<AssignUserToTeamRequest>();
            MockFor<ITeamRepository>().Setup(e => e.GetByIdAsync(request.TeamId))
                .ReturnsAsync((Team)null);

            var result = await ClassUnderTest.AddUserToTeam(request);

            result.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.TeamNotFound);
        }

        [Test]
        public async Task AddUserToTeam_WhenUserNotFound_ReturnUserNotFoundResponse()
        {
            var request = Fixture.Freeze<AssignUserToTeamRequest>();
            MockFor<ITeamRepository>().Setup(e => e.GetByIdAsync(request.TeamId))
                .ReturnsAsync(new Team());
            MockFor<IUserRepository>().Setup(e => e.GetByIdAsync(request.UserId))
                .ReturnsAsync((TimeTrackingUser)null);

            var result = await ClassUnderTest.AddUserToTeam(request);

            result.VerifyNotSuccessResponseWithErrorCodeAndMessage(ErrorCode.UserNotFound);
        }

        [Test]
        public async Task AddUserToTeam_WhenUserUpdateSuccess_ReturnMappedUserModel()
        {
            var request = Fixture.Freeze<AssignUserToTeamRequest>();
            var foundedUser = UsersDbSet.TimeTrackingUserBuilder().Create<TimeTrackingUser>();
            var mappedModel = UsersDbSet.TimeTrackingUserBuilder().Create<TimeTrackingUserDto>();
            MockFor<ITeamRepository>().Setup(e => e.GetByIdAsync(request.TeamId))
                .ReturnsAsync(new Team());
            MockFor<IUserRepository>().Setup(e => e.GetByIdAsync(request.UserId))
                .ReturnsAsync(foundedUser);
            MockFor<IBaseMapper<TimeTrackingUser, TimeTrackingUserDto>>().Setup(e => e.MapToModel(It.IsAny<TimeTrackingUser>()))
                .Returns(mappedModel);
            MockFor<IUserRepository>().Setup(e => e.UpdateAsync(foundedUser))
                .ReturnsAsync(new TimeTrackingUser());

            var result = await ClassUnderTest.AddUserToTeam(request);

            request.TeamId.Should().Be(request.TeamId);
            result.VerifySuccessResponseWithData(mappedModel);
        }

        [Test]
        public async Task AddUserToTeam_WhenExceptionThrown_ReturnInternalError()
        {
            var request = Fixture.Freeze<AssignUserToTeamRequest>();
            var foundedUser = UsersDbSet.TimeTrackingUserBuilder().Create<TimeTrackingUser>();
            var mappedModel = UsersDbSet.TimeTrackingUserBuilder().Create<TimeTrackingUserDto>();
            MockFor<ITeamRepository>().Setup(e => e.GetByIdAsync(request.TeamId))
                .ReturnsAsync(new Team());
            MockFor<IUserRepository>().Setup(e => e.GetByIdAsync(request.UserId))
                .ReturnsAsync(foundedUser);
            MockFor<IBaseMapper<TimeTrackingUser, TimeTrackingUserDto>>().Setup(e => e.MapToModel(foundedUser))
                .Returns(mappedModel);
            MockFor<IUserRepository>().Setup(e => e.UpdateAsync(foundedUser))
                .ThrowsAsync(new NullReferenceException());

            var result = await ClassUnderTest.AddUserToTeam(request);

            result.VerifyInternalError();
        }

        #endregion
    }
}