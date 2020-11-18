using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class SeamCarverContext : DbContext
    {
        public DbSet<ScUser> Users { get; set; }

        public DbSet<UserAction> UserActions { get; set; }

        public SeamCarverContext(DbContextOptions<SeamCarverContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb
                .Entity<UserAction>()
                .Property(e => e.ActionType)
                .HasConversion(
                    v => v.ToString(),
                    v => (ActionType)Enum.Parse(typeof(ActionType), v)
                );
        }
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

    public enum ActionType
    {
        AccountCreation,
        AccountDeletion,
        ImageUpload,
        ImageCarving,
    }

    [Table("UserActions")]
    public class UserAction
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public ActionType ActionType { get; set; }
        public string Descr { get; set; }
        public DateTime Timestamp { get; set; }
        public string Param0 { get; set; }
        public string Param1 { get; set; }
        public string Param2 { get; set; }
        public string Param3 { get; set; }
        public string Param4 { get; set; }
    }
}
