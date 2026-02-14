using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TimeTrackerApp.Data;
using TimeTrackerApp.Models;

namespace TimeTrackerApp.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects
                .Include(p => p.Employees)
                    .ThenInclude(e => e.User)
                .Include(p => p.TimeEntries)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(projects);
        }

        // GET: Projects/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.AllEmployees = await _context.Employees
                .Include(e => e.User)
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToListAsync();

            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project, List<int> selectedEmployees)
        {
            if (ModelState.IsValid)
            {
                // Dodaj projekt
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                // Przypisz pracowników
                if (selectedEmployees != null && selectedEmployees.Any())
                {
                    var employees = await _context.Employees
                        .Where(e => selectedEmployees.Contains(e.Id))
                        .ToListAsync();

                    project.Employees = employees;
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Projekt został utworzony.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AllEmployees = await _context.Employees
                .Include(e => e.User)
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToListAsync();

            return View(project);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.Employees)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            ViewBag.AllEmployees = await _context.Employees
                .Include(e => e.User)
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToListAsync();

            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project project, List<int> selectedEmployees)
        {
            if (id != project.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProject = await _context.Projects
                        .Include(p => p.Employees)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (existingProject == null) return NotFound();

                    // Aktualizuj właściwości
                    existingProject.Name = project.Name;
                    existingProject.Description = project.Description;
                    existingProject.Status = project.Status;
                    existingProject.StartDate = project.StartDate;
                    existingProject.EndDate = project.EndDate;
                    existingProject.HoursBudget = project.HoursBudget;
                    existingProject.IsActive = project.IsActive;

                    // Aktualizuj przypisanych pracowników
                    existingProject.Employees.Clear();
                    if (selectedEmployees != null && selectedEmployees.Any())
                    {
                        var employees = await _context.Employees
                            .Where(e => selectedEmployees.Contains(e.Id))
                            .ToListAsync();

                        existingProject.Employees = employees;
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Projekt został zaktualizowany.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            ViewBag.AllEmployees = await _context.Employees
                .Include(e => e.User)
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToListAsync();

            return View(project);
        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.Employees)
                    .ThenInclude(e => e.User)
                .Include(p => p.TimeEntries)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Projekt został usunięty.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }
    }
}