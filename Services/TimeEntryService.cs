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
                .Include(e => e.Project)
                .Include(e => e.Employee)
                    .ThenInclude(e => e.User)
                .Where(e => e.EmployeeId == employeeId && e.EntryDate >= from && e.EntryDate <= to)
                .OrderBy(e => e.EntryDate)
                .ThenBy(e => e.StartTime)
                .ToListAsync();
        }

        public async Task<List<TimeEntry>> GetTimeEntriesForProjectAsync(int projectId)
        {
            return await _context.TimeEntries
                .Include(e => e.Employee)
                    .ThenInclude(e => e.User)
                .Include(e => e.Project)
                .Where(e => e.ProjectId == projectId)
                .OrderBy(e => e.EntryDate)
                .ThenBy(e => e.StartTime)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalHoursAsync(int employeeId, DateTime from, DateTime to)
        {
            var entries = await GetTimeEntriesForEmployeeAsync(employeeId, from, to);
            return entries.Sum(e => e.TotalHours);
        }

        public async Task<decimal> GetTotalEarningsAsync(int employeeId, DateTime from, DateTime to)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null || !employee.HourlyRate.HasValue)
                return 0;

            var totalHours = await GetTotalHoursAsync(employeeId, from, to);
            return totalHours * employee.HourlyRate.Value;
        }

        public async Task<List<TimeEntry>> GetUnapprovedEntriesAsync()
        {
            return await _context.TimeEntries
                .Include(e => e.Employee)
                    .ThenInclude(e => e.User)
                .Include(e => e.Project)
                .Where(e => e.ApprovedBy == null)
                .OrderBy(e => e.EntryDate)
                .ThenBy(e => e.StartTime)
                .ToListAsync();
        }

        public async Task ApproveTimeEntryAsync(int entryId)
        {
            var entry = await _context.TimeEntries.FindAsync(entryId);
            if (entry != null)
            {
                entry.ApprovedBy = 1; // TODO: Use actual approver user ID
                entry.ApprovedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpsertDailyHoursAsync(int employeeId, DateTime date, decimal hours, int? projectId, string? description)
        {
            // Find existing entry for this day
            var existingEntry = await _context.TimeEntries
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.EntryDate.Date == date.Date);

            if (existingEntry != null)
            {
                // Update existing
                existingEntry.StartTime = TimeSpan.Zero;
                existingEntry.EndTime = TimeSpan.FromHours((double)hours);
                existingEntry.ProjectId = projectId;
                existingEntry.Description = description;
            }
            else
            {
                // Create new
                var newEntry = new TimeEntry
                {
                    EmployeeId = employeeId,
                    EntryDate = date.Date,
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.FromHours((double)hours),
                    ProjectId = projectId,
                    Description = description ?? string.Empty,
                    CreatedBy = 1 // TODO: Use actual user ID
                };
                _context.TimeEntries.Add(newEntry);
            }

            await _context.SaveChangesAsync();
        }

        // Additional helper methods
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
