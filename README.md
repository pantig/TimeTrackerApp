# TimeTrackerApp

System do rejestracji czasu pracy pracowników w projektach.

## Funkcjonalności

### Zarządzanie użytkownikami i pracownikami
- Trzystopniowy system ról: **Admin**, **Manager** (Kierownik), **Employee** (Pracownik)
- Panel zarządzania pracownikami (dostarcza Admin i Manager)
- Profile pracowników z informacjami: stanowisko, departament, data zatrudnienia

### Projekty
- Tworzenie i edycja projektów
- **Opiekun projektu** - każdy projekt musi mieć przypisanego Kierownika jako opiekuna
- Przypisywanie pracowników do projektów (relacja wiele-do-wielu)
- Budżet godzinowy i monitorowanie jego wykorzystania
- Statusy projektów: Aktywny, Planowanie, Wstrzymany, Zakończony
- Walidacja: pracownicy mogą rejestrować czas tylko w przypisanych im projektach

### Rejestracja czasu
- **Kalendarz tygodniowy** (widok siatki) - rejestracja czasu z dokładnością do minut
- Dodawanie wpisów czasu bezpośrednio w kalendarzu (AJAX)
- Edycja i usuwanie wpisów czasu
- Przydział wpisu do projektu (opcjonalnie)
- Markery dni: Urlop, Choroba, Urlop okolicznościowy, Szkolenie
- Pracownicy widzą tylko swój kalendarz; Admin/Manager widzą wszystkich

### Raporty
- **Raport miesięczny** (Excel) - szczegółowy czas pracy dla każdego pracownika
- Eksport do formatu Excel (.xlsx)
- Filtrowanie raportów po miesiącu i pracowniku

### Zabezpieczenia
- Autoryzacja oparta na rolach
- Haszowanie haseł (BCrypt)
- Walidacja po stronie serwera
- Pracownicy mają dostęp tylko do swoich danych

## Technologie

- **Backend**: ASP.NET Core 8.0 (MVC)
- **Baza danych**: SQLite + Entity Framework Core
- **Frontend**: Razor Views, CSS (custom), JavaScript (vanilla)
- **Eksport**: EPPlus (Excel)

## Struktura projektów

```
TimeTrackerApp/
├── Controllers/         # Kontrolery MVC
├── Data/                # DbContext i konfiguracja EF
├── Migrations/          # Migracje bazy danych
├── Models/              # Modele danych i ViewModels
├── Services/            # Logika biznesowa (np. generowanie raportów)
├── Views/               # Widoki Razor
├── wwwroot/             # Pliki statyczne (CSS, JS)
└── docs/                # Dokumentacja techniczna
```

## Dokumentacja

- [Opiekunowie projektów (Project Managers)](docs/FEATURE_PROJECT_MANAGERS.md)
- [Refactoring i uproszczenie kodu](docs/REFACTORING_SIMPLIFICATION.md)

## Instalacja i uruchomienie

### Wymagania
- .NET 8.0 SDK
- SQLite (wbudowane w projekt)

### Kroki

1. Klonowanie repozytorium:
```bash
git clone https://github.com/pantig/TimeTrackerApp.git
cd TimeTrackerApp
```

2. Przywracanie pakietów:
```bash
dotnet restore
```

3. Aktualizacja bazy danych (uruchomienie migracji):
```bash
dotnet ef database update
```

4. Uruchomienie aplikacji:
```bash
dotnet run
```

5. Otwórz przeglądarkę:
```
http://localhost:5000
```

### Domyślne konta (seed data)

Po pierwszym uruchomieniu system tworzy konta testowe:

- **Admin**: admin@test.pl / haslo123
- **Manager**: kierownik@test.pl / haslo123
- **Pracownik 1**: kamil@test.pl / haslo123
- **Pracownik 2**: piotr@test.pl / haslo123

## Branches

- `master` - wersja produkcyjna
- `feature/project-managers` - Opiekunowie projektów
- `refactor/code-simplification-and-polish` - Refactoring i uproszczenia
- `feature/weekly-calendar-ui-fixes` - Poprawki UI kalendarza
- `feature/monthly-reports-and-ui-improvements` - Raporty miesięczne
- `feature/employee-management-and-audit` - Zarządzanie pracownikami

## Autor

Projekt stworzony jako system zarządzania czasem pracy dla małych i średnich zespołów.

## Licencja

Projekt prywatny.
