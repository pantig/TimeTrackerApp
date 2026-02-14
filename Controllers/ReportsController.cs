using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TimeTrackerApp.Data;
using TimeTrackerApp.Models;
using TimeTrackerApp.Models.ViewModels;
using TimeTrackerApp.Services;

namespace TimeTrackerApp.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITimeEntryService _timeEntryService;

        public ReportsController(ApplicationDbContext context, ITimeEntryService timeEntryService)
        {
            _context = context;
            _timeEntryService = timeEntryService;
        }

        public async Task<IActionResult> Summary()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            var fromDate = DateTime.UtcNow.AddMonths(-1);
            var toDate = DateTime.UtcNow;

            var timeEntries = new List<TimeEntry>();

            if (user.Role == UserRole.Employee)
            {
                var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
                if (employee != null)
                    timeEntries = await _timeEntryService.GetTimeEntriesForEmployeeAsync(employee.Id, fromDate, toDate);
            }
            else if (user.Role == UserRole.Manager || user.Role == UserRole.Admin)
            {
                timeEntries = await _context.TimeEntries
                    .Where(t => t.EntryDate >= fromDate && t.EntryDate <= toDate)
                    .Include(t => t.Employee)
                        .ThenInclude(e => e.User)
                    .Include(t => t.Project)
                    .ToListAsync();
            }

            var viewModel = new ReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                TimeEntries = timeEntries,
                Employees = await _context.Employees.ToListAsync(),
                Projects = await _context.Projects.ToListAsync()
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Approval()
        {
            var unapprovedEntries = await _timeEntryService.GetUnapprovedEntriesAsync();
            return View(unapprovedEntries);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public async Task<IActionResult> ApproveEntry(int id)
        {
            await _timeEntryService.ApproveTimeEntryAsync(id);
            return RedirectToAction(nameof(Approval));
        }
    }
}
