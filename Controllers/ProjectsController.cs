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
using TimeTrackerApp.Models.ViewModels;
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

        // ✅ ADDED: Filtrowanie projektów
        public async Task<IActionResult> Index(string searchName, int? managerId, ProjectStatus? status)
        {
            // pobieramy wszystkie projekty z pracownikami, wpisami czasu i managerem
            var projektyQuery = _context.Projects
                .Include(p => p.Employees)
                .Include(p => p.TimeEntries)
                .Include(p => p.Manager)
                    .ThenInclude(m => m.User)
                .AsQueryable();

            // ✅ Filtrowanie po nazwie
            if (!string.IsNullOrWhiteSpace(searchName))
            {
                projektyQuery = projektyQuery.Where(p => p.Name.Contains(searchName));
                ViewBag.SearchName = searchName;
            }

            // ✅ Filtrowanie po opiekunie
            if (managerId.HasValue)
            {
                projektyQuery = projektyQuery.Where(p => p.ManagerId == managerId.Value);
                ViewBag.ManagerId = managerId.Value;
            }

            // ✅ Filtrowanie po statusie
            if (status.HasValue)
            {
                projektyQuery = projektyQuery.Where(p => p.Status == status.Value);
                ViewBag.Status = status.Value;
            }

            var projekty = await projektyQuery.OrderBy(p => p.Name).ToListAsync();

            // ✅ Lista opiekunów (managerów) do filtra
            var managers = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive && e.User.Role == UserRole.Manager)
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToListAsync();

            ViewBag.Managers = managers;

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

            // ✅ FIXED: Dodanie klientów do ViewBag
            var klienci = await _context.Clients
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Employees = pracownicy;
            ViewBag.Managers = kierownicy;
            ViewBag.Clients = klienci;
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

            // Usuń błędy dla właściwości nawigacyjnych (EF wypełni je automatycznie)
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

            // ✅ FIXED: Dodanie klientów
            var klienci = await _context.Clients
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Employees = listaPracownikow;
            ViewBag.Managers = kierownicy;
            ViewBag.Clients = klienci;
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

            // ✅ FIXED: Dodanie klientów do ViewBag
            var klienci = await _context.Clients
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Employees = pracownicy;
            ViewBag.Managers = kierownicy;
            ViewBag.Clients = klienci;
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

            // Usuń błędy dla właściwości nawigacyjnych (EF wypełni je automatycznie)
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

                    // aktualizujemy WSZYSTKIE dane projektu
                    projekt.Name = model.Name;
                    projekt.Description = model.Description;
                    projekt.Status = model.Status;
                    projekt.StartDate = model.StartDate;
                    projekt.EndDate = model.EndDate;
                    projekt.HoursBudget = model.HoursBudget;
                    projekt.ManagerId = model.ManagerId;
                    projekt.ClientId = model.ClientId;
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

            // ✅ FIXED: Dodanie klientów
            var klienci = await _context.Clients
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Employees = listaPracownikow;
            ViewBag.Managers = kierownicy;
            ViewBag.Clients = klienci;
            
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

        // GET: Projects/Report/5
        public async Task<IActionResult> Report(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Manager)
                    .ThenInclude(m => m.User)
                .Include(p => p.Client)
                .Include(p => p.Employees)
                    .ThenInclude(e => e.User)
                .Include(p => p.TimeEntries)
                    .ThenInclude(te => te.Employee)
                        .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                TempData["ErrorMessage"] = "Projekt nie został znaleziony.";
                return RedirectToAction(nameof(Index));
            }

            // Grupowanie wpisów czasu według pracowników
            var employeeTimeEntries = new List<EmployeeTimeEntry>();

            var employeesWithEntries = project.TimeEntries
                .GroupBy(te => te.EmployeeId)
                .Select(g => new
                {
                    EmployeeId = g.Key,
                    Employee = g.First().Employee,
                    TotalHours = g.Sum(te => te.TotalHours),
                    EntriesCount = g.Count(),
                    FirstEntry = g.Min(te => te.EntryDate),
                    LastEntry = g.Max(te => te.EntryDate)
                });

            foreach (var emp in employeesWithEntries)
            {
                employeeTimeEntries.Add(new EmployeeTimeEntry
                {
                    EmployeeId = emp.EmployeeId,
                    EmployeeName = $"{emp.Employee.User.FirstName} {emp.Employee.User.LastName}",
                    Position = emp.Employee.Position ?? "Nie określono",
                    TotalHours = emp.TotalHours,
                    EntriesCount = emp.EntriesCount,
                    FirstEntry = emp.FirstEntry,
                    LastEntry = emp.LastEntry
                });
            }

            // Sortowanie według godzin malejąco
            employeeTimeEntries = employeeTimeEntries.OrderByDescending(e => e.TotalHours).ToList();

            // Obliczanie statystyk projektu
            var totalHours = project.TimeEntries.Sum(te => te.TotalHours);
            decimal? budgetUsagePercentage = null;

            if (project.HoursBudget.HasValue && project.HoursBudget.Value > 0)
            {
                budgetUsagePercentage = (totalHours / project.HoursBudget.Value) * 100;
            }

            var daysActive = 0;
            if (project.TimeEntries.Any())
            {
                var firstEntry = project.TimeEntries.Min(te => te.EntryDate);
                var lastEntry = project.TimeEntries.Max(te => te.EntryDate);
                daysActive = (lastEntry - firstEntry).Days + 1;
            }

            var summary = new ProjectSummary
            {
                TotalEmployees = project.Employees.Count,
                ActiveEmployees = employeeTimeEntries.Count,
                TotalHoursLogged = totalHours,
                HoursBudget = project.HoursBudget,
                BudgetUsagePercentage = budgetUsagePercentage,
                TotalEntries = project.TimeEntries.Count,
                ProjectStartDate = project.StartDate,
                ProjectEndDate = project.EndDate,
                DaysActive = daysActive
            };

            var viewModel = new ProjectReportViewModel
            {
                Project = project,
                EmployeeTimeEntries = employeeTimeEntries,
                Summary = summary
            };

            return View(viewModel);
        }
    }
}