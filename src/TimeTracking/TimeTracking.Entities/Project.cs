using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeTracking.Entities
{
    public class Project : IKeyEntity<Guid>
    {


        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public string Description { get; set; }
        public DateTimeOffset InitialRisk { get; set; }
        public ICollection<Milestone> Milestones { get; set; }
        public ICollection<Issue> Issues { get; set; }
        public ICollection<Team> Teams { get; set; }
    }
}