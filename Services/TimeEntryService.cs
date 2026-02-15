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

        public async Task<bool> HasOverlapAsync(int employeeId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeEntryId = null)
        {
            var existingEntries = await _context.TimeEntries
                .Where(e => e.EmployeeId == employeeId && e.EntryDate.Date == date.Date)
                .ToListAsync();

            if (excludeEntryId.HasValue)
            {
                existingEntries = existingEntries.Where(e => e.Id != excludeEntryId.Value).ToList();
            }

            foreach (var entry in existingEntries)
            {
                // Sprawdzamy czy nowy wpis nachodzi na istniejący
                if ((startTime >= entry.StartTime && startTime < entry.EndTime) ||
                    (endTime > entry.StartTime && endTime <= entry.EndTime) ||
                    (startTime <= entry.StartTime && endTime >= entry.EndTime))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<decimal> GetTotalHoursForDayAsync(int employeeId, DateTime date)
        {
            var entries = await _context.TimeEntries
                .Where(e => e.EmployeeId == employeeId && e.EntryDate.Date == date.Date)
                .ToListAsync();

            return entries.Sum(e => e.TotalHours);
        }

        public Task<bool> CanDeleteAsync(int entryId, int currentUserId)
        {
            // Synchronous operation - just return completed task
            var entry = _context.TimeEntries.Find(entryId);
            var result = entry != null && entry.CreatedBy == currentUserId;
            return Task.FromResult(result);
        }

        public Task<bool> CanEditAsync(int entryId, int currentUserId)
        {
            // Synchronous operation - just return completed task
            var entry = _context.TimeEntries.Find(entryId);
            var result = entry != null && entry.CreatedBy == currentUserId;
            return Task.FromResult(result);
        }

        public async Task<List<TimeEntry>> GetEntriesForEmployeeAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            return await _context.TimeEntries
                .Include(e => e.Project)
                .Include(e => e.Employee)
                    .ThenInclude(e => e.User)
                .Where(e => e.EmployeeId == employeeId && e.EntryDate >= startDate && e.EntryDate <= endDate)
                .ToListAsync();
        }

        public async Task<List<TimeEntry>> GetEntriesForProjectAsync(int projectId, DateTime startDate, DateTime endDate)
        {
            return await _context.TimeEntries
                .Include(e => e.Employee)
                    .ThenInclude(e => e.User)
                .Where(e => e.ProjectId == projectId && e.EntryDate >= startDate && e.EntryDate <= endDate)
                .ToListAsync();
        }

        public async Task<bool> ValidateTimeEntryAsync(TimeEntry entry)
        {
            if (entry.StartTime >= entry.EndTime)
                return false;

            if (entry.TotalHours > 24)
                return false;

            // Sprawdzamy nakładanie się wpisów
            bool hasOverlap = await HasOverlapAsync(
                entry.EmployeeId,
                entry.EntryDate,
                entry.StartTime,
                entry.EndTime,
                entry.Id > 0 ? entry.Id : null
            );

            return !hasOverlap;
        }

        public async Task<Dictionary<DateTime, DayMarker?>> GetDayMarkersForMonthAsync(int employeeId, int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var markers = await _context.DayMarkers
                .Where(d => d.EmployeeId == employeeId && d.Date >= startDate && d.Date <= endDate)
                .ToListAsync();

            var result = new Dictionary<DateTime, DayMarker?>();
            var daysInMonth = DateTime.DaysInMonth(year, month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);
                result[date] = markers.FirstOrDefault(m => m.Date.Date == date);
            }

            return result;
        }
    }
}
