using Microsoft.EntityFrameworkCore;

namespace Reservo.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
          : base(options)
        { }

        // Stare tabele (kompatybilność)
        public DbSet<Login> Loginy { get; set; }
        public DbSet<Dane>  Dane   { get; set; }
        
        // Nowe tabele dla systemu rezerwacji
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder model)
        {
            // Stare tabele
            model.Entity<Login>().ToTable("loginy");
            model.Entity<Dane>().ToTable("dane");
            
            // Nowe tabele
            model.Entity<User>().ToTable("users");
            model.Entity<Category>().ToTable("categories");
            model.Entity<Resource>().ToTable("resources");
            model.Entity<Reservation>().ToTable("reservations");
            
            // Konfiguracja relacji
            model.Entity<Resource>()
                .HasOne(r => r.Category)
                .WithMany(c => c.Resources)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
                
            model.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            model.Entity<Reservation>()
                .HasOne(r => r.Resource)
                .WithMany(res => res.Reservations)
                .HasForeignKey(r => r.ResourceId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Indeksy
            model.Entity<User>().HasIndex(u => u.Username).IsUnique();
            model.Entity<User>().HasIndex(u => u.ApiKey).IsUnique();
            model.Entity<Resource>().HasIndex(r => r.Name);
            model.Entity<Reservation>().HasIndex(r => new { r.StartDate, r.EndDate });
        }
    }
}
