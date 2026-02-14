# TimeTrackerApp - Instrukcja wdrożenia nowych funkcji

## Przegłąd zmian w branch `feature/monthly-reports-and-ui-improvements`

### 1. **Zarządzanie projektami**
- Nowa zakładka "Projekty" (Admin/Manager)
- CRUD operations: tworzenie, edycja, usuwanie projektów
- Budżet godzinowy projektów z wizualizacją wykorzystania
- Przypisywanie pracowników do projektów

### 2. **Filtrowanie projektów**
- Pracownicy widzą tylko projekty, do których są przypisani
- Admin/Manager widzą wszystkie projekty

### 3. **Raport miesięczny - Excel Export**
- Przycisk "Export Excel" w raporcie miesięcznym
- Format zgodność ze wzorem (plik załączony)
- Struktura: dane dzienne, podsumowanie godzin, projekty, statystyki

### 4. **Oznaczanie całych dni w kalendarzu**
- Delegacja (fioletowa poświata)
- Dzień wolny (szara poświata)
- Choroba (żółta poświata)
- Urlop (bladoróżowa poświata)
- Menu dropdown w nagłówku każdego dnia (przycisk •••)

---

## Instrukcja wdrożenia

### Krok 1: Pobierz branch
```bash
git checkout feature/monthly-reports-and-ui-improvements
git pull origin feature/monthly-reports-and-ui-improvements
```

### Krok 2: Zainstaluj pakiety
```bash
dotnet restore
```

### Krok 3: Usuń starą bazę danych (tylko rozwojowo!)
```bash
rm timetracker.db
```

### Krok 4: Utwórz migrację
```bash
dotnet ef migrations add AddProjectsAndDayMarkers
```

### Krok 5: Zaktualizuj bazę danych
```bash
dotnet ef database update
```

### Krok 6: Uruchom aplikację
```bash
dotnet run
```

### Krok 7: Sprawdź domyślne konta
Dane logowania są w `Data/DbInitializer.cs`:
- **Admin**: admin@timetracker.pl / Admin123!
- **Manager**: manager@timetracker.pl / Manager123!
- **Employee**: jan.kowalski@timetracker.pl / Employee123!

---

## Nowe modele w bazie danych

### `DayMarker`
- `Id` (int) - Primary Key
- `EmployeeId` (int) - Foreign Key
- `Date` (DateTime) - Data dnia
- `Type` (DayType enum) - Typ oznaczenia (1=Delegacja, 2=Dzień wolny, 3=Choroba, 4=Urlop)
- `Note` (string, nullable) - Opcjonalna notatka
- `CreatedBy` (int) - Foreign Key do User
- `CreatedAt` (DateTime) - Data utworzenia

### Zmiany w `Project`
- Dodano `HoursBudget` (decimal?, nullable) - Budżet godzinowy

### Zmiany w `TimeEntry`
- Rozszerzona walidacja: maksymalnie 24h/dzień, wpisy starsze niż 90 dni

---

## Nowe kontrolery i akcje

### `ProjectsController`
- `Index()` - Lista projektów
- `Create()` - Tworzenie projektu
- `Edit(int id)` - Edycja projektu
- `Delete(int id)` - Usuwanie projektu

### `CalendarController` (nowe akcje)
- `SetDayMarker()` - Ustawienie oznaczenia dnia
- `RemoveDayMarker()` - Usunięcie oznaczenia dnia

### `ReportsController` (nowe akcje)
- `ExportMonthlyExcel(int employeeId, int year, int month)` - Export raportu do Excel

---

## Nowe serwisy

### `ExcelExportService`
- `GenerateMonthlyReport()` - Generowanie raportu miesięcznego w formacie Excel
- Wykorzystuje EPPlus 7.0.0

---

## Testowanie funkcjonalności

### 1. Projekty
1. Zaloguj się jako Admin
2. Przejdź do zakładki "Projekty"
3. Utwórz nowy projekt z budżetem 160h
4. Przypisz pracowników (zaznacz checkboxy)
5. Sprawdź wizualizację wykorzystania budżetu

### 2. Filtrowanie projektów
1. Zaloguj się jako Employee (jan.kowalski@timetracker.pl)
2. Przejdź do Kalendarza
3. Spróbuj dodać wpis - lista projektów powinna zawierać tylko przypisane projekty

### 3. Export Excel
1. Zaloguj się jako dowolny użytkownik
2. Przejdź do "Raport miesięczny"
3. Wybierz miesiąc
4. Kliknij przycisk "💾 Export Excel"
5. Sprawdź pobrany plik

### 4. Oznaczanie dni
1. Przejdź do Kalendarza
2. Kliknij przycisk "•••" w nagłówku dowolnego dnia
3. Wybierz "Delegacja" z menu
4. Sprawdź fioletowe tło dnia
5. Kliknij ponownie "•••" i wybierz "Usuń oznaczenie"

---

## Znane problemy i uwagi

1. **EPPlus License**: Aplikacja używa EPPlus w trybie NonCommercial (wymaga licencji komercyjnej dla użytku komercyjnego)
2. **Migracja danych**: Usuwanie starej bazy danych usuwa wszystkie dane - w produkcji użyj migracji
3. **Wydajność**: Dla dużej ilości projektów/pracowników, rozważ dodanie stronicowania

---

## Następne kroki (opcjonalne rozszerzenia)

- [ ] Powiadomienia email przy przekroczeniu budżetu projektu
- [ ] Eksport zbiorczy wielu raportów
- [ ] Statystyki wykorzystania czasu w Dashboard
- [ ] Historia zmian w projektach (audit log)
- [ ] API REST do integracji z zewnętrznymi systemami
