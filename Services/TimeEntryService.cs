using Microsoft.EntityFrameworkCore;
using TimeTrackerApp.Data;
using TimeTrackerApp.Models;

namespace TimeTrackerApp.Services
{
    public class TimeEntryService : ITimeEntryService
    {
        private readonly ApplicationDbContext _context;

        public TimeEntryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TimeEntry>> GetTimeEntriesForEmployeeAsync(int employeeId, DateTime from, DateTime to)
        {
            return await _context.TimeEntries
                .Where(t => t.EmployeeId == employeeId && t.EntryDate >= from && t.EntryDate <= to)
                .Include(t => t.Employee)
                .Include(t => t.Project)
                .OrderByDescending(t => t.EntryDate)
                .ToListAsync();
        }

        public async Task<List<TimeEntry>> GetTimeEntriesForProjectAsync(int projectId)
        {
            return await _context.TimeEntries
                .Where(t => t.ProjectId == projectId)
                .Include(t => t.Employee)
                .OrderByDescending(t => t.EntryDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalHoursAsync(int employeeId, DateTime from, DateTime to)
        {
            return await _context.TimeEntries
                .Where(t => t.EmployeeId == employeeId && t.EntryDate >= from && t.EntryDate <= to)
                .SumAsync(t => (decimal)(t.EndTime - t.StartTime).TotalHours);
        }

        public async Task<decimal> GetTotalEarningsAsync(int employeeId, DateTime from, DateTime to)
        {
            return await _context.TimeEntries
                .Where(t => t.EmployeeId == employeeId && t.EntryDate >= from && t.EntryDate <= to)
                .Include(t => t.Employee)
                .SumAsync(t => t.Employee.HourlyRate * (decimal)(t.EndTime - t.StartTime).TotalHours);
        }

        public async Task<List<TimeEntry>> GetUnapprovedEntriesAsync()
        {
            return await _context.TimeEntries
                .Where(t => !t.IsApproved)
                .Include(t => t.Employee)
                .Include(t => t.Project)
                .OrderBy(t => t.EntryDate)
                .ToListAsync();
        }

        public async Task ApproveTimeEntryAsync(int entryId)
        {
            var entry = await _context.TimeEntries.FindAsync(entryId);
            if (entry != null)
            {
                entry.IsApproved = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
