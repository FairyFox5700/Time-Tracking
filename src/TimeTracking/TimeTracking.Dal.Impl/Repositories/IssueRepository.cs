using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeTracking.Common.Pagination;
using TimeTracking.Common.Repository;
using TimeTracking.Dal.Abstract.Repositories;
using TimeTracking.Entities;

namespace TimeTracking.Dal.Impl.Repositories
{
    public class IssueRepository : Repository<Guid, Issue, TimeTrackingDbContext>, IIssueRepository
    {
        public IssueRepository(TimeTrackingDbContext dbContext) : base(dbContext)
        {
        }

        public Task<Issue> GetIssueWithDetails(Guid id)
        {
            var query = _dbContext.Issues
                .Where(e => e.Id.Equals(id))
                .Include(e => e.WorkLogs)
                .Include(e => e.Milestone)
                .Include(e => e.TimeTrackingUserReporter)
                .Include(e => e.TimeTrackingUserAssigned)
               ;

            return query.FirstOrDefaultAsync();
        }

        public Task<PagedResult<Issue>> GetAllIssueWithDetails(int page, int size)
        {
            var query = _dbContext.Issues
                .Include(e => e.Milestone)
                .Include(e => e.TimeTrackingUserReporter)
                .Include(e => e.TimeTrackingUserAssigned)
                .Include(e => e.WorkLogs);

            return query.PaginateAsync(page, size);
        }
    }
}