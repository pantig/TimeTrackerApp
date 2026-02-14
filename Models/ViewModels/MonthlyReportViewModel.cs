namespace TimeTrackerApp.Models.ViewModels
{
    public class MonthlyReportViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<DailyHoursReport> EntriesByDay { get; set; } = new();
        public List<ProjectHoursReport> EntriesByProject { get; set; } = new();
        public decimal TotalHours { get; set; }
        public int TotalDays { get; set; }
        public List<Employee>? AllEmployees { get; set; }
        public bool CanSelectEmployee { get; set; }

        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("pl-PL"));
    }

    public class DailyHoursReport
    {
        public DateTime Date { get; set; }
        public decimal TotalHours { get; set; }
        public List<TimeEntry> Entries { get; set; } = new();
    }

    public class ProjectHoursReport
    {
        public string ProjectName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public int EntryCount { get; set; }
    }
}