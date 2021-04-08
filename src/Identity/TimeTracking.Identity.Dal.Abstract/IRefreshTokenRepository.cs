using System;
using TimeTracking.Common.Abstract.Repository;
using TimeTracking.Identity.Entities;

namespace TimeTracking.Identity.Dal.Abstract
{
    public interface IRefreshTokenRepository : IRepository<Guid, RefreshToken>
    {

    }
}