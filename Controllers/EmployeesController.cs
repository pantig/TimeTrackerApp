using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
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
            var employees = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive)
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToListAsync();

            return View(employees);
        }

        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.TimeEntries)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return NotFound();

            return View(employee);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var currentUser = _context.Users.Find(userId);

            var model = new CreateEmployeeViewModel
            {
                // For managers, default to Employee role
                Role = currentUser.Role == UserRole.Manager ? UserRole.Employee : UserRole.Employee
            };

            return View(model);
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var currentUser = await _context.Users.FindAsync(userId);

            // Authorization check: Manager can only create Employees, Admin can create Employees and Managers
            if (currentUser.Role == UserRole.Manager && model.Role != UserRole.Employee)
            {
                ModelState.AddModelError("", "Kierownik może dodawać tylko pracowników.");
                return View(model);
            }

            if (currentUser.Role != UserRole.Admin && model.Role == UserRole.Admin)
            {
                ModelState.AddModelError("", "Nie masz uprawnień do dodawania administratorów.");
                return View(model);
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Użytkownik z tym adresem email już istnieje.");
                return View(model);
            }

            // Create User
            var user = new User
            {
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Role = model.Role,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create Employee profile
            var employee = new Employee
            {
                UserId = user.Id,
                Position = model.Position,
                Department = model.Department,
                HireDate = model.HireDate ?? DateTime.Today,
                IsActive = true
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Pracownik {user.FirstName} {user.LastName} został pomyślnie dodany.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return NotFound();

            var model = new EditEmployeeViewModel
            {
                Id = employee.Id,
                Email = employee.User.Email,
                FirstName = employee.User.FirstName,
                LastName = employee.User.LastName,
                Position = employee.Position,
                Department = employee.Department,
                HireDate = employee.HireDate,
                IsActive = employee.IsActive
            };

            return View(model);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditEmployeeViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return NotFound();

            // Update User
            employee.User.FirstName = model.FirstName;
            employee.User.LastName = model.LastName;
            employee.User.Email = model.Email;

            // Update Employee
            employee.Position = model.Position;
            employee.Department = model.Department;
            employee.HireDate = model.HireDate;
            employee.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Dane pracownika zostały zaktualizowane.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Employees/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return NotFound();

            employee.IsActive = false;
            employee.User.IsActive = false;

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