using BellwoodGlobal.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BellwoodGlobal.API.Data
{
    public class BellwoodGlobalDbContext : DbContext
    {
        public BellwoodGlobalDbContext(DbContextOptions<BellwoodGlobalDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Quote> Quotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Quote>()
                .Property(q => q.EstimatedCost)
                .HasPrecision(18, 2);
        }
    }
}
