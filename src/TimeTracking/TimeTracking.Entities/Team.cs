using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeTracking.Entities
{
    public class Team : IKeyEntity<Guid>
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int MembersCount { get; set; }
        #region references
        public Guid ProjectId { get; set; }
        [ForeignKey(nameof(ProjectId))]
        public Project Project { get; set; }

        public ICollection<TimeTrackingUser> Users { get; set; }
        #endregion
    }
}