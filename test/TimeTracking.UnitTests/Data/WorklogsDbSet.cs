using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.Dsl;
using TimeTracking.Entities;

namespace TimeTracking.UnitTests.Data
{
    public class WorklogsDbSet
    {
        private static Fixture Fixture = new Fixture();

        private static IQueryable<WorkLog> _workLogDbSet;

        public static IQueryable<WorkLog> Get()
        {
            return _workLogDbSet ??= new List<WorkLog>()
            {
                WorkLogBuilder()
                    .With(i => i.Id, new Guid("29C2F600-4F76-4753-A54D-A422DEF8EB9E"))
                    .With(x=>x.IssueId,new Guid("EC3CB528-45E3-4ABA-8E6E-DB40D0C0A400"))
                    .With(x=>x.UserId,new Guid("57C10EE7-4108-4FD9-BCE9-5477FE8BFF9B"))
                    .Create(),
                WorkLogBuilder()
                    .With(i => i.Id, new Guid("8124091A-39CA-496D-A639-FE236107C63D"))
                    .With(x=>x.IssueId,new Guid("EC3CB528-45E3-4ABA-8E6E-DB40D0C0A400"))
                    .With(x=>x.UserId,new Guid("57C10EE7-4108-4FD9-BCE9-5477FE8BFF9B"))
                    .Create(),
                WorkLogBuilder()
                    .With(i => i.Id, new Guid("44A345F7-DD98-4CA3-B193-CEB4D4904864"))
                    .With(x=>x.IssueId,new Guid("EC3CB528-45E3-4ABA-8E6E-DB40D0C0A400"))
                    .With(x=>x.UserId,new Guid("57C10EE7-4108-4FD9-BCE9-5477FE8BFF9B"))
                    .Create(),
                WorkLogBuilder()
                    .With(i => i.Id, new Guid("18EE2379-2438-451B-A00D-18757F6B6C18"))
                    .With(x=>x.IssueId,new Guid("EC3CB528-45E3-4ABA-8E6E-DB40D0C0A400"))
                    .With(x=>x.UserId,new Guid("57C10EE7-4108-4FD9-BCE9-5477FE8BFF9B"))
                    .Create(),
                WorkLogBuilder()
                    .With(i => i.Id, new Guid("0CAAA323-155B-4CDB-87CF-C2E7215A9011"))
                    .With(x=>x.IssueId,new Guid("EC3CB528-45E3-4ABA-8E6E-DB40D0C0A400"))
                    .With(x=>x.UserId,new Guid("57C10EE7-4108-4FD9-BCE9-5477FE8BFF9B"))
                    .Create(),
                WorkLogBuilder()
                    .With(i => i.Id, new Guid("3E9B807E-2A1D-4B43-A191-865081E34E7E"))
                    .With(x=>x.IssueId,new Guid("BB25D21B-2CD9-4EA3-A82F-1E9EB669E6FA"))
                    .With(x=>x.UserId,new Guid("57C10EE7-4108-4FD9-BCE9-5477FE8BFF9B"))
                    .Create(),
            }.AsQueryable();
        }
        public static IPostprocessComposer<WorkLog> WorkLogBuilder()
        {
            return Fixture.Build<WorkLog>()
                .Without(x => x.Issue)
                .Without(x => x.TimeTrackingUser);
        }
    }
}