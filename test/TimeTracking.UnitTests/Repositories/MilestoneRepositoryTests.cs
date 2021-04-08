using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TimeTracking.Common.Pagination;
using TimeTracking.Dal.Impl;
using TimeTracking.Dal.Impl.Repositories;
using TimeTracking.UnitTests.Data;

namespace TimeTracking.UnitTests.Repositories
{
    [TestFixture]
    public class MilestoneRepositoryTests
    {
        private DbContextOptions<TimeTrackingDbContext> _dbOptions;

        [SetUp]
        public void SetUp()
        {
            this._dbOptions = DbOptionsFactory.GetTimeTrackingDbOptions();
        }

        [TestCase("40B40CF5-46E1-4F03-8968-FA1F5DA459B3")]
        [TestCase("9245950B-0F82-4B9E-9AF7-DEC9ACF171FA")]
        public async Task MilestoneRepository_GetById_ShouldReturnCorrectItem(string id)
        {
            var guidId = Guid.Parse(id);
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var expected = MilestonesDbSet.Get().First(x => x.Id == guidId);
            var milestoneRepository = new MilestoneRepository(context);

            var actual = await milestoneRepository.GetByIdAsync(guidId);

            Assert.That(actual, Is.EqualTo(expected).Using(EqualityComparers.MilestoneComparer));
        }

        [Test]
        public async Task MilestoneRepository_DeleteAsync_DeletesEntity()
        {
            var guidId = Guid.Parse("9245950B-0F82-4B9E-9AF7-DEC9ACF171FA");
            var entityToDelete = MilestonesDbSet.Get().First(x => x.Id == guidId);
            var expectedCount = MilestonesDbSet.Get().ToList().Count - 1;
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var milestoneRepository = new MilestoneRepository(context);

            await milestoneRepository.DeleteAsync(entityToDelete);

            context.Milestones.Should().HaveCount(expectedCount);
        }

        [Test]
        public async Task MilestoneRepository_AddAsync_AddsValueToDatabase()
        {
            var expectedCount = MilestonesDbSet.Get().ToList().Count + 1;
            var entityToAdd = MilestonesDbSet.MilestoneBuilder().Create();
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var milestoneRepository = new MilestoneRepository(context);

            await milestoneRepository.AddAsync(entityToAdd);

            context.Milestones.Should().HaveCount(expectedCount);
            var entityFound = await context.Milestones.FindAsync(entityToAdd.Id);
            Assert.That(entityFound, Is.EqualTo(entityToAdd).Using(EqualityComparers.MilestoneComparer));
        }

        [Test]
        public async Task MilestoneRepository_UpdateAsync_UpdateEntity()
        {
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var milestoneRepository = new MilestoneRepository(context);
            var entityToUpdate = MilestonesDbSet.Get().First();
            entityToUpdate.Description = "New description";

            var actual = await milestoneRepository.UpdateAsync(entityToUpdate);

            Assert.That(actual, Is.EqualTo(entityToUpdate).Using(EqualityComparers.MilestoneComparer));
        }

        [Test]
        public async Task MilestoneRepository_GetAllAsync_ReturnsAllValues()
        {
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var milestoneRepository = new MilestoneRepository(context);
            var expected = MilestonesDbSet.Get();

            var actual = await milestoneRepository.GetAllAsync();

            Assert.That(actual.OrderBy(x => x.Id), Is.EqualTo(expected.OrderBy(x => x.Id))
                .Using(EqualityComparers.MilestoneComparer));
        }

        [Test]
        public async Task MilestoneRepository_GetAllPagedAsync_ReturnsAllResultsPaged()
        {
            var page = 1;
            var size = 1;
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var milestoneRepository = new MilestoneRepository(context);
            var expected = MilestonesDbSet.Get()
                .Skip(0)
                .Take(size)
                .OrderBy(e => e.Id)
                .ToList();

            var actual = await milestoneRepository.GetAllPagedAsync(page, size);

            actual.EnsurePagedResult(MilestonesDbSet.Get().ToList().Count, size, page);
            var actualItems = actual.Items.ToList();
            Assert.That(actualItems.OrderBy(e => e.Id), Is.EqualTo(expected.OrderBy(e => e.Id))
                .Using(EqualityComparers.MilestoneComparer));
        }

    }
}