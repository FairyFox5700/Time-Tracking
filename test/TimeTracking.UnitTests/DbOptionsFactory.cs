using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TimeTracking.Dal.Impl;
using TimeTracking.UnitTests.Data;

namespace TimeTracking.UnitTests
{
    internal static class DbOptionsFactory
    {
        public static DbContextOptions<TimeTrackingDbContext> GetTimeTrackingDbOptions()
        {
            var options = new DbContextOptionsBuilder<TimeTrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new TimeTrackingDbContext(options);
            SeedData(context);
            return options;
        }

        private static void SeedData(TimeTrackingDbContext context)
        {
            context.Issues.AddRange(IssuesDbSet.Get().ToList());
            context.Projects.AddRange(ProjectsDbSet.Get().ToList());
            context.Milestones.AddRange(MilestonesDbSet.Get().ToList());
            context.Teams.AddRange(TeamsDbSet.Get().ToList());
            context.WorkLogs.AddRange(WorklogsDbSet.Get().ToList());
            context.Users.AddRange(UsersDbSet.Get().ToList());
            context.SaveChanges();
        }

    }
}