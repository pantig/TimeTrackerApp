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
        public DbSet<TimeEntry> TimeEntries { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<DayMarker> DayMarkers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - Employee (1:1)
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.User)
                .WithMany(u => u.Employees)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.UserId)
                .IsUnique();

            // Employee - TimeEntry (1:many)
            modelBuilder.Entity<TimeEntry>()
                .HasOne(t => t.Employee)
                .WithMany(e => e.TimeEntries)
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Project - TimeEntry (1:many)
            modelBuilder.Entity<TimeEntry>()
                .HasOne(t => t.Project)
                .WithMany(p => p.TimeEntries)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            // Employee - Project (many:many)
            modelBuilder.Entity<Employee>()
                .HasMany(e => e.Projects)
                .WithMany(p => p.Employees)
                .UsingEntity("EmployeeProject");

            // Project - Manager (Employee) - relacja 1:many
            // Każdy projekt ma jednego opiekuna (Manager)
            // Każdy Employee może być opiekunem wielu projektów
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Manager)
                .WithMany()  // Employee nie ma kolekcji ManagedProjects
                .HasForeignKey(p => p.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);  // nie usuwamy projektów gdy usuwamy managera

            // User - TimeEntry (created by)
            modelBuilder.Entity<TimeEntry>()
                .HasOne(t => t.CreatedByUser)
                .WithMany(u => u.TimeEntries)
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee - DayMarker (1:many)
            modelBuilder.Entity<DayMarker>()
                .HasOne(d => d.Employee)
                .WithMany()
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - DayMarker (created by)
            modelBuilder.Entity<DayMarker>()
                .HasOne(d => d.CreatedByUser)
                .WithMany()
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Indeksy
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<DayMarker>()
                .HasIndex(d => new { d.EmployeeId, d.Date })
                .IsUnique();

            // Wartości domyślne
            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<TimeEntry>()
                .Property(t => t.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<DayMarker>()
                .Property(d => d.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
