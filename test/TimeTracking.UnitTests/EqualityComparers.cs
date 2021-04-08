using System;
using System.Collections.Generic;
using TimeTracking.Entities;

namespace TimeTracking.UnitTests
{
    public static class EqualityComparers
    {
        public sealed class IssueEqualityComparer : IEqualityComparer<Issue>
        {
            public bool Equals(Issue x, Issue y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Id.Equals(y.Id) && x.Title == y.Title && x.Description == y.Description && x.Status == y.Status && x.CreatedAt.Equals(y.CreatedAt) && x.OpenedAt.Equals(y.OpenedAt) && x.UpdatedAt.Equals(y.UpdatedAt) && x.ClosedAt.Equals(y.ClosedAt) && Nullable.Equals(x.AssignedToUserId, y.AssignedToUserId) && x.ReportedByUserId.Equals(y.ReportedByUserId) && x.ProjectId.Equals(y.ProjectId) && Nullable.Equals(x.MilestoneId, y.MilestoneId);
            }

            public int GetHashCode(Issue obj)
            {
                var hashCode = new HashCode();
                hashCode.Add(obj.Id);
                hashCode.Add(obj.Title);
                hashCode.Add(obj.Description);
                hashCode.Add((int)obj.Status);
                hashCode.Add(obj.CreatedAt);
                hashCode.Add(obj.OpenedAt);
                hashCode.Add(obj.UpdatedAt);
                hashCode.Add(obj.ClosedAt);
                hashCode.Add(obj.AssignedToUserId);
                hashCode.Add(obj.ReportedByUserId);
                hashCode.Add(obj.ProjectId);
                hashCode.Add(obj.MilestoneId);
                return hashCode.ToHashCode();
            }
        }

        public static IEqualityComparer<Issue> IssueComparer { get; } = new IssueEqualityComparer();


        private sealed class MilestoneEqualityComparer : IEqualityComparer<Milestone>
        {
            public bool Equals(Milestone x, Milestone y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Id.Equals(y.Id) && x.State == y.State && x.Title == y.Title && x.Description == y.Description && x.OpenedAt.Equals(y.OpenedAt) && x.CreatedAt.Equals(y.CreatedAt) && x.UpdatedAt.Equals(y.UpdatedAt) && x.DueDate.Equals(y.DueDate) && x.CreatedByUserId.Equals(y.CreatedByUserId) && x.ProjectId.Equals(y.ProjectId);
            }

            public int GetHashCode(Milestone obj)
            {
                var hashCode = new HashCode();
                hashCode.Add(obj.Id);
                hashCode.Add((int)obj.State);
                hashCode.Add(obj.Title);
                hashCode.Add(obj.Description);
                hashCode.Add(obj.OpenedAt);
                hashCode.Add(obj.CreatedAt);
                hashCode.Add(obj.UpdatedAt);
                hashCode.Add(obj.DueDate);
                hashCode.Add(obj.CreatedByUserId);
                hashCode.Add(obj.ProjectId);
                return hashCode.ToHashCode();
            }
        }

        public static IEqualityComparer<Milestone> MilestoneComparer { get; } = new MilestoneEqualityComparer();

        private sealed class ProjectEqualityComparer : IEqualityComparer<Project>
        {
            public bool Equals(Project x, Project y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Id.Equals(y.Id) && x.Name == y.Name && x.Abbreviation == y.Abbreviation && x.Description == y.Description && x.InitialRisk.Equals(y.InitialRisk);
            }

            public int GetHashCode(Project obj)
            {
                return HashCode.Combine(obj.Id, obj.Name, obj.Abbreviation, obj.Description, obj.InitialRisk);
            }
        }

        public static IEqualityComparer<Project> ProjectComparer { get; } = new ProjectEqualityComparer();


        private sealed class WorkLogEqualityComparer : IEqualityComparer<WorkLog>
        {
            public bool Equals(WorkLog x, WorkLog y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;

                return x.Id.Equals(y.Id) && x.Description == y.Description && x.TimeSpent.Equals(y.TimeSpent) && x.IsApproved == y.IsApproved && x.StartDate.Equals(y.StartDate) && x.ActivityType == y.ActivityType && x.CreatedAt.Equals(y.CreatedAt) && x.UpdatedAt.Equals(y.UpdatedAt) && x.UserId.Equals(y.UserId) && x.IssueId.Equals(y.IssueId);
            }

            public int GetHashCode(WorkLog obj)
            {
                var hashCode = new HashCode();
                hashCode.Add(obj.Id);
                hashCode.Add(obj.Description);
                hashCode.Add(obj.TimeSpent);
                hashCode.Add(obj.IsApproved);
                hashCode.Add(obj.StartDate);
                hashCode.Add((int)obj.ActivityType);
                hashCode.Add(obj.CreatedAt);
                hashCode.Add(obj.UpdatedAt);
                hashCode.Add(obj.UserId);
                hashCode.Add(obj.IssueId);
                return hashCode.ToHashCode();
            }
        }

        public static IEqualityComparer<WorkLog> WorkLogComparer { get; } = new WorkLogEqualityComparer();

        private sealed class TimeTrackingUserEqualityComparer : IEqualityComparer<TimeTrackingUser>
        {
            public bool Equals(TimeTrackingUser x, TimeTrackingUser y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Id.Equals(y.Id) && x.Email == y.Email && x.FirstName == y.FirstName && x.LastName == y.LastName && Nullable.Equals(x.TeamId, y.TeamId);
            }

            public int GetHashCode(TimeTrackingUser obj)
            {
                return HashCode.Combine(obj.Id, obj.Email, obj.FirstName, obj.LastName, obj.TeamId);
            }
        }

        public static IEqualityComparer<TimeTrackingUser> TimeTrackingUserComparer { get; } = new TimeTrackingUserEqualityComparer();


        private sealed class TeamEqualityComparer : IEqualityComparer<Team>
        {
            public bool Equals(Team x, Team y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Id.Equals(y.Id) && x.Name == y.Name && x.MembersCount == y.MembersCount && x.ProjectId.Equals(y.ProjectId);
            }

            public int GetHashCode(Team obj)
            {
                return HashCode.Combine(obj.Id, obj.Name, obj.MembersCount, obj.ProjectId);
            }
        }

        public static IEqualityComparer<Team> TeamComparer { get; } = new TeamEqualityComparer();

    }
}