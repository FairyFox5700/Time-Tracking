using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TimeTracking.Common.Helpers;
using TimeTracking.Common.Wrapper;
using TimeTracking.Identity.BL.Abstract.Factories;
using TimeTracking.Identity.BL.Impl.Services;
using TimeTracking.Identity.Dal.Abstract;
using TimeTracking.Identity.Entities;
using TimeTracking.Identity.Models;
using TimeTracking.Identity.Models.Requests;

namespace Identity.UnitTests
{
    public class TokenServiceTests
    {
        private static Fixture Fixture = new Fixture();
        private Mock<UserManager<User>> _mockUserManager;
        private Mock<IJwtFactory> _mockFactory;
        private TokenService sut;
        private Mock<ISystemClock> _mockSystemClock;
        private Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
        private Mock<IUserRepository> _mockUserRepository;

        [SetUp]
        public void SetUp()
        {
            _mockUserManager = MockHelpers.MockUserManager<User>();
            var logger = new Mock<ILogger<TokenService>>();
            _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
            _mockFactory = new Mock<IJwtFactory>();
            _mockUserRepository = new Mock<IUserRepository>();
            this._mockSystemClock = new Mock<ISystemClock>();
            this.sut = new TokenService(_mockUserManager.Object,
                _mockRefreshTokenRepository.Object,
                _mockUserRepository.Object,
                _mockFactory.Object,
                _mockSystemClock.Object,
                logger.Object);
        }

        #region LoginAsync

        [Test]
        public async Task LoginAsync_WhenUserNotFound_ShouldReturnAuthResponseWithMessage()
        {

            var tokenExchangeRequest = new TokenExchangeRequest()
            {
                Password = "passsword",
                Email = "email",
            };
            _mockUserManager.Setup(e => e.FindByEmailAsync(tokenExchangeRequest.Email))
                .ReturnsAsync((User)null);

            var response = await sut.LoginAsync(tokenExchangeRequest);

            response.Message.Should().Be($"No Accounts Registered with {tokenExchangeRequest.Email}.");
        }

        [Test]
        public async Task LoginAsync_WhenUserEmailNotConfirmed_ShouldReturnAuthResponseWithMessage()
        {
            var tokenExchangeRequest = new TokenExchangeRequest()
            {
                Password = "password",
                Email = "email",
            };
            var user = Fixture.Build<User>().With(w => w.EmailConfirmed, false).Create();
            _mockUserManager.Setup(e => e.FindByEmailAsync(tokenExchangeRequest.Email))
                .ReturnsAsync(user);

            var response = await sut.LoginAsync(tokenExchangeRequest);

            response.Message.Should().Be($"Current email {tokenExchangeRequest.Email} is not confirmed.");
        }


        [Test]
        public async Task LoginAsync_WhenUserIsLockedOut_ShouldReturnAuthResponseWithMessage()
        {
            var tokenExchangeRequest = new TokenExchangeRequest()
            {
                Password = "password",
                Email = "email",
            };
            var user = Fixture.Build<User>().
                With(w => w.EmailConfirmed, true).
                With(w => w.LockoutEnd, DateTimeOffset.MaxValue).
                Create();
            _mockUserManager.Setup(e => e.FindByEmailAsync(tokenExchangeRequest.Email))
                .ReturnsAsync(user);

            var response = await sut.LoginAsync(tokenExchangeRequest);

            response.Message.Should().Be($"This account has been locked.");
        }


        [Test]
        public async Task LoginAsync_WhenCheckPasswordAsyncFailed_ShouldReturnAuthResponseWithMessage()
        {
            var tokenExchangeRequest = new TokenExchangeRequest()
            {
                Password = "password",
                Email = "email",
            };
            var user = Fixture.Build<User>().
                With(w => w.EmailConfirmed, true).
                With(w => w.LockoutEnd, DateTimeOffset.MinValue).
                Create();
            _mockUserManager.Setup(e => e.FindByEmailAsync(tokenExchangeRequest.Email))
                .ReturnsAsync(user);
            _mockUserManager.Setup(e => e.CheckPasswordAsync(user, tokenExchangeRequest.Password))
                .ReturnsAsync(false);

            var response = await sut.LoginAsync(tokenExchangeRequest);

            response.Message.Should().Be($"Incorrect Credentials for user {user.Email}.");
        }

        [Test]
        public async Task LoginAsync_WhenUserWithActiveRefreshTokenPassed_ShouldReturnAuthResponse()
        {
            var tokenExchangeRequest = new TokenExchangeRequest()
            {
                Password = "password",
                Email = "email",
            };
            var liStOfTokens = Fixture.Build<RefreshToken>()
                .With(e => e.Expires, DateTimeOffset.MaxValue)
                .With(e => e.Revoked, (DateTimeOffset?)null)
                .CreateMany().ToList();
            var user = Fixture.Build<User>().
                With(w => w.EmailConfirmed, true).
                With(w => w.LockoutEnd, DateTimeOffset.MinValue).
                With(e => e.RefreshTokens, liStOfTokens).
                Create();
            var activeToken = liStOfTokens.FirstOrDefault(e => e.IsActive);
            _mockUserManager.Setup(e => e.FindByEmailAsync(tokenExchangeRequest.Email))
                .ReturnsAsync(user);
            _mockUserManager.Setup(e => e.CheckPasswordAsync(user, tokenExchangeRequest.Password))
                .ReturnsAsync(true);
            var token = new JwtAccessToken();
            _mockFactory.Setup(e => e.GenerateEncodedAccessToken(user))
                .ReturnsAsync(token);
            _mockRefreshTokenRepository.Setup(e => e.FilterOneAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync(activeToken);

            var response = await sut.LoginAsync(tokenExchangeRequest);

            Debug.Assert(activeToken != null, nameof(activeToken) + " != null");
            response.RefreshToken.Should().BeEquivalentTo(activeToken.Token);
            response.Token.Should().BeEquivalentTo(token.Token);
            response.ExpiredAt.Should().Be((activeToken.Expires - DateTimeOffset.UtcNow).Seconds);
        }


        [Test]
        public async Task LoginAsync_WhenUserWithRefreshTokenNotActiveAndUSerUpdatedSuccessfully_ShouldGenerateNewRefreshToken()
        {
            var tokenExchangeRequest = new TokenExchangeRequest()
            {
                Password = "password",
                Email = "email",
            };
            var liStOfTokens = Fixture.Build<RefreshToken>()
                .With(e => e.Expires, DateTimeOffset.MaxValue)
                .With(e => e.Revoked)
                .CreateMany().ToList();
            var user = Fixture.Build<User>().
                With(w => w.EmailConfirmed, true).
                With(w => w.LockoutEnd, DateTimeOffset.MinValue).
                With(e => e.RefreshTokens, liStOfTokens).
                Create();
            _mockUserManager.Setup(e => e.FindByEmailAsync(tokenExchangeRequest.Email))
                .ReturnsAsync(user);
            _mockUserManager.Setup(e => e.CheckPasswordAsync(user, tokenExchangeRequest.Password))
                .ReturnsAsync(true);
            var token = new JwtAccessToken();
            _mockFactory.Setup(e => e.GenerateEncodedAccessToken(user))
                .ReturnsAsync(token);
            var refreshToken = new RefreshToken();
            _mockFactory.Setup(e => e.GenerateRefreshToken(32))
                .Returns(refreshToken);
            _mockUserManager.Setup(e => e.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _mockRefreshTokenRepository.Setup(e => e.FilterOneAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync((RefreshToken)null);

            var response = await sut.LoginAsync(tokenExchangeRequest);

            response.RefreshToken.Should().BeEquivalentTo(refreshToken.Token);
            response.Token.Should().BeEquivalentTo(token.Token);
            response.ExpiredAt.Should().Be((refreshToken.Expires - DateTimeOffset.UtcNow).Seconds);
        }


        [Test]
        public async Task LoginAsync_WhenUserWithRefreshTokenNotActiveAndUserUpdatedFailed_ShouldGenerateNewRefreshToken()
        {
            var tokenExchangeRequest = new TokenExchangeRequest()
            {
                Password = "password",
                Email = "email",
            };
            var liStOfTokens = Fixture.Build<RefreshToken>()
                .With(e => e.Expires, DateTimeOffset.MaxValue)
                .With(e => e.Revoked)
                .CreateMany().ToList();
            var user = Fixture.Build<User>().
                With(w => w.EmailConfirmed, true).
                With(w => w.LockoutEnd, DateTimeOffset.MinValue).
                With(e => e.RefreshTokens, liStOfTokens).
                Create();
            _mockUserManager.Setup(e => e.FindByEmailAsync(tokenExchangeRequest.Email))
                .ReturnsAsync(user);
            _mockUserManager.Setup(e => e.CheckPasswordAsync(user, tokenExchangeRequest.Password))
                .ReturnsAsync(true);
            var token = new JwtAccessToken();
            _mockFactory.Setup(e => e.GenerateEncodedAccessToken(user))
                .ReturnsAsync(token);
            var refreshToken = new RefreshToken();
            _mockFactory.Setup(e => e.GenerateRefreshToken(32))
                .Returns(refreshToken);
            _mockUserManager.Setup(e => e.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Failed());
            _mockRefreshTokenRepository.Setup(e => e.FilterOneAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync((RefreshToken)null);

            var response = await sut.LoginAsync(tokenExchangeRequest);

            response.Message.Should().Contain("Failed to generate refresh token.");
        }
        #endregion


        #region RefreshTokenAsync

        [Test]
        public async Task RefreshTokenAsync_WhenUserWithPassedTokenNotFound_ShouldGenerateAuthMessage()
        {
            var tokenPassed = Fixture.Create<string>();
            _mockUserRepository.Setup(e => e.GetUserWithActiveRefreshToken(tokenPassed))
                .ReturnsAsync((User)null);

            var response = await sut.RefreshTokenAsync(tokenPassed);

            response.Message.Should().Be(ErrorCode.RefreshTokenRevocationFailed.GetDescription());
        }

        [Test]
        public async Task
            RefreshTokenAsync_WhenUserWithPassedTokenFoundButTokenNotActive_ShouldGenerateNewRefreshToken()
        {
            var tokenPassed = Fixture.Create<string>();
            var listOfToken = Fixture.Build<RefreshToken>()
                .With(w => w.Token, tokenPassed + "1")
                .CreateMany().ToList();
            var user = Fixture.Build<User>().With(w => w.RefreshTokens, listOfToken).Create();
            _mockUserRepository.Setup(e => e.GetUserWithActiveRefreshToken(tokenPassed))
                .ReturnsAsync(user);
            _mockRefreshTokenRepository.Setup(r => r.FilterOneAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync((RefreshToken)null);

            var response = await sut.RefreshTokenAsync(tokenPassed);

            response.Message.Should().Be(ErrorCode.RefreshTokenRevocationFailed.GetDescription());
        }

        [Test]
        public async Task
            RefreshTokenAsync_WhenUserWithPassedTokenFoundButTokenNotActiveAndUserUpdateFailed_ShouldGenerateNewRefreshToken()
        {
            var tokenPassed = Fixture.Create<string>();
            var listOfToken = Fixture.Build<RefreshToken>()
                .With(w => w.Token, tokenPassed + "1")
                .CreateMany().ToList();
            var user = Fixture.Build<User>().With(w => w.RefreshTokens, listOfToken).Create();
            _mockUserRepository.Setup(e => e.GetUserWithActiveRefreshToken(tokenPassed))
                .ReturnsAsync(user);
            _mockRefreshTokenRepository.Setup(r => r.FilterOneAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync(new RefreshToken());
            _mockUserManager.Setup(e => e.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Failed());

            var response = await sut.RefreshTokenAsync(tokenPassed);

            response.Message.Should().Be("Failed to generate refresh token.");
        }

        [Test]
        public async Task
            RefreshTokenAsync_WhenUserWithPassedTokenFoundButTokenNotActiveAndUserUpdateSuccess_ShouldGenerateNewRefreshToken()
        {
            var tokenPassed = Fixture.Create<string>();
            var listOfToken = Fixture.Build<RefreshToken>()
                .With(w => w.Token, tokenPassed + "1")
                .CreateMany().ToList();
            var user = Fixture.Build<User>().With(w => w.RefreshTokens, listOfToken).Create();
            _mockUserRepository.Setup(e => e.GetUserWithActiveRefreshToken(tokenPassed))
                .ReturnsAsync(user);
            _mockRefreshTokenRepository.Setup(r => r.FilterOneAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync(new RefreshToken());
            _mockUserManager.Setup(e => e.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);
            var token = new JwtAccessToken();
            _mockFactory.Setup(e => e.GenerateEncodedAccessToken(It.IsAny<User>()))
                .ReturnsAsync(token);
            var newRefreshToken = new RefreshToken();
            _mockFactory.Setup(e => e.GenerateRefreshToken(32))
                .Returns(newRefreshToken);

            var response = await sut.RefreshTokenAsync(tokenPassed);

            response.Token.Should().Be(token.Token);
            response.RefreshToken.Should().Be(newRefreshToken.Token);
            response.ExpiredAt.Should().Be((newRefreshToken.Expires - DateTimeOffset.UtcNow).Seconds);
        }

        #endregion

        #region RevokeTokenAsync

        [Test]
        public async Task RevokeTokenAsync_WhenUserWithPassedTokenNotFound_ShouldGenerateAuthMessage()
        {
            var tokenPassed = Fixture.Create<string>();
            _mockUserRepository.Setup(e => e.GetUserWithActiveRefreshToken(tokenPassed))
                .ReturnsAsync((User)null);

            var response = await sut.RefreshTokenAsync(tokenPassed);

            response.Message.Should().Be((ErrorCode.RefreshTokenRevocationFailed.GetDescription()));
        }

        [Test]
        public async Task RevokeTokenAsync_WhenUserWithPassedTokenFoundButTokenNotActive_ShouldGenerateNewRefreshToken()
        {
            var tokenPassed = Fixture.Create<string>();
            var listOfToken = Fixture.Build<RefreshToken>()
                .With(w => w.Token, tokenPassed + "1")
                .CreateMany().ToList();
            var user = Fixture.Build<User>().With(w => w.RefreshTokens, listOfToken).Create();
            _mockUserRepository.Setup(e => e.GetUserWithActiveRefreshToken(tokenPassed))
                .ReturnsAsync(user);
            _mockRefreshTokenRepository.Setup(r => r.FilterOneAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync((RefreshToken)null);

            var response = await sut.RevokeTokenAsync(tokenPassed);

            response.Message.Should().Be(ErrorCode.RefreshTokenRevocationFailed.GetDescription());
        }

        [Test]
        public async Task
            RevokeTokenAsync_WhenUserWithPassedTokenFoundButTokenNotActiveAndUserUpdateFailed_ShouldGenerateNewRefreshToken()
        {
            var tokenPassed = Fixture.Create<string>();
            var listOfToken = Fixture.Build<RefreshToken>()
                .With(w => w.Token, tokenPassed + "1")
                .CreateMany().ToList();
            var user = Fixture.Build<User>().With(w => w.RefreshTokens, listOfToken).Create();
            _mockUserRepository.Setup(e => e.GetUserWithActiveRefreshToken(tokenPassed))
                .ReturnsAsync(user);
            _mockRefreshTokenRepository.Setup(r => r.FilterOneAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync(new RefreshToken());
            _mockUserManager.Setup(e => e.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Failed());

            var response = await sut.RevokeTokenAsync(tokenPassed);

            response.Message.Should().Be(ErrorCode.RefreshTokenRevocationFailed.GetDescription());
        }

        [Test]
        public async Task
            RevokeTokenAsync_WhenUserWithPassedTokenFoundButTokenNotActiveAndUserUpdateSuccess_ShouldGenerateNewRefreshToken()
        {
            var tokenPassed = Fixture.Create<string>();
            var listOfToken = Fixture.Build<RefreshToken>()
                .With(w => w.Token, tokenPassed + "1")
                .CreateMany().ToList();
            var user = Fixture.Build<User>().With(w => w.RefreshTokens, listOfToken).Create();
            _mockUserRepository.Setup(e => e.GetUserWithActiveRefreshToken(tokenPassed))
                .ReturnsAsync(user);
            _mockRefreshTokenRepository.Setup(r => r.FilterOneAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync(new RefreshToken());
            _mockUserManager.Setup(e => e.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);

            var response = await sut.RevokeTokenAsync(tokenPassed);

            response.Message.Should().Be("Token revoked successfully");
        }
        #endregion
    }
}