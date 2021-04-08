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
    public class ProjectsRepositoryTests
    {
        private DbContextOptions<TimeTrackingDbContext> _dbOptions;

        [SetUp]
        public void SetUp()
        {
            this._dbOptions = DbOptionsFactory.GetTimeTrackingDbOptions();
        }

        [TestCase("40B40CF5-46E1-4F03-8968-FA1F5DA459B3")]
        [TestCase("9245950B-0F82-4B9E-9AF7-DEC9ACF171FA")]
        public async Task ProjectRepository_GetById_ShouldReturnCorrectItem(string id)
        {
            var guidId = Guid.Parse(id);
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var expected = ProjectsDbSet.Get().First(x => x.Id == guidId);
            var projectRepository = new ProjectRepository(context);

            var actual = await projectRepository.GetByIdAsync(guidId);

            Assert.That(actual, Is.EqualTo(expected).Using(EqualityComparers.ProjectComparer));
        }

        [Test]
        public async Task ProjectRepository_DeleteAsync_DeletesEntity()
        {
            var guidId = Guid.Parse("9245950B-0F82-4B9E-9AF7-DEC9ACF171FA");
            var entityToDelete = ProjectsDbSet.Get().First(x => x.Id == guidId);
            var expectedCount = ProjectsDbSet.Get().ToList().Count - 1;
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var projectRepository = new ProjectRepository(context);

            await projectRepository.DeleteAsync(entityToDelete);

            context.Milestones.Should().HaveCount(expectedCount);
        }

        [Test]
        public async Task ProjectRepository_AddAsync_AddsValueToDatabase()
        {
            var expectedCount = ProjectsDbSet.Get().ToList().Count + 1;
            var entityToAdd = ProjectsDbSet.ProjectBuilder().Create();
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var projectRepository = new ProjectRepository(context);

            await projectRepository.AddAsync(entityToAdd);

            context.Projects.Should().HaveCount(expectedCount);
            var entityFound = await context.Projects.FindAsync(entityToAdd.Id);
            entityFound.Should().BeEquivalentTo(entityToAdd);
            // Assert.That(entityFound, Is.EqualTo(entityToAdd).Using(EqualityComparers.ProjectComparer));
        }

        [Test]
        public async Task ProjectRepository_UpdateAsync_UpdateEntity()
        {
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var projectRepository = new ProjectRepository(context);
            var entityToUpdate = ProjectsDbSet.Get().First();
            entityToUpdate.Description = "New description";

            var actual = await projectRepository.UpdateAsync(entityToUpdate);
            actual.Should().BeEquivalentTo(entityToUpdate);
            //Assert.That( actual, Is.EqualTo(entityToUpdate).Using(EqualityComparers.ProjectComparer));
        }

        [Test]
        public async Task ProjectRepository_GetAllAsync_ReturnsAllValues()
        {
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var projectRepository = new ProjectRepository(context);
            var expected = ProjectsDbSet.Get();

            var actual = await projectRepository.GetAllAsync();

            Assert.That(actual.OrderBy(x => x.Id), Is.EqualTo(expected.OrderBy(x => x.Id))
                .Using(EqualityComparers.ProjectComparer));
        }

        [Test]
        public async Task ProjectRepository_GetAllPagedAsync_ReturnsAllResultsPaged()
        {
            var page = 1;
            var size = 1;
            await using var context = new TimeTrackingDbContext(_dbOptions);
            var projectRepository = new ProjectRepository(context);
            var expected = ProjectsDbSet.Get().Take(size).ToList();

            var actual = await projectRepository.GetAllPagedAsync(page, size);

            actual.EnsurePagedResult(ProjectsDbSet.Get().ToList().Count, size, page);
            var actualItems = actual.Items.ToList();
            actualItems.Should().BeEquivalentTo(expected, options => options
                .Excluding(e => e.Milestones)
                .Excluding(e => e.Issues)
                .Excluding(e => e.Teams));

        }
    }
}