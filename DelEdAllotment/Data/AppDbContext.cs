using Microsoft.EntityFrameworkCore;
using DelEdAllotment.Models;

namespace DelEdAllotment.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        public DbSet <Centres> Centre {  get; set; }
        public DbSet <Registrations> Registration { get; set; }
        public DbSet <Seat_Allotment> seat_allotments { get; set; }

        public DbSet <Room> Rooms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Centres>(entity =>
            {
                // Configure CityName
                entity.Property(e => e.CityName)
                      .HasMaxLength(200)
                      .UseCollation("utf8mb4_unicode_ci")
                      .HasCharSet("utf8mb4");

                // Configure CentreName
                entity.Property(e => e.CentreName)
                      .HasMaxLength(255)
                      .UseCollation("utf8mb4_unicode_ci")
                      .HasCharSet("utf8mb4");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
