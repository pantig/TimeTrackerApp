using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TimeTrackerApp.Data;
using TimeTrackerApp.Models;
using TimeTrackerApp.Models.ViewModels;
using TimeTrackerApp.Services;

namespace TimeTrackerApp.Controllers
{
    [Authorize]
    public class TimeEntriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITimeEntryService _timeEntryService;

        public TimeEntriesController(ApplicationDbContext context, ITimeEntryService timeEntryService)
        {
            _context = context;
            _timeEntryService = timeEntryService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            var query = _context.TimeEntries
                .Include(t => t.Employee)
                    .ThenInclude(e => e.User)
                .Include(t => t.Project)
                .Include(t => t.CreatedByUser)
                .AsQueryable();

            if (user.Role == UserRole.Employee)
            {
                var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
                if (employee != null)
                    query = query.Where(t => t.EmployeeId == employee.Id);
            }

            var timeEntries = await query.OrderByDescending(t => t.EntryDate).ToListAsync();

            return View(timeEntries);
        }

        [HttpGet]
        public async Task<IActionResult> Create(DateTime? date)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            var employees = _context.Employees.Include(e => e.User).AsQueryable();
            if (user.Role == UserRole.Employee)
            {
                employees = employees.Where(e => e.UserId == userId);
            }

            var viewModel = new TimeEntryViewModel
            {
                Employees = await employees.ToListAsync(),
                Projects = await _context.Projects.Where(p => p.IsActive).ToListAsync(),
                EntryDate = date ?? DateTime.Today,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(16, 0, 0)
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TimeEntryViewModel model)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            if (!ModelState.IsValid)
            {
                var employeesQuery = _context.Employees.Include(e => e.User).AsQueryable();
                if (user.Role == UserRole.Employee)
                {
                    employeesQuery = employeesQuery.Where(e => e.UserId == userId);
                }
                model.Employees = await employeesQuery.ToListAsync();
                model.Projects = await _context.Projects.Where(p => p.IsActive).ToListAsync();
                return View(model);
            }

            var timeEntry = new TimeEntry
            {
                EmployeeId = model.EmployeeId,
                ProjectId = model.ProjectId,
                EntryDate = model.EntryDate,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Description = model.Description,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            // Sprawdzenie uprawnień: Pracownik może dodawać tylko dla siebie
            if (user.Role == UserRole.Employee)
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                if (employee == null || timeEntry.EmployeeId != employee.Id)
                {
                    ModelState.AddModelError("", "Nie masz uprawnień do dodawania wpisów dla innych pracowników.");
                    model.Employees = new List<Employee> { employee };
                    model.Projects = await _context.Projects.Where(p => p.IsActive).ToListAsync();
                    return View(model);
                }
            }

            _context.TimeEntries.Add(timeEntry);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            var timeEntry = await _context.TimeEntries.FindAsync(id);
            if (timeEntry == null)
                return NotFound();

            // Sprawdzenie uprawnień
            if (user.Role == UserRole.Employee)
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                if (employee == null || timeEntry.EmployeeId != employee.Id)
                    return Forbid();
            }

            var employeesQuery = _context.Employees.Include(e => e.User).AsQueryable();
            if (user.Role == UserRole.Employee)
            {
                employeesQuery = employeesQuery.Where(e => e.UserId == userId);
            }

            var viewModel = new TimeEntryViewModel
            {
                Id = timeEntry.Id,
                EmployeeId = timeEntry.EmployeeId,
                ProjectId = timeEntry.ProjectId,
                EntryDate = timeEntry.EntryDate,
                StartTime = timeEntry.StartTime,
                EndTime = timeEntry.EndTime,
                Description = timeEntry.Description,
                Employees = await employeesQuery.ToListAsync(),
                Projects = await _context.Projects.Where(p => p.IsActive).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TimeEntryViewModel model)
        {
            if (id != model.Id)
                return BadRequest();

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            if (!ModelState.IsValid)
            {
                var employeesQuery = _context.Employees.Include(e => e.User).AsQueryable();
                if (user.Role == UserRole.Employee)
                {
                    employeesQuery = employeesQuery.Where(e => e.UserId == userId);
                }
                model.Employees = await employeesQuery.ToListAsync();
                model.Projects = await _context.Projects.Where(p => p.IsActive).ToListAsync();
                return View(model);
            }

            var timeEntry = await _context.TimeEntries.FindAsync(id);
            if (timeEntry == null)
                return NotFound();

            // Sprawdzenie uprawnień
            if (user.Role == UserRole.Employee)
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                if (employee == null || timeEntry.EmployeeId != employee.Id || model.EmployeeId != employee.Id)
                    return Forbid();
            }

            timeEntry.EmployeeId = model.EmployeeId;
            timeEntry.ProjectId = model.ProjectId;
            timeEntry.EntryDate = model.EntryDate;
            timeEntry.StartTime = model.StartTime;
            timeEntry.EndTime = model.EndTime;
            timeEntry.Description = model.Description;

            _context.TimeEntries.Update(timeEntry);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            var timeEntry = await _context.TimeEntries
                .Include(t => t.Employee)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (timeEntry == null)
                return NotFound();

            // Sprawdzenie uprawnień
            if (user.Role == UserRole.Employee)
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                if (employee == null || timeEntry.EmployeeId != employee.Id)
                    return Forbid();
            }

            return View(timeEntry);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            var timeEntry = await _context.TimeEntries.FindAsync(id);
            if (timeEntry == null)
                return NotFound();

            // Sprawdzenie uprawnień
            if (user.Role == UserRole.Employee)
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                if (employee == null || timeEntry.EmployeeId != employee.Id)
                    return Forbid();
            }

            _context.TimeEntries.Remove(timeEntry);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
