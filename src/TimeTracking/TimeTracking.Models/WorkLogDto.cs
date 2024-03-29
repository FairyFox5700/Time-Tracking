﻿using System;
using TimeTracking.Common.Enums;

namespace TimeTracking.Models
{
    public class WorkLogDto
    {
        public string Description { get; set; }
        public long TimeSpent { get; set; }
        public ActivityType ActivityType { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public Guid IssueId { get; set; }
    }
    public class WorkLogDetailsDto : WorkLogDto
    {
        public Guid WorkLogId { get; set; }
        public Guid UserId { get; set; }
        public bool IsApproved { get; set; }
    }
}