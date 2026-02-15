using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeTrackerApp.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Stanowisko jest wymagane")]
        [MaxLength(200)]
        public string Position { get; set; } = null!;

        [Required(ErrorMessage = "Departament jest wymagany")]
        [MaxLength(200)]
        public string Department { get; set; } = null!;

        public DateTime HireDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Klucz obcy
        [ForeignKey("User")]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        // Nawigacja
        public virtual ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}