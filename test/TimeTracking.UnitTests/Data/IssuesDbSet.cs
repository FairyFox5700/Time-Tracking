using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.Dsl;
using TimeTracking.Entities;

namespace TimeTracking.UnitTests.Data
{
    public static class IssuesDbSet
    {
        private static Fixture Fixture = new Fixture();

        private static IQueryable<Issue> _issuesDbSet;

        public static IQueryable<Issue> Get()
        {
            return _issuesDbSet ??= new List<Issue>()
            {
                IssueBuilder()
                    .With(i => i.Id, new Guid("EC3CB528-45E3-4ABA-8E6E-DB40D0C0A400"))
                    .With(x => x.MilestoneId,new Guid("40B40CF5-46E1-4F03-8968-FA1F5DA459B3"))
                    .With(x => x.ProjectId,new Guid("40B40CF5-46E1-4F03-8968-FA1F5DA459B3"))
                    .With(x => x.AssignedToUserId,new Guid("403FD7F2-027B-42B6-875A-72555E999D61"))
                    .With(x => x.ReportedByUserId,new Guid("403FD7F2-027B-42B6-875A-72555E999D61"))
                    .Create(),

                IssueBuilder()
                    .With(i => i.Id, new Guid("BB25D21B-2CD9-4EA3-A82F-1E9EB669E6FA"))
                    .With(x => x.MilestoneId,new Guid("9245950B-0F82-4B9E-9AF7-DEC9ACF171FA"))
                    .With(x => x.ProjectId,new Guid("40B40CF5-46E1-4F03-8968-FA1F5DA459B3"))
                    .With(x => x.AssignedToUserId,new Guid("57C10EE7-4108-4FD9-BCE9-5477FE8BFF9B"))
                    .With(x => x.ReportedByUserId,new Guid("57C10EE7-4108-4FD9-BCE9-5477FE8BFF9B"))
                    .Create(),
            }.AsQueryable();
        }

        public static IPostprocessComposer<Issue> IssueBuilder()
        {
            return Fixture.Build<Issue>()
                .Without(x => x.Milestone)
                .Without(x => x.Project)
                .Without(x => x.WorkLogs)
                .Without(w => w.TimeTrackingUserReporter)
                .Without(w => w.TimeTrackingUserAssigned);
        }
    }
}