using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TimeTracking.Dal.Impl;
using TimeTracking.Dal.Impl.Repositories;
using TimeTracking.Entities;
using TimeTracking.UnitTests.Data;

namespace TimeTracking.UnitTests.Repositories
{
    [TestFixture]
    public class WorkLogRepositoryTests
    {
        private DbContextOptions<TimeTrackingDbContext> _dbOptions;

        [SetUp]
        public void SetUp()
        {
            this._dbOptions = DbOptionsFactory.GetTimeTrackingDbOptions();
        }


        [TestCase("29C2F600-4F76-4753-A54D-A422DEF8EB9E")]
        [TestCase("8124091A-39CA-496D-A639-FE236107C63D")]
        public async Task WorklogRepository_GetById_ShouldReturnCorrectItem(string id)
        {
            var guidId = Guid.Parse(id);
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var expected = WorklogsDbSet.Get().First(x => x.Id == guidId);
            var workLogRepository = new WorklogRepository(context);

            var actual = await workLogRepository.GetByIdAsync(guidId);

            Assert.That(actual, Is.EqualTo(expected).Using(EqualityComparers.WorkLogComparer));
        }

        [Test]
        public async Task WorklogRepository_DeleteAsync_DeletesEntity()
        {
            var guidId = Guid.Parse("29C2F600-4F76-4753-A54D-A422DEF8EB9E");
            var entityToDelete = WorklogsDbSet.Get().First(x => x.Id == guidId);
            var expectedCount = WorklogsDbSet.Get().ToList().Count - 1;
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var workLogRepository = new WorklogRepository(context);

            await workLogRepository.DeleteAsync(entityToDelete);

            context.WorkLogs.Should().HaveCount(expectedCount);
        }

        [Test]
        public async Task WorklogRepository_AddAsync_AddsValueToDatabase()
        {
            var expectedCount = WorklogsDbSet.Get().ToList().Count + 1;
            var entityToAdd = WorklogsDbSet.WorkLogBuilder().Create();
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var workLogRepository = new WorklogRepository(context);

            await workLogRepository.AddAsync(entityToAdd);

            context.WorkLogs.Should().HaveCount(expectedCount);
            var entityFound = await context.WorkLogs.FindAsync(entityToAdd.Id);
            entityFound.Should().BeEquivalentTo(entityFound);
        }

        [Test]
        public async Task WorklogRepository_UpdateAsync_UpdateEntity()
        {
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var workLogRepository = new WorklogRepository(context);
            var entityToUpdate = WorklogsDbSet.Get().First();
            entityToUpdate.Description = "New description";

            var actual = await workLogRepository.UpdateAsync(entityToUpdate);

            actual.Should().BeEquivalentTo(entityToUpdate);
        }


        [Test]
        public async Task WorklogRepository_GetAllAsync_ReturnsAllValues()
        {
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var workLogRepository = new WorklogRepository(context);
            var expected = WorklogsDbSet.Get();

            var actual = await workLogRepository.GetAllAsync();

            Assert.That(actual.OrderBy(x => x.Id), Is.EqualTo(expected.OrderBy(x => x.Id))
                .Using(EqualityComparers.WorkLogComparer));
        }

        [Test]
        public async Task WorklogRepository_GetAllPagedAsync_ReturnsAllResultsPaged()
        {
            var page = 1;
            var size = 3;
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var workLogRepository = new WorklogRepository(context);
            var expected = WorklogsDbSet.Get().Take(size).ToList();

            var actual = await workLogRepository.GetAllPagedAsync(page, size);

            actual.EnsurePagedResult(WorklogsDbSet.Get().ToList().Count, size, page);
            var actualItems = actual.Items.ToList();
            actualItems.Should().BeEquivalentTo(expected, options => options
                .Excluding(e => e.TimeTrackingUser)
                .Excluding(e => e.Issue));
        }

        [Test]
        public async Task WorklogRepository_GetActivitiesWithDetailsByUserId_ReturnstActivitiesWithDetails()
        {
            var userId = new Guid("57C10EE7-4108-4FD9-BCE9-5477FE8BFF9B");
            var projectId = new Guid("40B40CF5-46E1-4F03-8968-FA1F5DA459B3");
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var workLogRepository = new WorklogRepository(context);
            var expected = WorklogsDbSet.Get().Where(e => e.UserId == userId)
                .Include(x => x.Issue)
                .ThenInclude(e => e.Project)
                .Where(e => e.Issue.ProjectId.Equals(projectId))
                .ToList();

            var actual = await workLogRepository.GetActivitiesWithDetailsByUserId(userId, projectId);

            Assert.That(actual.OrderBy(e => e.Id), Is.EqualTo(expected.OrderBy(e => e.Id))
                .Using(EqualityComparers.WorkLogComparer));
        }


        [Test]
        public async Task WorklogRepository_GetByIdWithUserAsync_ReturnsWorkLogForWithUser()
        {
            var workLogId = new Guid("29C2F600-4F76-4753-A54D-A422DEF8EB9E");
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var workLogRepository = new WorklogRepository(context);
            var expected = WorklogsDbSet.Get().Where(e => e.Id == workLogId)
                .Include(e => e.TimeTrackingUser)
                .FirstOrDefault();

            var actual = await workLogRepository.GetByIdWithUserAsync(workLogId);

            Assert.That(actual, Is.EqualTo(expected).Using(EqualityComparers.WorkLogComparer));
            Assert.That(actual.TimeTrackingUser,
                Is.EqualTo(expected.TimeTrackingUser).Using(EqualityComparers.TimeTrackingUserComparer));

        }

    }
}