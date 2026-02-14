using System.ComponentModel.DataAnnotations;

namespace TimeTrackerApp.Models.ViewModels
{
    public class WeeklyTimeGridViewModel
    {
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd => WeekStart.AddDays(6);

        public string EmployeeName { get; set; } = string.Empty;
        public int EmployeeId { get; set; }

        public List<Project> Projects { get; set; } = new();

        // All entries for the week, grouped by day and time
        public Dictionary<DateTime, List<TimeGridEntry>> EntriesByDay { get; set; } = new();

        public DateTime PrevWeek => WeekStart.AddDays(-7);
        public DateTime NextWeek => WeekStart.AddDays(7);

        public List<DateTime> DaysOfWeek => Enumerable.Range(0, 7).Select(i => WeekStart.AddDays(i)).ToList();
    }
}
