using System.Collections.Generic;
using System.Linq;

namespace TimeTrackerApp.Models.ViewModels
{
    public class NoProjectEntriesViewModel
    {
        public List<TimeEntry> Entries { get; set; } = new List<TimeEntry>();
        public List<Project> AvailableProjects { get; set; } = new List<Project>();
        public List<Employee>? AllEmployees { get; set; }
        public int? SelectedEmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public bool IsManagerView { get; set; }

        public decimal TotalHours => Entries.Sum(e => e.TotalHours);
        public int TotalDays => Entries.Select(e => e.EntryDate.Date).Distinct().Count();
    }
}
