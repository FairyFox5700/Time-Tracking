using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeTracking.Common.Abstract.Repository;
using TimeTracking.Identity.Entities;

namespace TimeTracking.Identity.Dal.Impl
{
    public class TimeTrackingIdentityDbContext : IdentityDbContext<User, Role, Guid>, IDbContext
    {
        public TimeTrackingIdentityDbContext(DbContextOptions<TimeTrackingIdentityDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}