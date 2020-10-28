using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class SeamCarverContext : DbContext
    {
        public DbSet<ScUserIdentity> Users { get; set; }

        public SeamCarverContext(DbContextOptions<SeamCarverContext> options) : base(options) { }
    }

    [Table("Identity")]
    public class ScUserIdentity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string LocalFolder { get; set; }
    }
}
