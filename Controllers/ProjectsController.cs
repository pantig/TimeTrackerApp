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
            // pobieramy wszystkie projekty z pracownikami, wpisami czasu i managerem
            var projekty = await _context.Projects
                .Include(p => p.Employees)
                .Include(p => p.TimeEntries)
                .Include(p => p.Manager)
                    .ThenInclude(m => m.User)
                .ToListAsync();
            
            projekty = projekty.OrderBy(p => p.Name).ToList();

            return View(projekty);
        }

        public async Task<IActionResult> Create()
        {
            // pobieramy wszystkich aktywnych pracowników
            var pracownicy = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive)
                .ToListAsync();
            
            // sortujemy alfabetycznie
            pracownicy = pracownicy
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();

            // pobieramy tylko kierowników (Manager) dla pola opiekuna projektu
            var kierownicy = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive && e.User.Role == UserRole.Manager)
                .ToListAsync();
            
            kierownicy = kierownicy
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();

            ViewBag.Employees = pracownicy;
            ViewBag.Managers = kierownicy;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Project model, int[] selectedEmployees)
        {
            // DEBUG: Wypisz wszystkie błędy walidacji
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            // ✅ FIXED: Usuń błędy dla właściwości nawigacyjnych (EF wypełni je automatycznie)
            ModelState.Remove("Manager");
            ModelState.Remove("Client");
            ModelState.Remove("TimeEntries");
            ModelState.Remove("Employees");

            if (ModelState.IsValid)
            {
                // sprawdzamy czy wybrany manager jest kierownikiem
                var manager = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == model.ManagerId);

                if (manager == null || manager.User.Role != UserRole.Manager)
                {
                    ModelState.AddModelError("ManagerId", "Opiekunem projektu może być tylko kierownik.");
                }
                else
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
            }

            // jeśli wystąpił błąd - przeładuj listy
            var listaPracownikow = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive)
                .ToListAsync();
            
            listaPracownikow = listaPracownikow
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();

            var kierownicy = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive && e.User.Role == UserRole.Manager)
                .ToListAsync();
            
            kierownicy = kierownicy
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();

            ViewBag.Employees = listaPracownikow;
            ViewBag.Managers = kierownicy;
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var projekt = await _context.Projects
                .Include(p => p.Employees)
                .Include(p => p.Manager)
                    .ThenInclude(m => m.User)
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

            var kierownicy = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive && e.User.Role == UserRole.Manager)
                .ToListAsync();
            
            kierownicy = kierownicy
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();

            ViewBag.Employees = pracownicy;
            ViewBag.Managers = kierownicy;
            return View(projekt);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Project model, int[] selectedEmployees)
        {
            if (id != model.Id)
                return NotFound();

            // DEBUG: Wypisz wszystkie błędy walidacji
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            // ✅ FIXED: Usuń błędy dla właściwości nawigacyjnych (EF wypełni je automatycznie)
            ModelState.Remove("Manager");
            ModelState.Remove("Client");
            ModelState.Remove("TimeEntries");
            ModelState.Remove("Employees");

            if (ModelState.IsValid)
            {
                // sprawdzamy czy wybrany manager jest kierownikiem
                var manager = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == model.ManagerId);

                if (manager == null || manager.User.Role != UserRole.Manager)
                {
                    ModelState.AddModelError("ManagerId", "Opiekunem projektu może być tylko kierownik.");
                }
                else
                {
                    var projekt = await _context.Projects
                        .Include(p => p.Employees)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (projekt == null)
                        return NotFound();

                    // ✅ FIXED: aktualizujemy WSZYSTKIE dane projektu
                    projekt.Name = model.Name;
                    projekt.Description = model.Description;
                    projekt.Status = model.Status;
                    projekt.StartDate = model.StartDate;
                    projekt.EndDate = model.EndDate;
                    projekt.HoursBudget = model.HoursBudget;
                    projekt.ManagerId = model.ManagerId;
                    projekt.ClientId = model.ClientId; // ✅ FIXED: Dodano aktualizację ClientId
                    projekt.IsActive = model.IsActive;

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
            }

            // jeśli wystąpił błąd - przeładuj listy
            var listaPracownikow = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive)
                .ToListAsync();
            
            listaPracownikow = listaPracownikow
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();

            var kierownicy = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive && e.User.Role == UserRole.Manager)
                .ToListAsync();
            
            kierownicy = kierownicy
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();

            ViewBag.Employees = listaPracownikow;
            ViewBag.Managers = kierownicy;
            
            // przeładuj projekt z bazy dla widoku
            var projektDoWidoku = await _context.Projects
                .Include(p => p.Employees)
                .Include(p => p.Manager)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            return View(projektDoWidoku);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, string dummy) // Zmieniony podpis metody
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