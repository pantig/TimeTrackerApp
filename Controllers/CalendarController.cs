using System;
using System.Collections.Generic;
using System.Globalization;
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

        public async Task<IActionResult> Index(DateTime? date, int? employeeId)
        {
            // tutaj pobieramy aktualnego zalogowanego użytkownika
            var aktualnyUzytkownik = await PobierzAktualnegoUzytkownika();
            
            Employee? wybranyPracownik;
            List<Employee>? wszyscyPracownicy = null;

            // admin i manager mogą oglądać kalendarz każdego pracownika
            if (CzyMaUprawnienia(aktualnyUzytkownik.Role))
            {
                wszyscyPracownicy = await _context.Employees
                    .Include(e => e.User)
                    .ToListAsync();
                
                // sortujemy alfabetycznie
                wszyscyPracownicy = wszyscyPracownicy
                    .OrderBy(e => e.User.LastName)
                    .ThenBy(e => e.User.FirstName)
                    .ToList();

                if (employeeId.HasValue)
                {
                    wybranyPracownik = wszyscyPracownicy.FirstOrDefault(e => e.Id == employeeId.Value);
                }
                else
                {
                    // domyślnie pokazujemy pierwszy z listy lub własny profil
                    wybranyPracownik = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == aktualnyUzytkownik.Id);
                    if (wybranyPracownik == null && wszyscyPracownicy.Any())
                    {
                        wybranyPracownik = wszyscyPracownicy.First();
                    }
                }
            }
            else
            {
                // zwykły pracownik widzi tylko swój kalendarz
                wybranyPracownik = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Projects)
                    .FirstOrDefaultAsync(e => e.UserId == aktualnyUzytkownik.Id);
            }

            if (wybranyPracownik == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono profilu pracownika. Skontaktuj się z administratorem.";
                return RedirectToAction("Index", "TimeEntries");
            }

            // obliczamy początek i koniec tygodnia
            var dataKalendarza = (date ?? DateTime.Today).Date;
            var poczatekTygodnia = PoczatekTygodnia(dataKalendarza, DayOfWeek.Monday);
            var koniecTygodnia = poczatekTygodnia.AddDays(6);

            // pobieramy wpisy czasu z bazy
            var wpisyCzasu = await _context.TimeEntries
                .Include(e => e.Employee)
                    .ThenInclude(e => e.User)
                .Include(e => e.Project)
                .Include(e => e.CreatedByUser)
                .Where(e => e.EmployeeId == wybranyPracownik.Id && e.EntryDate >= poczatekTygodnia && e.EntryDate <= koniecTygodnia)
                .ToListAsync();

            // sortujemy po dacie i godzinie rozpoczęcia
            wpisyCzasu = wpisyCzasu
                .OrderBy(e => e.EntryDate)
                .ThenBy(e => e.StartTime)
                .ToList();

            // pobieramy markery dni (urlopy, choroby itp)
            var markeryDni = await _context.DayMarkers
                .Where(d => d.EmployeeId == wybranyPracownik.Id && d.Date >= poczatekTygodnia && d.Date <= koniecTygodnia)
                .ToListAsync();

            // filtrujemy projekty - pracownik widzi tylko przypisane mu projekty
            List<Project> dostepneProjekty;
            if (aktualnyUzytkownik.Role == UserRole.Employee)
            {
                var pracownikZProjektami = await _context.Employees
                    .Include(e => e.Projects)
                    .FirstOrDefaultAsync(e => e.Id == wybranyPracownik.Id);
                
                var listaProjektow = pracownikZProjektami?.Projects.ToList() ?? new List<Project>();
                dostepneProjekty = listaProjektow.OrderBy(p => p.Name).ToList();
            }
            else
            {
                // dla admin/manager pokazujemy projekty przypisane do wybranego pracownika
                var pracownikZProjektami = await _context.Employees
                    .Include(e => e.Projects)
                    .FirstOrDefaultAsync(e => e.Id == wybranyPracownik.Id);
                
                dostepneProjekty = pracownikZProjektami?.Projects.OrderBy(p => p.Name).ToList() ?? new List<Project>();
            }

            // organizujemy wpisy według dni
            var wpisyPoDniach = new Dictionary<DateTime, List<TimeGridEntry>>();
            var markeryPoDniach = new Dictionary<DateTime, DayMarker>();

            for (int i = 0; i < 7; i++)
            {
                var dzien = poczatekTygodnia.AddDays(i);
                var wpisyDnia = wpisyCzasu
                    .Where(e => e.EntryDate.Date == dzien)
                    .Select(e => new TimeGridEntry
                    {
                        Id = e.Id,
                        Date = e.EntryDate.Date,
                        StartTime = e.StartTime,
                        EndTime = e.EndTime,
                        ProjectId = e.ProjectId,
                        ProjectName = e.Project?.Name,
                        Description = e.Description,
                        CreatedBy = e.CreatedByUser != null ? $"{e.CreatedByUser.FirstName} {e.CreatedByUser.LastName}" : "System"
                    })
                    .ToList();
                
                wpisyDnia = wpisyDnia.OrderBy(e => e.StartTime).ToList();
                wpisyPoDniach[dzien] = wpisyDnia;

                var marker = markeryDni.FirstOrDefault(d => d.Date.Date == dzien);
                if (marker != null)
                {
                    markeryPoDniach[dzien] = marker;
                }
            }

            var nazwaPracownika = $"{wybranyPracownik.User.FirstName} {wybranyPracownik.User.LastName}";
            
            // tworzymy model widoku do wyświetlenia
            var viewModel = new WeeklyTimeGridViewModel
            {
                WeekStart = poczatekTygodnia,
                EmployeeId = wybranyPracownik.Id,
                EmployeeName = nazwaPracownika,
                Projects = dostepneProjekty,
                EntriesByDay = wpisyPoDniach,
                DayMarkers = markeryPoDniach,
                AllEmployees = wszyscyPracownicy,
                CanSelectEmployee = CzyMaUprawnienia(aktualnyUzytkownik.Role)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AddEntry([FromBody] AddEntryRequest request)
        {
            var aktualnyUzytkownik = await PobierzAktualnegoUzytkownika();
            var pracownik = await _context.Employees
                .Include(e => e.Projects)
                .FirstOrDefaultAsync(e => e.UserId == aktualnyUzytkownik.Id);

            // sprawdzamy uprawnienia
            if (pracownik == null && !CzyMaUprawnienia(aktualnyUzytkownik.Role))
            {
                return Json(new { success = false, message = "Pracownik nie znaleziony" });
            }

            if (pracownik != null && request.EmployeeId != pracownik.Id && !CzyMaUprawnienia(aktualnyUzytkownik.Role))
            {
                return Json(new { success = false, message = "Brak uprawnień" });
            }

            // sprawdzamy czy pracownik jest przypisany do projektu (jeśli projekt został wybrany)
            // ZAWSZE sprawdzamy dla pracowników (Employee), admin/manager mogą dodawać bez ograniczeń
            if (request.ProjectId.HasValue && aktualnyUzytkownik.Role == UserRole.Employee)
            {
                // pobieramy pracownika dla którego dodajemy wpis
                var celPracownik = await _context.Employees
                    .Include(e => e.Projects)
                    .FirstOrDefaultAsync(e => e.Id == request.EmployeeId);

                if (celPracownik != null)
                {
                    // pracownik może rejestrowaC czas tylko w przypisanych projektach
                    var czyPrzypisany = celPracownik.Projects.Any(p => p.Id == request.ProjectId.Value);
                    if (!czyPrzypisany)
                    {
                        return Json(new { success = false, message = "Nie jesteś przypisany do tego projektu" });
                    }
                }
            }

            // tworzymy nowy wpis czasu
            var nowyWpis = new TimeEntry
            {
                EmployeeId = request.EmployeeId,
                EntryDate = request.Date,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                ProjectId = request.ProjectId,
                Description = request.Description,
                CreatedBy = aktualnyUzytkownik.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.TimeEntries.Add(nowyWpis);
            await _context.SaveChangesAsync();

            return Json(new { success = true, entryId = nowyWpis.Id });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEntry([FromBody] UpdateEntryRequest request)
        {
            var wpis = await _context.TimeEntries.FindAsync(request.Id);
            if (wpis == null)
            {
                return Json(new { success = false, message = "Wpis nie znaleziony" });
            }

            var aktualnyUzytkownik = await PobierzAktualnegoUzytkownika();
            var pracownik = await _context.Employees
                .Include(e => e.Projects)
                .FirstOrDefaultAsync(e => e.UserId == aktualnyUzytkownik.Id);

            // tylko właściciel lub admin/manager może edytować
            if (pracownik != null && wpis.EmployeeId != pracownik.Id && !CzyMaUprawnienia(aktualnyUzytkownik.Role))
            {
                return Json(new { success = false, message = "Brak uprawnień" });
            }

            // sprawdzamy czy pracownik jest przypisany do nowego projektu
            // ZAWSZE sprawdzamy dla pracowników (Employee), admin/manager mogą edytować bez ograniczeń
            if (request.ProjectId.HasValue && aktualnyUzytkownik.Role == UserRole.Employee)
            {
                // pobieramy pracownika którego wpis edytujemy
                var celPracownik = await _context.Employees
                    .Include(e => e.Projects)
                    .FirstOrDefaultAsync(e => e.Id == wpis.EmployeeId);

                if (celPracownik != null)
                {
                    var czyPrzypisany = celPracownik.Projects.Any(p => p.Id == request.ProjectId.Value);
                    if (!czyPrzypisany)
                    {
                        return Json(new { success = false, message = "Nie jesteś przypisany do tego projektu" });
                    }
                }
            }

            // aktualizujemy dane (włącznie z czasem!)
            if (request.StartTime.HasValue)
            {
                wpis.StartTime = request.StartTime.Value;
            }
            if (request.EndTime.HasValue)
            {
                wpis.EndTime = request.EndTime.Value;
            }
            wpis.ProjectId = request.ProjectId;
            wpis.Description = request.Description;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEntry([FromBody] DeleteEntryRequest request)
        {
            var wpis = await _context.TimeEntries.FindAsync(request.Id);
            if (wpis == null)
            {
                return Json(new { success = false, message = "Wpis nie znaleziony" });
            }

            var aktualnyUzytkownik = await PobierzAktualnegoUzytkownika();
            var pracownik = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == aktualnyUzytkownik.Id);

            // tylko właściciel lub admin/manager może usunąć
            if (pracownik != null && wpis.EmployeeId != pracownik.Id && !CzyMaUprawnienia(aktualnyUzytkownik.Role))
            {
                return Json(new { success = false, message = "Brak uprawnień" });
            }

            _context.TimeEntries.Remove(wpis);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> SetDayMarker([FromBody] SetDayMarkerRequest request)
        {
            var aktualnyUzytkownik = await PobierzAktualnegoUzytkownika();
            var pracownik = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == aktualnyUzytkownik.Id);

            if (pracownik == null && !CzyMaUprawnienia(aktualnyUzytkownik.Role))
            {
                return Json(new { success = false, message = "Pracownik nie znaleziony" });
            }

            if (pracownik != null && request.EmployeeId != pracownik.Id && !CzyMaUprawnienia(aktualnyUzytkownik.Role))
            {
                return Json(new { success = false, message = "Brak uprawnień" });
            }

            // sprawdzamy czy marker już istnieje
            var istniejacyMarker = await _context.DayMarkers
                .FirstOrDefaultAsync(d => d.EmployeeId == request.EmployeeId && d.Date.Date == request.Date.Date);

            if (istniejacyMarker != null)
            {
                // aktualizujemy istniejący
                istniejacyMarker.Type = request.Type;
                istniejacyMarker.Note = request.Note;
            }
            else
            {
                // tworzymy nowy
                var nowyMarker = new DayMarker
                {
                    EmployeeId = request.EmployeeId,
                    Date = request.Date.Date,
                    Type = request.Type,
                    Note = request.Note,
                    CreatedBy = aktualnyUzytkownik.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DayMarkers.Add(nowyMarker);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveDayMarker([FromBody] RemoveDayMarkerRequest request)
        {
            var aktualnyUzytkownik = await PobierzAktualnegoUzytkownika();
            var pracownik = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == aktualnyUzytkownik.Id);

            if (pracownik == null && !CzyMaUprawnienia(aktualnyUzytkownik.Role))
            {
                return Json(new { success = false, message = "Pracownik nie znaleziony" });
            }

            var marker = await _context.DayMarkers
                .FirstOrDefaultAsync(d => d.EmployeeId == request.EmployeeId && d.Date.Date == request.Date.Date);

            if (marker != null)
            {
                if (pracownik != null && marker.EmployeeId != pracownik.Id && !CzyMaUprawnienia(aktualnyUzytkownik.Role))
                {
                    return Json(new { success = false, message = "Brak uprawnień" });
                }

                _context.DayMarkers.Remove(marker);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
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

        private static DateTime PoczatekTygodnia(DateTime data, DayOfWeek startDnia)
        {
            int roznica = (7 + (data.DayOfWeek - startDnia)) % 7;
            return data.AddDays(-1 * roznica).Date;
        }
    }

    // klasy pomocnicze dla requestów
    public class AddEntryRequest
    {
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int? ProjectId { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateEntryRequest
    {
        public int Id { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int? ProjectId { get; set; }
        public string? Description { get; set; }
    }

    public class DeleteEntryRequest
    {
        public int Id { get; set; }
    }

    public class SetDayMarkerRequest
    {
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public DayType Type { get; set; }
        public string? Note { get; set; }
    }

    public class RemoveDayMarkerRequest
    {
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
    }
}
