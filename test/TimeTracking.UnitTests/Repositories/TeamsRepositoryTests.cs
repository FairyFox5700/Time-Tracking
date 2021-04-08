using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TimeTracking.Dal.Impl;
using TimeTracking.Dal.Impl.Repositories;
using TimeTracking.UnitTests.Data;

namespace TimeTracking.UnitTests.Repositories
{
    [TestFixture]
    public class TeamsRepositoryTests
    {
        private DbContextOptions<TimeTrackingDbContext> _dbOptions;

        [SetUp]
        public void SetUp()
        {
            this._dbOptions = DbOptionsFactory.GetTimeTrackingDbOptions();
        }


        [TestCase("40B40CF5-46E1-4F03-8968-FA1F5DA459B3")]
        [TestCase("9245950B-0F82-4B9E-9AF7-DEC9ACF171FA")]
        public async Task TeamRepository_GetById_ShouldReturnCorrectItem(string id)
        {
            var guidId = Guid.Parse(id);
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var expected = TeamsDbSet.Get().First(x => x.Id == guidId);
            var teamRepository = new TeamRepository(context);

            var actual = await teamRepository.GetByIdAsync(guidId);

            Assert.That(actual, Is.EqualTo(expected).Using(EqualityComparers.TeamComparer));
        }

        [Test]
        public async Task TeamRepository_DeleteAsync_DeletesEntity()
        {
            var guidId = Guid.Parse("9245950B-0F82-4B9E-9AF7-DEC9ACF171FA");
            var entityToDelete = TeamsDbSet.Get().First(x => x.Id == guidId);
            var expectedCount = TeamsDbSet.Get().ToList().Count - 1;
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var teamRepository = new TeamRepository(context);

            await teamRepository.DeleteAsync(entityToDelete);

            context.Teams.Should().HaveCount(expectedCount);
        }

        [Test]
        public async Task TeamRepository_AddAsync_AddsValueToDatabase()
        {
            var expectedCount = TeamsDbSet.Get().ToList().Count + 1;
            var entityToAdd = TeamsDbSet.TeamBuilder().Create();
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var teamRepository = new TeamRepository(context);

            await teamRepository.AddAsync(entityToAdd);

            context.Teams.Should().HaveCount(expectedCount);
            var entityFound = await context.Teams.FindAsync(entityToAdd.Id);
            Assert.That(entityFound, Is.EqualTo(entityToAdd).Using(EqualityComparers.TeamComparer));
        }

        [Test]
        public async Task TeamRepository_UpdateAsync_UpdateEntity()
        {
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var teamRepository = new TeamRepository(context);
            var entityToUpdate = TeamsDbSet.Get().First();
            entityToUpdate.Name = "New name";

            var actual = await teamRepository.UpdateAsync(entityToUpdate);

            Assert.That(actual, Is.EqualTo(entityToUpdate).Using(EqualityComparers.TeamComparer));
        }

        [Test]
        public async Task TeamRepository_GetAllAsync_ReturnsAllValues()
        {
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var teamRepository = new TeamRepository(context);
            var expected = TeamsDbSet.Get();

            var actual = await teamRepository.GetAllAsync();

            Assert.That(actual.OrderBy(x => x.Id), Is.EqualTo(expected.OrderBy(x => x.Id))
                .Using(EqualityComparers.TeamComparer));
        }

        [Test]
        public async Task TeamRepository_GetAllPagedAsync_ReturnsAllResultsPaged()
        {
            var page = 1;
            var size = 1;
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var teamRepository = new TeamRepository(context);
            var expected = TeamsDbSet.Get()
                .Skip(0)
                .Take(size)
                .OrderBy(e => e.Id)
                .ToList();

            var actual = await teamRepository.GetAllPagedAsync(page, size);

            actual.EnsurePagedResult(TeamsDbSet.Get().ToList().Count, size, page);
            var actualItems = actual.Items.ToList();
            Assert.That(actualItems.OrderBy(e => e.Id), Is.EqualTo(expected.OrderBy(e => e.Id))
                .Using(EqualityComparers.TeamComparer));
        }

        [Test]
        public async Task TeamRepository_GetByIdWithDetails_ReturnsTeamWithProjectById()
        {
            var teamId = new Guid("40B40CF5-46E1-4F03-8968-FA1F5DA459B3");
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var teamRepository = new TeamRepository(context);
            var expected = TeamsDbSet.Get()
                .Include(e => e.Project)
                .First(e => e.Id.Equals(teamId));

            var actual = await teamRepository.GetByIdWithDetails(teamId);

            Assert.That(actual, Is.EqualTo(expected).Using(EqualityComparers.TeamComparer));
            Assert.That(actual.Project, Is.EqualTo(expected.Project).Using(EqualityComparers.ProjectComparer));
        }

    }
}