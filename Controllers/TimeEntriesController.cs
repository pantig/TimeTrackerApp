using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

            var query = _context.TimeEntries.AsQueryable();

            if (user.Role == UserRole.Employee)
            {
                var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
                if (employee != null)
                    query = query.Where(t => t.EmployeeId == employee.Id);
            }

            var timeEntries = await System.Linq.Dynamic.Core.DynamicQueryableExtensions
                .ToDynamicListAsync(query.OrderByDescending(t => t.EntryDate));

            return View(timeEntries);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new TimeEntryViewModel
            {
                Employees = _context.Employees.ToList(),
                Projects = _context.Projects.Where(p => p.IsActive).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TimeEntryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Employees = _context.Employees.ToList();
                model.Projects = _context.Projects.Where(p => p.IsActive).ToList();
                return View(model);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            var timeEntry = new TimeEntry
            {
                EmployeeId = model.EmployeeId,
                ProjectId = model.ProjectId,
                EntryDate = model.EntryDate,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Description = model.Description,
                CreatedBy = userId
            };

            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(timeEntry);
            if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(timeEntry, validationContext, validationResults, true))
            {
                foreach (var result in validationResults)
                    ModelState.AddModelError("", result.ErrorMessage);

                model.Employees = _context.Employees.ToList();
                model.Projects = _context.Projects.Where(p => p.IsActive).ToList();
                return View(model);
            }

            _context.TimeEntries.Add(timeEntry);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var timeEntry = await _context.TimeEntries.FindAsync(id);
            if (timeEntry == null)
                return NotFound();

            var viewModel = new TimeEntryViewModel
            {
                Id = timeEntry.Id,
                EmployeeId = timeEntry.EmployeeId,
                ProjectId = timeEntry.ProjectId,
                EntryDate = timeEntry.EntryDate,
                StartTime = timeEntry.StartTime,
                EndTime = timeEntry.EndTime,
                Description = timeEntry.Description,
                Employees = _context.Employees.ToList(),
                Projects = _context.Projects.Where(p => p.IsActive).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TimeEntryViewModel model)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                model.Employees = _context.Employees.ToList();
                model.Projects = _context.Projects.Where(p => p.IsActive).ToList();
                return View(model);
            }

            var timeEntry = await _context.TimeEntries.FindAsync(id);
            if (timeEntry == null)
                return NotFound();

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
            var timeEntry = await _context.TimeEntries.Include(t => t.Employee).FirstOrDefaultAsync(t => t.Id == id);
            if (timeEntry == null)
                return NotFound();

            return View(timeEntry);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var timeEntry = await _context.TimeEntries.FindAsync(id);
            if (timeEntry != null)
            {
                _context.TimeEntries.Remove(timeEntry);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
