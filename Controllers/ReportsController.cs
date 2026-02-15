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
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITimeEntryService _timeEntryService;
        private readonly ExcelExportService _excelExportService;

        public ReportsController(ApplicationDbContext context, ITimeEntryService timeEntryService, ExcelExportService excelExportService)
        {
            _context = context;
            _timeEntryService = timeEntryService;
            _excelExportService = excelExportService;
        }

        // raport organizacji z godzinami wszystkich pracowników
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Summary(int? year, int? month)
        {
            var aktualnyUzytkownik = await PobierzAktualnegoUzytkownika();

            // jeśli nie podano - bierzemy obecny miesiąc
            var wybranyRok = year ?? DateTime.UtcNow.Year;
            var wybranyMiesiac = month ?? DateTime.UtcNow.Month;

            var dataOd = new DateTime(wybranyRok, wybranyMiesiac, 1);
            var dataDo = dataOd.AddMonths(1).AddDays(-1);

            // pobieramy wszystkie wpisy czasu w tym okresie
            var wpisyCzasu = await _context.TimeEntries
                .Include(t => t.Employee)
                    .ThenInclude(e => e.User)
                .Include(t => t.Project)
                .Where(t => t.EntryDate >= dataOd && t.EntryDate <= dataDo)
                .ToListAsync();

            // pobieramy projekty z wpisami
            var projekty = await _context.Projects
                .Include(p => p.TimeEntries.Where(te => te.EntryDate >= dataOd && te.EntryDate <= dataDo))
                .ToListAsync();

            // grupujemy godziny po pracownikach
            var godzinyPracownikow = wpisyCzasu
                .GroupBy(t => new { t.EmployeeId, NazwaPracownika = $"{t.Employee.User.FirstName} {t.Employee.User.LastName}" })
                .Select(g => new EmployeeHoursSummary
                {
                    EmployeeId = g.Key.EmployeeId,
                    EmployeeName = g.Key.NazwaPracownika,
                    TotalHours = g.Sum(t => t.TotalHours),
                    EntryCount = g.Count()
                })
                .OrderByDescending(e => e.TotalHours)
                .ToList();

            // grupujemy godziny po projektach
            var godzinyProjektow = projekty
                .Select(p => new ProjectBudgetSummary
                {
                    ProjectId = p.Id,
                    ProjectName = p.Name,
                    TotalHours = p.TimeEntries.Sum(te => te.TotalHours),
                    HoursBudget = p.HoursBudget,
                    IsOverBudget = p.HoursBudget.HasValue && p.TimeEntries.Sum(te => te.TotalHours) > p.HoursBudget.Value,
                    EntryCount = p.TimeEntries.Count
                })
                .OrderByDescending(p => p.TotalHours)
                .ToList();

            var viewModel = new OrganizationSummaryViewModel
            {
                Year = wybranyRok,
                Month = wybranyMiesiac,
                FromDate = dataOd,
                ToDate = dataDo,
                TotalHours = wpisyCzasu.Sum(t => t.TotalHours),
                TotalEmployees = godzinyPracownikow.Count,
                TotalProjects = godzinyProjektow.Count(p => p.TotalHours > 0),
                EmployeeHours = godzinyPracownikow,
                ProjectHours = godzinyProjektow
            };

            return View(viewModel);
        }

        // raport miesięczny pojedynczego pracownika
        public async Task<IActionResult> Monthly(int? employeeId, int? year, int? month)
        {
            var aktualnyUzytkownik = await PobierzAktualnegoUzytkownika();

            // jeśli nie podano - bierzemy obecny miesiąc
            var wybranyRok = year ?? DateTime.UtcNow.Year;
            var wybranyMiesiac = month ?? DateTime.UtcNow.Month;

            var dataOd = new DateTime(wybranyRok, wybranyMiesiac, 1);
            var dataDo = dataOd.AddMonths(1).AddDays(-1);

            Employee? wybranyPracownik = null;
            List<Employee>? wszyscyPracownicy = null;

            // określamy kto może zobaczyć raport
            if (aktualnyUzytkownik.Role == UserRole.Employee)
            {
                // zwykły pracownik widzi tylko swój raport
                wybranyPracownik = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == aktualnyUzytkownik.Id);
            }
            else if (CzyMaUprawnienia(aktualnyUzytkownik.Role))
            {
                // admin/manager wybiera pracownika z listy
                wszyscyPracownicy = await PobierzPosortowanychPracownikow();

                if (employeeId.HasValue)
                {
                    wybranyPracownik = wszyscyPracownicy.FirstOrDefault(e => e.Id == employeeId.Value);
                }
                else if (wszyscyPracownicy.Any())
                {
                    wybranyPracownik = wszyscyPracownicy.First();
                }
            }

            if (wybranyPracownik == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono pracownika.";
                return RedirectToAction(nameof(Summary));
            }

            // pobieramy wpisy czasu dla wybranego pracownika
            var wpisyCzasu = await _context.TimeEntries
                .Include(t => t.Project)
                .Include(t => t.CreatedByUser)
                .Where(t => t.EmployeeId == wybranyPracownik.Id && t.EntryDate >= dataOd && t.EntryDate <= dataDo)
                .ToListAsync();
            
            // sortujemy w pamięci (SQLite nie obsługuje sortowania po TimeSpan)
            wpisyCzasu = wpisyCzasu.OrderBy(t => t.EntryDate).ToList();

            // grupujemy po dniach
            var wpisyPoDniach = wpisyCzasu
                .GroupBy(t => t.EntryDate.Date)
                .Select(g => new DailyHoursReport
                {
                    Date = g.Key,
                    TotalHours = g.Sum(t => t.TotalHours),
                    Entries = g.ToList()
                })
                .OrderBy(g => g.Date)
                .ToList();

            // grupujemy po projektach
            var wpisyPoProjektach = wpisyCzasu
                .GroupBy(t => t.Project != null ? t.Project.Name : "(brak projektu)")
                .Select(g => new ProjectHoursReport
                {
                    ProjectName = g.Key,
                    TotalHours = g.Sum(t => t.TotalHours),
                    EntryCount = g.Count()
                })
                .OrderByDescending(p => p.TotalHours)
                .ToList();

            var nazwaPracownika = $"{wybranyPracownik.User.FirstName} {wybranyPracownik.User.LastName}";
            
            var viewModel = new MonthlyReportViewModel
            {
                EmployeeId = wybranyPracownik.Id,
                EmployeeName = nazwaPracownika,
                Year = wybranyRok,
                Month = wybranyMiesiac,
                FromDate = dataOd,
                ToDate = dataDo,
                EntriesByDay = wpisyPoDniach,
                EntriesByProject = wpisyPoProjektach,
                TotalHours = wpisyCzasu.Sum(t => t.TotalHours),
                TotalDays = wpisyPoDniach.Count,
                AllEmployees = wszyscyPracownicy,
                CanSelectEmployee = CzyMaUprawnienia(aktualnyUzytkownik.Role)
            };

            return View(viewModel);
        }

        // eksport raportu do excela
        public async Task<IActionResult> ExportMonthlyExcel(int employeeId, int year, int month)
        {
            var aktualnyUzytkownik = await PobierzAktualnegoUzytkownika();

            var pracownik = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (pracownik == null)
            {
                return NotFound();
            }

            // sprawdzamy czy użytkownik ma prawo eksportować ten raport
            if (aktualnyUzytkownik.Role == UserRole.Employee && pracownik.UserId != aktualnyUzytkownik.Id)
            {
                return Forbid();
            }

            // ✅ NOWE - sprawdzamy czy pracownik ma wpisy bez projektu
            var hasEntriesWithoutProject = await _context.TimeEntries
                .AnyAsync(e => e.EmployeeId == employeeId && e.ProjectId == null);

            if (hasEntriesWithoutProject)
            {
                TempData["ErrorMessage"] = "Nie możesz wyeksportować raportu - istnieją wpisy bez przypisanego projektu. Uzupełnij je w zakładce '⚠️ Brak projektu'.";
                return RedirectToAction("Monthly", new { employeeId, year, month });
            }

            var dataOd = new DateTime(year, month, 1);
            var dataDo = dataOd.AddMonths(1).AddDays(-1);

            // pobieramy wpisy czasu
            var wpisyCzasu = await _context.TimeEntries
                .Include(t => t.Project)
                .Include(t => t.CreatedByUser)
                .Where(t => t.EmployeeId == employeeId && t.EntryDate >= dataOd && t.EntryDate <= dataDo)
                .ToListAsync();
            
            // sortujemy w pamięci (SQLite nie obsługuje sortowania po TimeSpan w LINQ)
            wpisyCzasu = wpisyCzasu
                .OrderBy(t => t.EntryDate)
                .ThenBy(t => t.StartTime)
                .ToList();

            // pobieramy markery dni
            var markeryDni = await _context.DayMarkers
                .Where(d => d.EmployeeId == employeeId && d.Date >= dataOd && d.Date <= dataDo)
                .ToListAsync();

            // tworzymy słownik markerów dla każdego dnia
            var slownikMarkerow = new Dictionary<DateTime, DayMarker?>();
            var iloscDni = DateTime.DaysInMonth(year, month);
            for (int dzien = 1; dzien <= iloscDni; dzien++)
            {
                var data = new DateTime(year, month, dzien);
                slownikMarkerow[data] = markeryDni.FirstOrDefault(d => d.Date.Date == data);
            }

            var nazwaPracownika = $"{pracownik.User.FirstName} {pracownik.User.LastName}";
            var plikExcel = _excelExportService.GenerateMonthlyReport(nazwaPracownika, year, month, wpisyCzasu, slownikMarkerow);

            var nazwaMiesiaca = new DateTime(year, month, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("pl-PL"));
            var nazwaPliku = $"{pracownik.User.FirstName}-{pracownik.User.LastName}-{nazwaMiesiaca}.xlsx";

            return File(plikExcel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nazwaPliku);
        }

        // metody pomocnicze
        private async Task<User> PobierzAktualnegoUzytkownika()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            return await _context.Users.FindAsync(userId);
        }

        private bool CzyMaUprawnienia(UserRole rola)
        {
            return rola == UserRole.Admin || rola == UserRole.Manager;
        }

        private async Task<List<Employee>> PobierzPosortowanychPracownikow()
        {
            var pracownicy = await _context.Employees
                .Include(e => e.User)
                .ToListAsync();
            
            return pracownicy
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToList();
        }
    }
}