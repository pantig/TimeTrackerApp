using System.ComponentModel.DataAnnotations;

namespace TimeTrackerApp.Models.ViewModels
{
    public class TimeEntryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Pracownik jest wymagany")]
        public int EmployeeId { get; set; }

        public int? ProjectId { get; set; }

        [Required(ErrorMessage = "Data jest wymagana")]
        [DataType(DataType.Date)]
        public DateTime EntryDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Godzina rozpoczęcia jest wymagana")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Godzina zakończenia jest wymagana")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public List<Employee> Employees { get; set; }
        public List<Project> Projects { get; set; }

        public decimal TotalHours => (decimal)(EndTime - StartTime).TotalHours;
    }
}