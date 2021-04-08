using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeTracking.Common.Repository;
using TimeTracking.Dal.Abstract.Repositories;
using TimeTracking.Entities;

namespace TimeTracking.Dal.Impl.Repositories
{
    public class TeamRepository : Repository<Guid, Team, TimeTrackingDbContext>, ITeamRepository
    {
        public TeamRepository(TimeTrackingDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Team> GetByIdWithDetails(Guid teamId)
        {
            var query = await _dbContext.Teams
                                        .Include(e => e.Project)
                                        .FirstOrDefaultAsync(e => e.Id == teamId);
            return query;
        }
    }
}