using Microsoft.EntityFrameworkCore;
using TimeTrackerApp.Models;

namespace TimeTrackerApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<TimeEntry> TimeEntries { get; set; }
        public DbSet<DayMarker> DayMarkers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - Employee relationship (1:1)
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<Employee>(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Project - Manager relationship
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Manager)
                .WithMany()
                .HasForeignKey(p => p.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Client - Projects relationship (1:Many)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Client)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Project - Employees relationship (Many:Many)
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Employees)
                .WithMany(e => e.Projects);

            // TimeEntry - Employee relationship
            modelBuilder.Entity<TimeEntry>()
                .HasOne(te => te.Employee)
                .WithMany()
                .HasForeignKey(te => te.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // TimeEntry - Project relationship (optional)
            modelBuilder.Entity<TimeEntry>()
                .HasOne(te => te.Project)
                .WithMany(p => p.TimeEntries)
                .HasForeignKey(te => te.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            // TimeEntry - CreatedByUser relationship
            modelBuilder.Entity<TimeEntry>()
                .HasOne(te => te.CreatedByUser)
                .WithMany()
                .HasForeignKey(te => te.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // DayMarker - Employee relationship
            modelBuilder.Entity<DayMarker>()
                .HasOne(dm => dm.Employee)
                .WithMany()
                .HasForeignKey(dm => dm.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<TimeEntry>()
                .HasIndex(te => te.EntryDate);

            modelBuilder.Entity<TimeEntry>()
                .HasIndex(te => te.EmployeeId);

            modelBuilder.Entity<TimeEntry>()
                .HasIndex(te => te.ProjectId);

            modelBuilder.Entity<Client>()
                .HasIndex(c => c.Name);

            modelBuilder.Entity<Client>()
                .HasIndex(c => c.IsActive);
        }
    }
}
