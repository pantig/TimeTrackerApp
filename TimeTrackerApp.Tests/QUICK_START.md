# ⚡ Quick Start - Uruchom Testy w 5 Minut

Szybki przewodnik dla osób, które chcą szybko uruchomić testy.

## 🚀 Krok 1: Przygotowanie (1 min)

```bash
# Przejdź na branch z testami
git checkout feature/calendar-precise-time-input
git pull

# Zbuduj projekt
dotnet build
```

## 🧪 Krok 2: Otwórz Rider (30 sek)

1. Otwórz folder `TimeTrackerApp` w Rider
2. Poczekaj aż Rider załaduje projekt

## 🧰 Krok 3: Otwórz Unit Tests (10 sek)

**Windows/Linux:** `Alt + 8`

**Mac:** `⌘ + 8`

Lub: `View` → `Tool Windows` → `Unit Tests`

## ▶️ Krok 4: Uruchom Testy (3 min)

W oknie Unit Tests:

1. **Kliknij zieloną ikonkę** ▶️ `Run All Tests` u góry okna
2. **Poczekaj** 30-60 sekund
3. **Zobacz wyniki:**
   ```
   ✅ 27 passed
   ❌ 0 failed
   ⏱️ Duration: 45.3s
   ```

## 🎉 Gotowe!

Jeśli wszystkie testy są zielone (✅), wszystko działa!

---

## 🔍 Co Dalej?

### Chcę uruchomić pojedynczy test

1. Rozwijaj drzewo testów w Unit Tests
2. Kliknij prawy na testu → `Run`

### Chcę debugować test

1. Otwórz plik testowy (np. `AuthenticationTests.cs`)
2. Znajdź metodę z `[Fact]`
3. Kliknij ikonkę 🐞 `Debug` po lewej stronie

### Chcę zobaczyć pokrycie kodu

**Windows/Linux:** `Ctrl + Alt + K`

**Mac:** `⌘ + ⌥ + K`

Lub: Prawy przycisk na projekcie testowym → `Cover Unit Tests`

---

## 📚 Pełna Dokumentacja

Więcej szczegółów znajdziesz w:

- **[RIDER_TUTORIAL.md](RIDER_TUTORIAL.md)** - Szczegółowy tutorial krok po kroku
- **[README.md](README.md)** - Pełna dokumentacja testów

---

## ❓ Problemy?

### Testy się nie uruchamiają

```bash
# Rebuild projektu
dotnet clean
dotnet build

# Przywracanie pakietów
dotnet restore
```

W Rider:
- `Build` → `Rebuild All`
- Kliknij 🔄 `Refresh` w Unit Tests window

### Rider nie wykrywa testów

1. `File` → `Invalidate Caches / Restart`
2. Wybierz `Invalidate and Restart`
3. Poczekaj na restart

### Inne problemy

Zobacz: [RIDER_TUTORIAL.md - Częste Problemy](RIDER_TUTORIAL.md#-cz%C4%99ste-problemy-i-rozwi%C4%85zania)

---

## 📊 Podsumowanie Testów

| Kategoria | Liczba Testów | Opis |
|-----------|----------------|------|
| **Autentykacja** | 8 | Logowanie, rejestracja, wylogowanie |
| **Projekty** | 5 | CRUD projektów |
| **Wpisy Czasu** | 8 | Dodawanie, edycja, usuwanie, zatwierdzanie |
| **Kalendarz** | 6 | Widok kalendarza, oznaczenia dni, nawigacja |
| **RAZEM** | **27** | **Wszystkie funkcje aplikacji** |

---

**Powodzenia! 🚀**
