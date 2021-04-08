using System;
using System.Threading.Tasks;
using TimeTracking.Common.Abstract.Repository;
using TimeTracking.Identity.Entities;

namespace TimeTracking.Identity.Dal.Abstract
{
    public interface IUserRepository : IRepository<Guid, User>
    {
        Task<User> GetUserWithActiveRefreshToken(string token);
    }
}