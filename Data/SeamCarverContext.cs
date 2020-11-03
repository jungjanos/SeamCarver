using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class SeamCarverContext : DbContext
    {
        public DbSet<ScUser> Users { get; set; }

        public SeamCarverContext(DbContextOptions<SeamCarverContext> options) : base(options) { }
    }

    [Table("Users")]
    public class ScUser
    {
        public Guid Id { get; set; }        
        public string IdentityProvider { get; set; }
        public string PrimaryDomain { get; set; }
        public string LocalFolder { get; set; }
        public DateTime WhenCreated { get; set; }
        public DateTime WhenChanged { get; set; }
    }
}
