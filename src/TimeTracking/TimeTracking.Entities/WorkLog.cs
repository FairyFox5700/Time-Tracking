using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TimeTracking.Common.Enums;

namespace TimeTracking.Entities
{
    public class WorkLog : IKeyEntity<Guid>, IAuditable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Description { get; set; }
        public TimeSpan TimeSpent { get; set; }
        public bool IsApproved { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public ActivityType ActivityType { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        #region references
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public TimeTrackingUser TimeTrackingUser { get; set; }
        public Guid IssueId { get; set; }
        [ForeignKey(nameof(IssueId))]
        public Issue Issue { get; set; }
        #endregion
    }
}