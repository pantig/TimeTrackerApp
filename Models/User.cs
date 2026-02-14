using System.ComponentModel.DataAnnotations;

namespace TimeTrackerApp.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
        [MaxLength(255)]
        public string Email { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków")]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "Imię jest wymagane")]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Nazwisko jest wymagane")]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.Employee;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Nawigacja
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public virtual ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
    }

    public enum UserRole
    {
        Employee = 0,
        Manager = 1,
        Admin = 2
    }
}