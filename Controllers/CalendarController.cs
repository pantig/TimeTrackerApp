using System;
using System.Collections.Generic;
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

        public async Task<IActionResult> Index(int? year, int? month, int? employeeId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            Employee? employee;

            if (employeeId.HasValue && (user.Role == UserRole.Admin || user.Role == UserRole.Manager))
            {
                employee = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == employeeId.Value);
            }
            else
            {
                employee = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == userId);
            }

            if (employee == null)
            {
                return RedirectToAction("Index", "TimeEntries");
            }

            int targetYear = year ?? DateTime.Today.Year;
            int targetMonth = month ?? DateTime.Today.Month;

            var selectedMonth = new DateTime(targetYear, targetMonth, 1);
            var firstDayOfMonth = selectedMonth;
            var lastDayOfMonth = selectedMonth.AddMonths(1).AddDays(-1);

            var days = new List<DateTime>();
            var startDay = (int)firstDayOfMonth.DayOfWeek;
            int offset = startDay == 0 ? 6 : startDay - 1;

            for (int i = 0; i < offset; i++)
            {
                days.Add(DateTime.MinValue);
            }

            for (int i = 1; i <= lastDayOfMonth.Day; i++)
            {
                days.Add(new DateTime(targetYear, targetMonth, i));
            }

            // Fix: include whole last day
            var to = lastDayOfMonth.Date.AddDays(1).AddTicks(-1);
            var entries = await _timeEntryService.GetTimeEntriesForEmployeeAsync(employee.Id, firstDayOfMonth.Date, to);

            var viewModel = new CalendarViewModel
            {
                Year = targetYear,
                Month = targetMonth,
                SelectedMonth = selectedMonth,
                TimeEntries = entries,
                DaysInCalendar = days
            };

            ViewBag.EmployeeName = $"{employee.User.FirstName} {employee.User.LastName}";
            ViewBag.EmployeeId = employee.Id;

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Week(DateTime? date, int? employeeId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            Employee? employee;
            if (employeeId.HasValue && (user.Role == UserRole.Admin || user.Role == UserRole.Manager))
            {
                employee = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == employeeId.Value);
            }
            else
            {
                employee = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == userId);
            }

            if (employee == null)
            {
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

            var vm = new WeekReportViewModel
            {
                WeekStart = weekStart,
                EmployeeId = employee.Id,
                EmployeeName = $"{employee.User.FirstName} {employee.User.LastName}",
                Projects = projects,
                Days = Enumerable.Range(0, 7).Select(i =>
                {
                    var d = weekStart.AddDays(i);
                    var dayEntries = entries.Where(e => e.EntryDate.Date == d).ToList();
                    var hours = dayEntries.Sum(e => e.TotalHours);

                    return new WeekDayReportRow
                    {
                        Date = d,
                        Hours = hours,
                        ProjectId = dayEntries.FirstOrDefault()?.ProjectId,
                        Description = dayEntries.FirstOrDefault()?.Description,
                        IsApproved = dayEntries.Any() && dayEntries.All(e => e.IsApproved)
                    };
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Week(WeekReportViewModel model, int? employeeId)
        {
            if (!ModelState.IsValid)
            {
                model.Projects = await _context.Projects.OrderBy(p => p.Name).ToListAsync();
                return View(model);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            var targetEmployeeId = model.EmployeeId;
            if (employeeId.HasValue)
            {
                targetEmployeeId = employeeId.Value;
            }

            // Authorization: only admin/manager can submit for others
            var ownEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (ownEmployee == null)
            {
                return Forbid();
            }

            if (targetEmployeeId != ownEmployee.Id && !(user.Role == UserRole.Admin || user.Role == UserRole.Manager))
            {
                return Forbid();
            }

            foreach (var d in model.Days)
            {
                // Skip approved days
                if (d.IsApproved)
                {
                    continue;
                }

                await _timeEntryService.UpsertDailyHoursAsync(
                    targetEmployeeId,
                    d.Date,
                    d.Hours,
                    d.ProjectId,
                    d.Description);
            }

            return RedirectToAction(nameof(Week), new { date = model.WeekStart.ToString("yyyy-MM-dd"), employeeId = targetEmployeeId });
        }

        private static DateTime StartOfWeek(DateTime date, DayOfWeek start)
        {
            int diff = (7 + (date.DayOfWeek - start)) % 7;
            return date.AddDays(-1 * diff).Date;
        }
    }
}
