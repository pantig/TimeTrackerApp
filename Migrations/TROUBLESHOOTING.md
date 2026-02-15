# Rozwiązywanie problemów z migracjami

## Problem: "No such table: __MigrationHistory"

### Przyczyna
Migracje SQL nie zostały uruchomione przy starcie aplikacji.

### Rozwiązanie

#### 1. Sprawdź czy pliki .sql są kopiowane
```bash
# Zbuduj projekt
dotnet build

# Sprawdź czy pliki .sql są w katalogu output
ls -la bin/Debug/net8.0/Migrations/

# Powinny być:
# - 20260215_AddClientAndClientIdToProject.sql
# - AddProjectManager.sql
```

**Jeśli plików NIE MA:**
```bash
# Upewnij się że TimeTrackerApp.csproj zawiera:
<ItemGroup>
  <None Update="Migrations\*.sql">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>

# Wyczyść i przebuduj
dotnet clean
dotnet build
```

#### 2. Uruchom aplikację z logami
```bash
dotnet run

# Sprawdź logi - powinny zawierać:
# [INFO] Found migrations directory: /path/to/Migrations
# [INFO] Found 2 migration file(s):
#   - 20260215_AddClientAndClientIdToProject.sql
#   - AddProjectManager.sql
# [RUN] Applying migration: ...
```

**Jeśli widzisz: "[WARNING] Migrations directory not found"**
```bash
# Sprawdź ścieżki które są sprawdzane
# i utwórz katalog ręcznie jeśli trzeba

cd bin/Debug/net8.0
mkdir -p Migrations
cp ../../../Migrations/*.sql Migrations/
```

#### 3. Ręczne uruchomienie migracji

**Linux/macOS:**
```bash
# Utwórz backup
cp timetracker.db timetracker.db.backup

# Uruchom migrację
sqlite3 timetracker.db < Migrations/20260215_AddClientAndClientIdToProject.sql

# Sprawdź wynik
sqlite3 timetracker.db "SELECT * FROM __MigrationHistory;"
```

**Windows PowerShell:**
```powershell
# Utwórz backup
Copy-Item timetracker.db timetracker.db.backup

# Uruchom migrację
Get-Content Migrations\20260215_AddClientAndClientIdToProject.sql | sqlite3 timetracker.db

# Sprawdź wynik
sqlite3 timetracker.db "SELECT * FROM __MigrationHistory;"
```

---

## Problem: SQLite Error - "no such column"

### Przyczyna
Baza danych nie ma wymaganej kolumny (np. ClientId w Projects).

### Rozwiązanie

#### 1. Sprawdź strukturę bazy
```bash
sqlite3 timetracker.db "PRAGMA table_info(Projects);"

# Powinno zawierać kolumnę ClientId:
# 7|ClientId|INTEGER|1||0
```

#### 2. Sprawdź czy migracja była zastosowana
```bash
sqlite3 timetracker.db "SELECT * FROM __MigrationHistory;"

# Powinno zawierać:
# 20260215_AddClientAndClientIdToProject|2026-02-15T...
```

#### 3. Zastosuj migrację ręcznie (jeśli nie była)
```bash
# Backup
cp timetracker.db timetracker.db.backup

# Migracja
sqlite3 timetracker.db < Migrations/20260215_AddClientAndClientIdToProject.sql
```

---

## Problem: "table already exists"

### Przyczyna
Migracja próbuje utworzyć tabelę która już istnieje.

### Rozwiązanie
**To jest OK!** MigrationRunner powinien automatycznie pominąć już zastosowane migracje.

Jeśli problem występuje:
```bash
# 1. Sprawdź historię
sqlite3 timetracker.db "SELECT * FROM __MigrationHistory;"

# 2. Jeśli migracja NIE jest w historii, dodaj ręcznie:
sqlite3 timetracker.db "
INSERT INTO __MigrationHistory (MigrationId, AppliedAt) 
VALUES ('20260215_AddClientAndClientIdToProject', datetime('now'));
"
```

---

## Problem: Aplikacja crashuje przy starcie

### Debug krok po kroku

#### 1. Sprawdź logi startowe
```bash
dotnet run 2>&1 | grep -E "\[(INFO|ERROR|WARNING)\]"
```

#### 2. Testuj połączenie z bazą
```bash
sqlite3 timetracker.db "SELECT COUNT(*) FROM Users;"

# Jeśli działa, baza jest OK
```

#### 3. Sprawdź cały schemat
```bash
sqlite3 timetracker.db ".schema" > schema.txt
cat schema.txt

# Sprawdź czy zawiera:
# - CREATE TABLE "Clients"
# - ClientId w Projects
```

#### 4. Uruchom testy schematu
```bash
dotnet test --filter DatabaseSchemaTests

# Powinno przejść 5 testów
```

---

## Kompletne resetowanie bazy (OSTATECZNOŚĆ)

**UWAGA: Usunie wszystkie dane!**

```bash
# 1. Backup (na wszelki wypadek)
cp timetracker.db timetracker.db.full_backup

# 2. Usuń bazę
rm timetracker.db

# 3. Uruchom aplikację - utworzy nową bazę z migracjami
dotnet run
```

---

## Przydatne komendy diagnostyczne

```bash
# Lista wszystkich tabel
sqlite3 timetracker.db ".tables"

# Struktura określonej tabeli
sqlite3 timetracker.db "PRAGMA table_info(Projects);"

# Historia migracji
sqlite3 timetracker.db "SELECT * FROM __MigrationHistory ORDER BY AppliedAt;"

# Liczba klientów
sqlite3 timetracker.db "SELECT COUNT(*) FROM Clients;"

# Projekty bez klienta (powinno być 0)
sqlite3 timetracker.db "SELECT COUNT(*) FROM Projects WHERE ClientId IS NULL;"

# Kompletny schemat bazy
sqlite3 timetracker.db ".fullschema"
```

---

## Pomoc

Jeśli nadal występują problemy:

1. Uruchom diagnostykę:
```bash
dotnet run > app.log 2>&1
cat app.log | grep -E "\[(ERROR|WARNING)\]"
```

2. Zbierz informacje:
```bash
# Wersja .NET
dotnet --version

# Struktura projektów
ls -la bin/Debug/net8.0/Migrations/

# Zawartość bazy
sqlite3 timetracker.db ".tables"
```

3. Zgłoś issue z:
- Logami z app.log
- Wynikami powyższych komend
- Opisem problemu
