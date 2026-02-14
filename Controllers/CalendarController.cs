using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTrackerApp.Data;
using TimeTrackerApp.Models;
using TimeTrackerApp.Models.ViewModels;
using TimeTrackerApp.Services;

namespace TimeTrackerApp.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITimeEntryService _timeEntryService;

        public CalendarController(ApplicationDbContext context, ITimeEntryService timeEntryService)
        {
            _context = context;
            _timeEntryService = timeEntryService;
        }

        public async Task<IActionResult> Index(DateTime? date, int? employeeId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            Employee? employee;
            List<Employee>? allEmployees = null;

            // Admin/Manager can view any employee's calendar
            if (user.Role == UserRole.Admin || user.Role == UserRole.Manager)
            {
                allEmployees = await _context.Employees
                    .Include(e => e.User)
                    .OrderBy(e => e.User.LastName)
                    .ThenBy(e => e.User.FirstName)
                    .ToListAsync();

                if (employeeId.HasValue)
                {
                    employee = allEmployees.FirstOrDefault(e => e.Id == employeeId.Value);
                }
                else
                {
                    // Default to first employee or own employee if exists
                    employee = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == userId);
                    if (employee == null && allEmployees.Any())
                    {
                        employee = allEmployees.First();
                    }
                }
            }
            else
            {
                // Regular employee can only view own calendar
                employee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Projects)
                    .FirstOrDefaultAsync(e => e.UserId == userId);
            }

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono profilu pracownika. Skontaktuj się z administratorem.";
                return RedirectToAction("Index", "TimeEntries");
            }

            var anchor = (date ?? DateTime.Today).Date;
            var weekStart = StartOfWeek(anchor, DayOfWeek.Monday);
            var weekEnd = weekStart.AddDays(6);

            // Fetch entries from database
            var entries = await _context.TimeEntries
                .Include(e => e.Employee)
                    .ThenInclude(e => e.User)
                .Include(e => e.Project)
                .Include(e => e.CreatedByUser)
                .Where(e => e.EmployeeId == employee.Id && e.EntryDate >= weekStart && e.EntryDate <= weekEnd)
                .OrderBy(e => e.EntryDate)
                .ToListAsync();

            // Sort by StartTime in memory
            entries = entries.OrderBy(e => e.EntryDate).ThenBy(e => e.StartTime).ToList();

            // Fetch day markers
            var dayMarkers = await _context.DayMarkers
                .Where(d => d.EmployeeId == employee.Id && d.Date >= weekStart && d.Date <= weekEnd)
                .ToListAsync();

            // Filter projects: Employee sees only assigned projects, Admin/Manager see all
            List<Project> projects;
            if (user.Role == UserRole.Employee)
            {
                // Load employee with projects
                var empWithProjects = await _context.Employees
                    .Include(e => e.Projects)
                    .FirstOrDefaultAsync(e => e.Id == employee.Id);
                
                projects = empWithProjects?.Projects.OrderBy(p => p.Name).ToList() ?? new List<Project>();
            }
            else
            {
                projects = await _context.Projects.OrderBy(p => p.Name).ToListAsync();
            }

            var entriesByDay = new Dictionary<DateTime, List<TimeGridEntry>>();
            var markersByDay = new Dictionary<DateTime, DayMarker>();

            for (int i = 0; i < 7; i++)
            {
                var day = weekStart.AddDays(i);
                var dayEntries = entries
                    .Where(e => e.EntryDate.Date == day)
                    .Select(e => new TimeGridEntry
                    {
                        Id = e.Id,
                        Date = e.EntryDate.Date,
                        StartTime = e.StartTime,
                        EndTime = e.EndTime,
                        ProjectId = e.ProjectId,
                        ProjectName = e.Project?.Name,
                        Description = e.Description,
                        CreatedBy = e.CreatedByUser != null ? $"{e.CreatedByUser.FirstName} {e.CreatedByUser.LastName}" : "System"
                    })
                    .OrderBy(e => e.StartTime)
                    .ToList();

                entriesByDay[day] = dayEntries;

                var marker = dayMarkers.FirstOrDefault(d => d.Date.Date == day);
                if (marker != null)
                {
                    markersByDay[day] = marker;
                }
            }

            var vm = new WeeklyTimeGridViewModel
            {
                WeekStart = weekStart,
                EmployeeId = employee.Id,
                EmployeeName = $"{employee.User.FirstName} {employee.User.LastName}",
                Projects = projects,
                EntriesByDay = entriesByDay,
                DayMarkers = markersByDay,
                AllEmployees = allEmployees,
                CanSelectEmployee = user.Role == UserRole.Admin || user.Role == UserRole.Manager
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> AddEntry([FromBody] AddEntryRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null && !(user.Role == UserRole.Admin || user.Role == UserRole.Manager))
            {
                return Json(new { success = false, message = "Pracownik nie znaleziony" });
            }

            if (employee != null && request.EmployeeId != employee.Id && !(user.Role == UserRole.Admin || user.Role == UserRole.Manager))
            {
                return Json(new { success = false, message = "Brak uprawnień" });
            }

            var entry = new TimeEntry
            {
                EmployeeId = request.EmployeeId,
                EntryDate = request.Date,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                ProjectId = request.ProjectId,
                Description = request.Description,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.TimeEntries.Add(entry);
            await _context.SaveChangesAsync();

            return Json(new { success = true, entryId = entry.Id });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEntry([FromBody] UpdateEntryRequest request)
        {
            var entry = await _context.TimeEntries.FindAsync(request.Id);
            if (entry == null)
            {
                return Json(new { success = false, message = "Wpis nie znaleziony" });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee != null && entry.EmployeeId != employee.Id && !(user.Role == UserRole.Admin || user.Role == UserRole.Manager))
            {
                return Json(new { success = false, message = "Brak uprawnień" });
            }

            entry.ProjectId = request.ProjectId;
            entry.Description = request.Description;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEntry([FromBody] DeleteEntryRequest request)
        {
            var entry = await _context.TimeEntries.FindAsync(request.Id);
            if (entry == null)
            {
                return Json(new { success = false, message = "Wpis nie znaleziony" });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee != null && entry.EmployeeId != employee.Id && !(user.Role == UserRole.Admin || user.Role == UserRole.Manager))
            {
                return Json(new { success = false, message = "Brak uprawnień" });
            }

            _context.TimeEntries.Remove(entry);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> SetDayMarker([FromBody] SetDayMarkerRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null && !(user.Role == UserRole.Admin || user.Role == UserRole.Manager))
            {
                return Json(new { success = false, message = "Pracownik nie znaleziony" });
            }

            if (employee != null && request.EmployeeId != employee.Id && !(user.Role == UserRole.Admin || user.Role == UserRole.Manager))
            {
                return Json(new { success = false, message = "Brak uprawnień" });
            }

            // Check if marker already exists
            var existing = await _context.DayMarkers
                .FirstOrDefaultAsync(d => d.EmployeeId == request.EmployeeId && d.Date.Date == request.Date.Date);

            if (existing != null)
            {
                // Update
                existing.Type = request.Type;
                existing.Note = request.Note;
            }
            else
            {
                // Create
                var marker = new DayMarker
                {
                    EmployeeId = request.EmployeeId,
                    Date = request.Date.Date,
                    Type = request.Type,
                    Note = request.Note,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DayMarkers.Add(marker);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveDayMarker([FromBody] RemoveDayMarkerRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null && !(user.Role == UserRole.Admin || user.Role == UserRole.Manager))
            {
                return Json(new { success = false, message = "Pracownik nie znaleziony" });
            }

            var marker = await _context.DayMarkers
                .FirstOrDefaultAsync(d => d.EmployeeId == request.EmployeeId && d.Date.Date == request.Date.Date);

            if (marker != null)
            {
                if (employee != null && marker.EmployeeId != employee.Id && !(user.Role == UserRole.Admin || user.Role == UserRole.Manager))
                {
                    return Json(new { success = false, message = "Brak uprawnień" });
                }

                _context.DayMarkers.Remove(marker);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        private static DateTime StartOfWeek(DateTime date, DayOfWeek start)
        {
            int diff = (7 + (date.DayOfWeek - start)) % 7;
            return date.AddDays(-1 * diff).Date;
        }
    }

    public class AddEntryRequest
    {
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int? ProjectId { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateEntryRequest
    {
        public int Id { get; set; }
        public int? ProjectId { get; set; }
        public string? Description { get; set; }
    }

    public class DeleteEntryRequest
    {
        public int Id { get; set; }
    }

    public class SetDayMarkerRequest
    {
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public DayType Type { get; set; }
        public string? Note { get; set; }
    }

    public class RemoveDayMarkerRequest
    {
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
    }
}
