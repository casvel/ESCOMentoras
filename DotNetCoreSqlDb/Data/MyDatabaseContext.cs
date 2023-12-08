using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DotNetCoreSqlDb.Models;

namespace DotNetCoreSqlDb.Data
{
    public class MyDatabaseContext : DbContext
    {
        public MyDatabaseContext (DbContextOptions<MyDatabaseContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Profile>()
                .Property(e => e.State)
                .HasConversion(
                    v => v.ToString(),
                    v => (ProfileState)Enum.Parse(typeof(ProfileState), v));
        }

        public DbSet<Todo> Todo { get; set; } = default!;
        public DbSet<Profile> Profile { get; set; } = default!;
    }
}
