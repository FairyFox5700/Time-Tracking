using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TimeTracking.Common.Wrapper;
using TimeTracking.Identity.BL.Impl.Services;
using TimeTracking.Identity.Entities;
using TimeTracking.Identity.Models.Requests;

namespace Identity.UnitTests
{
    [TestFixture]
    public class RoleServiceTests
    {
        private Mock<UserManager<User>> _mockUserManger;
        private Mock<RoleManager<Role>> _mockRoleManger;
        private RoleService sut;

        [SetUp]
        public void SetUp()
        {
            _mockUserManger = MockHelpers.MockUserManager<User>();
            _mockRoleManger = MockHelpers.MockRoleManager<Role>();
            var logger = new Mock<ILogger<RoleService>>();
            this.sut = new RoleService(_mockUserManger.Object, _mockRoleManger.Object, logger.Object);
        }

        [Test]
        public async Task AddUserToRoleAsync_WhenUserNotFound_ShouldReturnUserNotFound()
        {
            var request = new AddToRoleRequest()
            {
                UserId = Guid.NewGuid(),
                RoleName = "engineer"
            };
            this._mockUserManger.Setup(e => e.FindByIdAsync(request.UserId.ToString()))
                .ReturnsAsync((User)null);

            var result = await sut.AddUserToRoleAsync(request);

            result.IsSuccess.Should().BeFalse();
            result.ResponseException.Should().NotBeNull();
            result.ResponseException!.ErrorCode.Should().Be(ErrorCode.UserNotFound);
            result.ResponseException.ErrorMessage.Should().Be($"No Accounts Registered with id {request.UserId}.");
        }

        [Test]
        public async Task AddUserToRoleAsync_WhenRoleNotFound_ShouldReturnUserNotFound()
        {
            var request = new AddToRoleRequest()
            {
                UserId = Guid.NewGuid(),
                RoleName = "engineer"
            };
            this._mockUserManger.Setup(e => e.FindByIdAsync(request.UserId.ToString()))
                .ReturnsAsync(new User());
            this._mockRoleManger.Setup(t => t.RoleExistsAsync(request.RoleName))
                .ReturnsAsync(false);

            var result = await sut.AddUserToRoleAsync(request);

            result.IsSuccess.Should().BeFalse();
            result.ResponseException.Should().NotBeNull();
            result.ResponseException!.ErrorCode.Should().Be(ErrorCode.RoleNotFound);
            result.ResponseException.ErrorMessage.Should().Be($"Role {request.RoleName} not found.");
        }

        [Test]
        public async Task AddUserToRoleAsync_WhenAddToRoleFailed_ShouldReturnAddToRoleFailed()
        {
            var request = new AddToRoleRequest()
            {
                UserId = Guid.NewGuid(),
                RoleName = "engineer"
            };
            var user = new User();
            this._mockUserManger.Setup(e => e.FindByIdAsync(request.UserId.ToString()))
                .ReturnsAsync(user);
            this._mockRoleManger.Setup(t => t.RoleExistsAsync(request.RoleName))
                .ReturnsAsync(true);
            this._mockUserManger.Setup(e => e.AddToRoleAsync(user, request.RoleName))
                .ReturnsAsync(IdentityResult.Failed());

            var result = await sut.AddUserToRoleAsync(request);

            result.IsSuccess.Should().BeFalse();
            result.ResponseException.Should().NotBeNull();
            result.ResponseException!.ErrorCode.Should().Be(ErrorCode.AddToRoleFailed);
            result.ResponseException.ErrorMessage.Should().Be($"Add user {request.UserId} to role {request.RoleName} failed.");
        }


        [Test]
        public async Task AddUserToRoleAsync_WhenAddToRoleSuccess_ShouldReturnSuccess()
        {
            var request = new AddToRoleRequest()
            {
                UserId = Guid.NewGuid(),
                RoleName = "engineer"
            };
            var user = new User();
            this._mockUserManger.Setup(e => e.FindByIdAsync(request.UserId.ToString()))
                .ReturnsAsync(user);
            this._mockRoleManger.Setup(t => t.RoleExistsAsync(request.RoleName))
                .ReturnsAsync(true);
            this._mockUserManger.Setup(e => e.AddToRoleAsync(user, request.RoleName))
                .ReturnsAsync(IdentityResult.Success);

            var result = await sut.AddUserToRoleAsync(request);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
        }
    }
}