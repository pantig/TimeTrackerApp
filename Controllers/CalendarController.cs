using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTrackerApp.Data;
using TimeTrackerApp.Models;
using TimeTrackerApp.Models.ViewModels;
using TimeTrackerApp.Services;

namespace TimeTrackerApp.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITimeEntryService _timeEntryService;

        public CalendarController(ApplicationDbContext context, ITimeEntryService timeEntryService)
        {
            _context = context;
            _timeEntryService = timeEntryService;
        }

        public async Task<IActionResult> Index(int? year, int? month, int? employeeId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);
            
            Employee employee = null;
            
            if (employeeId.HasValue && (user.Role == UserRole.Admin || user.Role == UserRole.Manager))
            {
                employee = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == employeeId.Value);
            }
            else
            {
                employee = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == userId);
            }

            if (employee == null)
            {
                return RedirectToAction("Index", "TimeEntries");
            }

            int targetYear = year ?? DateTime.Today.Year;
            int targetMonth = month ?? DateTime.Today.Month;
            
            var selectedMonth = new DateTime(targetYear, targetMonth, 1);
            var firstDayOfMonth = selectedMonth;
            var lastDayOfMonth = selectedMonth.AddMonths(1).AddDays(-1);
            
            // Przygotowanie dni do wyświetlenia w siatce kalendarza (z uwzględnieniem pustych miejsc na początku)
            var days = new List<DateTime>();
            var startDay = (int)firstDayOfMonth.DayOfWeek;
            // W C# DayOfWeek.Sunday = 0, Monday = 1...
            // Chcemy żeby poniedziałek był pierwszy (1), a niedziela ostatnia (7)
            int offset = startDay == 0 ? 6 : startDay - 1;
            
            for (int i = 0; i < offset; i++)
            {
                days.Add(DateTime.MinValue); // Miejsce wypełniające
            }
            
            for (int i = 1; i <= lastDayOfMonth.Day; i++)
            {
                days.Add(new DateTime(targetYear, targetMonth, i));
            }

            var entries = await _timeEntryService.GetTimeEntriesForEmployeeAsync(employee.Id, firstDayOfMonth, lastDayOfMonth.AddHours(23).AddMinutes(59));

            var viewModel = new CalendarViewModel
            {
                Year = targetYear,
                Month = targetMonth,
                SelectedMonth = selectedMonth,
                TimeEntries = entries,
                DaysInCalendar = days
            };

            ViewBag.EmployeeName = $"{employee.User.FirstName} {employee.User.LastName}";
            ViewBag.EmployeeId = employee.Id;

            return View(viewModel);
        }
    }
}
