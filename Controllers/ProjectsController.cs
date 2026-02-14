using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TimeTrackerApp.Data;
using TimeTrackerApp.Models;
using TimeTrackerApp.Services;

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

        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects
                .Include(p => p.Employees)
                .Include(p => p.TimeEntries)
                .ToListAsync();
            
            projects = System.Linq.Enumerable.OrderBy(projects, p => p.Name).ToList();

            return View(projects);
        }

        public IActionResult Create()
        {
            var employees = _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive)
                .ToList();
            
            employees = System.Linq.Enumerable.OrderBy(employees, e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();

            ViewBag.Employees = employees;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project model, int[] selectedEmployees)
        {
            if (ModelState.IsValid)
            {
                _context.Projects.Add(model);
                await _context.SaveChangesAsync();

                // Assign selected employees
                if (selectedEmployees != null && selectedEmployees.Length > 0)
                {
                    var employees = await _context.Employees
                        .Where(e => selectedEmployees.Contains(e.Id))
                        .ToListAsync();

                    foreach (var emp in employees)
                    {
                        emp.Projects.Add(model);
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Projekt został utworzony.";
                return RedirectToAction(nameof(Index));
            }

            var employeesList = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive)
                .ToListAsync();
            
            employeesList = System.Linq.Enumerable.OrderBy(employeesList, e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();
            ViewBag.Employees = employeesList;
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Employees)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            var employees = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive)
                .ToListAsync();
            
            employees = System.Linq.Enumerable.OrderBy(employees, e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();

            ViewBag.Employees = employees;
            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project model, int[] selectedEmployees)
        {
            if (id != model.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                var project = await _context.Projects
                    .Include(p => p.Employees)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (project == null)
                    return NotFound();

                project.Name = model.Name;
                project.Description = model.Description;
                project.HoursBudget = model.HoursBudget;

                // Update assigned employees
                project.Employees.Clear();

                if (selectedEmployees != null && selectedEmployees.Length > 0)
                {
                    var employees = await _context.Employees
                        .Where(e => selectedEmployees.Contains(e.Id))
                        .ToListAsync();

                    foreach (var emp in employees)
                    {
                        project.Employees.Add(emp);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Projekt został zaktualizowany.";
                return RedirectToAction(nameof(Index));
            }

            var employeesList = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive)
                .ToListAsync();
            
            employeesList = System.Linq.Enumerable.OrderBy(employeesList, e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();
            ViewBag.Employees = employeesList;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _context.Projects
                .Include(p => p.TimeEntries)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            if (project.TimeEntries.Any())
            {
                TempData["ErrorMessage"] = "Nie można usunąć projektu, który ma przypisane wpisy czasu.";
                return RedirectToAction(nameof(Index));
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Projekt został usunięty.";
            return RedirectToAction(nameof(Index));
        }
    }
}