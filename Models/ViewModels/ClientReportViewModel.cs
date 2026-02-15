namespace TimeTrackerApp.Models.ViewModels
{
    public class ClientReportViewModel
    {
        public Client Client { get; set; } = null!;
        public List<ProjectStatistics> ProjectStatistics { get; set; } = new();
        public ClientSummary Summary { get; set; } = new();
    }

    public class ProjectStatistics
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectStatus { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public int TeamSize { get; set; }
        public decimal TotalHours { get; set; }
        public decimal? HoursBudget { get; set; }
        public decimal? BudgetUsagePercentage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ClientSummary
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public decimal TotalHoursAllProjects { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal TotalBudgetUsed { get; set; }
        public decimal AverageBudgetUsagePercentage { get; set; }
    }
}
