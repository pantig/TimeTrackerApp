# 🎯 Jak Uruchomić Testy w Rider IDE - Tutorial Krok po Kroku

Ten dokument nauczy Cię, jak uruchamiać i analizować testy funkcjonalne w JetBrains Rider.

## 📚 Spis Treści

1. [Przygotowanie Projektu](#1-przygotowanie-projektu)
2. [Pierwsze Uruchomienie Testów](#2-pierwsze-uruchomienie-testów)
3. [Uruchamianie Pojedynczych Testów](#3-uruchamianie-pojedynczych-testów)
4. [Debugowanie Testów](#4-debugowanie-testów)
5. [Analiza Wyników](#5-analiza-wyników)
6. [Pokrycie Kodu (Code Coverage)](#6-pokrycie-kodu-code-coverage)
7. [Skróty Klawiszowe](#7-skróty-klawiszowe)

---

## 1. Przygotowanie Projektu

### Krok 1.1: Otwórz Projekt w Rider

```bash
# Sklonuj repozytorium (jeśli jeszcze nie masz)
git clone https://github.com/pantig/TimeTrackerApp.git
cd TimeTrackerApp

# Przejdź na branch z testami
git checkout feature/calendar-precise-time-input
```

**W Rider:**
1. `File` → `Open...`
2. Wybierz folder `TimeTrackerApp`
3. Kliknij `OK`

### Krok 1.2: Zbuduj Solution

**Metoda A: Przez menu**
```
Build → Build Solution
```

**Metoda B: Skrót klawiszowy**
- Windows/Linux: `Ctrl + Shift + B`
- Mac: `⌘ + Shift + B`

⚠️ **Ważne:** Poczekaj aż build się zakończy. Zobaczysz komunikat w pasku u dołu:
```
Build: succeeded
```

### Krok 1.3: Przywracanie Pakietów NuGet

Jeśli widzisz błędy z brakującymi pakietami:

1. Kliknij prawy przycisk na Solution w Solution Explorer
2. Wybierz `Restore NuGet Packages`
3. Poczekaj na zakończenie

Lub użyj terminala:
```bash
dotnet restore
```

---

## 2. Pierwsze Uruchomienie Testów

### Krok 2.1: Otwórz Unit Tests Explorer

**Metoda A: Przez menu**
```
View → Tool Windows → Unit Tests
```

**Metoda B: Skrót klawiszowy**
- Windows/Linux: `Alt + 8`
- Mac: `⌘ + 8`

### Krok 2.2: Poczekaj na Wykrycie Testów

Rider automatycznie zeskanuje projekt i wykryje testy. Zobaczysz:

```
📂 TimeTrackerApp.Tests
  📂 IntegrationTests
    📋 AuthenticationTests (8 tests)
    📋 CalendarTests (6 tests)
    📋 ProjectTests (5 tests)
    📋 TimeEntryTests (8 tests)
```

⚠️ 

 **Jeśli nie widzisz testów:**
1. Upewnij się, że projekt się zbudował
2. Kliknij ikonkę 🔄 `Refresh` w Unit Tests window
3. Zrestartuj Rider

### Krok 2.3: Uruchom Wszystkie Testy

**W oknie Unit Tests:**

1. Znajdź zieloną ikonkę **▶️ Run All** u góry okna
2. Kliknij ją
3. Poczekaj na wykonanie testów (30-60 sekund)

**Zobaczysz progress bar:**
```
Running tests... [=========>    ] 15/27
```

### Krok 2.4: Interpretacja Wyników

Po zakończeniu zobaczysz podsumowanie:

```
✅ 27 passed
❌ 0 failed
⚠️ 0 skipped
⏱️ Duration: 45.3s
```

**Legenda ikon:**
- ✅ 🟢 Zielona - test przeszedł
- ❌ 🔴 Czerwona - test nie przeszedł
- ⚠️ 🟡 Żółta - test pominięty
- ⏱️ Czas wykonania

---

## 3. Uruchamianie Pojedynczych Testów

### Metoda A: Z Okna Unit Tests

1. **Rozwijaj drzewo testów:**
   ```
   ▼ TimeTrackerApp.Tests
     ▼ IntegrationTests
       ▼ AuthenticationTests
         ▶️ Login_WithValidCredentials_RedirectsToDashboard
   ```

2. **Kliknij prawy przycisk** na testu

3. **Wybierz opcję:**
   - `Run 'Login_WithValidCredentials...'` - uruchom test
   - `Debug 'Login_WithValidCredentials...'` - debuguj test
   - `Cover 'Login_WithValidCredentials...'` - test z coverage

### Metoda B: Z Edytora Kodu

1. **Otwórz plik testowy:**
   ```
   TimeTrackerApp.Tests/IntegrationTests/AuthenticationTests.cs
   ```

2. **Znajdź metodę testową:**
   ```csharp
   [Fact]  // ← Atrybut testowy
   public async Task Login_WithValidCredentials_RedirectsToDashboard()
   {
       // Arrange
       var loginData = new FormUrlEncodedContent(...);
       
       // Act
       var response = await Client.PostAsync("/Account/Login", loginData);
       
       // Assert
       response.StatusCode.Should().Be(HttpStatusCode.Redirect);
   }
   ```

3. **Zobaczysz zieloną ikonkę po lewej stronie** linii z `[Fact]`:
   - 🟢 Zielony trójkąt = gotowy do uruchomienia
   - ✅ Zielony check = ostatnio przeszedł
   - ❌ Czerwony X = ostatnio nie przeszedł

4. **Kliknij ikonkę** i wybierz:
   - `Run 'Login_WithValidCredentials...'`
   - `Debug 'Login_WithValidCredentials...'`
   - `Profile 'Login_WithValidCredentials...'`
   - `Cover 'Login_WithValidCredentials...'`

### Metoda C: Skróty Klawiszowe

1. **Ustaw kursor** wewnątrz metody testowej

2. **Naciśnij:**
   - `Ctrl + T, R` (Windows/Linux) - Run test
   - `Ctrl + T, D` (Windows/Linux) - Debug test
   - `⌘ + T, R` (Mac) - Run test
   - `⌘ + T, D` (Mac) - Debug test

---

## 4. Debugowanie Testów

### Krok 4.1: Ustaw Breakpoint

1. **Otwórz plik testowy**

2. **Znajdź linię, którą chcesz zbadac:**
   ```csharp
   var response = await Client.PostAsync("/Account/Login", loginData);  // ← Tutaj
   ```

3. **Kliknij na marginesie** (szary pasek po lewej) obok numeru linii
   - Pojawi się czerwona kropka 🔴

4. **Lub użyj skrótu:** `F9` (toggle breakpoint)

### Krok 4.2: Uruchom Test w Trybie Debug

**Metoda A:**
- Kliknij ikonkę 🐞 `Debug` obok nazwy testu

**Metoda B:**
- `Ctrl + T, D` (Windows/Linux)
- `⌘ + T, D` (Mac)

### Krok 4.3: Nawigacja w Debuggerze

Gdy test zatrzyma się na breakpoincie:

**Dostępne akcje:**

| Akcja | Skrót | Opis |
|-------|--------|------|
| **Resume Program** | `F9` | Kontynuuj do następnego breakpoint |
| **Step Over** | `F8` | Wykonaj linię i przejdź dalej |
| **Step Into** | `F7` | Wejdź do wywołanej metody |
| **Step Out** | `Shift + F8` | Wyjście z aktualnej metody |
| **Run to Cursor** | `Alt + F9` | Uruchom do linii z kursorem |
| **Evaluate Expression** | `Alt + F8` | Sprawdź wartość wyrażenia |

### Krok 4.4: Inspekcja Zmiennych

**Panel Variables (automatycznie widoczny podczas debug):**

Zobaczysz wszystkie lokalne zmienne:
```
📊 Variables
  ▼ this = AuthenticationTests
  ▼ loginData = FormUrlEncodedContent
    ▼ Headers
      Content-Type: "application/x-www-form-urlencoded"
  ▶ response = {StatusCode: 302 Found}
```

**Hover nad zmiennymi:**
- Najedzieś kursorem na `response`
- Zobaczysz quick preview z wartością

**Watches:**
1. Kliknij prawy na zmiennej → `Add to Watches`
2. Lub ręcznie dodaj w panelu `Watches`
3. Zmienna będzie śledzona przez całą sesję debug

### Krok 4.5: Console Output

**Panel Debug Console** pokazuje:
- Output z `Console.WriteLine()`
- Logi aplikacji
- Stack trace

```
Running test: Login_WithValidCredentials_RedirectsToDashboard
Sending POST request to /Account/Login
Received response: 302 Redirect
Assertion passed: StatusCode should be Redirect
Test passed in 234ms
```

---

## 5. Analiza Wyników

### Test Przeszedł (✅ Passed)

```
✅ Login_WithValidCredentials_RedirectsToDashboard (125ms)
```

**Co widoczne:**
- ✅ Zielony check
- Czas wykonania: `125ms`
- Status: `Passed`

### Test Nie Przeszedł (❌ Failed)

```
❌ Login_WithInvalidCredentials_ReturnsLoginPage (87ms)
  Expected: HttpStatusCode.OK
  Actual: HttpStatusCode.Redirect
  at AuthenticationTests.Login_WithInvalidCredentials_ReturnsLoginPage() 
     in AuthenticationTests.cs:line 42
```

**Co widoczne:**
- ❌ Czerwony X
- Komunikat błędu
- Oczekiwana vs rzeczywista wartość
- Stack trace z numerem linii

**Jak naprawić:**
1. Kliknij dwukrotnie na test w Unit Tests window
2. Rider otworzy plik i przeskoczy do linii 42
3. Przeanalizuj błąd
4. Napraw kod
5. Uruchom test ponownie

### Panel Test Results

**Zakładki:**
- **Output** - pełny output testu
- **Messages** - komunikaty z testu
- **Console** - console output

**Przykład Output:**
```
Test Name: Login_WithValidCredentials_RedirectsToDashboard
Test Duration: 0:00:00.125

Test Output:
Arranging test data...
Sending login request...
Received response: 302 Redirect
Assertion: StatusCode should be 302
✅ Assertion passed

Result: Passed
```

---

## 6. Pokrycie Kodu (Code Coverage)

### Krok 6.1: Uruchom Testy z Coverage

**Metoda A: Wszystkie testy**
1. W oknie Unit Tests kliknij ikonkę 🛡️ `Cover All Tests`
2. Lub: Prawy przycisk na projekcie testowym → `Cover Unit Tests`

**Metoda B: Pojedynczy test**
1. Prawy przycisk na teście → `Cover 'TestName'`

**Metoda C: Skrót**
- Windows/Linux: `Ctrl + Alt + K`
- Mac: `⌘ + ⌥ + K`

### Krok 6.2: Analiza Wyniku Coverage

**Otworzy się okno `Unit Test Coverage`:**

```
📈 Coverage Results

📁 TimeTrackerApp (Total: 78.5%)
  📂 Controllers (85.2%)
    📝 AccountController.cs (92.1%)
      ✅ Login() - 100%
      🟡 Register() - 85%
      ❌ ForgotPassword() - 0%
  📂 Services (71.3%)
    📝 TimeEntryService.cs (68.5%)
```

**Legenda kolorów:**
- 🟢 **Zielony (>80%)** - dobry coverage
- 🟡 **Żółty (50-80%)** - średni coverage
- 🔴 **Czerwony (<50%)** - niski coverage

### Krok 6.3: Podkreślenie w Kodzie

Rider automatycznie podkreśli kod:

**W edytorze zobaczysz:**

```csharp
public async Task<IActionResult> Login(LoginViewModel model)
{  // 🟢 Zielone tło - pokryte testami
    if (!ModelState.IsValid)
    {  // 🔴 Czerwone tło - NIE pokryte testami
        return View(model);
    }
    
    var user = await _userService.AuthenticateAsync(model.Username, model.Password);
    // 🟢 Zielone tło - pokryte testami
}
```

**Kolory marginesu:**
- 🟢 **Zielony pasek** = linia pokryta testami
- 🔴 **Czerwony pasek** = linia NIE pokryta testami
- ⚪ **Biały/szary** = linia niewykonywalna (komentarze, nawiasy)

### Krok 6.4: Szczegóły Pokrycia

**Hover nad liniją:**
```
This line was hit 5 times during test execution
```

**Kliknij na linii:**
- Zobaczysz które testy ją wykonały
```
Covered by:
  ✅ Login_WithValidCredentials_RedirectsToDashboard
  ✅ Login_WithInvalidCredentials_ReturnsLoginPage
```

### Krok 6.5: Eksport Raportu

1. W oknie `Unit Test Coverage` kliknij ikonkę 💾 `Export`
2. Wybierz format:
   - HTML Report
   - XML Report
   - JSON Report
3. Wybierz lokalizację
4. Kliknij `Save`

**Otwórz HTML Report:**
- Szczegółowy raport z wizualizacjami
- Można udostępnić zespołowi

---

## 7. Skróty Klawiszowe

### Podstawowe

| Akcja | Windows/Linux | Mac |
|-------|---------------|-----|
| Otwórz Unit Tests | `Alt + 8` | `⌘ + 8` |
| Run Test | `Ctrl + T, R` | `⌘ + T, R` |
| Debug Test | `Ctrl + T, D` | `⌘ + T, D` |
| Cover Test | `Ctrl + Alt + K` | `⌘ + ⌥ + K` |
| Rerun Last Test | `Ctrl + T, L` | `⌘ + T, L` |
| Run Failed Tests | `Ctrl + T, Y` | `⌘ + T, Y` |

### Debugowanie

| Akcja | Windows/Linux | Mac |
|-------|---------------|-----|
| Toggle Breakpoint | `F9` | `F9` |
| Resume | `F9` | `F9` |
| Step Over | `F8` | `F8` |
| Step Into | `F7` | `F7` |
| Step Out | `Shift + F8` | `Shift + F8` |
| Evaluate Expression | `Alt + F8` | `⌥ + F8` |

### Nawigacja

| Akcja | Windows/Linux | Mac |
|-------|---------------|-----|
| Go to Test | `Ctrl + Shift + T` | `⌘ + Shift + T` |
| Go to Implementation | `Ctrl + Alt + B` | `⌘ + ⌥ + B` |
| Find Usages | `Alt + F7` | `⌥ + F7` |

---

## 📝 Praktyczne Ćwiczenie

### Ćwiczenie 1: Uruchomienie Pierwszego Testu

1. ✅ Otwórz `Unit Tests` (`Alt + 8`)
2. ✅ Rozwijaj drzewo do `AuthenticationTests`
3. ✅ Uruchom `Login_WithValidCredentials_RedirectsToDashboard`
4. ✅ Sprawdź czy test przeszedł (✅)

### Ćwiczenie 2: Debugowanie Testu

1. ✅ Otwórz `AuthenticationTests.cs`
2. ✅ Ustaw breakpoint na linii z `Client.PostAsync()`
3. ✅ Uruchom test w trybie debug (`Ctrl + T, D`)
4. ✅ Poczekaj aż test zatrzyma się na breakpoincie
5. ✅ Zbadaj zmienną `loginData` w panelu Variables
6. ✅ Naciśnij `F8` (Step Over) aby przejść dalej
7. ✅ Zbadaj `response.StatusCode`
8. ✅ Naciśnij `F9` (Resume) aby dokończyć test

### Ćwiczenie 3: Analiza Coverage

1. ✅ Uruchom wszystkie testy z coverage (`Ctrl + Alt + K`)
2. ✅ Poczekaj na zakończenie
3. ✅ Otwórz okno `Unit Test Coverage`
4. ✅ Sprawdź % pokrycia dla `AccountController`
5. ✅ Kliknij na `AccountController.cs` w drzewie
6. ✅ Zobacz które linie są pokryte (zielone) a które nie (czerwone)

### Ćwiczenie 4: Naprawianie Failed Testu

1. ✅ Uruchom wszystkie testy
2. ✅ Jeśli któryś failuje (❌), kliknij dwukrotnie na niego
3. ✅ Przeczytaj komunikat błędu
4. ✅ Ustaw breakpoint w tescie
5. ✅ Debuguj test (`Ctrl + T, D`)
6. ✅ Znajdź przyczynę błędu
7. ✅ Napraw kod
8. ✅ Uruchom test ponownie
9. ✅ Sprawdź czy teraz przechodzi (✅)

---

## ❓ Częste Problemy i Rozwiązania

### Problem: Testy się nie uruchamiają

**Rozwiązanie:**
1. ✅ Upewnij się, że projekt się buduje (`Ctrl + Shift + B`)
2. ✅ Zresetuj cache: `File` → `Invalidate Caches / Restart`
3. ✅ Rebuild projektu: `Build` → `Rebuild All`
4. ✅ Sprawdź czy wszystkie pakiety NuGet są zainstalowane

### Problem: Rider nie wykrywa testów

**Rozwiązanie:**
1. ✅ Kliknij 🔄 `Refresh` w Unit Tests window
2. ✅ Sprawdź czy klasa testowa dziedziczy z `IntegrationTestBase`
3. ✅ Sprawdź czy metody mają atrybut `[Fact]`
4. ✅ Zrestartuj Rider

### Problem: Test failuje, ale nie wiem dlaczego

**Rozwiązanie:**
1. ✅ Przeczytaj komunikat błędu w Unit Tests window
2. ✅ Sprawdź Stack Trace
3. ✅ Uruchom test w trybie debug (`Ctrl + T, D`)
4. ✅ Ustaw breakpoint przed miejscem błędu
5. ✅ Krok po kroku przeanalizuj wykonanie (`F8`)

### Problem: Testy są wolne

**Rozwiązanie:**
1. ✅ Uruchom tylko potrzebne testy zamiast wszystkich
2. ✅ Użyj `Run Failed Tests` aby uruchomić tylko failed
3. ✅ Sprawdź czy testy nie czekają na timeout
4. ✅ Użyj parallel execution (domyślnie włączone w xUnit)

---

## 🎓 Gratulacje!

Też znasz już wszystkie podstawy uruchamiania testów w Rider IDE!

**Następne kroki:**
1. ✅ Przejrzyj wszystkie testy i zrozum co testują
2. ✅ Spróbuj napisać własny prosty test
3. ✅ Eksperymentuj z debuggerem
4. ✅ Monitoruj pokrycie kodu
5. ✅ Uruchamiaj testy przed każdym commitem

**Zasoby:**
- [xUnit Documentation](https://xunit.net/)
- [Rider Testing Guide](https://www.jetbrains.com/help/rider/Unit_Testing.html)
- [FluentAssertions Docs](https://fluentassertions.com/)

---

**Powodzenia! 🚀**
