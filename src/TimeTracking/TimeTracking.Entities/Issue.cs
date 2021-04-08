using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeTracking.Entities
{
    public class Issue : IKeyEntity<Guid>, IAuditable
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Status Status { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset OpenedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset ClosedAt { get; set; }
        public ICollection<WorkLog> WorkLogs { get; set; }

        #region references
        public Guid? AssignedToUserId { get; set; }
        [ForeignKey(nameof(AssignedToUserId))]
        public TimeTrackingUser TimeTrackingUserAssigned { get; set; }
        public Guid ReportedByUserId { get; set; }
        [ForeignKey(nameof(ReportedByUserId))]
        public TimeTrackingUser TimeTrackingUserReporter { get; set; }
        public Guid ProjectId { get; set; }
        [ForeignKey(nameof(ProjectId))]
        public Project Project { get; set; }
        public Guid? MilestoneId { get; set; }
        [ForeignKey(nameof(MilestoneId))]
        public Milestone Milestone { get; set; }
        #endregion

    }

    public enum Status
    {
        Open,
        Closed,
        Review,
    }
}