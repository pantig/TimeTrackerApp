# Funkcjonalność: Wpisy bez projektu

## Opis

System został rozszerzony o funkcjonalność zarządzania wpisami czasu pracy, które nie mają przypisanego projektu.

## Implementowane wymagania

### 1. 🟡 Wizualizacja wpisów bez projektu (Warning)

**Gdzie:** Kalendarz i widok "Wpisy"

**Zachowanie:**
- Bloczek w kalendarzu bez przypisanego projektu ma **ciemnożółty gradient** (warning color: `#eab308`)
- Wpis w tabeli "Wpisy" ma **żółte obramowanie** po lewej stronie + lekkie tło
- CSS klasy:
  - Kalendarz: `.timegrid-entry[data-project-id=""]`
  - Tabela: `.no-project-entry-row`

**Implementacja:**
```css
.timegrid-entry[data-project-id=""] {
  background: linear-gradient(135deg, rgba(234, 179, 8, 0.85) 0%, rgba(234, 179, 8, 0.95) 100%) !important;
  border-left: 3px solid #ca8a04 !important;
}

.no-project-entry-row {
  border-left: 3px solid #eab308;
  background: rgba(234, 179, 8, 0.05);
}
```

---

### 2. 📄 Raport kierownika - wszystkie wpisy bez projektów

**Endpoint:** `/NoProjectReport/AllEntries`

**Dostęp:** Tylko `Admin` i `Manager`

**Funkcjonalność:**
- Wyświetla **wszystkie** wpisy bez projektu wszystkich pracowników
- Filtorwanie po pracowniku (dropdown)
- Możliwość przypisania projektu do każdego wpisu
- Statystyki:
  - Razem godzin bez projektu
  - Liczba wpisów
  - Liczba pracowników z brakującymi projektami

**UI:**
- Tabela z kolumnami: Pracownik, Data, Czas, Godziny, Opis, Projekt (dropdown), Akcja
- Przycisk "Przypisz" dla każdego wpisu

**Link w nawigacji:**
```html
<a asp-controller="NoProjectReport" asp-action="AllEntries" style="color: #eab308;">
    ⚠️ Wszystkie bez projektu
</a>
```

---

### 3. 👤 Raport pracownika - własne wpisy bez projektów

**Endpoint:** `/NoProjectReport/MyEntries`

**Dostęp:** Wszyscy zalogowani użytkownicy

**Funkcjonalność:**
- Pracownik widzi **tylko swoje** wpisy bez projektu
- Możliwość uzupełnienia brakujących projektów
- Statystyki:
  - Razem godzin bez projektu
  - Liczba wpisów
  - Liczba dni

**UI:**
- Podobna tabela jak dla kierownika, ale tylko własne wpisy
- Komunikat sukcesu gdy wszystkie projekty są uzupełnione

**Link w nawigacji:**
```html
<a asp-controller="NoProjectReport" asp-action="MyEntries" style="color: #eab308;">
    ⚠️ Brak projektu
</a>
```

---

### 4. 🚫 Blokada exportu raportu miesięcznego

**Endpoint:** `/Reports/ExportMonthlyExcel`

**Zachowanie:**
- Przed exportem sprawdzana jest obecność wpisów bez projektu dla danego pracownika
- Jeśli istnieją wpisy bez projektu:
  - **Export jest zablokowany**
  - Wyświetlany jest komunikat błędu w `TempData["ErrorMessage"]`
  - Użytkownik jest przekierowywany do widoku raportu miesięcznego

**Komunikat błędu:**
```
Nie możesz wyeksportować raportu - istnieją wpisy bez przypisanego projektu. 
Uzupełnij je w zakładce '⚠️ Brak projektu'.
```

**Implementacja:**
```csharp
var hasEntriesWithoutProject = await _context.TimeEntries
    .AnyAsync(e => e.EmployeeId == employeeId && e.ProjectId == null);

if (hasEntriesWithoutProject)
{
    TempData["ErrorMessage"] = "Nie możesz wyeksportować raportu...";
    return RedirectToAction("Monthly", new { employeeId, year, month });
}
```

---

## Architektura

### Kontroler: `NoProjectReportController`

**Akcje:**

1. **`MyEntries()`** - GET
   - Pobiera wpisy bez projektu dla zalogowanego pracownika
   - Zwraca widok z listą wpisów i dostępnych projektów

2. **`AllEntries(int? employeeId)`** - GET
   - [Authorize(Roles = "Admin,Manager")]
   - Pobiera wszystkie wpisy bez projektu (opcjonalnie filtrowane po pracowniku)
   - Zwraca widok z listą wpisów, pracowników i projektów

3. **`AssignProject([FromBody] AssignProjectRequest request)`** - POST
   - Przypisuje projekt do wpisu
   - Waliduje uprawnienia:
     - Pracownik może przypisywać tylko do swoich wpisów
     - Manager/Admin może przypisywać do wszystkich
   - Zwraca JSON: `{ success: true/false, message?: string }`

### ViewModel: `NoProjectEntriesViewModel`

```csharp
public class NoProjectEntriesViewModel
{
    public List<TimeEntry> Entries { get; set; }
    public List<Project> AvailableProjects { get; set; }
    public List<Employee>? AllEmployees { get; set; }
    public int? SelectedEmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public bool IsManagerView { get; set; }
    
    public decimal TotalHours => Entries.Sum(e => e.TotalHours);
    public int TotalDays => Entries.Select(e => e.EntryDate.Date).Distinct().Count();
}
```

---

## Testy

### Testy jednostkowe: `NoProjectReportControllerTests`

**Scenariusze:**

1. ✅ `MyEntries_ReturnsViewWithEntriesWithoutProject`
   - Zwraca tylko wpisy bez projektu dla zalogowanego pracownika

2. ✅ `MyEntries_WithoutEmployeeProfile_RedirectsWithError`
   - Redirect gdy brak profilu pracownika

3. ✅ `AllEntries_AsManager_ReturnsViewWithAllEntriesWithoutProject`
   - Manager widzi wszystkie wpisy bez projektu

4. ✅ `AllEntries_WithEmployeeFilter_ReturnsFilteredEntries`
   - Filtrowanie po pracowniku działa poprawnie

5. ✅ `AssignProject_WithValidData_AssignsProjectSuccessfully`
   - Przypisanie projektu zapisuje się w bazie

6. ✅ `AssignProject_WithNonExistentEntry_ReturnsFailure`
   - Błąd dla nieistniejącego wpisu

7. ✅ `AssignProject_ToOtherEmployeeEntry_AsEmployee_ReturnsForbidden`
   - Pracownik nie może przypisywać do cudzych wpisów

8. ✅ `TotalHours_CalculatesCorrectly`
   - Suma godzin jest poprawnie obliczana

9. ✅ `TotalDays_CalculatesCorrectly`
   - Liczba dni jest poprawnie obliczana

### Testy integracyjne: `NoProjectReportTests`

1. ✅ `MyEntries_AsEmployee_ReturnsMyEntriesWithoutProject`
2. ✅ `AllEntries_AsManager_ReturnsAllEntriesWithoutProject`
3. ✅ `AllEntries_AsEmployee_IsForbidden`
4. ✅ `AssignProject_WithValidData_AssignsProjectSuccessfully`
5. ✅ `AssignProject_ToOtherEmployeeEntry_AsEmployee_IsForbidden`
6. ✅ `MyEntries_WithoutAuth_RedirectsToLogin`

### Testy blokady exportu: `ExportBlockTests`

1. ✅ `ExportMonthlyExcel_WithEntriesWithoutProject_IsBlocked`
   - Export jest zablokowany gdy są wpisy bez projektu

2. ✅ `ExportMonthlyExcel_WithAllProjectsAssigned_AllowsExport`
   - Export działa gdy wszystkie wpisy mają projekt

3. ✅ `ExportMonthlyExcel_AfterAssigningProject_AllowsExport`
   - Po uzupełnieniu projektów export jest odblokowany

---

## Przepływ użytkownika

### Pracownik

1. Pracownik tworzy wpis czasu bez przypisania projektu
2. W kalendarzu wpis ma **żółty kolor** (warning)
3. W nawigacji pojawia się link "⚠️ Brak projektu"
4. Pracownik klika link i widzi listę swoich wpisów bez projektu
5. Wybiera projekt z dropdown i klika "Przypisz"
6. Wpis zostaje zaktualizowany, kolor zmienia się na standardowy niebieski
7. Pracownik może teraz wyeksportować raport miesięczny

### Kierownik

1. Kierownik widzi w nawigacji "⚠️ Wszystkie bez projektu"
2. Po kliknięciu widzi listę wszystkich wpisów bez projektu
3. Może filtrować po pracowniku
4. Przypisuje projekty do wpisów pracowników
5. Pomaga pracownikom uzupełnić brakujące dane

---

## Bezpieczeństwo

### Autoryzacja

- `MyEntries` - dostępne dla wszystkich zalogowanych
- `AllEntries` - tylko dla `Admin` i `Manager`
- `AssignProject` - walidacja właściciela wpisu

### Walidacja

- Sprawdzanie czy wpis należy do pracownika (dla role=Employee)
- Sprawdzanie czy pracownik jest przypisany do projektu
- Sprawdzanie istnienia wpisu przed przypisaniem

---

## Zmiany w istniejących plikach

### `wwwroot/css/site.css`
- Dodano style `.timegrid-entry[data-project-id=""]`
- Dodano style `.no-project-entry-row`

### `Views/Shared/_Layout.cshtml`
- Dodano link "⚠️ Brak projektu" dla pracowników
- Dodano link "⚠️ Wszystkie bez projektu" dla managerów

### `Controllers/ReportsController.cs`
- Dodano walidację w `ExportMonthlyExcel` sprawdzającą wpisy bez projektu
- Dodano komunikat TempData przy próbie exportu z brakami

---

## Podsumowanie

✅ **Funkcjonalność 1:** Wizualizacja wpisów bez projektu (kolor warning)
✅ **Funkcjonalność 2:** Raport dla kierownika z możliwością uzupełniania
✅ **Funkcjonalność 3:** Raport dla pracownika z możliwością uzupełniania
✅ **Funkcjonalność 4:** Blokada exportu przy brakujących projektach
✅ **Testy:** Pełne pokrycie testami jednostkowymi i integracyjnymi

Wszystkie wymagania zostały zaimplementowane i objęte testami! 🎉
