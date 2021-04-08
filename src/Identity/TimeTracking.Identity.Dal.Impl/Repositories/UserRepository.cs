using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeTracking.Common.Repository;
using TimeTracking.Identity.Dal.Abstract;
using TimeTracking.Identity.Entities;

namespace TimeTracking.Identity.Dal.Impl.Repositories
{
    public class UserRepository : Repository<Guid, User, TimeTrackingIdentityDbContext>, IUserRepository
    {
        public UserRepository(TimeTrackingIdentityDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<User> GetUserWithActiveRefreshToken(string token)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(e =>
                e.RefreshTokens.Any(t => t.Token == token && t.IsActive == true));
        }
    }
}