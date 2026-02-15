# TimeTrackerApp - Testy Funkcjonalne

Projekt zawiera kompleksowe testy funkcjonalne dla aplikacji TimeTrackerApp.

## 📦 Struktura Testów

```
TimeTrackerApp.Tests/
├── IntegrationTests/
│   ├── IntegrationTestBase.cs       # Klasa bazowa z konfiguracją
│   ├── AuthenticationTests.cs        # Testy logowania/rejestracji
│   ├── ProjectTests.cs               # Testy CRUD projektów
│   ├── TimeEntryTests.cs             # Testy wpisów czasu
│   └── CalendarTests.cs              # Testy kalendarza
└── TimeTrackerApp.Tests.csproj
```

## 🧑‍💻 Jak Uruchomić Testy w Rider IDE

### Metoda 1: Uruchomienie Wszystkich Testów

1. **Otwórz Unit Tests Explorer:**
   - `View` → `Tool Windows` → `Unit Tests`
   - Lub: `Alt + 8` (Windows/Linux) / `⌘ + 8` (Mac)

2. **Uruchom wszystkie testy:**
   - Kliknij zieloną ikonkę "Run All" ▶️ na górze okna Unit Tests
   - Lub: Kliknij prawym na projekt testowy → `Run Unit Tests`

3. **Zobacz wyniki:**
   - Zielone ✓ = test przeszedł
   - Czerwone ✗ = test nie przeszedł
   - Żółte ⚠️ = test został pominięty

### Metoda 2: Uruchomienie Pojedynczego Testu

1. **Otwórz plik z testem** (np. `AuthenticationTests.cs`)

2. **Znajdź metodę testową** oznaczoną `[Fact]`:
   ```csharp
   [Fact]
   public async Task Login_WithValidCredentials_RedirectsToDashboard()
   {
       // ...
   }
   ```

3. **Kliknij zieloną ikonkę** po lewej stronie metody:
   - `Run` - uruchom test
   - `Debug` - uruchom test w trybie debugowania
   - `Cover` - uruchom test z pokryciem kodu

### Metoda 3: Uruchomienie Testów z Klawiatury

1. **Ustaw kursor** na metodzie testowej lub nazwie klasy
2. **Naciśnij:**
   - `Ctrl + T, R` (Windows/Linux) - uruchom testy
   - `Ctrl + T, D` (Windows/Linux) - debuguj testy
   - `⌘ + T, R` (Mac) - uruchom testy

### Metoda 4: Uruchomienie z Terminala

```bash
# Przejdź do folderu głównego
cd TimeTrackerApp

# Uruchom wszystkie testy
dotnet test

# Uruchom testy z konkretnej klasy
dotnet test --filter "FullyQualifiedName~AuthenticationTests"

# Uruchom konkretny test
dotnet test --filter "FullyQualifiedName~Login_WithValidCredentials_RedirectsToDashboard"

# Uruchom testy z szczegółowym outputem
dotnet test --verbosity detailed
```

## 🛠️ Konfiguracja

### Wymagane Pakiety (już zainstalowane)

- `xunit` - Framework testowy
- `xunit.runner.visualstudio` - Runner dla Visual Studio/Rider
- `Microsoft.AspNetCore.Mvc.Testing` - Testy integracyjne ASP.NET Core
- `FluentAssertions` - Czytelne asercje
- `coverlet.collector` - Pokrycie kodu

### Baza Danych Testowa

Testy używają **In-Memory Database**, więc:
- ✅ Nie trzeba konfigurować bazy danych
- ✅ Testy są szybkie
- ✅ Każdy test ma czyste środowisko
- ✅ Dane testowe są seedowane automatycznie

## 📈 Pokrycie Kodu (Code Coverage)

### W Rider IDE:

1. **Uruchom testy z coverage:**
   - Kliknij prawy na projekt testowy
   - Wybierz `Cover Unit Tests`
   - Lub: `Ctrl + Alt + K` (Windows) / `⌘ + ⌥ + K` (Mac)

2. **Zobacz wyniki:**
   - Okno `Unit Test Coverage` pokaże:
     - % pokrycia linii kodu
     - % pokrycia metod
     - % pokrycia klas
   - Kod w edytorze zostanie podkreślony:
     - 🟢 Zielony = pokryty testami
     - 🔴 Czerwony = NIE pokryty testami

### Z Terminala:

```bash
# Uruchom testy z coverage
dotnet test /p:CollectCoverage=true

# Generuj raport HTML
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=html
```

## 🐞 Debugowanie Testów

### W Rider:

1. **Ustaw breakpoint** w kodzie testu (kliknij na marginesie obok linii)

2. **Uruchom test w trybie debug:**
   - Kliknij ikonkę 🐞 `Debug` obok nazwy testu
   - Lub: `Ctrl + T, D` na metodzie testowej

3. **Debuguj:**
   - `F9` - Toggle breakpoint
   - `F8` - Step Over (następna linia)
   - `F7` - Step Into (wejdź do metody)
   - `Shift + F8` - Step Out (wyjście z metody)
   - `F5` - Continue (kontynuuj do następnego breakpoint)

## 📊 Analiza Wyników Testów

### Okno Unit Tests pokazuje:

- **Duration** - czas wykonania testu
- **Output** - szczegółowy output testu
- **Stack Trace** - ścieżka błędu (dla failed tests)

### Przykład wyniku:

```
✓ Login_WithValidCredentials_RedirectsToDashboard (125ms)
✗ Login_WithInvalidCredentials_ReturnsLoginPage (87ms)
  Expected: HttpStatusCode.OK
  Actual: HttpStatusCode.Redirect
  at AuthenticationTests.Login_WithInvalidCredentials_ReturnsLoginPage() line 42
```

## 📄 Lista Testów

### AuthenticationTests (8 testów)
- ✓ Login z poprawnymi danymi
- ✓ Login z błędnymi danymi
- ✓ Wylogowanie
- ✓ Rejestracja nowego użytkownika
- ✓ Rejestracja z istniejącym username
- ✓ Dostęp do chronionej strony bez auth
- ✓ Dostęp do chronionej strony z auth

### ProjectTests (5 testów)
- ✓ Pobranie listy projektów
- ✓ Utworzenie nowego projektu
- ✓ Edycja projektu
- ✓ Usunięcie projektu
- ✓ Dostęp bez autoryzacji

### TimeEntryTests (8 testów)
- ✓ Pobranie listy wpisów
- ✓ Dodanie nowego wpisu
- ✓ Edycja wpisu
- ✓ Usunięcie wpisu
- ✓ Walidacja niepoprawnego zakresu czasu
- ✓ Zatwierdzenie wpisu (manager)
- ✓ Odrzucenie wpisu (manager)

### CalendarTests (6 testów)
- ✓ Widok kalendarza bieżącego tygodnia
- ✓ Widok kalendarza konkretnej daty
- ✓ Ustawienie oznaczenia dnia
- ✓ Usunięcie oznaczenia dnia
- ✓ Nawigacja do poprzedniego tygodnia
- ✓ Nawigacja do następnego tygodnia

## 💡 Najlepsze Praktyki

### 1. Uruchamiaj testy często
- Przed każdym commitem
- Po każdej zmianie w kodzie
- Przed merge do głównej gałęzi

### 2. Czytaj komunikaty błędów
- FluentAssertions daje bardzo czytelne komunikaty
- Stack trace pokazuje dokładnie gdzie wystąpił błąd

### 3. Używaj Test Explorer
- Grupuj testy po przestrzeni nazw
- Filtruj testy po statusie (passed/failed)
- Używaj "Run Failed Tests" do szybkiej naprawy

### 4. Monitoruj pokrycie kodu
- Staraj się uzyskać >80% pokrycia
- Zwracaj uwagę na krytyczne ścieżki

## ❓ FAQ

**Q: Testy się nie uruchamiają?**
A: Upewnij się, że:
- Projekt testowy jest zbudowany (`Build` → `Build Solution`)
- Wszystkie pakiety NuGet są zainstalowane
- Rider wykrył testy (może zająć chwilę)

**Q: Test przechodzi lokalnie, ale failuje na CI/CD?**
A: Sprawdź:
- Czy test nie polega na konkretnej dacie/czasie
- Czy nie ma race conditions
- Czy dane testowe są dobrze seedowane

**Q: Jak dodać nowy test?**
A:
1. Otwórz odpowiedni plik testowy
2. Dodaj nową metodę z atrybutem `[Fact]`
3. Użyj wzorca Arrange-Act-Assert
4. Użyj FluentAssertions dla asercji

```csharp
[Fact]
public async Task MyNewTest_Scenario_ExpectedResult()
{
    // Arrange
    var cookie = await LoginAsAsync("user", "pass");
    SetAuthCookie(cookie);
    
    // Act
    var response = await Client.GetAsync("/some/endpoint");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## 📞 Pomoc

Jeśli masz problemy:
1. Sprawdź output testów w Rider
2. Sprawdź logi aplikacji
3. Użyj debuggera
4. Sprawdź dokumentację xUnit: https://xunit.net/

---

**Powodzenia z testowaniem! 🚀**
