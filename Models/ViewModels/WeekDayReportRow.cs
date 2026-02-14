using System.ComponentModel.DataAnnotations;

namespace TimeTrackerApp.Models.ViewModels
{
    public class WeekDayReportRow
    {
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        public decimal Hours { get; set; }

        public int? ProjectId { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsApproved { get; set; }
    }
}
