using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using TimeTrackerApp.Data;
using TimeTrackerApp.Models;
using TimeTrackerApp.Models.ViewModels;

namespace TimeTrackerApp.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // pobieramy wszystkich aktywnych pracowników z bazy
            var pracownicy = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive)
                .ToListAsync();
            
            // sortujemy alfabetycznie po nazwisku
            pracownicy = pracownicy
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();

            return View(pracownicy);
        }

        public async Task<IActionResult> Details(int id)
        {
            var pracownik = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.TimeEntries)
                .Include(e => e.Projects)  // dodajemy ładowanie projektów!
                .FirstOrDefaultAsync(e => e.Id == id);

            if (pracownik == null)
                return NotFound();

            return View(pracownik);
        }

        public IActionResult Create()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var aktualnyUzytkownik = _context.Users.Find(userId);

            // domyślnie ustawiamy rolę Employee
            var model = new CreateEmployeeViewModel
            {
                Role = UserRole.Employee
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var aktualnyUzytkownik = await _context.Users.FindAsync(userId);

            // sprawdzamy uprawnienia - kierownik może dodać tylko pracowników
            if (aktualnyUzytkownik.Role == UserRole.Manager && model.Role != UserRole.Employee)
            {
                ModelState.AddModelError("", "Kierownik może dodawać tylko pracowników.");
                return View(model);
            }

            // tylko admin może dodawać innych adminów
            if (aktualnyUzytkownik.Role != UserRole.Admin && model.Role == UserRole.Admin)
            {
                ModelState.AddModelError("", "Nie masz uprawnień do dodawania administratorów.");
                return View(model);
            }

            // sprawdzamy czy email już istnieje
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Użytkownik z tym adresem email już istnieje.");
                return View(model);
            }

            // tworzymy nowego użytkownika
            var nowyUzytkownik = new User
            {
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Role = model.Role,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(nowyUzytkownik);
            await _context.SaveChangesAsync();

            // tworzymy profil pracownika
            var nowyPracownik = new Employee
            {
                UserId = nowyUzytkownik.Id,
                Position = model.Position,
                Department = model.Department,
                HireDate = model.HireDate ?? DateTime.Today,
                IsActive = true
            };

            _context.Employees.Add(nowyPracownik);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Pracownik {nowyUzytkownik.FirstName} {nowyUzytkownik.LastName} został pomyślnie dodany.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var pracownik = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (pracownik == null)
                return NotFound();

            var model = new EditEmployeeViewModel
            {
                Id = pracownik.Id,
                Email = pracownik.User.Email,
                FirstName = pracownik.User.FirstName,
                LastName = pracownik.User.LastName,
                Position = pracownik.Position,
                Department = pracownik.Department,
                HireDate = pracownik.HireDate,
                IsActive = pracownik.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditEmployeeViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var pracownik = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (pracownik == null)
                return NotFound();

            // aktualizujemy dane użytkownika
            pracownik.User.FirstName = model.FirstName;
            pracownik.User.LastName = model.LastName;
            pracownik.User.Email = model.Email;

            // aktualizujemy dane pracownika
            pracownik.Position = model.Position;
            pracownik.Department = model.Department;
            pracownik.HireDate = model.HireDate;
            pracownik.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Dane pracownika zostały zaktualizowane.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var pracownik = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (pracownik == null)
                return NotFound();

            // dezaktywujemy pracownika i jego konto
            pracownik.IsActive = false;
            pracownik.User.IsActive = false;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pracownik został dezaktywowany.";
            return RedirectToAction(nameof(Index));
        }
    }

    public class EditEmployeeViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Imię jest wymagane")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwisko jest wymagane")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Stanowisko jest wymagane")]
        [MaxLength(200)]
        public string Position { get; set; } = string.Empty;

        [Required(ErrorMessage = "Departament jest wymagany")]
        [MaxLength(200)]
        public string Department { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; }

        public bool IsActive { get; set; }
    }
}