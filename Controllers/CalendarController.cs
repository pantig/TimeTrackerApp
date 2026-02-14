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
                employee = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == userId);
            }

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono profilu pracownika. Skontaktuj się z administratorem.";
                return RedirectToAction("Index", "TimeEntries");
            }

            var anchor = (date ?? DateTime.Today).Date;
            var weekStart = StartOfWeek(anchor, DayOfWeek.Monday);
            var weekEnd = weekStart.AddDays(6);

            var entries = await _timeEntryService.GetTimeEntriesForEmployeeAsync(
                employee.Id,
                weekStart,
                weekEnd.AddDays(1).AddTicks(-1));

            var projects = await _context.Projects.OrderBy(p => p.Name).ToListAsync();

            var entriesByDay = new Dictionary<DateTime, List<TimeGridEntry>>();
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
                        IsApproved = e.IsApproved
                    })
                    .OrderBy(e => e.StartTime)
                    .ToList();

                entriesByDay[day] = dayEntries;
            }

            var vm = new WeeklyTimeGridViewModel
            {
                WeekStart = weekStart,
                EmployeeId = employee.Id,
                EmployeeName = $"{employee.User.FirstName} {employee.User.LastName}",
                Projects = projects,
                EntriesByDay = entriesByDay,
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

            // Check if user is authorized to add for this employee
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
                IsApproved = false
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

            if (entry.IsApproved)
            {
                return Json(new { success = false, message = "Nie można edytować zatwierdzonego wpisu" });
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

            if (entry.IsApproved)
            {
                return Json(new { success = false, message = "Nie można usunąć zatwierdzonego wpisu" });
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
}
