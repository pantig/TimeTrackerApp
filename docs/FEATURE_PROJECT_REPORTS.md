# Raporty projektów

## Opis funkcjonalności

Raport projektu to szczegółowa analiza czasu pracy i zaangażowania zespołu w danym projekcie. Funkcjonalność jest analogiczna do raportów klientów i umożliwia:

- Przegląd podstawowych informacji o projekcie (status, opiekun, klient, daty)
- Podsumowanie statystyk projektu (liczba pracowników, godziny, wykorzystanie budżetu)
- Szczegółowy wykaz godzin zaraportowanych przez każdego pracownika
- Wizualizację udziału poszczólnych pracowników w projekcie

## Dostęp

- **Role**: Admin, Manager
- **Ścieżka**: `/Projects/Report/{id}`
- **Przyciski dostępu**: 
  - Widok `Projects/Index` - przycisk "📄 Raport" w kolumnie "Akcje"

## Struktura danych

### ProjectReportViewModel

```csharp
public class ProjectReportViewModel
{
    public Project Project { get; set; }
    public List<EmployeeTimeEntry> EmployeeTimeEntries { get; set; }
    public ProjectSummary Summary { get; set; }
}
```

### EmployeeTimeEntry

Przedstawia zsumowane dane o czasie pracy pracownika w projekcie:

- `EmployeeId` - ID pracownika
- `EmployeeName` - Pełne imię i nazwisko
- `Position` - Stanowisko
- `TotalHours` - Całkowita liczba godzin
- `EntriesCount` - Liczba wpisów czasowych
- `FirstEntry` - Data pierwszego wpisu
- `LastEntry` - Data ostatniego wpisu

### ProjectSummary

Podsumowanie statystyk projektu:

- `TotalEmployees` - Liczba przypisanych pracowników
- `ActiveEmployees` - Liczba pracowników z wpisami czasu
- `TotalHoursLogged` - Całkowita liczba godzin
- `HoursBudget` - Budżet godzinowy projektu
- `BudgetUsagePercentage` - Procent wykorzystania budżetu
- `TotalEntries` - Całkowita liczba wpisów
- `ProjectStartDate` - Data rozpoczęcia projektu
- `ProjectEndDate` - Data zakończenia projektu
- `DaysActive` - Liczba dni od pierwszego do ostatniego wpisu

## Implementacja

### Kontroler

Akcja `Report` w `ProjectsController.cs`:

1. Pobiera projekt z pełną nawigacją (Manager, Client, Employees, TimeEntries)
2. Grupuje wpisy czasu według pracowników
3. Oblicza statystyki dla każdego pracownika
4. Oblicza ogólne statystyki projektu
5. Przesyła dane do widoku przez ViewModel

### Widok

`Views/Projects/Report.cshtml` składa się z trzech głównych sekcji:

1. **Informacje o projekcie** - podstawowe dane (status, opiekun, klient, daty, opis)
2. **Podsumowanie** - statystyki w formie kolorowych kart (stat-box)
3. **Wykaz godzin pracowników** - tabela ze szczegółowymi danymi i wizualizacją udziału

## Sortowanie

Pracownicy w wykazie są sortowani według liczby godzin malejąco - pracownicy z największym wkładem wyświetlają się na początku listy.

## Wizualizacja

- **Paski postępu** - pokazują udział procentowy każdego pracownika
- **Kolorowe karty** - różne kolory dla różnych typów statystyk
- **Badges** - dla statusów i liczników
- **Stopka tabeli** - podsumowanie całkowitych godzin i wpisów

## Spójność z raportami klientów

Raport projektu został zaprojektowany analogicznie do raportu klienta (`Clients/Report`), zachowując:

- Podobną strukturę ViewModeli
- Spójny layout widoku
- Ten sam styl wizualny (kolory, karty, tabele)
- Podobną logikę biznesową w kontrolerze

## Pliki zmienione/dodane

1. `Models/ViewModels/ProjectReportViewModel.cs` - nowy ViewModel
2. `Controllers/ProjectsController.cs` - dodana akcja `Report`
3. `Views/Projects/Report.cshtml` - nowy widok raportu
4. `Views/Projects/Index.cshtml` - dodany przycisk "Raport"
5. `docs/FEATURE_PROJECT_REPORTS.md` - dokumentacja funkcjonalności

## Przykładowe użycie

1. Przejdź do zakładki **Projekty**
2. Kliknij przycisk **📄 Raport** przy wybranym projekcie
3. Zobaczysz:
   - Informacje o projekcie
   - Statystyki (liczba pracowników, godziny, wykorzystanie budżetu)
   - Tabelę z wyszczególnieniem godzin każdego pracownika

## Przyszłe rozszerzenia

Możliwe kierunki rozwoju:

- Eksport raportu do Excel
- Filtry czasowe (np. raport za ostatni miesiąc)
- Wykresy wizualizujące rozkład czasu
- Porównanie rzeczywistych godzin z planem
- Historia zmian w projekcie
