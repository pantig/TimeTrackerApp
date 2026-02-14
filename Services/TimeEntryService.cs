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
                    .ThenInclude(e => e.User)
                .Include(t => t.Project)
                .OrderByDescending(t => t.EntryDate)
                .ToListAsync();
        }

        public async Task<List<TimeEntry>> GetTimeEntriesForProjectAsync(int projectId)
        {
            return await _context.TimeEntries
                .Where(t => t.ProjectId == projectId)
                .Include(t => t.Employee)
                    .ThenInclude(e => e.User)
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
            // Feature removed - no salary calculations
            return 0;
        }

        public async Task<List<TimeEntry>> GetUnapprovedEntriesAsync()
        {
            // Feature removed - no approval workflow
            return new List<TimeEntry>();
        }

        public async Task ApproveTimeEntryAsync(int entryId)
        {
            // Feature removed - no approval workflow
            await Task.CompletedTask;
        }

        public async Task UpsertDailyHoursAsync(int employeeId, DateTime date, decimal hours, int? projectId, string? description)
        {
            // If hours == 0, remove any existing entries for that day
            var dayStart = date.Date;
            var dayEnd = date.Date.AddDays(1).AddTicks(-1);

            var existing = await _context.TimeEntries
                .Where(t => t.EmployeeId == employeeId && t.EntryDate >= dayStart && t.EntryDate <= dayEnd)
                .OrderBy(t => t.Id)
                .ToListAsync();

            if (hours <= 0)
            {
                if (existing.Count > 0)
                {
                    _context.TimeEntries.RemoveRange(existing);
                    await _context.SaveChangesAsync();
                }
                return;
            }

            // Represent daily hours as a single entry: 09:00 -> 09:00 + hours
            var start = new TimeSpan(9, 0, 0);
            var end = start.Add(TimeSpan.FromHours((double)hours));

            var entry = existing.FirstOrDefault();
            if (entry == null)
            {
                entry = new TimeEntry
                {
                    EmployeeId = employeeId,
                    EntryDate = date.Date,
                    StartTime = start,
                    EndTime = end,
                    ProjectId = projectId,
                    Description = description,
                    CreatedBy = employeeId
                };
                _context.TimeEntries.Add(entry);
            }
            else
            {
                entry.EntryDate = date.Date;
                entry.StartTime = start;
                entry.EndTime = end;
                entry.ProjectId = projectId;
                entry.Description = description;
            }

            // Remove duplicates for the day (if any)
            if (existing.Count > 1)
            {
                _context.TimeEntries.RemoveRange(existing.Skip(1));
            }

            await _context.SaveChangesAsync();
        }
    }
}
