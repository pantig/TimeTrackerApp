# 🔧 INSTRUKCJA NAPRAWY - Krok po Kroku

## ✅ **PROBLEM ROZWIĄZANY!**

Naprawiono konfigurację projektów. Wykonaj poniższe kroki:

---

## 🚀 **SZYBKA NAPRAWA (5 MINUT)**

### **Krok 1: Zaciągnij Zmiany**

```bash
cd ~/RiderProjects/TimeTrackerApp
git pull origin feature/calendar-precise-time-input
```

### **Krok 2: Przywróć Pakiety**

```bash
# Przywróć pakiety dla CAŁEGO solution
dotnet restore TimeTrackerApp.sln
```

### **Krok 3: Clean & Build**

```bash
# Clean
dotnet clean TimeTrackerApp.sln

# Build głównego projektu
dotnet build TimeTrackerApp.csproj

# Build projektu testowego
dotnet build TimeTrackerApp.Tests/TimeTrackerApp.Tests.csproj
```

### **Krok 4: Weryfikacja**

```bash
# Test discovery
dotnet test --list-tests

# Powinno pokazać 27 testów
```

### **Krok 5: Uruchom Testy**

```bash
dotnet test

# Oczekiwany output:
# ✅ Passed!  - Failed:     0, Passed:    27
```

---

## 🔍 **CO ZOSTAŁO NAPRAWIONE**

### **1. TimeTrackerApp.csproj - Dodano Exclude**

Główny projekt teraz **ignoruje** folder testowy:

```xml
<ItemGroup>
  <Compile Remove="TimeTrackerApp.Tests/**" />
  <EmbeddedResource Remove="TimeTrackerApp.Tests/**" />
  <None Remove="TimeTrackerApp.Tests/**" />
</ItemGroup>
```

**Co to zmienia:**
- Główny projekt NIE próbuje kompilować plików testowych
- Brak błędów o brakujących pakietach (xUnit, FluentAssertions)

### **2. TimeTrackerApp.sln - Dodano Projekt Testowy**

Solution teraz zawiera OBA projekty:

```
TimeTrackerApp.sln
  ├── TimeTrackerApp.csproj          (główny)
  └── TimeTrackerApp.Tests
      └── TimeTrackerApp.Tests.csproj (testy)
```

**Co to zmienia:**
- Rider widzi oba projekty
- Można budować je osobno lub razem
- Unit Tests window poprawnie wykrywa testy

---

## 📊 **WERYFIKACJA PO NAPRAWIE**

### **Test 1: Build Głównego Projektu**

```bash
dotnet build TimeTrackerApp.csproj
```

**Oczekiwany output:**
```
✅ Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### **Test 2: Build Projektu Testowego**

```bash
dotnet build TimeTrackerApp.Tests/TimeTrackerApp.Tests.csproj
```

**Oczekiwany output:**
```
✅ Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### **Test 3: Test Discovery**

```bash
dotnet test --list-tests
```

**Oczekiwany output:**
```
The following Tests are available:
    Login_WithValidCredentials_RedirectsToDashboard
    Login_WithInvalidCredentials_ReturnsLoginPage
    ...
    (27 testów total)
```

### **Test 4: Uruchomienie Testów**

```bash
dotnet test --verbosity normal
```

**Oczekiwany output:**
```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    27, Skipped:     0, Total:    27, Duration: 45s
```

---

## 🎮 **W RIDER IDE**

### **Po Zaciągnięciu Zmian:**

1. **Restart Rider:**
   - Zamknij Rider
   - Otwórz ponownie folder `TimeTrackerApp`

2. **Poczekaj na Indexowanie:**
   - Rider przeskanuje projekty
   - Powinien wykryć oba projekty w Solution Explorer

3. **Sprawdź Solution Explorer:**
   ```
   📁 TimeTrackerApp (solution)
     📁 TimeTrackerApp
       📄 Program.cs
       📄 TimeTrackerApp.csproj
     📁 TimeTrackerApp.Tests
       📁 IntegrationTests
       📄 TimeTrackerApp.Tests.csproj
   ```

4. **Restore Packages w Rider:**
   - Prawy przycisk na **Solution**
   - `Restore NuGet Packages`

5. **Rebuild w Rider:**
   - `Build` → `Rebuild All`
   - Lub: `Ctrl + Shift + B`

6. **Invalidate Caches:**
   - `File` → `Invalidate Caches / Restart...`
   - `Invalidate and Restart`

7. **Unit Tests Window:**
   - `Alt + 8` (otwórz Unit Tests)
   - Kliknij 🔄 `Refresh`
   - Powinno pokazać 27 testów

8. **Uruchom Testy:**
   - Kliknij ▶️ `Run All Tests`
   - Poczekaj ~60 sekund
   - ✅ **27 passed**

---

## ⚠️ **JEŚLI NADAL WYSTĘPUJĄ BŁĘDY**

### **Problem: "Cannot resolve symbol" w Rider**

**Rozwiązanie:**

```bash
# 1. Clean wszystko
find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null
find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null

# 2. Restore
dotnet restore TimeTrackerApp.sln

# 3. Build
dotnet build TimeTrackerApp.sln

# 4. Restart Rider + Invalidate Caches
```

### **Problem: Testy się nie uruchamiają**

**Sprawdź czy pakiety są zainstalowane:**

```bash
cd TimeTrackerApp.Tests
dotnet list package
```

**Powinno pokazać:**
```
xunit                           2.6.6
xunit.runner.visualstudio       2.5.6
Microsoft.AspNetCore.Mvc.Testing 8.0.0
FluentAssertions                6.12.0
```

**Jeśli nie ma pakietów:**

```bash
cd TimeTrackerApp.Tests
dotnet restore
dotnet build
```

### **Problem: Build failuje z innymi błędami**

**Sprawdź .NET SDK:**

```bash
dotnet --version
# Powinno być: 8.0.x
```

**Jeśli nie masz .NET 8.0:**

```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

---

## 📊 **STRUKTURA PO NAPRAWIE**

```
TimeTrackerApp/
├── TimeTrackerApp.csproj        ← Główny projekt (z exclude)
├── TimeTrackerApp.sln           ← Solution (2 projekty)
├── Program.cs                   ← Zawiera public partial class Program
├── Controllers/
├── Models/
├── Views/
└── TimeTrackerApp.Tests/        ← Projekt testowy (wewnątrz folderu)
    ├── TimeTrackerApp.Tests.csproj
    ├── IntegrationTests/
    │   ├── IntegrationTestBase.cs
    │   ├── AuthenticationTests.cs
    │   ├── ProjectTests.cs
    │   ├── TimeEntryTests.cs
    │   └── CalendarTests.cs
    ├── README.md
    ├── QUICK_START.md
    ├── RIDER_TUTORIAL.md
    └── TROUBLESHOOTING.md
```

---

## ✅ **GOTOWE!**

Po wykonaniu powyższych kroków:

1. ✅ Build głównego projektu działa
2. ✅ Build projektu testowego działa
3. ✅ Testy są wykrywane
4. ✅ Testy można uruchamiać
5. ✅ Rider poprawnie widzi oba projekty

---

## 🎯 **NASTĘPNE KROKI**

Po naprawie:

1. ✅ Uruchom wszystkie testy: `dotnet test`
2. ✅ Sprawdź w Rider: `Alt + 8` → Run All Tests
3. ✅ Przejrzyj dokumentację:
   - `QUICK_START.md` - szybki start
   - `RIDER_TUTORIAL.md` - pełny tutorial
   - `TROUBLESHOOTING.md` - rozwiązywanie problemów

---

**Powodzenia! 🚀**
