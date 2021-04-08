using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.Dsl;
using TimeTracking.Entities;

namespace TimeTracking.UnitTests.Data
{
    public class MilestonesDbSet
    {
        private static Fixture Fixture = new Fixture();

        private static IQueryable<Milestone> _milestonesDbSet;

        public static IQueryable<Milestone> Get()
        {
            return _milestonesDbSet ??= new List<Milestone>()
            {
                MilestoneBuilder()
                    .With(i => i.Id, new Guid("40B40CF5-46E1-4F03-8968-FA1F5DA459B3"))
                    .With(x=>x.ProjectId,new Guid("40B40CF5-46E1-4F03-8968-FA1F5DA459B3"))
                    .With(x=>x.CreatedByUserId,new Guid("57C10EE7-4108-4FD9-BCE9-5477FE8BFF9B"))
                    .Create(),
                MilestoneBuilder()
                    .With(i => i.Id, new Guid("9245950B-0F82-4B9E-9AF7-DEC9ACF171FA"))
                    .With(x =>x.ProjectId,new Guid("9245950B-0F82-4B9E-9AF7-DEC9ACF171FA"))
                    .With(x=>x.CreatedByUserId,new Guid("57C10EE7-4108-4FD9-BCE9-5477FE8BFF9B"))
                    .Create(),
            }.AsQueryable();
        }

        public static IPostprocessComposer<Milestone> MilestoneBuilder()
        {
            return Fixture.Build<Milestone>()
                .Without(x => x.Issues)
                .Without(x => x.CreatedByUser)
                .Without(x => x.State)
                .Without(x => x.Project);
        }
    }
}