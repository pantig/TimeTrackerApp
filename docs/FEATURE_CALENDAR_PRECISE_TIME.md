# Feature: Precyzyjne wprowadzanie czasu w kalendarzu

**Branch:** `feature/calendar-precise-time-input`  
**Data:** 2026-02-15  
**Status:** ✅ Gotowe do testów

---

## 🎯 **Cel funkcjonalności**

Umożliwienie użytkownikom precyzyjnego wprowadzania i edycji czasu początku i końca wpisu czasowego w widoku kalendarza z dokładnością do **15 minut**, zamiast obecnego zaokrąglenia do pełnych godzin.

---

## ⚖️ **Porównanie: PRZED vs PO**

### **PRZED zmianami:**

❌ Kliknięcie i przeciągnięcie na siatce kalendarza:
- Zaznaczenie: 09:00 - 17:00 (pełne godziny)
- Modal wyświetla: **tylko tekst** "09:00 – 17:00"
- Brak możliwości zmiany czasu w modalu
- Aby ustawić np. 09:15 - 17:30, trzeba iść do widoku "Wpisy"

### **PO zmianach:**

✅ Kliknięcie i przeciągnięcie na siatce kalendarza:
- Zaznaczenie: nadal 09:00 - 17:00 (pełne godziny jako wstępna wartość)
- Modal wyświetla: **dwa input time**
  - **Od:** 09:00 (edytowalne!)
  - **Do:** 17:00 (edytowalne!)
- Można zmienić na: 09:15 - 17:30
- Automatyczne obliczanie i wyświetlanie czasu trwania: "8h 15min"
- Precyzja: **15 minut** (step="900" sekund)

---

## 🛠️ **Zmiany techniczne**

### **1. Views/Calendar/Index.cshtml** [cite:180]

**Przed:**
```html
<div class="form-group">
    <label>Czas</label>
    <div id="timeDisplay" class="time-display"></div>
</div>
```

**Po:**
```html
<div class="form-group">
    <label>Czas</label>
    <div style="display: grid; grid-template-columns: 1fr auto 1fr; gap: 0.75rem;">
        <div>
            <label>Od</label>
            <input type="time" id="entryStartTime" class="form-control" step="900" required />
        </div>
        <span>—</span>
        <div>
            <label>Do</label>
            <input type="time" id="entryEndTime" class="form-control" step="900" required />
        </div>
    </div>
    <div id="durationDisplay" style="..."></div>
</div>
```

**Kluczowe zmiany:**
- Zamiana statycznego tekstu na **dwa input[type="time"]**
- Atrybut `step="900"` = 15 minut (900 sekund)
- Dodanie funkcji `updateDurationDisplay()` - automatyczne obliczanie czasu trwania
- Walidacja: końcowa godzina musi być późniejsza niż początkowa

---

### **2. Controllers/CalendarController.cs** [cite:182]

**Przed:**
```csharp
public class UpdateEntryRequest
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public string? Description { get; set; }
    // BRAK pól StartTime i EndTime!
}
```

**Po:**
```csharp
public class UpdateEntryRequest
{
    public int Id { get; set; }
    public TimeSpan? StartTime { get; set; }  // ✅ DODANE
    public TimeSpan? EndTime { get; set; }    // ✅ DODANE
    public int? ProjectId { get; set; }
    public string? Description { get; set; }
}
```

**Zmiana w metodzie UpdateEntry:**
```csharp
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
```

---

## 🎓 **Funkcje dodane do JavaScript**

### **1. updateDurationDisplay()**
Automatyczne obliczanie i wyświetlanie czasu trwania wpisu:

```javascript
function updateDurationDisplay() {
    const startInput = document.getElementById('entryStartTime');
    const endInput = document.getElementById('entryEndTime');
    const durationDiv = document.getElementById('durationDisplay');
    
    const [startH, startM] = startInput.value.split(':').map(Number);
    const [endH, endM] = endInput.value.split(':').map(Number);
    
    const startMinutes = startH * 60 + startM;
    const endMinutes = endH * 60 + endM;
    const durationMinutes = endMinutes - startMinutes;
    
    if (durationMinutes <= 0) {
        durationDiv.textContent = '⚠️ Czas zakończenia musi być późniejszy';
        return;
    }
    
    const hours = Math.floor(durationMinutes / 60);
    const minutes = durationMinutes % 60;
    
    durationDiv.textContent = `Czas trwania: ${hours}h ${minutes}min`;
}
```

**Przykłady:**
- 09:00 → 17:00 = "Czas trwania: 8h"
- 09:15 → 17:45 = "Czas trwania: 8h 30min"
- 14:00 → 14:15 = "Czas trwania: 15min"

### **2. Walidacja czasu w saveEntry()**

```javascript
const [startH, startM] = document.getElementById('entryStartTime').value.split(':').map(Number);
const [endH, endM] = document.getElementById('entryEndTime').value.split(':').map(Number);
const startMinutes = startH * 60 + startM;
const endMinutes = endH * 60 + endM;

if (endMinutes <= startMinutes) {
    alert('Czas zakończenia musi być późniejszy niż czas rozpoczęcia');
    return;
}
```

---

## ✨ **Użytkowanie**

### **Scenariusz 1: Dodawanie nowego wpisu z precyzyjnym czasem**

1. Przejdź do widoku **Kalendarz**
2. Kliknij i przeciągnij na siatce (np. od 09:00 do 17:00)
3. Otworzy się modal z polami:
   - **Od:** 09:00 (możesz zmienić!)
   - **Do:** 17:00 (możesz zmienić!)
4. Zmień czas:
   - **Od:** 09:15
   - **Do:** 17:30
5. Automatycznie wyświetla się: "Czas trwania: 8h 15min"
6. Wybierz projekt i dodaj opis
7. Kliknij **Zapisz**

**Rezultat:** ✅ Wpis zapisany z czasem 09:15 - 17:30

---

### **Scenariusz 2: Edycja istniejącego wpisu**

1. Kliknij przycisk **✎** (ołówek) na istniejącym wpisie
2. Modal wyświetla obecne wartości:
   - **Od:** 09:00
   - **Do:** 17:00
3. Zmień godzinę końca na 17:45
4. Automatycznie wyświetla się: "Czas trwania: 8h 45min"
5. Kliknij **Zapisz**

**Rezultat:** ✅ Wpis zaktualizowany z nowym czasem

---

### **Scenariusz 3: Dodawanie krótkiego wpisu (15 min)**

1. Kliknij i przeciągnij krótki odcinek (np. 14:00 - 15:00)
2. W modalu zmień:
   - **Od:** 14:00
   - **Do:** 14:15
3. Wyświetla się: "Czas trwania: 15min"
4. Kliknij **Zapisz**

**Rezultat:** ✅ Wpis 15-minutowy zapisany poprawnie

---

## ✅ **Testy do wykonania**

### **Test 1: Dodawanie wpisu z pełnymi godzinami**
- [ ] Kliknij i przeciągnij 09:00 - 17:00
- [ ] Zostaw domyślne wartości
- [ ] Zapisz
- [ ] **Oczekiwany rezultat:** Wpis 09:00 - 17:00

### **Test 2: Dodawanie wpisu z czasem 15-minutowym**
- [ ] Kliknij i przeciągnij 09:00 - 10:00
- [ ] Zmień na 09:15 - 09:30
- [ ] **Oczekiwany rezultat:** Wpis 09:15 - 09:30 (15 min)

### **Test 3: Edycja istniejącego wpisu**
- [ ] Kliknij ✎ na wpisie 09:00 - 17:00
- [ ] Zmień czas końca na 17:45
- [ ] Zapisz
- [ ] **Oczekiwany rezultat:** Wpis zaktualizowany do 09:00 - 17:45

### **Test 4: Walidacja - końcowa < początkowa**
- [ ] Ustaw **Od:** 10:00, **Do:** 09:00
- [ ] Kliknij Zapisz
- [ ] **Oczekiwany rezultat:** Błąd walidacji + alert

### **Test 5: Wyświetlanie czasu trwania**
- [ ] Ustaw **Od:** 08:00, **Do:** 17:30
- [ ] **Oczekiwany rezultat:** Wyświetla "Czas trwania: 9h 30min"

### **Test 6: Precyzja 15 minut**
- [ ] Sprawdź czy pole time pozwala wybrać:
  - [x] 09:00
  - [x] 09:15
  - [x] 09:30
  - [x] 09:45
  - [ ] 09:10 (NIE powinno być dostępne)

---

## 📊 **Porównanie z widokiem "Wpisy"**

| Funkcja | Kalendarz (PRZED) | Kalendarz (PO) | Wpisy |
|---------|-------------------|----------------|-------|
| Precyzja czasu | Pełne godziny | **15 minut** | 1 minuta |
| Edycja czasu w modalu | ❌ Nie | ✅ **Tak** | ✅ Tak |
| Wyświetlanie czasu trwania | ❌ Nie | ✅ **Tak** | ❌ Nie |
| Walidacja czasu | ❌ Nie | ✅ **Tak** | ✅ Tak |
| Przeciąganie na siatce | ✅ Tak | ✅ Tak | ❌ Nie |

---

## 🔧 **Instrukcja merge'a**

### **1. Przetestuj branch:**
```bash
git checkout feature/calendar-precise-time-input
git pull origin feature/calendar-precise-time-input
dotnet run
```

### **2. Przetestuj wszystkie 6 testów powyżej**

### **3. Jeśli wszystko działa, merge do głównego brancha:**
```bash
git checkout feature/project-managers
git merge feature/calendar-precise-time-input
git push origin feature/project-managers
```

---

## 📝 **Pliki zmodyfikowane**

1. **Views/Calendar/Index.cshtml** - dodanie input[type="time"] z precyzją 15 min
2. **Controllers/CalendarController.cs** - obsługa StartTime/EndTime w UpdateEntry

**Commity:**
- `201733330e` - Feature: Dodanie precyzyjnego wprowadzania czasu w kalendarzu (15 min)
- `8a0e77fca5` - Feature: Obsługa precyzyjnej edycji czasu w UpdateEntry

---

## 🎉 **Podsumowanie**

✅ **Domyślne zaznaczenie:** Pełne godziny (kompatybilne wstecz)  
✅ **Precyzja manualna:** 15 minut (step="900")  
✅ **Automatyczne obliczenia:** Czas trwania wyświetlany na żywo  
✅ **Walidacja:** Końcowa > początkowa  
✅ **Edycja:** Zmiana czasu w istniejących wpisach  

**Użytkownicy mogą teraz rejestrować czas z precyzją 15 minut bezpośrednio z kalendarza!** 🚀
