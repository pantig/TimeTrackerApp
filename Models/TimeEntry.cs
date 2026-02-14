using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeTrackerApp.Models
{
    public class TimeEntry : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Employee")]
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        [ForeignKey("Project")]
        public int? ProjectId { get; set; }
        public virtual Project Project { get; set; }

        [Required(ErrorMessage = "Data wpisu jest wymagana")]
        public DateTime EntryDate { get; set; }

        [Required(ErrorMessage = "Godzina rozpoczęcia jest wymagana")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Godzina zakończenia jest wymagana")]
        public TimeSpan EndTime { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("User")]
        public int CreatedBy { get; set; }
        public virtual User CreatedByUser { get; set; }

        // Właściwość obliczona
        [NotMapped]
        public decimal TotalHours
        {
            get
            {
                return (decimal)(EndTime - StartTime).TotalHours;
            }
        }

        // Walidacja
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndTime <= StartTime)
            {
                yield return new ValidationResult(
                    "Godzina zakończenia musi być później niż godzina rozpoczęcia",
                    new[] { nameof(EndTime) });
            }

            if (TotalHours > 24)
            {
                yield return new ValidationResult(
                    "Maksymalna liczba godzin na dzień to 24",
                    new[] { nameof(EndTime) });
            }

            if (EntryDate.Date > DateTime.UtcNow.Date)
            {
                yield return new ValidationResult(
                    "Nie można tworzyć wpisów na przyszłość",
                    new[] { nameof(EntryDate) });
            }

            if (EntryDate.Date < DateTime.UtcNow.AddDays(-90).Date)
            {
                yield return new ValidationResult(
                    "Nie można dodawać wpisów starszych niż 90 dni",
                    new[] { nameof(EntryDate) });
            }
        }
    }
}
