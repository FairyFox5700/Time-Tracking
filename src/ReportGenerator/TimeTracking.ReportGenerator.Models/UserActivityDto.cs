﻿using System;
using System.Collections.Generic;
using TimeTracking.Common.Enums;

namespace TimeTracking.ReportGenerator.Models
{
    public class UserActivityDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string UserSurname { get; set; }
        public string ProjectName { get; set; }
        public string UserEmail { get; set; }
        public long TotalWorkLogInSeconds { get; set; }
        public List<WorkLogDetailsDto> WorkLogItems { get; set; }
    }
    public class WorkLogDetailsDto
    {
        public string Description { get; set; }
        public long TimeSpent { get; set; }
        public ActivityType ActivityType { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public Guid IssueId { get; set; }
        public Guid WorkLogId { get; set; }
        public Guid UserId { get; set; }
        public string ProjectName { get; set; }
    }

}