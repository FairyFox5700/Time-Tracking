using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.Dsl;
using TimeTracking.Entities;

namespace TimeTracking.UnitTests.Data
{
    public class TeamsDbSet
    {
        private static Fixture Fixture = new Fixture();

        private static IQueryable<Team> _teamsDbSet;

        public static IQueryable<Team> Get()
        {
            return _teamsDbSet ??= new List<Team>()
            {
                TeamBuilder()
                    .With(i => i.Id, new Guid("40B40CF5-46E1-4F03-8968-FA1F5DA459B3"))
                    .With(i => i.ProjectId, new Guid("40B40CF5-46E1-4F03-8968-FA1F5DA459B3"))
                    .Create(),
                TeamBuilder()
                    .With(i => i.Id, new Guid("9245950B-0F82-4B9E-9AF7-DEC9ACF171FA"))
                    .With(i => i.ProjectId, new Guid("40B40CF5-46E1-4F03-8968-FA1F5DA459B3"))
                    .Create(),
            }.AsQueryable();
        }

        public static IPostprocessComposer<Team> TeamBuilder()
        {
            return Fixture.Build<Team>()
                .Without(x => x.Project)
                .Without(x => x.Users);
        }
    }
}