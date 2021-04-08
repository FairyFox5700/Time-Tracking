using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TimeTracking.Common.Helpers;
using TimeTracking.Common.Wrapper;
using TimeTracking.Identity.BL.Abstract.Factories;
using TimeTracking.Identity.BL.Abstract.Services;
using TimeTracking.Identity.Dal.Abstract;
using TimeTracking.Identity.Entities;
using TimeTracking.Identity.Models.Requests;
using TimeTracking.Identity.Models.Responses;

namespace TimeTracking.Identity.BL.Impl.Services
{
    public class TokenService : ITokenService
    {
        private readonly UserManager<User> _userManager;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IJwtFactory _jwtFactory;
        private readonly ISystemClock _systemClock;
        private readonly ILogger<TokenService> _logger;

        public TokenService(UserManager<User> userManager,
            IRefreshTokenRepository refreshTokenRepository,
            IUserRepository userRepository,
            IJwtFactory jwtFactory,
            ISystemClock systemClock,
            ILogger<TokenService> logger)
        {
            _userManager = userManager;
            _refreshTokenRepository = refreshTokenRepository;
            _userRepository = userRepository;
            _jwtFactory = jwtFactory;
            _systemClock = systemClock;
            _logger = logger;
        }

        public async Task<AuthResponse> LoginAsync(TokenExchangeRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new AuthResponse()
                {
                    Message = $"No Accounts Registered with {request.Email}.",
                };
            }
            // Only allow login if email is confirmed
            if (!user.EmailConfirmed)
            {
                return new AuthResponse()
                {
                    Message = $"Current email {request.Email} is not confirmed.",
                };
            }
            // Used as user lock
            if (user.LockoutEnd >= DateTimeOffset.UtcNow)
            {
                return new AuthResponse()
                {
                    Message = $"This account has been locked.",
                };
            }

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return new AuthResponse()
                {
                    Message = $"Incorrect Credentials for user {user.Email}.",
                };
            }

            var jwtSecurityToken = await _jwtFactory.GenerateEncodedAccessToken(user);
            var activeRefreshToken = await _refreshTokenRepository.FilterOneAsync(e => e.IsActive);
            if (activeRefreshToken != null)
            {
                return new AuthResponse()
                {
                    RefreshToken = activeRefreshToken!.Token,
                    ExpiredAt = (activeRefreshToken.Expires - DateTimeOffset.UtcNow).Seconds,
                    Token = jwtSecurityToken.Token,
                };
            }

            return await RefreshTokenInternal(user);
        }


        private async Task<AuthResponse> RefreshTokenInternal(User user)
        {
            var newRefreshToken = _jwtFactory.GenerateRefreshToken();
            user.RefreshTokens.Add(newRefreshToken);
            var updateResponse = await _userManager.UpdateAsync(user);
            if (updateResponse.Succeeded)
            {
                return new AuthResponse()
                {
                    Token = (await _jwtFactory.GenerateEncodedAccessToken(user)).Token,
                    RefreshToken = newRefreshToken.Token,
                    ExpiredAt = (newRefreshToken.Expires - DateTimeOffset.UtcNow).Seconds,
                };
            }

            _logger.LogWarning("Updating user with id {0} failed with reason {1}", user.Id, updateResponse.ToString());
            return new AuthResponse()
            {
                Message = "Failed to generate refresh token.",
            };

        }
        public async Task<AuthResponse> RefreshTokenAsync(string token)
        {
            var user = await _userRepository.GetUserWithActiveRefreshToken(token);
            var revokeResponse = await RevokeTokenInternal(user, token);
            if (revokeResponse != null)
            {
                return revokeResponse;
            }

            return await RefreshTokenInternal(user);
        }


        private async Task<AuthResponse> RevokeTokenInternal(User user, string token)
        {
            var refreshToken =
                (await _refreshTokenRepository.FilterOneAsync(e => e.Token == token && e.IsActive == true));
            if (refreshToken == null)
            {
                return new AuthResponse()
                {
                    Message = ErrorCode.RefreshTokenRevocationFailed.GetDescription(),
                };
            }
            refreshToken.Revoked = _systemClock.UtcNow;
            return null;
        }

        public async Task<AuthResponse> RevokeTokenAsync(string token)
        {
            var user = await _userRepository.GetUserWithActiveRefreshToken(token);
            var revokeResponse = await RevokeTokenInternal(user, token);
            if (revokeResponse != null)
            {
                return revokeResponse;
            }
            var updateResponse = await _userManager.UpdateAsync(user);
            if (updateResponse.Succeeded)
            {
                return new AuthResponse()
                {
                    Message = "Token revoked successfully"
                };
            }

            _logger.LogWarning("Updating user with id {0} failed with reason {1}", user.Id, updateResponse.ToString());
            return new AuthResponse()
            {
                Message = ErrorCode.RefreshTokenRevocationFailed.GetDescription(),
            };

        }
    }
}