using System.ComponentModel.DataAnnotations;

namespace TimeTrackerApp.Models.ViewModels
{
    public class WeekReportViewModel
    {
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd => WeekStart.AddDays(6);

        public string EmployeeName { get; set; } = string.Empty;
        public int EmployeeId { get; set; }

        public List<Project>? Projects { get; set; }

        [Required]
        public List<WeekDayReportRow> Days { get; set; } = new();

        public decimal TotalHours => Days.Sum(d => d.Hours);

        public DateTime PrevWeek => WeekStart.AddDays(-7);
        public DateTime NextWeek => WeekStart.AddDays(7);
    }
}
