# Opiekunowie Projektów (Project Managers)

## Cel funkcjonalności

Każdy projekt w systemie musi mieć przypisanego **opiekuna projektu**. Opiekunem może być wyłącznie użytkownik z rolą **Manager** (Kierownik).

## Wymagania biznesowe

1. **Każdy projekt MUSI mieć opiekuna** - pole `ManagerId` jest wymagane
2. **Opiekunem może być tylko Kierownik** - walidacja na poziomie kontrolera
3. **Istniejące projekty** - podczas migracji przypisano pierwszego dostępnego kierownika

## Zmiany w bazie danych

### Model `Project`

```csharp
public class Project
{
    // ... istniejące pola ...
    
    // NOWE POLE
    [Required(ErrorMessage = "Opiekun projektu jest wymagany")]
    public int ManagerId { get; set; }
    
    public Employee? Manager { get; set; }  // relacja do Employee (Manager)
}
```

### Konfiguracja w `ApplicationDbContext`

```csharp
// Project - Manager (wiele:1)
modelBuilder.Entity<Project>()
    .HasOne(p => p.Manager)
    .WithMany()
    .HasForeignKey(p => p.ManagerId)
    .OnDelete(DeleteBehavior.Restrict);
```

**Wazne**: `OnDelete(DeleteBehavior.Restrict)` - nie można usunąć kierownika który jest opiekunem projektu.

### Migracja SQL

```sql
-- Dodanie kolumny ManagerId
ALTER TABLE Projects ADD COLUMN ManagerId INTEGER NOT NULL DEFAULT 0;

-- Przypisanie pierwszego dostępnego kierownika do istniejących projektów
UPDATE Projects 
SET ManagerId = (
    SELECT e.Id 
    FROM Employees e 
    INNER JOIN Users u ON e.UserId = u.Id 
    WHERE u.Role = 1  -- UserRole.Manager
    LIMIT 1
)
WHERE ManagerId = 0;

-- Utworzenie indeksu dla wydajności
CREATE INDEX IX_Projects_ManagerId ON Projects(ManagerId);

-- Dodanie klucza obcego
CREATE INDEX FK_Projects_Employees_ManagerId ON Projects(ManagerId);
```

## Zmiany w kontrolerze

### `ProjectsController`

#### `Create` (GET)
```csharp
// Pobranie tylko kierowników dla pola opiekuna
var kierownicy = await _context.Employees
    .Include(e => e.User)
    .Where(e => e.IsActive && e.User.Role == UserRole.Manager)
    .ToListAsync();

ViewBag.Managers = kierownicy;
```

#### `Create` (POST)
```csharp
// Walidacja - sprawdzenie czy wybrany manager jest kierownikiem
var manager = await _context.Employees
    .Include(e => e.User)
    .FirstOrDefaultAsync(e => e.Id == model.ManagerId);

if (manager == null || manager.User.Role != UserRole.Manager)
{
    ModelState.AddModelError("ManagerId", "Opiekunem projektu może być tylko kierownik.");
    // ... zwrot do widoku z błędem
}
```

#### `Edit` (GET & POST)
Analogicznie jak w `Create` - pobieranie kierowników i walidacja.

#### `Index`
```csharp
// Ładowanie opiekuna projektu
var projekty = await _context.Projects
    .Include(p => p.Manager)
        .ThenInclude(m => m.User)  // potrzebne do wyświetlenia imienia/nazwiska
    .ToListAsync();
```

## Zmiany w widokach

### `Views/Projects/Create.cshtml` i `Edit.cshtml`

```html
<div class="form-group">
    <label asp-for="ManagerId">Opiekun projektu (Kierownik) *</label>
    <select asp-for="ManagerId" class="form-control" required>
        <option value="">-- Wybierz opiekuna --</option>
        @foreach (var manager in ViewBag.Managers)
        {
            <option value="@manager.Id">@manager.User.FirstName @manager.User.LastName (@manager.Position)</option>
        }
    </select>
    <span asp-validation-for="ManagerId" class="text-danger"></span>
</div>
```

### `Views/Projects/Index.cshtml`

Dodano kolumnę "Opiekun" w tabeli:

```html
<th>Opiekun</th>
...
<td>
    @if (project.Manager != null && project.Manager.User != null)
    {
        <span>@project.Manager.User.FirstName @project.Manager.User.LastName</span>
        <div style="color: var(--text-tertiary); font-size: 0.875rem;">@project.Manager.Position</div>
    }
    else
    {
        <span style="color: var(--text-tertiary);">Brak opiekuna</span>
    }
</td>
```

## Testowanie

### Przypadki testowe

1. **Utworzenie nowego projektu z opiekunem**
   - Wybierz kierownika z listy
   - Sprawdź czy projekt został utworzony z przypisanym opiekunem

2. **Próba utworzenia projektu bez opiekuna**
   - Pozostaw pole "Opiekun projektu" puste
   - Sprawdź czy pojawia się komunikat błędu

3. **Edycja istniejącego projektu - zmiana opiekuna**
   - Zmień opiekuna na innego kierownika
   - Sprawdź czy zmiana została zapisana

4. **Lista projektów**
   - Sprawdź czy kolumna "Opiekun" wyświetla poprawne dane
   - Sprawdź czy widoczne są: imię, nazwisko i stanowisko

5. **Istniejące projekty po migracji**
   - Sprawdź czy wszystkie istniejące projekty mają przypisanego opiekuna

## Ograniczenia i uwagi

1. **Usuwanie kierownika** - nie można usunąć kierownika, który jest opiekunem jakiegokolwiek projektu (`DeleteBehavior.Restrict`)
2. **Lista rozwijana** - wyświetla tylko aktywnych kierowników
3. **Migracja** - istniejące projekty otrzymują pierwszego dostępnego kierownika (trzeba ręcznie dopasować w razie potrzeby)
4. **Walidacja po stronie serwera** - nawet jeśli ktoś obejdzie walidację front-endu, kontroler sprawdza rolę

## Commit history

1. `09545002` - Dodanie pola Manager do modelu Project
2. `8a643005` - Konfiguracja relacji w DbContext
3. `49438f37` - Migracja SQL (dodanie kolumny + przypisanie domyślnego)
4. `1b80297e` - Aktualizacja ProjectsController (walidacja)
5. `bf41163c` - Widok Create
6. `b09c1c7c` - Widok Edit
7. `0a017438` - Widok Index (kolumna z opiekunem)
8. Dokumentacja (ten plik)

## Autor

Funkcjonalność zaimplementowana na branchu `feature/project-managers`.
