using System;
using System.Threading.Tasks;

namespace TimeTracking.Common.Jwt
{
    public class JwtSettings
    {
        public string Key { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public DateTime IssuedAt => DateTime.UtcNow;
        public DateTime AccessTokenExpiration => IssuedAt.Add(AccessTokenValidFor);
        public TimeSpan AccessTokenValidFor { get; set; } = TimeSpan.FromMinutes(120 * 3);
        public TimeSpan RefreshTokenValidFor { get; set; } = TimeSpan.FromMinutes(120);
        public DateTime NotBefore => DateTime.UtcNow;
        public virtual Func<Task<string>> JtiGenerator
        {
            get => () => Task.FromResult(Guid.NewGuid().ToString());
        }
    }
}