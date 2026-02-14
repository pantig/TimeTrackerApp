using TimeTrackerApp.Models;

namespace TimeTrackerApp.Services
{
    public interface ITimeEntryService
    {
        Task<List<TimeEntry>> GetTimeEntriesForEmployeeAsync(int employeeId, DateTime from, DateTime to);
        Task<List<TimeEntry>> GetTimeEntriesForProjectAsync(int projectId);
        Task<decimal> GetTotalHoursAsync(int employeeId, DateTime from, DateTime to);
        Task<decimal> GetTotalEarningsAsync(int employeeId, DateTime from, DateTime to);
        Task<List<TimeEntry>> GetUnapprovedEntriesAsync();
        Task ApproveTimeEntryAsync(int entryId);
    }
}