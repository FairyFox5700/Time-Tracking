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
    public class IssueRepositoryTests
    {
        private DbContextOptions<TimeTrackingDbContext> _dbOptions;

        [SetUp]
        public void SetUp()
        {
            _dbOptions = DbOptionsFactory.GetTimeTrackingDbOptions();
        }

        [TestCase("EC3CB528-45E3-4ABA-8E6E-DB40D0C0A400")]
        [TestCase("BB25D21B-2CD9-4EA3-A82F-1E9EB669E6FA")]
        public async Task IssueRepository_GetById_ShouldReturnCorrectItem(string id)
        {
            var guidId = Guid.Parse(id);
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var expected = IssuesDbSet.Get().First(x => x.Id == guidId);
            var issueRepository = new IssueRepository(context);

            var issue = await issueRepository.GetByIdAsync(guidId);

            Assert.That(issue, Is.EqualTo(expected).Using(EqualityComparers.IssueComparer));
        }


        [Test]
        public async Task IssueRepository_DeleteAsync_DeletesEntity()
        {
            var guidId = Guid.Parse("EC3CB528-45E3-4ABA-8E6E-DB40D0C0A400");
            var entityToDelete = IssuesDbSet.Get().First(x => x.Id == guidId);
            var expectedCount = IssuesDbSet.Get().ToList().Count - 1;
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var issueRepository = new IssueRepository(context);

            await issueRepository.DeleteAsync(entityToDelete);

            context.Issues.Should().HaveCount(expectedCount);
        }


        [Test]
        public async Task IssueRepository_AddAsync_AddsValueToDatabase()
        {
            var expectedCount = IssuesDbSet.Get().ToList().Count + 1;
            var entityToAdd = IssuesDbSet.IssueBuilder().Create();
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var issueRepository = new IssueRepository(context);

            await issueRepository.AddAsync(entityToAdd);

            context.Issues.Should().HaveCount(expectedCount);
            var entityFound = context.Issues.Find(entityToAdd.Id);
            entityFound.Should().BeEquivalentTo(entityFound);
        }

        [TestCase("BB25D21B-2CD9-4EA3-A82F-1E9EB669E6FA")]
        public async Task IssueRepository_GetIssueWithDetails_ShouldReturnCorrectItem(string id)
        {
            var guidId = Guid.Parse(id);
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var issueRepository = new IssueRepository(context);
            var expected = IssuesDbSet.Get().First(x => x.Id == guidId);
            var expectedMilestone = MilestonesDbSet.Get().First(e => e.Id == expected.MilestoneId);
            var expectedWorkLogs = WorklogsDbSet.Get().Where(e => e.IssueId == expected.Id).OrderBy(e => e.Id).ToList();
            var expectedTimeTrackingUserReporter = UsersDbSet.Get().First(e => e.Id == expected.ReportedByUserId);
            var expectedTimeTrackingUserAssigned = UsersDbSet.Get().First(e => e.Id == expected.AssignedToUserId);

            var issue = await issueRepository.GetIssueWithDetails(guidId);

            Assert.That(issue, Is.EqualTo(expected).Using(EqualityComparers.IssueComparer));
            Assert.That(issue.Milestone, Is.EqualTo(expectedMilestone).Using(EqualityComparers.MilestoneComparer));
            Assert.That(issue.WorkLogs.ToList(), Is.EqualTo(expectedWorkLogs).Using(EqualityComparers.WorkLogComparer));
            Assert.That(issue.TimeTrackingUserReporter, Is.EqualTo(expectedTimeTrackingUserReporter).Using(EqualityComparers.TimeTrackingUserComparer));
            Assert.That(issue.TimeTrackingUserAssigned, Is.EqualTo(expectedTimeTrackingUserAssigned).Using(EqualityComparers.TimeTrackingUserComparer));
        }

        [Test]
        public async Task IssueRepository_GetAllPagedAsync_ReturnsAllResultsPaged()
        {
            var page = 1;
            var size = 1;
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var issueRepository = new IssueRepository(context);
            var expected = IssuesDbSet.Get()
                .Skip(0)
                .Take(size)
                .OrderBy(e => e.Id)
                .ToList();

            var issues = await issueRepository.GetAllPagedAsync(page, size);

            issues.EnsurePagedResult(IssuesDbSet.Get().ToList().Count, size, page);
            var actualItems = issues.Items.ToList();
            Assert.That(actualItems.OrderBy(e => e.Id), Is.EqualTo(expected.OrderBy(e => e.Id))
                .Using(EqualityComparers.IssueComparer));
        }


        [Test]
        public async Task IssueRepository_GetAllAsync_ReturnsAllValues()
        {
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var issueRepository = new IssueRepository(context);
            var expected = IssuesDbSet.Get();

            var actual = await issueRepository.GetAllAsync();

            Assert.That(actual.OrderBy(e => e.Id), Is.EqualTo(expected.OrderBy(e => e.Id))
                .Using(EqualityComparers.IssueComparer));
        }

        [Test]
        public async Task IssueRepository_UpdateAsync_UpdateEntity()
        {
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var issueRepository = new IssueRepository(context);
            var entityToUpdate = IssuesDbSet.Get().First();
            entityToUpdate.Title = "New title";

            var actual = await issueRepository.UpdateAsync(entityToUpdate);

            Assert.That(actual, Is.EqualTo(entityToUpdate).Using(EqualityComparers.IssueComparer));
        }

        [Test]
        public async Task IssueRepository_GetAllIssueWithDetails_ShouldReturnPagedResult()
        {
            var page = 1;
            var size = 1;
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var issueRepository = new IssueRepository(context);
            var expected = IssuesDbSet.Get()
                .Skip(0)
                .Take(size)
                .OrderBy(e => e.Id)
                .ToList();

            var issues = await issueRepository.GetAllIssueWithDetails(page, size);
            issues.EnsurePagedResult(IssuesDbSet.Get().ToList().Count, size, page);
            var actualItems = issues.Items.ToList();
            Assert.That(actualItems.OrderBy(e => e.Id), Is.EqualTo(expected.OrderBy(e => e.Id))
                .Using(EqualityComparers.IssueComparer));
        }
    }
}