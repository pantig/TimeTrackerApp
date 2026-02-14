using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeTrackerApp.Models
{
    // Nowy model dla oznaczania całych dni
    public class DayMarker
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Employee")]
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public DayType Type { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("User")]
        public int CreatedBy { get; set; }
        public virtual User CreatedByUser { get; set; }
    }

    public enum DayType
    {
        BusinessTrip = 1,    // Delegacja - fioletowa
        DayOff = 2,          // Dzień wolny - szara
        Sick = 3,            // Choroba - żółta
        Vacation = 4         // Urlop - bladoróżowa
    }
}