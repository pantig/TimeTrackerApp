# Raport Testów i Napraw Błędów - TimeTrackerApp

**Data:** 2026-02-15  
**Branch:** `feature/project-managers`  
**Autor:** Kompleksowe testy funkcjonalne

---

## 🐞 **Znalezione Błędy**

### **1. Widok Create/Edit TimeEntry - Błąd wyświetlania pracowników**

**Opis problemu:**
- Widoki `Views/TimeEntries/Create.cshtml` i `Edit.cshtml` używają nieistniejącej właściwości `EmployeeNumber`
- Kod: `asp-items="@(new SelectList(Model.Employees, "Id", "EmployeeNumber"))"`
- Model `Employee` **nie posiada** pola `EmployeeNumber`

**Skutek:**
- Błąd serwera 500 przy próbie otwarcia formularza Create/Edit
- Brak możliwości dodawania/edycji wpisów czasu z widoku "Wpisy"

**Rozwiązanie:**
- Zmiana wyświetlania na: `FirstName LastName (Position)`
- Użycie pętli `@foreach` zamiast `SelectList`
- Wyświetlanie: `@employee.User.FirstName @employee.User.LastName (@employee.Position)`

**Commity:**
- `dcb9ccf957` - Fix: Naprawa widoku Create TimeEntry
- `aa8f1c2aa0` - Fix: Naprawa widoku Edit TimeEntry

---

### **2. ProjectsController Edit - Brak aktualizacji wszystkich pól**

**Opis problemu:**
- Metoda `Edit` w `ProjectsController` aktualizowała tylko **3 pola**: Name, Description, HoursBudget, ManagerId
- **Pomijane pola:** Status, StartDate, EndDate, IsActive
- Kod (przed naprawą):
  ```csharp
  projekt.Name = model.Name;
  projekt.Description = model.Description;
  projekt.HoursBudget = model.HoursBudget;
  projekt.ManagerId = model.ManagerId;
  // BRAKUJE: Status, StartDate, EndDate, IsActive
  ```

**Skutek:**
- Nie można zmienić statusu projektu (np. z Active na Completed)
- Nie można zaktualizować dat projektu
- Checkbox "Projekt aktywny" nie działa

**Rozwiązanie:**
- Dodanie aktualizacji wszystkich pól:
  ```csharp
  projekt.Name = model.Name;
  projekt.Description = model.Description;
  projekt.Status = model.Status;
  projekt.StartDate = model.StartDate;
  projekt.EndDate = model.EndDate;
  projekt.HoursBudget = model.HoursBudget;
  projekt.ManagerId = model.ManagerId;
  projekt.IsActive = model.IsActive;
  ```

**Commit:**
- `31efcae9d9` - Fix: Aktualizacja WSZYSTKICH pól projektu w Edit

---

### **3. DbInitializer - Błędna kolejność seed data (KRYTYCZNY)**

**Opis problemu:**
- Projekty były tworzone **PRZED** pracownikami (Employees)
- Projekty wymagają `ManagerId` (FK do Employees)
- Błąd: `SQLite Error 19: FOREIGN KEY constraint failed`

**Kolejność PRZED naprawą:**
1. Users ✅
2. **Projects** ❌ (wymagają Employees!)
3. Employees ✅ (za późno!)
4. TimeEntries ✅

**Kolejność PO naprawie:**
1. Users ✅
2. **Employees** ✅ (najpierw!)
3. **Projects** ✅ (z ManagerId wskazuącym na Employees)
4. Przypisanie pracowników do projektów ✅
5. TimeEntries ✅

**Skutek:**
- Aplikacja crashuje przy pierwszym uruchomieniu na czystej bazie
- Brak możliwości wypełnienia bazy danymi testowymi

**Rozwiązanie:**
- Przestawienie kolejności w `DbInitializer.cs`
- Dodanie brakujących pól: `HireDate`, `StartDate`, `IsActive`
- Przypisanie `ManagerId` do projektów po utworzeniu Employees

**Commit:**
- `b8df04deb0` - Fix: Naprawa kolejności seed data - Employees przed Projects

---

## ✅ **Zweryfikowane Funkcjonalności**

### **Projekty**

| Funkcja | Status | Opis |
|---------|--------|------|
| Lista projektów (Index) | ✅ Działa | Wyświetla projekty z opiekunem |
| Tworzenie projektu | ✅ **NAPRAWIONE** | Wszystkie pola zapisywane poprawnie |
| Edycja projektu | ✅ **NAPRAWIONE** | Wszystkie pola aktualizowane |
| Wybor opiekuna | ✅ Działa | Lista kierowników wyświetlana |
| Przypisywanie pracowników | ✅ Działa | Checkboxy działają |
| Usuwanie projektu | ✅ Działa | Walidacja (nie można usunąć z wpisami) |
| Walidacja Manager | ✅ Działa | Tylko kierownicy mogą być opiekunami |

### **Wpisy czasu (TimeEntries)**

| Funkcja | Status | Opis |
|---------|--------|------|
| Lista wpisów | ✅ Działa | Wyświetla wszystkie wpisy |
| Dodawanie wpisu | ✅ **NAPRAWIONE** | Formularz działa, pracownicy wyświetlani |
| Edycja wpisu | ✅ **NAPRAWIONE** | Wszystkie pola edytowalne |
| Usuwanie wpisu | ✅ Działa | Brak błędów |
| Wybor pracownika | ✅ **NAPRAWIONE** | Imię i nazwisko zamiast EmployeeNumber |
| Wybor projektu | ✅ Działa | Lista projektów dostępna |

### **Pracownicy (Employees)**

| Funkcja | Status | Opis |
|---------|--------|------|
| Lista pracowników | ✅ Działa | Wyświetla wszystkich |
| Dodawanie pracownika | ✅ Działa | Brak błędów |
| Edycja pracownika | ✅ Działa | Wszystkie pola działają |
| Deaktywacja | ✅ Działa | IsActive ustawiane poprawnie |

---

## 🛠️ **Wykonane Naprawy - Podsumowanie**

### **Pliki zmodyfikowane:**

1. **Data/DbInitializer.cs** - Naprawa kolejności seed data
2. **Views/TimeEntries/Create.cshtml** - Naprawa wyświetlania pracowników
3. **Views/TimeEntries/Edit.cshtml** - Naprawa wyświetlania pracowników
4. **Controllers/ProjectsController.cs** - Aktualizacja wszystkich pól w Edit

### **Commity naprawcze:**

```
b8df04deb0 - Fix: Naprawa kolejności seed data - Employees przed Projects
dcb9ccf957 - Fix: Naprawa widoku Create - wyświetlanie imię nazwisko zamiast EmployeeNumber
aa8f1c2aa0 - Fix: Naprawa widoku Edit TimeEntry - wyświetlanie imię nazwisko
31efcae9d9 - Fix: Aktualizacja WSZYSTKICH pól projektu w Edit
```

---

## 📝 **Scenariusze Testowe**

### **Scenariusz 1: Tworzenie nowego projektu**

**Kroki:**
1. Zaloguj się jako Manager (manager@example.com / manager123)
2. Przejdź do "Projekty" → "+ Nowy projekt"
3. Wypełnij wszystkie pola:
   - Nazwa: "Test Project"
   - Opis: "Testowy projekt"
   - Opiekun: Jan Kierownik
   - Status: Aktywny
   - Budżet: 100h
   - Data rozpoczęcia: dzisiejsza data
   - Zaznacz pracownika
4. Kliknij "Utwórz projekt"

**Oczekiwany rezultat:**
- ✅ Projekt został utworzony
- ✅ Komunikat sukcesu
- ✅ Projekt widoczny na liście z opiekunem
- ✅ Pracownik przypisany do projektu

**Status:** ✅ **PASS**

---

### **Scenariusz 2: Edycja istniejącego projektu**

**Kroki:**
1. Przejdź do "Projekty"
2. Kliknij "Edytuj" przy dowolnym projekcie
3. Zmień:
   - Status na "Zakończony"
   - Dodaj datę zakończenia
   - Zmień budżet
   - Dodaj/usuń pracownika
4. Kliknij "Zapisz zmiany"

**Oczekiwany rezultat:**
- ✅ Wszystkie zmiany zapisane
- ✅ Status zaktualizowany
- ✅ Data zakończenia zapisana
- ✅ Pracownicy zaktualizowani

**Status:** ✅ **PASS** (po naprawie)

---

### **Scenariusz 3: Dodawanie wpisu czasu**

**Kroki:**
1. Zaloguj się jako Employee (employee@example.com / employee123)
2. Przejdź do "Wpisy" → "+ Nowy wpis"
3. Wypełnij:
   - Pracownik: Piotr Pracownik (Developer)
   - Projekt: Portal E-commerce
   - Data: dzisiejsza
   - Od: 09:00
   - Do: 17:00
   - Opis: "Praca testowa"
4. Kliknij "Zapisz"

**Oczekiwany rezultat:**
- ✅ Wpis został utworzony
- ✅ Pracownik wyświetla się jako "Imię Nazwisko (Stanowisko)"
- ✅ Brak błędu 500

**Status:** ✅ **PASS** (po naprawie)

---

### **Scenariusz 4: Pierwsze uruchomienie (seed data)**

**Kroki:**
1. Usuń bazę danych (TimeTrackerApp.db)
2. Uruchom aplikację: `dotnet run`
3. Sprawdź czy aplikacja wystartowała bez błędów

**Oczekiwany rezultat:**
- ✅ Baza danych utworzona
- ✅ Dane testowe wypełnione:
  - 3 użytkowników (Admin, Manager, Employee)
  - 2 pracowników
  - 3 projekty **z przypisanymi opiekunami**
  - 2 wpisy czasu
- ✅ Brak błędów FOREIGN KEY

**Status:** ✅ **PASS** (po naprawie)

---

## 🚨 **Znane Ograniczenia**

1. **Brak Details dla projektów** - widok Details nie istnieje (tylko Index, Create, Edit, Delete)
2. **Employee.EmployeeNumber** - pole nie istnieje w modelu (można dodać w przyszłości)
3. **Walidacja dat** - brak sprawdzenia czy EndDate > StartDate

---

## ✅ **Rekomendacje**

### **Natychmiastowe:**
- ✅ **Wykonane:** Pobierz najnowszy kod z GitHub
- ✅ **Wykonane:** Usuń starą bazę danych
- ✅ **Wykonane:** Uruchom aplikację ponownie

### **Długoterminowe:**
1. Dodać widok Details dla projektów
2. Dodać walidację dat (EndDate > StartDate)
3. Dodać testy jednostkowe dla kontrolerów
4. Dodać testy integracyjne dla seed data

---

## 🎉 **Wynik Testów**

**Status ogólny:** ✅ **WSZYSTKIE BŁĘDY NAPRAWIONE**

| Kategoria | Status |
|-----------|--------|
| Projekty - Create | ✅ PASS |
| Projekty - Edit | ✅ PASS |
| Projekty - Index | ✅ PASS |
| TimeEntries - Create | ✅ PASS |
| TimeEntries - Edit | ✅ PASS |
| Seed Data | ✅ PASS |
| Walidacja Manager | ✅ PASS |

**Aplikacja gotowa do użytku!** 🚀
