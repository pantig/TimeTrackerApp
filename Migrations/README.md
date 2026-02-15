# Migracje bazy danych

## Automatyczne uruchamianie

Migracje SQL są uruchamiane automatycznie przy starcie aplikacji przez `MigrationRunner`.

## Ręczne uruchamianie

Jeśli potrzebujesz ręcznie uruchomić migrację:

### Linux/macOS:
```bash
sqlite3 timetracker.db < Migrations/20260215_AddClientAndClientIdToProject.sql
```

### Windows:
```powershell
Get-Content Migrations\20260215_AddClientAndClientIdToProject.sql | sqlite3 timetracker.db
```

### Z poziomu C#:
```csharp
using TimeTrackerApp.Migrations;

MigrationRunner.RunMigrations("Data Source=timetracker.db");
```

## Lista migracji

### 20260215_AddClientAndClientIdToProject.sql
**Opis:** Dodanie modułu Client

**Zmiany:**
- Utworzenie tabeli `Clients` z polami:
  - Id (PK)
  - Name (required)
  - Description, Email, Phone
  - Address, City, PostalCode, Country
  - NIP, IsActive
  - CreatedAt, UpdatedAt
- Dodanie kolumny `ClientId` do tabeli `Projects` (FK do Clients)
- Utworzenie domyślnego klienta dla istniejących projektów
- Indeksy dla wydajności

**Wymagane akcje po migracji:**
- Przypisz właściwych klientów do istniejących projektów
- Usuń lub zaktualizuj "Klienta domyślnego" jeśli nie jest potrzebny

### AddProjectManager.sql
**Opis:** Dodanie opiekuna projektu (Manager)

**Zmiany:**
- Dodanie kolumny `ManagerId` do tabeli `Projects`

## Historia migracji

Historia zastosowanych migracji jest przechowywana w tabeli `__MigrationHistory`:

```sql
SELECT * FROM __MigrationHistory ORDER BY AppliedAt DESC;
```

## Rozwiązywanie problemów

### Problem: "no such column"
**Przyczyna:** Baza danych nie została zaktualizowana

**Rozwiązanie:**
1. Zatrzymaj aplikację
2. Utwórz backup bazy: `cp timetracker.db timetracker.db.backup`
3. Uruchom aplikację ponownie (migracje uruchomią się automatycznie)
4. Lub uruchom migracje ręcznie (zobacz powyżej)

### Problem: "table already exists"
**Przyczyna:** Migracja została już zastosowana

**Rozwiązanie:**
- To jest OK! MigrationRunner pomija już zastosowane migracje
- Sprawdź: `SELECT * FROM __MigrationHistory;`

### Problem: Aplikacja nie uruchamia się po migracji
**Rozwiązanie:**
1. Sprawdź logi: `dotnet run`
2. Przywroć backup: `cp timetracker.db.backup timetracker.db`
3. Zgłoś issue z pełnymi logami

## Testowanie migracji

Testy schematu bazy danych znajdują się w:
```
TimeTrackerApp.Tests/IntegrationTests/DatabaseSchemaTests.cs
```

Uruchom:
```bash
dotnet test --filter DatabaseSchemaTests
```

Testy sprawdzają:
- Istnienie tabel (Clients, Projects, itp.)
- Istnienie kolumn (ClientId w Projects)
- Poprawność relacji (FK)
- Możliwość zapisu/odczytu danych
