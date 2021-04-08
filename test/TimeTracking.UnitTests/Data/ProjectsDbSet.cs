using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.Dsl;
using TimeTracking.Entities;

namespace TimeTracking.UnitTests.Data
{
    public class ProjectsDbSet
    {
        private static Fixture Fixture = new Fixture();

        private static IQueryable<Project> _projectsDbSet;

        public static IQueryable<Project> Get()
        {
            return _projectsDbSet ??= new List<Project>()
            {
                ProjectBuilder()
                    .With(i => i.Id, new Guid("40B40CF5-46E1-4F03-8968-FA1F5DA459B3"))
                    .Create(),
                ProjectBuilder()
                    .With(i => i.Id, new Guid("9245950B-0F82-4B9E-9AF7-DEC9ACF171FA"))
                    .Create(),
            }.AsQueryable();
        }

        public static IPostprocessComposer<Project> ProjectBuilder()
        {
            return Fixture.Build<Project>()
                .Without(x => x.Issues)
                .Without(x => x.Teams)
                .Without(x => x.Milestones);
        }
    }
}