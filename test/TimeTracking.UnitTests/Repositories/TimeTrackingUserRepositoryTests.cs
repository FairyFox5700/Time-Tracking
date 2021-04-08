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
    public class TimeTrackingUserRepositoryTests
    {
        private DbContextOptions<TimeTrackingDbContext> _dbOptions;

        [SetUp]
        public void SetUp()
        {
            this._dbOptions = DbOptionsFactory.GetTimeTrackingDbOptions();
        }


        [TestCase("29C2F600-4F76-4753-A54D-A422DEF8EB9E")]
        [TestCase("57C10EE7-4108-4FD9-BCE9-5477FE8BFF9B")]
        public async Task UserRepository_GetById_ShouldReturnCorrectItem(string id)
        {
            var guidId = Guid.Parse(id);
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var expected = UsersDbSet.Get().First(x => x.Id == guidId);
            var userRepository = new UserRepository(context);

            var actual = await userRepository.GetByIdAsync(guidId);

            Assert.That(actual, Is.EqualTo(expected).Using(EqualityComparers.TimeTrackingUserComparer));
        }

        [Test]
        public async Task UserRepository_DeleteAsync_DeletesEntity()
        {
            var guidId = Guid.Parse("57C10EE7-4108-4FD9-BCE9-5477FE8BFF9B");
            var entityToDelete = UsersDbSet.Get().First(x => x.Id == guidId);
            var expectedCount = UsersDbSet.Get().ToList().Count - 1;
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var userRepository = new UserRepository(context);

            await userRepository.DeleteAsync(entityToDelete);

            context.Users.Should().HaveCount(expectedCount);
        }

        [Test]
        public async Task UserRepository_AddAsync_AddsValueToDatabase()
        {
            var expectedCount = UsersDbSet.Get().ToList().Count + 1;
            var entityToAdd = UsersDbSet.TimeTrackingUserBuilder().Create();
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var userRepository = new UserRepository(context);

            await userRepository.AddAsync(entityToAdd);

            context.Users.Should().HaveCount(expectedCount);
            var entityFound = await context.Users.FindAsync(entityToAdd.Id);
            entityFound.Should().BeEquivalentTo(entityFound);
            Assert.That(entityToAdd, Is.EqualTo(entityFound).Using(EqualityComparers.TimeTrackingUserComparer));
        }

        [Test]
        public async Task UserRepository_UpdateAsync_UpdateEntity()
        {
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var userRepository = new UserRepository(context);
            var entityToUpdate = UsersDbSet.Get().First();
            entityToUpdate.FirstName = "New first name";

            var actual = await userRepository.UpdateAsync(entityToUpdate);

            actual.Should().BeEquivalentTo(entityToUpdate);
        }


        [Test]
        public async Task UserRepository_GetAllAsync_ReturnsAllValues()
        {
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var userRepository = new UserRepository(context);
            var expected = UsersDbSet.Get();

            var actual = await userRepository.GetAllAsync();

            Assert.That(actual.OrderBy(x => x.Id), Is.EqualTo(expected.OrderBy(x => x.Id))
                .Using(EqualityComparers.TimeTrackingUserComparer));
        }

        [Test]
        public async Task UserRepository_GetAllPagedAsync_ReturnsAllResultsPaged()
        {
            var page = 1;
            var size = 3;
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var userRepository = new UserRepository(context);
            var expected = UsersDbSet.Get()
                .Skip(0)
                .Take(size)
                .OrderBy(e => e.Id)
                .ToList();

            var actual = await userRepository.GetAllPagedAsync();

            actual.EnsurePagedResult(UsersDbSet.Get().ToList().Count, size, page);
            var actualItems = actual.Items.ToList();
            Assert.That(actualItems.OrderBy(e => e.Id), Is.EqualTo(expected.OrderBy(e => e.Id))
                .Using(EqualityComparers.TimeTrackingUserComparer));
        }
    }
}