using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTrackerApp.Data;
using TimeTrackerApp.Models;
using TimeTrackerApp.Models.ViewModels;

namespace TimeTrackerApp.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class ClientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Clients
        public async Task<IActionResult> Index()
        {
            var clients = await _context.Clients
                .Include(c => c.Projects)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(clients);
        }

        // GET: Clients/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var client = await _context.Clients
                .Include(c => c.Projects)
                    .ThenInclude(p => p.Manager)
                        .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                TempData["ErrorMessage"] = "Klient nie został znaleziony.";
                return RedirectToAction(nameof(Index));
            }

            return View(client);
        }

        // GET: Clients/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Email,Phone,Address,City,PostalCode,Country,NIP,IsActive")] Client client)
        {
            if (ModelState.IsValid)
            {
                client.CreatedAt = DateTime.UtcNow;
                _context.Add(client);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Klient {client.Name} został dodany pomyślnie.";
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // GET: Clients/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                TempData["ErrorMessage"] = "Klient nie został znaleziony.";
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // POST: Clients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Email,Phone,Address,City,PostalCode,Country,NIP,IsActive,CreatedAt")] Client client)
        {
            if (id != client.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    client.UpdatedAt = DateTime.UtcNow;
                    _context.Update(client);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Klient {client.Name} został zaktualizowany.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(client.Id))
                    {
                        TempData["ErrorMessage"] = "Klient nie istnieje.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // GET: Clients/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var client = await _context.Clients
                .Include(c => c.Projects)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                TempData["ErrorMessage"] = "Klient nie został znaleziony.";
                return RedirectToAction(nameof(Index));
            }

            return View(client);
        }

        // POST: Clients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = await _context.Clients
                .Include(c => c.Projects)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                TempData["ErrorMessage"] = "Klient nie istnieje.";
                return RedirectToAction(nameof(Index));
            }

            if (client.Projects.Any())
            {
                TempData["ErrorMessage"] = $"Nie można usunąć klienta {client.Name}, ponieważ ma przypisane projekty.";
                return RedirectToAction(nameof(Index));
            }

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Klient {client.Name} został usunięty.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Clients/Report/5
        public async Task<IActionResult> Report(int id)
        {
            var client = await _context.Clients
                .Include(c => c.Projects)
                    .ThenInclude(p => p.Manager)
                        .ThenInclude(m => m.User)
                .Include(c => c.Projects)
                    .ThenInclude(p => p.Employees)
                .Include(c => c.Projects)
                    .ThenInclude(p => p.TimeEntries)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                TempData["ErrorMessage"] = "Klient nie został znaleziony.";
                return RedirectToAction(nameof(Index));
            }

            var projectStats = new List<ProjectStatistics>();

            foreach (var project in client.Projects)
            {
                var totalHours = project.TimeEntries.Sum(te => te.TotalHours);
                decimal? budgetUsagePercentage = null;

                if (project.HoursBudget.HasValue && project.HoursBudget.Value > 0)
                {
                    budgetUsagePercentage = (totalHours / project.HoursBudget.Value) * 100;
                }

                projectStats.Add(new ProjectStatistics
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    ProjectStatus = project.Status.ToString(),
                    ManagerName = $"{project.Manager.User.FirstName} {project.Manager.User.LastName}",
                    TeamSize = project.Employees.Count,
                    TotalHours = totalHours,
                    HoursBudget = project.HoursBudget,
                    BudgetUsagePercentage = budgetUsagePercentage,
                    StartDate = project.StartDate,
                    EndDate = project.EndDate
                });
            }

            var summary = new ClientSummary
            {
                TotalProjects = client.Projects.Count,
                ActiveProjects = client.Projects.Count(p => p.Status == ProjectStatus.Active),
                CompletedProjects = client.Projects.Count(p => p.Status == ProjectStatus.Completed),
                TotalHoursAllProjects = projectStats.Sum(ps => ps.TotalHours),
                TotalBudget = client.Projects.Where(p => p.HoursBudget.HasValue).Sum(p => p.HoursBudget!.Value),
                TotalBudgetUsed = projectStats.Sum(ps => ps.TotalHours),
                AverageBudgetUsagePercentage = projectStats
                    .Where(ps => ps.BudgetUsagePercentage.HasValue)
                    .Select(ps => ps.BudgetUsagePercentage!.Value)
                    .DefaultIfEmpty(0)
                    .Average()
            };

            var viewModel = new ClientReportViewModel
            {
                Client = client,
                ProjectStatistics = projectStats,
                Summary = summary
            };

            return View(viewModel);
        }

        private bool ClientExists(int id)
        {
            return _context.Clients.Any(e => e.Id == id);
        }
    }
}
