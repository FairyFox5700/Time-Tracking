using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeTracking.Entities
{
    public class TimeTrackingUser : IKeyEntity<Guid>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }

        #region references
        public Guid? TeamId { get; set; }
        [ForeignKey(nameof(TeamId))]
        public Team Team { get; set; }

        public ICollection<WorkLog> WorkLogs { get; set; }
        public ICollection<Issue> ReportedIssues { get; set; }
        public ICollection<Issue> AssignedIssues { get; set; }
        public ICollection<Milestone> CreatedMilestones { get; set; }
        #endregion
    }
}