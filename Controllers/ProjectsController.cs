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
            // pobieramy wszystkie projekty z pracownikami i wpisami czasu
            var projekty = await _context.Projects
                .Include(p => p.Employees)
                .Include(p => p.TimeEntries)
                .ToListAsync();
            
            projekty = projekty.OrderBy(p => p.Name).ToList();

            return View(projekty);
        }

        public IActionResult Create()
        {
            var pracownicy = _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive)
                .ToList();
            
            // sortujemy alfabetycznie
            pracownicy = pracownicy
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();

            ViewBag.Employees = pracownicy;
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

                // przypisujemy wybranych pracowników do projektu
                if (selectedEmployees != null && selectedEmployees.Length > 0)
                {
                    var pracownicy = await _context.Employees
                        .Where(e => selectedEmployees.Contains(e.Id))
                        .ToListAsync();

                    foreach (var pracownik in pracownicy)
                    {
                        pracownik.Projects.Add(model);
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Projekt został utworzony.";
                return RedirectToAction(nameof(Index));
            }

            var listaPracownikow = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive)
                .ToListAsync();
            
            listaPracownikow = listaPracownikow
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();
            ViewBag.Employees = listaPracownikow;
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var projekt = await _context.Projects
                .Include(p => p.Employees)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (projekt == null)
                return NotFound();

            var pracownicy = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive)
                .ToListAsync();
            
            pracownicy = pracownicy
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();

            ViewBag.Employees = pracownicy;
            return View(projekt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project model, int[] selectedEmployees)
        {
            if (id != model.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                var projekt = await _context.Projects
                    .Include(p => p.Employees)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (projekt == null)
                    return NotFound();

                // aktualizujemy dane projektu
                projekt.Name = model.Name;
                projekt.Description = model.Description;
                projekt.HoursBudget = model.HoursBudget;

                // aktualizujemy przypisanych pracowników
                projekt.Employees.Clear();

                if (selectedEmployees != null && selectedEmployees.Length > 0)
                {
                    var pracownicy = await _context.Employees
                        .Where(e => selectedEmployees.Contains(e.Id))
                        .ToListAsync();

                    foreach (var pracownik in pracownicy)
                    {
                        projekt.Employees.Add(pracownik);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Projekt został zaktualizowany.";
                return RedirectToAction(nameof(Index));
            }

            var listaPracownikow = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive)
                .ToListAsync();
            
            listaPracownikow = listaPracownikow
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();
            ViewBag.Employees = listaPracownikow;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var projekt = await _context.Projects
                .Include(p => p.TimeEntries)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (projekt == null)
                return NotFound();

            // nie można usunąć projektu który ma wpisy czasu
            if (projekt.TimeEntries.Any())
            {
                TempData["ErrorMessage"] = "Nie można usunąć projektu, który ma przypisane wpisy czasu.";
                return RedirectToAction(nameof(Index));
            }

            _context.Projects.Remove(projekt);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Projekt został usunięty.";
            return RedirectToAction(nameof(Index));
        }
    }
}