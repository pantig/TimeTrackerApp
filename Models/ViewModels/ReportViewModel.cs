namespace TimeTrackerApp.Models.ViewModels
{
    public class ReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? EmployeeId { get; set; }
        public int? ProjectId { get; set; }

        public List<TimeEntry> TimeEntries { get; set; } = new();
        public List<Employee> Employees { get; set; } = new();
        public List<Project> Projects { get; set; } = new();

        public decimal TotalHours => TimeEntries?.Sum(t => t.TotalHours) ?? 0;
        public decimal TotalEarnings => TimeEntries?.Sum(t => t.Employee?.HourlyRate * t.TotalHours) ?? 0;
    }
}