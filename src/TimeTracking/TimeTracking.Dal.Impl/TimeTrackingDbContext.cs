using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeTracking.Common.Abstract.Repository;
using TimeTracking.Entities;

namespace TimeTracking.Dal.Impl
{
    public class TimeTrackingDbContext : DbContext, IDbContext
    {
        public TimeTrackingDbContext()
        {

        }
        public TimeTrackingDbContext(DbContextOptions<TimeTrackingDbContext> options)
        : base(options)
        {

        }
        public virtual DbSet<Issue> Issues { get; set; }
        public virtual DbSet<Milestone> Milestones { get; set; }
        public virtual DbSet<Project> Projects { get; set; }
        public virtual DbSet<Team> Teams { get; set; }
        public virtual DbSet<WorkLog> WorkLogs { get; set; }
        public virtual DbSet<TimeTrackingUser> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);





            modelBuilder.Entity<Issue>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<Issue>()
                .HasOne(e => e.TimeTrackingUserAssigned)
                .WithMany(e => e.AssignedIssues)
                .HasForeignKey(e => e.AssignedToUserId);
            modelBuilder.Entity<Issue>()
                .HasOne(e => e.TimeTrackingUserReporter)
                .WithMany(e => e.ReportedIssues)
                .HasForeignKey(e => e.ReportedByUserId);
            modelBuilder.Entity<Issue>()
                .HasMany(e => e.WorkLogs)
                .WithOne(w => w.Issue)
                .HasForeignKey(e => e.IssueId);
            modelBuilder.Entity<Issue>()
                .Property(e => e.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Team>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<Team>()
                .HasMany(e => e.Users)
                .WithOne(e => e.Team)
                .HasForeignKey(e => e.TeamId);

            modelBuilder.Entity<Project>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<Project>()
                .HasIndex(e => e.Name)
                .IsUnique(true);
            modelBuilder.Entity<Project>()
                .HasMany(e => e.Issues)
                .WithOne(e => e.Project)
                .HasForeignKey(e => e.ProjectId);
            modelBuilder.Entity<Project>()
                .HasMany(e => e.Teams)
                .WithOne(e => e.Project)
                .HasForeignKey(e => e.ProjectId);
            modelBuilder.Entity<Project>()
                .HasMany(e => e.Milestones)
                .WithOne(e => e.Project)
                .HasForeignKey(e => e.ProjectId);

            modelBuilder.Entity<Milestone>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<Milestone>()
                .HasMany(e => e.Issues)
                .WithOne(e => e.Milestone)
                .HasForeignKey(e => e.MilestoneId);
            modelBuilder.Entity<Milestone>()
                .Property(e => e.State)
                .HasConversion<string>();

            modelBuilder.Entity<WorkLog>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<WorkLog>()
                .HasIndex(e => e.UserId);
            modelBuilder.Entity<WorkLog>()
                .Property(e => e.ActivityType)
                .HasConversion<string>();

            modelBuilder.Entity<TimeTrackingUser>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<TimeTrackingUser>()
                .HasMany(e => e.WorkLogs)
                .WithOne(e => e.TimeTrackingUser)
                .HasForeignKey(e => e.UserId);
            modelBuilder.Entity<TimeTrackingUser>()
                .HasMany(e => e.CreatedMilestones)
                .WithOne(e => e.CreatedByUser)
                .HasForeignKey(e => e.CreatedByUserId);

        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            UpdateAuditableEntities();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateAuditableEntities();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
        {
            UpdateAuditableEntities();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void UpdateAuditableEntities()
        {
            var added = ChangeTracker.Entries<IAuditable>().Where(k => k.State == EntityState.Added).ToList();

            added.ForEach(e =>
            {
                e.Property(x => x.CreatedAt).CurrentValue = DateTimeOffset.UtcNow;
                e.Property(x => x.CreatedAt).IsModified = true;
            });

            var modified = ChangeTracker.Entries<IAuditable>().Where(entry => entry.State == EntityState.Modified).ToList();

            modified.ForEach(entry =>
            {
                entry.Property(x => x.UpdatedAt).CurrentValue = DateTimeOffset.UtcNow;
                entry.Property(x => x.UpdatedAt).IsModified = true;

                entry.Property(x => x.CreatedAt).CurrentValue = entry.Property(x => x.CreatedAt).OriginalValue;
                entry.Property(x => x.CreatedAt).IsModified = false;
            });

        }
    }
}