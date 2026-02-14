using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeTrackerApp.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Numer pracownika jest wymagany")]
        [StringLength(10)]
        public string EmployeeNumber { get; set; }

        [Required(ErrorMessage = "Stanowisko jest wymagane")]
        [MaxLength(100)]
        public string Position { get; set; }

        [Required(ErrorMessage = "Dział jest wymagany")]
        [MaxLength(100)]
        public string Department { get; set; }

        [Required]
        public decimal HourlyRate { get; set; }

        [Required]
        [Range(1, 40, ErrorMessage = "Normalna liczba godzin musi być między 1 a 40")]
        public decimal StandardHoursPerDay { get; set; } = 8;

        public DateTime HireDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Klucz obcy
        [ForeignKey("User")]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        // Nawigacja
        public virtual ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}