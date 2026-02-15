using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TimeTrackerApp.Data;
using TimeTrackerApp.Models;
using TimeTrackerApp.Models.ViewModels;

namespace TimeTrackerApp.Controllers
{
    [Authorize]
    public class NoProjectReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NoProjectReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /NoProjectReport/MyEntries - Pracownik widzi swoje wpisy bez projektu
        public async Task<IActionResult> MyEntries()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono profilu pracownika.";
                return RedirectToAction("Index", "TimeEntries");
            }

            var entriesWithoutProject = await _context.TimeEntries
                .Include(e => e.Employee)
                    .ThenInclude(e => e.User)
                .Include(e => e.CreatedByUser)
                .Where(e => e.EmployeeId == employee.Id && e.ProjectId == null)
                .OrderByDescending(e => e.EntryDate)
                .ThenBy(e => e.StartTime)
                .ToListAsync();

            var projects = await _context.Projects
                .Where(p => p.IsActive && p.Employees.Any(emp => emp.Id == employee.Id))
                .OrderBy(p => p.Name)
                .ToListAsync();

            var viewModel = new NoProjectEntriesViewModel
            {
                Entries = entriesWithoutProject,
                AvailableProjects = projects,
                EmployeeName = $"{employee.User.FirstName} {employee.User.LastName}",
                IsManagerView = false
            };

            return View(viewModel);
        }

        // GET: /NoProjectReport/AllEntries - Manager widzi wszystkie wpisy bez projektu
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AllEntries(int? employeeId)
        {
            var query = _context.TimeEntries
                .Include(e => e.Employee)
                    .ThenInclude(e => e.User)
                .Include(e => e.CreatedByUser)
                .Where(e => e.ProjectId == null);

            if (employeeId.HasValue)
            {
                query = query.Where(e => e.EmployeeId == employeeId.Value);
            }

            var entriesWithoutProject = await query
                .OrderByDescending(e => e.EntryDate)
                .ThenBy(e => e.StartTime)
                .ToListAsync();

            var employees = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive)
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToListAsync();

            var projects = await _context.Projects
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var viewModel = new NoProjectEntriesViewModel
            {
                Entries = entriesWithoutProject,
                AvailableProjects = projects,
                AllEmployees = employees,
                SelectedEmployeeId = employeeId,
                IsManagerView = true
            };

            return View(viewModel);
        }

        // POST: /NoProjectReport/AssignProject - Przypisanie projektu do wpisu
        [HttpPost]
        public async Task<IActionResult> AssignProject([FromBody] AssignProjectRequest request)
        {
            var entry = await _context.TimeEntries
                .Include(e => e.Employee)
                    .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == request.EntryId);

            if (entry == null)
            {
                return Json(new { success = false, message = "Wpis nie znaleziony" });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var currentUser = await _context.Users.FindAsync(userId);
            var employee = await _context.Employees
                .Include(e => e.Projects)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            // Sprawdzamy uprawnienia
            bool isManagerOrAdmin = currentUser.Role == UserRole.Admin || currentUser.Role == UserRole.Manager;
            bool isOwnEntry = employee != null && entry.EmployeeId == employee.Id;

            if (!isManagerOrAdmin && !isOwnEntry)
            {
                return Json(new { success = false, message = "Brak uprawnień" });
            }

            // Sprawdzamy czy pracownik jest przypisany do projektu (tylko dla pracowników)
            if (!isManagerOrAdmin && employee != null)
            {
                var targetEmployee = await _context.Employees
                    .Include(e => e.Projects)
                    .FirstOrDefaultAsync(e => e.Id == entry.EmployeeId);

                if (targetEmployee != null && !targetEmployee.Projects.Any(p => p.Id == request.ProjectId))
                {
                    return Json(new { success = false, message = "Nie jesteś przypisany do tego projektu" });
                }
            }

            entry.ProjectId = request.ProjectId;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }

    public class AssignProjectRequest
    {
        public int EntryId { get; set; }
        public int ProjectId { get; set; }
    }
}
