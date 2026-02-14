using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeTrackerApp.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa projektu jest wymagana")]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public ProjectStatus Status { get; set; } = ProjectStatus.Active;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Budżet godzinowy projektu
        public decimal? HoursBudget { get; set; }

        // Nawigacja
        public virtual ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }

    public enum ProjectStatus
    {
        Planning = 0,
        Active = 1,
        OnHold = 2,
        Completed = 3
    }
}