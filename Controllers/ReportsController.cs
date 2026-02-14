using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TimeTrackerApp.Data;
using TimeTrackerApp.Models;
using TimeTrackerApp.Models.ViewModels;
using TimeTrackerApp.Services;

namespace TimeTrackerApp.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITimeEntryService _timeEntryService;

        public ReportsController(ApplicationDbContext context, ITimeEntryService timeEntryService)
        {
            _context = context;
            _timeEntryService = timeEntryService;
        }

        public async Task<IActionResult> Summary()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            var fromDate = DateTime.UtcNow.AddMonths(-1);
            var toDate = DateTime.UtcNow;

            var timeEntries = new List<TimeEntry>();

            if (user.Role == UserRole.Employee)
            {
                var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
                if (employee != null)
                    timeEntries = await _timeEntryService.GetTimeEntriesForEmployeeAsync(employee.Id, fromDate, toDate);
            }
            else if (user.Role == UserRole.Manager || user.Role == UserRole.Admin)
            {
                timeEntries = await _context.TimeEntries
                    .Where(t => t.EntryDate >= fromDate && t.EntryDate <= toDate)
                    .Include(t => t.Employee)
                        .ThenInclude(e => e.User)
                    .Include(t => t.Project)
                    .ToListAsync();
            }

            var viewModel = new ReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                TimeEntries = timeEntries,
                Employees = await _context.Employees.ToListAsync(),
                Projects = await _context.Projects.ToListAsync()
            };

            return View(viewModel);
        }

        // Nowy raport miesięczny
        public async Task<IActionResult> Monthly(int? employeeId, int? year, int? month)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            // Domyślne wartości: bieżący miesiąc
            var selectedYear = year ?? DateTime.UtcNow.Year;
            var selectedMonth = month ?? DateTime.UtcNow.Month;

            var fromDate = new DateTime(selectedYear, selectedMonth, 1);
            var toDate = fromDate.AddMonths(1).AddDays(-1);

            Employee? selectedEmployee = null;
            List<Employee>? allEmployees = null;

            // Określenie zakresu pracowników
            if (user.Role == UserRole.Employee)
            {
                // Pracownik widzi tylko swój raport
                selectedEmployee = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == userId);
            }
            else if (user.Role == UserRole.Manager || user.Role == UserRole.Admin)
            {
                // Admin/Manager wybiera pracownika
                allEmployees = await _context.Employees
                    .Include(e => e.User)
                    .OrderBy(e => e.User.LastName)
                    .ThenBy(e => e.User.FirstName)
                    .ToListAsync();

                if (employeeId.HasValue)
                {
                    selectedEmployee = allEmployees.FirstOrDefault(e => e.Id == employeeId.Value);
                }
                else if (allEmployees.Any())
                {
                    selectedEmployee = allEmployees.First();
                }
            }

            if (selectedEmployee == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono pracownika.";
                return RedirectToAction(nameof(Summary));
            }

            // Pobierz wpisy czasu dla wybranego pracownika i miesiąca
            var timeEntries = await _context.TimeEntries
                .Include(t => t.Project)
                .Include(t => t.CreatedByUser)
                .Where(t => t.EmployeeId == selectedEmployee.Id && t.EntryDate >= fromDate && t.EntryDate <= toDate)
                .OrderBy(t => t.EntryDate)
                .ToListAsync();

            // Grupowanie po dniach
            var entriesByDay = timeEntries
                .GroupBy(t => t.EntryDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyHoursReport
                {
                    Date = g.Key,
                    TotalHours = g.Sum(t => t.TotalHours),
                    Entries = g.ToList()
                })
                .ToList();

            // Grupowanie po projektach
            var entriesByProject = timeEntries
                .GroupBy(t => t.Project != null ? t.Project.Name : "(brak projektu)")
                .Select(g => new ProjectHoursReport
                {
                    ProjectName = g.Key,
                    TotalHours = g.Sum(t => t.TotalHours),
                    EntryCount = g.Count()
                })
                .OrderByDescending(p => p.TotalHours)
                .ToList();

            var viewModel = new MonthlyReportViewModel
            {
                EmployeeId = selectedEmployee.Id,
                EmployeeName = $"{selectedEmployee.User.FirstName} {selectedEmployee.User.LastName}",
                Year = selectedYear,
                Month = selectedMonth,
                FromDate = fromDate,
                ToDate = toDate,
                EntriesByDay = entriesByDay,
                EntriesByProject = entriesByProject,
                TotalHours = timeEntries.Sum(t => t.TotalHours),
                TotalDays = entriesByDay.Count,
                AllEmployees = allEmployees,
                CanSelectEmployee = user.Role == UserRole.Admin || user.Role == UserRole.Manager
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Approval()
        {
            var unapprovedEntries = await _timeEntryService.GetUnapprovedEntriesAsync();
            return View(unapprovedEntries);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public async Task<IActionResult> ApproveEntry(int id)
        {
            await _timeEntryService.ApproveTimeEntryAsync(id);
            return RedirectToAction(nameof(Approval));
        }
    }
}