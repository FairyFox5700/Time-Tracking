using System;
using System.Collections.Generic;

namespace TimeTracking.Models
{
    public class UserActivityDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string UserSurname { get; set; }
        public string ProjectName { get; set; }
        public string UserEmail { get; set; }
        public long? TotalWorkLogInSeconds { get; set; }
        public List<WorkLogDetailsDto> WorkLogItems { get; set; }
    }


}