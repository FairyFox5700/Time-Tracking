using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.Kernel;
using AutoMockHelper.Core;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NUnit.Framework;
using TimeTracking.Common.Jwt;
using TimeTracking.Identity.BL.Abstract.Services;
using TimeTracking.Identity.BL.Impl.Factories;
using TimeTracking.Identity.BL.Impl.Helpers;
using TimeTracking.Identity.BL.Impl.Services;
using TimeTracking.Identity.Entities;
using TimeTracking.Identity.Models;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Identity.UnitTests
{
    [TestFixture]
    public class JwtFactoryTests : AutoMockContext<EmailHelperService>
    {
        private static Fixture Fixture = new Fixture();
        private Mock<ISystemClock> systemClock;
        private Mock<UserManager<User>> userManager;
        private Mock<IJwtTokenHandler> tokenHandler;
        private JwtSettings settings;
        private JwtFactory sut;

        [SetUp]
        public void SetUp()
        {
            this.userManager = MockHelpers.MockUserManager<User>();
            this.systemClock = new Mock<ISystemClock>();
            this.tokenHandler = new Mock<IJwtTokenHandler>();
            this.sut = new JwtFactory(tokenHandler.Object, userManager.Object, systemClock.Object, settings);
        }
        [Test]
        public async Task GenerateEncodedAccessToken_WhenUSerValidPassed_shouldReturnValidToken()
        {
            Fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            Fixture.Customize<Claim>(
                c => c.FromFactory(new MethodInvoker(new GreedyConstructorQuery())));
            var user = Fixture.Create<User>();
            var claimsArray = Fixture.CreateMany<Claim>(1).ToArray();
            var rolesArray = Fixture.CreateMany<string>(1).ToArray();
            this.userManager.Setup(e => e.GetClaimsAsync(user))
                .ReturnsAsync(claimsArray);
            this.userManager.Setup(e => e.GetRolesAsync(user))
                .ReturnsAsync(rolesArray);
            this.tokenHandler.Setup(e => e.WriteToken(It.IsAny<JwtSecurityToken>()))
                .Returns((JwtSecurityToken s) => new JwtSecurityTokenHandler().WriteToken(s));
            this.settings = new JwtSettings()
            {
                Issuer = "validIsssue",
                AccessTokenValidFor = TimeSpan.FromDays(3),
                Audience = "some aud",
                Key = "super secret key",//CHECK KEY WITH CONFIG!!!
                RefreshTokenValidFor = TimeSpan.FromDays(3),
            };
            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var token = await sut.GenerateEncodedAccessToken(user);

            token.Should().NotBeNull();
            token.ExpiredAt.Should().Be((long)settings.AccessTokenValidFor.TotalSeconds);
            var jwt = new JwtSecurityToken(token.Token);
            jwt.Issuer.Should().Be(settings.Issuer);
            jwt.Audiences.Should().Contain(settings.Audience);
            jwt.SigningCredentials.Should().Be(signingCredentials);
            //validate claims
            jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iss && c.Value == settings.Issuer);
            jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.UserName);
            jwt.Claims.Should().Contain(
                c => c.Type == JwtRegisteredClaimNames.Iat &&
                     c.Value == DateTimeHelpers.ToUnixEpochDate(settings.IssuedAt).ToString(),
                ClaimValueTypes.Integer64);
            jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
            jwt.Claims.Should().Contain(c => c.Type == Constants.Strings.JwtClaimIdentifiers.Id && c.Value == user.Id.ToString());
        }

        [Test]
        public void CallConstructor_WhenAccessTokenValidForLessOrEqualToZero_ShouldThrowArgumentException()
        {
            this.settings = new JwtSettings()
            {
                AccessTokenValidFor = TimeSpan.Zero
            };
            Func<JwtFactory> func = () => new JwtFactory(tokenHandler.Object, userManager.Object, systemClock.Object, settings);

            func.Should().Throw<ArgumentException>()
                .WithMessage($"Must be a non-zero TimeSpan. (Parameter '{nameof(JwtSettings.AccessTokenValidFor)}')")
                .Which.ParamName.Should().Be(nameof(JwtSettings.AccessTokenValidFor));
        }

        [Test]
        public void CallConstructor_WhenSettingAreNull_SArgumentNullException()
        {
            JwtSettings options = null;
            Func<JwtFactory> func = () => new JwtFactory(tokenHandler.Object, userManager.Object, systemClock.Object, options);

            func.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be(nameof(options));
        }

        [Test]
        public void CallConstructor_WhenSettingKetIsNull_ShouldThrowArgumentNullException()
        {
            JwtSettings options = new JwtSettings()
            {
                AccessTokenValidFor = TimeSpan.MaxValue,
                Key = null,
            };

            Func<JwtFactory> func = () => new JwtFactory(tokenHandler.Object, userManager.Object, systemClock.Object, options);

            func.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be(nameof(JwtSettings.Key));
        }

        [Test]
        public void CallConstructor_WhenSettingJtiGeneratorIsNull_ShouldThrowArgumentNullException()
        {

            var options = MockFor<JwtSettings>();
            options.Setup(e => e.JtiGenerator)
                 .Returns((Func<Task<string>>)null);

            Func<JwtFactory> func = () => new JwtFactory(tokenHandler.Object, userManager.Object, systemClock.Object, options.Object);

            func.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be(nameof(JwtSettings.Key));
        }


        [Theory]
        public void GenerateRefreshToken_WhenSizeSet_ShoulReturnRefreshToken()
        {
            var size = 32;
            var dataTimeOffset = Fixture.Create<DateTimeOffset>();
            systemClock.Setup(e => e.UtcNow)
                .Returns(dataTimeOffset);

            var result = sut.GenerateRefreshToken(size);

            result.Should().NotBeNull();
            result.Created.Should().Be(dataTimeOffset);
            result.Expires.Should().Be(dataTimeOffset + settings.RefreshTokenValidFor);
            result.Token.Should().NotBeNullOrEmpty();
        }

        /*   public RefreshToken GenerateRefreshToken(int size = 32)
        {
            var randomNumber = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return new RefreshToken()
            {
                Created = _systemClock.UtcNow,
                Expires = _systemClock.UtcNow + _jwtSettings.RefreshTokenValidFor,
                Token = Convert.ToBase64String(randomNumber),
            };
        }*/
    }
}