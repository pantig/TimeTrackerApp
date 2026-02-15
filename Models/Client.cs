using System.ComponentModel.DataAnnotations;

namespace TimeTrackerApp.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa klienta jest wymagana")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu email")]
        public string? Email { get; set; }

        [MaxLength(20)]
        [Phone(ErrorMessage = "Nieprawidłowy format numeru telefonu")]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(50)]
        public string? NIP { get; set; } // Tax ID

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Nawigacja
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
