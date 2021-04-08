using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TimeTracking.Common.Jwt;
using TimeTracking.Identity.BL.Abstract;
using TimeTracking.Identity.BL.Abstract.Factories;
using TimeTracking.Identity.BL.Abstract.Services;
using TimeTracking.Identity.BL.Impl.Settings;
using TimeTracking.Identity.Entities;
using TimeTracking.Identity.Models;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

[assembly: InternalsVisibleTo("Identity.UnitTests")]
namespace TimeTracking.Identity.BL.Impl.Factories
{
    internal sealed class JwtFactory : IJwtFactory
    {
        private readonly IJwtTokenHandler _jwtTokenHandler;
        private readonly UserManager<User> _userManager;
        private readonly ISystemClock _systemClock;
        private readonly JwtSettings _jwtSettings;

        public JwtFactory(IJwtTokenHandler jwtTokenHandler,
            UserManager<User> userManager,
            ISystemClock systemClock,
            JwtSettings jwtSettings)
        {
            _jwtTokenHandler = jwtTokenHandler;
            _userManager = userManager;
            _systemClock = systemClock;
            ThrowIfInvalidOptions(jwtSettings);
            _jwtSettings = jwtSettings;

        }

        public async Task<JwtAccessToken> GenerateEncodedAccessToken(User user)
        {
            var claims = await GenerateClaimsIdentity(user);
            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);
            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                notBefore: _jwtSettings.NotBefore,
                expires: _jwtSettings.AccessTokenExpiration,
                signingCredentials: signingCredentials);
            return new JwtAccessToken()
            {
                ExpiredAt = (long)_jwtSettings.AccessTokenValidFor.TotalSeconds,
                Token = _jwtTokenHandler.WriteToken(jwtSecurityToken),
            };
        }

        private async Task<IEnumerable<Claim>> GenerateClaimsIdentity(User user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = new List<Claim>();
            for (int i = 0; i < roles.Count; i++)
            {
                roleClaims.Add(new Claim("roles", roles[i]));
            }
            var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, await _jwtSettings.JtiGenerator()),
                    new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_jwtSettings.IssuedAt).ToString(),
                        ClaimValueTypes.Integer64),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(Constants.Strings.JwtClaimIdentifiers.Id, user.Id.ToString()),
                }
                .Union(userClaims)
                .Union(roleClaims);
            return claims;
        }


        /// <summary>
        ///  Coverts <param name="date"/> covered to seconds since Unix epoch (Jan 1, 1970, midnight UTC)  
        /// </summary>
        /// <param name="date"></param>
        /// <returns>Converted data in seconds since Unix epoch</returns>
        private static long ToUnixEpochDate(DateTime date)
          => (long)Math.Round((date.ToUniversalTime() -
                               new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
                              .TotalSeconds);



        private static void ThrowIfInvalidOptions(JwtSettings options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.AccessTokenValidFor <= TimeSpan.Zero)
            {
                throw new ArgumentException("Must be a non-zero TimeSpan.", nameof(JwtSettings.AccessTokenValidFor));
            }

            if (options.Key == null)
            {
                throw new ArgumentNullException(nameof(JwtSettings.Key));
            }

            if (options.JtiGenerator == null)
            {
                throw new ArgumentNullException(nameof(options.JtiGenerator));
            }
        }

        public RefreshToken GenerateRefreshToken(int size = 32)
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
        }
    }
}