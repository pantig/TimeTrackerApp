# 🔧 Rozwiązywanie Problemów - Testy

## ❌ Problem: "Cannot resolve symbol" w Rider

### Objawy:
```
Cannot resolve symbol 'FluentAssertions'
Cannot resolve symbol 'Xunit'
Cannot resolve symbol 'WebApplicationFactory'
```

### Rozwiązanie:

#### **Krok 1: Zaciągnij najnowsze zmiany**
```bash
git pull origin feature/calendar-precise-time-input
```

#### **Krok 2: Przywracanie Pakietów NuGet w Rider**

**Metoda A: Przez GUI**
1. Kliknij prawy przycisk na **Solution** w Solution Explorer
2. Wybierz **"Restore NuGet Packages"**
3. Poczekaj na zakończenie (zobaczysz komunikat u dołu)

**Metoda B: Przez terminal w Rider**
1. Otwórz terminal: `Alt + F12` (Windows) / `⌥ + F12` (Mac)
2. Wykonaj:
   ```bash
   dotnet restore
   ```

**Metoda C: Przez zewnętrzny terminal**
```bash
cd TimeTrackerApp
dotnet restore
```

#### **Krok 3: Rebuild Solution**

**W Rider:**
1. `Build` → `Rebuild All`
2. Lub: `Ctrl + Shift + B` (Windows) / `⌘ + Shift + B` (Mac)

#### **Krok 4: Invalidate Caches (jeśli nadal nie działa)**

1. `File` → `Invalidate Caches / Restart...`
2. Zaznacz:
   - ☑️ **Invalidate and Restart**
   - ☑️ **Clear downloaded shared indexes**
3. Kliknij **"Invalidate and Restart"**
4. Poczekaj na restart Rider

---

## ❌ Problem: "Inconsistent accessibility: type argument 'Program' is less accessible"

### Objawy:
```
Inconsistent accessibility: type argument 'Program' is less accessible 
than constructor 'IntegrationTestBase.IntegrationTestBase'
```

### Rozwiązanie:

**To już naprawione!** Zaciągnij najnowsze zmiany:

```bash
git pull origin feature/calendar-precise-time-input
```

Plik `Program.cs` zawiera teraz:
```csharp
// Make Program class accessible for integration tests
public partial class Program { }
```

---

## ❌ Problem: Build Fails

### Rozwiązanie:

#### **Krok 1: Clean Solution**
```bash
dotnet clean
```

Lub w Rider: `Build` → `Clean Solution`

#### **Krok 2: Restore Packages**
```bash
dotnet restore
```

#### **Krok 3: Build**
```bash
dotnet build
```

#### **Krok 4: Sprawdź Output**

W Rider:
1. Otwórz `View` → `Tool Windows` → `Build`
2. Przeczytaj błędy w czerwonym tekście
3. Kliknij dwukrotnie na błąd aby przejść do pliku

---

## ❌ Problem: Testy się nie uruchamiają

### Objawy:
- Unit Tests window jest puste
- Brak zielonej ikonki obok `[Fact]`
- "No tests found"

### Rozwiązanie:

#### **Krok 1: Sprawdź czy projekt testowy się buduje**
```bash
cd TimeTrackerApp.Tests
dotnet build
```

Spodziewany output:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

#### **Krok 2: Refresh Unit Tests w Rider**

1. Otwórz Unit Tests window: `Alt + 8`
2. Kliknij ikonkę 🔄 **"Refresh"** u góry okna
3. Poczekaj 10-30 sekund

#### **Krok 3: Invalidate Caches**

Jeśli nadal nie widoczne:
1. `File` → `Invalidate Caches / Restart...`
2. **"Invalidate and Restart"**

#### **Krok 4: Sprawdź czy xUnit runner jest zainstalowany**

```bash
dotnet list TimeTrackerApp.Tests/TimeTrackerApp.Tests.csproj package
```

Powinno być:
```
xunit                           2.6.6
xunit.runner.visualstudio       2.5.6
```

---

## ❌ Problem: "The type or namespace name 'X' could not be found"

### Rozwiązanie:

#### **Sprawdź czy projekt testowy ma referencję do głównego projektu:**

1. Otwórz `TimeTrackerApp.Tests/TimeTrackerApp.Tests.csproj`
2. Sprawdź czy jest:
   ```xml
   <ItemGroup>
     <ProjectReference Include="..\TimeTrackerApp.csproj" />
   </ItemGroup>
   ```

3. Jeśli nie ma, dodaj ręcznie lub przez Rider:
   - Prawy przycisk na projekcie testowym → `Add` → `Reference...`
   - Wybierz `TimeTrackerApp`

---

## ❌ Problem: Testy failują z błędem bazy danych

### Objawy:
```
System.InvalidOperationException: No database provider has been configured
```

### Rozwiązanie:

Sprawdź czy `TimeTrackerApp.Tests.csproj` zawiera:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
```

Jeśli nie, dodaj:
```bash
cd TimeTrackerApp.Tests
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

---

## 🛠️ Pełny Reset (Nuclear Option)

Gdy nic innego nie pomaga:

### **Krok 1: Cleanup**
```bash
# Usuń bin i obj
find . -type d -name "bin" -exec rm -rf {} +
find . -type d -name "obj" -exec rm -rf {} +

# Lub ręcznie usuń foldery:
# - TimeTrackerApp/bin
# - TimeTrackerApp/obj
# - TimeTrackerApp.Tests/bin
# - TimeTrackerApp.Tests/obj
```

### **Krok 2: Restore**
```bash
dotnet restore
```

### **Krok 3: Build**
```bash
dotnet build
```

### **Krok 4: Restart Rider**
1. Zamknij Rider całkowicie
2. Otwórz ponownie
3. Poczekaj na indexowanie

### **Krok 5: Invalidate Caches**
1. `File` → `Invalidate Caches / Restart...`
2. **"Invalidate and Restart"**

---

## 📞 Dalsze Kroki

Jeśli żadne z powyższych rozwiązań nie zadziałało:

1. **Sprawdź wersję .NET SDK:**
   ```bash
   dotnet --version
   ```
   Powinna być: `8.0.x`

2. **Sprawdź logi build:**
   ```bash
   dotnet build --verbosity detailed > build.log
   ```
   Przejrzyj `build.log`

3. **Sprawdź czy wszystkie pliki zostały zacommitowane:**
   ```bash
   git status
   git pull origin feature/calendar-precise-time-input
   ```

4. **Sprawdź Rider logs:**
   - `Help` → `Diagnostic Tools` → `Show Log in Explorer`
   - Szukaj błędów w `idea.log`

---

## ✅ Weryfikacja że Wszystko Działa

### **Test 1: Build**
```bash
dotnet build
```

Oczekiwany output:
```
✅ Build succeeded.
```

### **Test 2: Restore**
```bash
dotnet restore
```

Oczekiwany output:
```
✅ Restore completed
```

### **Test 3: Test Discovery**
```bash
dotnet test --list-tests
```

Powinno pokazać 27 testów:
```
TimeTrackerApp.Tests.IntegrationTests.AuthenticationTests.Login_WithValidCredentials_RedirectsToDashboard
TimeTrackerApp.Tests.IntegrationTests.AuthenticationTests.Login_WithInvalidCredentials_ReturnsLoginPage
...
```

### **Test 4: Run Tests**
```bash
dotnet test
```

Oczekiwany output:
```
✅ Passed: 27
❌ Failed: 0
```

### **Test 5: Rider UI**

1. Otwórz `AuthenticationTests.cs`
2. Powinieneś zobaczyć:
   - 🟢 Zieloną ikonkę obok `[Fact]`
   - Brak czerwonych podkreśleń
   - IntelliSense działa

---

## 📊 Podsumowanie Kroków Naprawy

Dla 95% problemów wystarczy:

```bash
# 1. Pull latest changes
git pull origin feature/calendar-precise-time-input

# 2. Restore packages
dotnet restore

# 3. Rebuild
dotnet clean
dotnet build

# 4. Restart Rider
# Zamknij i otwórz ponownie Rider

# 5. Invalidate caches w Rider
# File → Invalidate Caches / Restart
```

---

**Jeśli to nie pomoże, sprawdź sekcję "Pełny Reset" powyżej.**
