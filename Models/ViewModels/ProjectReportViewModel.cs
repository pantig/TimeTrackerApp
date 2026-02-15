namespace TimeTrackerApp.Models.ViewModels
{
    public class ProjectReportViewModel
    {
        public Project Project { get; set; } = null!;
        public List<EmployeeTimeEntry> EmployeeTimeEntries { get; set; } = new();
        public ProjectSummary Summary { get; set; } = null!;
    }

    public class EmployeeTimeEntry
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public int EntriesCount { get; set; }
        public DateTime FirstEntry { get; set; }
        public DateTime? LastEntry { get; set; }
    }

    public class ProjectSummary
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public decimal TotalHoursLogged { get; set; }
        public decimal? HoursBudget { get; set; }
        public decimal? BudgetUsagePercentage { get; set; }
        public int TotalEntries { get; set; }
        public DateTime? ProjectStartDate { get; set; }
        public DateTime? ProjectEndDate { get; set; }
        public int DaysActive { get; set; }
    }
}
