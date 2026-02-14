# Instrukcje aktualizacji bazy danych

## Po zaciągnięciu brancha feature/employee-management-and-audit

### Opcja 1: Automatyczna aktualizacja (SQLite)

Aplikacja automatycznie zaktualizuje bazę danych przy starcie dzięki `DbInitializer.Initialize()` w `Program.cs`.

Jednak jeśli masz istniejące dane w `TimeEntries` bez pola `CreatedBy`, musisz:

1. **Usunąć starą bazę danych:**
   ```bash
   rm timetracker.db
   ```

2. **Uruchomić aplikację ponownie:**
   ```bash
   dotnet run
   ```

Aplikacja utworzy nową bazę z aktualnymi polami.

### Opcja 2: Manualna migracja (zachowanie danych)

Jeśli chcesz zachować istniejące dane:

1. **Dodaj kolumny SQL ręcznie:**
   ```sql
   ALTER TABLE TimeEntries ADD COLUMN CreatedBy INTEGER NOT NULL DEFAULT 1;
   ALTER TABLE TimeEntries ADD COLUMN CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP;
   ```

2. **Zaktualizuj istniejące rekordy:**
   ```sql
   -- Ustaw CreatedBy na ID pierwszego admina dla wszystkich istniejących wpisów
   UPDATE TimeEntries 
   SET CreatedBy = (SELECT Id FROM Users WHERE Role = 2 LIMIT 1)
   WHERE CreatedBy = 1;
   ```

### Weryfikacja

Sprawdź strukturę tabeli:
```sql
PRAGMA table_info(TimeEntries);
```

Powinny pojawić się kolumny:
- `CreatedBy` (INTEGER, NOT NULL)
- `CreatedAt` (TEXT, NOT NULL)

---

## Zmiany w modelu

### TimeEntry
- **Dodano:** `CreatedBy` (int) - ID użytkownika, który utworzył wpis
- **Dodano:** `CreatedByUser` (User) - relacja do użytkownika
- **Dodano:** `CreatedAt` (DateTime) - timestamp utworzenia

### Kontrolery
- **CalendarController.AddEntry** - ustawia `CreatedBy = userId`
- **TimeEntriesController.Create** - ustawia `CreatedBy = userId`

### Widoki
- **Calendar/Index.cshtml** - wyświetla "✍️ CreatedBy" na wpisach
- **TimeEntries/Index.cshtml** - pokazuje "Dodane przez" w tabeli
