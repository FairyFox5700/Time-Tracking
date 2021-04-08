using System;
using TimeTracking.Common.Repository;
using TimeTracking.Identity.Dal.Abstract;
using TimeTracking.Identity.Entities;

namespace TimeTracking.Identity.Dal.Impl.Repositories
{
    public class RefreshTokenRepository : Repository<Guid, RefreshToken, TimeTrackingIdentityDbContext>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(TimeTrackingIdentityDbContext dbContext) : base(dbContext)
        {
        }


    }
}