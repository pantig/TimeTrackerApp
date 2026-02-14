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
        private readonly ExcelExportService _excelExportService;

        public ReportsController(ApplicationDbContext context, ITimeEntryService timeEntryService, ExcelExportService excelExportService)
        {
            _context = context;
            _timeEntryService = timeEntryService;
            _excelExportService = excelExportService;
        }

        // Raport zbiorczy organizacji
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Summary(int? year, int? month)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            // Domyślne wartości: bieżący miesiąc
            var selectedYear = year ?? DateTime.UtcNow.Year;
            var selectedMonth = month ?? DateTime.UtcNow.Month;

            var fromDate = new DateTime(selectedYear, selectedMonth, 1);
            var toDate = fromDate.AddMonths(1).AddDays(-1);

            // Pobierz wszystkie wpisy czasu w organizacji
            var timeEntries = await _context.TimeEntries
                .Include(t => t.Employee)
                    .ThenInclude(e => e.User)
                .Include(t => t.Project)
                .Where(t => t.EntryDate >= fromDate && t.EntryDate <= toDate)
                .ToListAsync();

            // Pobierz wszystkie projekty z sumą godzin
            var projects = await _context.Projects
                .Include(p => p.TimeEntries.Where(te => te.EntryDate >= fromDate && te.EntryDate <= toDate))
                .ToListAsync();

            // Grupowanie po pracownikach
            var employeeHours = timeEntries
                .GroupBy(t => new { t.EmployeeId, EmployeeName = $"{t.Employee.User.FirstName} {t.Employee.User.LastName}" })
                .Select(g => new EmployeeHoursSummary
                {
                    EmployeeId = g.Key.EmployeeId,
                    EmployeeName = g.Key.EmployeeName,
                    TotalHours = g.Sum(t => t.TotalHours),
                    EntryCount = g.Count()
                })
                .OrderByDescending(e => e.TotalHours)
                .ToList();

            // Grupowanie po projektach
            var projectHours = projects
                .Select(p => new ProjectBudgetSummary
                {
                    ProjectId = p.Id,
                    ProjectName = p.Name,
                    TotalHours = p.TimeEntries.Sum(te => te.TotalHours),
                    HoursBudget = p.HoursBudget,
                    IsOverBudget = p.HoursBudget.HasValue && p.TimeEntries.Sum(te => te.TotalHours) > p.HoursBudget.Value,
                    EntryCount = p.TimeEntries.Count
                })
                .OrderByDescending(p => p.TotalHours)
                .ToList();

            var viewModel = new OrganizationSummaryViewModel
            {
                Year = selectedYear,
                Month = selectedMonth,
                FromDate = fromDate,
                ToDate = toDate,
                TotalHours = timeEntries.Sum(t => t.TotalHours),
                TotalEmployees = employeeHours.Count,
                TotalProjects = projectHours.Count(p => p.TotalHours > 0),
                EmployeeHours = employeeHours,
                ProjectHours = projectHours
            };

            return View(viewModel);
        }

        // Raport miesięczny pracownika
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

        // Export Excel
        public async Task<IActionResult> ExportMonthlyExcel(int employeeId, int year, int month)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            // Sprawdź uprawnienia
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
            {
                return NotFound();
            }

            // Tylko właściciel lub Admin/Manager
            if (user.Role == UserRole.Employee && employee.UserId != userId)
            {
                return Forbid();
            }

            var fromDate = new DateTime(year, month, 1);
            var toDate = fromDate.AddMonths(1).AddDays(-1);

            // Pobierz wpisy czasu
            var timeEntries = await _context.TimeEntries
                .Include(t => t.Project)
                .Include(t => t.CreatedByUser)
                .Where(t => t.EmployeeId == employeeId && t.EntryDate >= fromDate && t.EntryDate <= toDate)
                .OrderBy(t => t.EntryDate)
                .ThenBy(t => t.StartTime)
                .ToListAsync();

            // Pobierz day markers
            var dayMarkers = await _context.DayMarkers
                .Where(d => d.EmployeeId == employeeId && d.Date >= fromDate && d.Date <= toDate)
                .ToListAsync();

            var dayMarkerDict = new Dictionary<DateTime, DayMarker?>();
            var daysInMonth = DateTime.DaysInMonth(year, month);
            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);
                dayMarkerDict[date] = dayMarkers.FirstOrDefault(d => d.Date.Date == date);
            }

            var employeeName = $"{employee.User.FirstName} {employee.User.LastName}";
            var excelBytes = _excelExportService.GenerateMonthlyReport(employeeName, year, month, timeEntries, dayMarkerDict);

            var monthName = new DateTime(year, month, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("pl-PL"));
            var fileName = $"{employee.User.FirstName}-{employee.User.LastName}-{monthName}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}