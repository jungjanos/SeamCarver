using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class SeamCarverContext : DbContext
    {
        public DbSet<ScUser> Users { get; set; }

        public DbSet<UserActionHistory> UserActions { get; set; }

        public DbSet<UserActionDetails> UserActionDetails{ get; set; }

        public SeamCarverContext(DbContextOptions<SeamCarverContext> options) : base(options) { }
    }

    [Table("Users")]
    public class ScUser
    {
        public Guid Id { get; set; }
        public string IdentityProvider { get; set; }
        public string TenantId { get; set; }
        public string LocalFolder { get; set; }
        public DateTime WhenCreated { get; set; }
        public DateTime WhenChanged { get; set; }
    }

    [Table("UserActionHistory")]
    public class UserActionHistory
    {
        public int ActionId { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid UserId { get; set; }
    }

    [Table("UserActionDetails")]
    public class UserActionDetails
    {
        public int ActionId { get; set; }
        public string Descr { get; set; }
        public string Param0 { get; set; }
        public string Param1 { get; set; }
        public string Param2 { get; set; }
        public string Param3 { get; set; }
        public string Param4 { get; set; }
    }
}
