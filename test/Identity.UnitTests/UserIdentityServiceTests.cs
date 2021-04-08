using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TimeTracking.Common.Helpers;
using TimeTracking.Common.Mappers;
using TimeTracking.Common.Wrapper;
using TimeTracking.Contracts.Events;
using TimeTracking.Identity.BL.Impl.Services;
using TimeTracking.Identity.Dal.Abstract;
using TimeTracking.Identity.Dal.Impl.Seed.Data;
using TimeTracking.Identity.Entities;
using TimeTracking.Identity.Models.Dtos;
using TimeTracking.Identity.Models.Requests;

namespace Identity.UnitTests
{
    [TestFixture]
    public class UserIdentityServiceTests
    {
        private static Fixture Fixture = new Fixture();
        private Mock<UserManager<User>> _mockUserManger;
        private Mock<ISystemClock> _mockSystemClock;
        private Mock<IEmailHelperService> mockEmailHelperService;
        private Mock<IBaseMapper<User, UserDto>> mockUserMapper;
        private Mock<IPublishEndpoint> mockPublishEndpoint;
        private UserIdentityService sut;
        private Mock<IUserRepository> _mockUserRepository;

        [SetUp]
        public void SetUp()
        {
            this._mockUserManger = MockHelpers.MockUserManager<User>();
            this._mockSystemClock = new Mock<ISystemClock>();
            this.mockEmailHelperService = new Mock<IEmailHelperService>();
            this.mockUserMapper = new Mock<IBaseMapper<User, UserDto>>();
            this.mockPublishEndpoint = new Mock<IPublishEndpoint>();
            var logger = new Mock<ILogger<UserIdentityService>>();
            _mockUserRepository = new Mock<IUserRepository>();
            this.sut = new UserIdentityService(
                _mockUserManger.Object,
                _mockUserRepository.Object,
                _mockSystemClock.Object,
                mockEmailHelperService.Object,
                mockUserMapper.Object,
                logger.Object,
                mockPublishEndpoint.Object);
        }


        #region RegisterAsync

        [Test]
        public async Task RegisterAsync_WhenCreatedSuccessfully_ShouldReturnSuccessResponse()
        {
            var request = new RegistrationRequest()
            {
                Email = "email",
                FirstName = "first name",
                LastName = "last name",
                Password = "password",
                Username = "userName"
            };
            _mockUserManger.Setup(e => e.CreateAsync(It.IsAny<User>(), request.Password))
                .ReturnsAsync(IdentityResult.Success);
            mockEmailHelperService.Setup(e => e.SendEmailConfirmationEmail(It.IsAny<User>()))
                .ReturnsAsync(ApiResponse.Success());

            var response = await sut.RegisterAsync(request);

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(200);
        }

        [Test]
        public async Task RegisterAsync_WhenUserCreationFailed_ShouldReturnSendEmailConfirmationFailed()
        {
            var request = new RegistrationRequest()
            {
                Email = "email",
                FirstName = "first name",
                LastName = "last name",
                Password = "password",
                Username = "userName"
            };
            var identityResult = IdentityResult.Failed();
            _mockUserManger.Setup(e => e.CreateAsync(It.IsAny<User>(), request.Password))
                .ReturnsAsync(identityResult);

            var response = await sut.RegisterAsync(request);

            response.IsSuccess.Should().BeFalse();
            response.ResponseException!.ErrorCode.Should().Be(ErrorCode.UserRegistrationFailed);
            response.ResponseException.ErrorMessage.Should().Be(identityResult.ToString());
        }

        [Test]
        public async Task RegisterAsync_WhenSendMessageFailed_ShouldReturnUserRegistrationFailed()
        {
            var request = new RegistrationRequest()
            {
                Email = "email",
                FirstName = "first name",
                LastName = "last name",
                Password = "password",
                Username = "userName"
            };
            var emailFailedResponse = ApiResponse.Failed();
            var identityResult = IdentityResult.Success;
            _mockUserManger.Setup(e => e.CreateAsync(It.IsAny<User>(), request.Password))
                .ReturnsAsync(identityResult);
            mockEmailHelperService.Setup(e => e.SendEmailConfirmationEmail(It.IsAny<User>()))
                .ReturnsAsync(emailFailedResponse);

            var response = await sut.RegisterAsync(request);

            response.IsSuccess.Should().BeFalse();
            response.Should().BeEquivalentTo(emailFailedResponse);
        }

        #endregion

        #region ForgotPasswordAsync

        [Test]
        public async Task ForgotPasswordAsync_WhenUserNotFound_ShouldReturnEmailConfirmationFailed()
        {
            var request = new ForgotPasswordRequest()
            {
                Email = "email"
            };
            _mockUserManger.Setup(e => e.FindByEmailAsync(request.Email))
                .ReturnsAsync((User)null);

            var response = await sut.ForgotPasswordAsync(request);

            response.IsSuccess.Should().BeFalse();
            response.ResponseException!.ErrorCode.Should().Be(ErrorCode.EmailConfirmationFailed);
            response.ResponseException.ErrorMessage.Should().Be(ErrorCode.EmailConfirmationFailed.GetDescription());
        }

        [Test]
        public async Task ForgotPasswordAsync_WhenEmailNotConfirmed_ShouldReturnEmailConfirmationFailed()
        {
            var request = new ForgotPasswordRequest()
            {
                Email = "email"
            };
            _mockUserManger.Setup(e => e.IsEmailConfirmedAsync(It.IsAny<User>()))
                .ReturnsAsync(false);

            var response = await sut.ForgotPasswordAsync(request);

            response.IsSuccess.Should().BeFalse();
            response.ResponseException!.ErrorCode.Should().Be(ErrorCode.EmailConfirmationFailed);
            response.ResponseException.ErrorMessage.Should().Be(ErrorCode.EmailConfirmationFailed.GetDescription());
        }

        [Test]
        public async Task ForgotPasswordAsync_WhenEmailConfirmedAndUserFound_ShouldSendResetPasswordConfirmationEmail()
        {
            var request = new ForgotPasswordRequest()
            {
                Email = "email"
            };
            var user = new User();
            _mockUserManger.Setup(e => e.IsEmailConfirmedAsync(It.IsAny<User>()))
                .ReturnsAsync(true);
            _mockUserManger.Setup(e => e.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            var response = await sut.ForgotPasswordAsync(request);

            mockEmailHelperService.Verify(e => e.SendResetPasswordConfirmationEmail(user));
        }

        #endregion

        #region ResetPasswordAsync

        [Test]
        public async Task ResetPasswordAsync_WhenUSerNotFound_ShouldReturnUserNotFoundResponse()
        {
            var request = new ResetPasswordRequest()
            {
                Password = "password",
                UserId = Guid.NewGuid()
            };
            _mockUserManger.Setup(e => e.FindByIdAsync(request.UserId.ToString()))
                .ReturnsAsync((User)null);

            var response = await sut.ResetPasswordAsync(request);

            response.IsSuccess.Should().BeFalse();
            response.ResponseException!.ErrorCode.Should().Be(ErrorCode.UserNotFound);
            response.ResponseException.ErrorMessage.Should().Be(ErrorCode.UserNotFound.GetDescription());
        }

        [Test]
        public async Task ResetPasswordAsync_WhenPasswordResetFailed_ShouldReturnResetPasswordFailed()
        {
            var request = new ResetPasswordRequest()
            {
                Password = "password",
                UserId = Guid.NewGuid(),
                Code = "verification code",
            };
            _mockUserManger.Setup(e => e.FindByIdAsync(request.UserId.ToString()))
                .ReturnsAsync(new User());
            _mockUserManger.Setup(e => e.ResetPasswordAsync(It.IsAny<User>(), request.Code, request.Password))
                .ReturnsAsync(IdentityResult.Failed());

            var response = await sut.ResetPasswordAsync(request);

            response.IsSuccess.Should().BeFalse();
            response.ResponseException!.ErrorCode.Should().Be(ErrorCode.ResetPasswordFailed);
            response.ResponseException.ErrorMessage.Should().Be(ErrorCode.ResetPasswordFailed.GetDescription());
        }

        [Test]
        public async Task ResetPasswordAsync_WhenPasswordResetSuccess_ShouldReturnSuccessResult()
        {
            var request = new ResetPasswordRequest()
            {
                Password = "password",
                UserId = Guid.NewGuid(),
                Code = "verification code",
            };
            _mockUserManger.Setup(e => e.FindByIdAsync(request.UserId.ToString()))
                .ReturnsAsync(new User());
            _mockUserManger.Setup(e => e.ResetPasswordAsync(It.IsAny<User>(), request.Code, request.Password))
                .ReturnsAsync(IdentityResult.Success);

            var response = await sut.ResetPasswordAsync(request);

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(200);
        }

        #endregion

        #region ResentEmailAsync

        [Test]
        public async Task ResentEmailAsync_WhenFailedToFindUSerByEmail_ShouldReturnUserNotFound()
        {

            var request = new ResendEmailRequest()
            {
                Email = "email",
            };
            _mockUserManger.Setup(e => e.FindByEmailAsync(request.Email))
                .ReturnsAsync((User)null);

            var response = await sut.ResentEmailAsync(request);

            response.IsSuccess.Should().BeFalse();
            response.ResponseException!.ErrorCode.Should().Be(ErrorCode.UserNotFound);
            response.ResponseException.ErrorMessage.Should().Be(ErrorCode.UserNotFound.GetDescription());
        }

        [Test]
        public async Task ResentEmailAsync_WhenFoundUserByEmail_ShouldCallSendEmailConfirmationEmail()
        {
            var request = new ResendEmailRequest()
            {
                Email = "email",
            };
            var user = Fixture.Create<User>();
            _mockUserManger.Setup(e => e.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            var response = await sut.ResentEmailAsync(request);

            mockEmailHelperService.Verify(e => e.SendEmailConfirmationEmail(user));
        }

        #endregion

        #region ConfirmEmailAsync

        [Test]
        public async Task ConfirmEmailAsync_WhenUserNotFound_ShouldReturnUserNotFound()
        {
            var request = new EmailConfirmationRequest()
            {
                Code = "code",
                UserId = Guid.NewGuid(),
            };
            _mockUserManger.Setup(e => e.FindByIdAsync(request.UserId.ToString()))
                .ReturnsAsync((User)null);

            var response = await sut.ConfirmEmailAsync(request);

            response.IsSuccess.Should().BeFalse();
            response.ResponseException!.ErrorCode.Should().Be(ErrorCode.UserNotFound);
            response.ResponseException.ErrorMessage.Should().Be(ErrorCode.UserNotFound.GetDescription());
        }

        [Test]
        public async Task ConfirmEmailAsync_WhenConfirmEmailFailed_ShouldReturnEmailConfirmationFailed()
        {
            var request = new EmailConfirmationRequest()
            {
                Code = "code",
                UserId = Guid.NewGuid(),
            };
            _mockUserManger.Setup(e => e.FindByIdAsync(request.UserId.ToString()))
                .ReturnsAsync((new User()));
            _mockUserManger.Setup(e => e.ConfirmEmailAsync(It.IsAny<User>(), request.Code))
                .ReturnsAsync(IdentityResult.Failed());

            var response = await sut.ConfirmEmailAsync(request);

            response.IsSuccess.Should().BeFalse();
            response.ResponseException!.ErrorCode.Should().Be(ErrorCode.EmailConfirmationFailed);
            response.ResponseException.ErrorMessage.Should().Be(ErrorCode.EmailConfirmationFailed.GetDescription());
        }

        [Test]
        public async Task ConfirmEmailAsync_WhenAddToRoleFailed_ShouldReturnAddUserToRoleFailed()
        {
            var request = new EmailConfirmationRequest()
            {
                Code = "code",
                UserId = Guid.NewGuid(),
            };
            var user = new User();
            _mockUserManger.Setup(e => e.FindByIdAsync(request.UserId.ToString()))
                .ReturnsAsync(user);
            _mockUserManger.Setup(e => e.ConfirmEmailAsync(It.IsAny<User>(), request.Code))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManger.Setup(e => e.AddToRoleAsync(user, AuthorizationData.DefaultRole.ToString()))
                .ReturnsAsync(IdentityResult.Failed());

            var response = await sut.ConfirmEmailAsync(request);

            response.IsSuccess.Should().BeFalse();
            response.ResponseException!.ErrorCode.Should().Be(ErrorCode.AddUserToRoleFailed);
            response.ResponseException.ErrorMessage.Should().Be(ErrorCode.AddUserToRoleFailed.GetDescription());
        }
        #endregion

        [Test]
        public async Task ConfirmEmailAsync_WhenAddToRoleSuccess_ShouldReturnSuccessResponse()
        {
            var request = new EmailConfirmationRequest()
            {
                Code = "code",
                UserId = Guid.NewGuid(),
            };
            var user = new User();
            _mockUserManger.Setup(e => e.FindByIdAsync(request.UserId.ToString()))
                .ReturnsAsync(user);
            _mockUserManger.Setup(e => e.ConfirmEmailAsync(It.IsAny<User>(), request.Code))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManger.Setup(e => e.AddToRoleAsync(user, AuthorizationData.DefaultRole.ToString()))
                .ReturnsAsync(IdentityResult.Success);

            var response = await sut.ConfirmEmailAsync(request);

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(200);
        }

        #region GetAllUsers

        public async Task GetAllUsers_WhenCalled_ShouldReturnMappedUsers()
        {
            var calls = 0;
            var users = Fixture.CreateMany<UserDto>().ToArray();
            mockUserMapper.Setup(e => e.MapToModel(It.IsAny<User>()))
                .Returns(() => users[calls])
                .Callback(() => calls++);

            var response = await sut.GetAllUsers();

            response.Data.Should().BeEquivalentTo(users);
            response.StatusCode.Should().Be(200);
        }

        #endregion

        #region GetUsersById

        [Test]
        public async Task GetUsersById_WhenUSerNotFound_ReturnsUSerNotFoundResponse()
        {
            var userId = Guid.NewGuid();
            _mockUserManger.Setup(e => e.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((User)null);
            var mappedUser = Fixture.Create<UserDto>();
            mockUserMapper.Setup(e => e.MapToModel(It.IsAny<User>()))
                .Returns(mappedUser);

            var response = await sut.GetUsersById(userId);

            response.IsSuccess.Should().BeFalse();
            response.ResponseException!.ErrorCode.Should().Be(ErrorCode.UserNotFound);
            response.ResponseException.ErrorMessage.Should().Be(ErrorCode.UserNotFound.GetDescription());
        }

        [Test]
        public async Task GetUsersById_WhenUserFound_ReturnsUSerMappedInResponse()
        {
            var userId = Guid.NewGuid();
            _mockUserManger.Setup(e => e.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(new User());
            var mappedUser = Fixture.Create<UserDto>();
            mockUserMapper.Setup(e => e.MapToModel(It.IsAny<User>()))
                .Returns(mappedUser);

            var response = await sut.GetUsersById(userId);

            response.IsSuccess.Should().BeTrue();
            response.Data.Should().BeEquivalentTo(mappedUser);
        }

        #endregion

        #region  GetAllUsers

        [Test]
        public async Task GetAllUsers_WhenRequested_ShouldReturnListOfAllUsers()
        {
            var calls = 0;
            var users = Fixture.CreateMany<User>().ToList();
            var itemsAfterMap = Fixture.CreateMany<UserDto>().ToList();
            _mockUserRepository.Setup(e => e.GetAllAsync())
                .ReturnsAsync(users);
            mockUserMapper.Setup(s => s.MapToModel(It.IsAny<User>()))
                .Returns(() => itemsAfterMap[calls])
                .Callback(() => calls++);

            var response = await sut.GetAllUsers();

            response.Data.Should().BeEquivalentTo(itemsAfterMap);
        }
        #endregion
    }
}