namespace TimeTrackerApp.Models.ViewModels
{
    public class OrganizationSummaryViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalHours { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalProjects { get; set; }
        public List<EmployeeHoursSummary> EmployeeHours { get; set; } = new();
        public List<ProjectBudgetSummary> ProjectHours { get; set; } = new();

        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("pl-PL"));
    }

    public class EmployeeHoursSummary
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public int EntryCount { get; set; }
    }

    public class ProjectBudgetSummary
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public decimal? HoursBudget { get; set; }
        public bool IsOverBudget { get; set; }
        public int EntryCount { get; set; }

        public decimal? BudgetUsagePercent
        {
            get
            {
                if (!HoursBudget.HasValue || HoursBudget.Value == 0) return null;
                return (TotalHours / HoursBudget.Value) * 100;
            }
        }
    }
}