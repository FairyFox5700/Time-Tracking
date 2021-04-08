using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeTracking.Entities
{
    public class Milestone : IKeyEntity<Guid>, IAuditable
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public State State { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTimeOffset OpenedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset DueDate { get; set; }
        public ICollection<Issue> Issues { get; set; }

        #region references
        public Guid CreatedByUserId { get; set; }
        [ForeignKey(nameof(CreatedByUserId))]
        public TimeTrackingUser CreatedByUser { get; set; }
        public Guid ProjectId { get; set; }
        [ForeignKey(nameof(ProjectId))]
        public Project Project { get; set; }
        #endregion
    }

    public enum State
    {
        Opened,
        Closed,
    }
}