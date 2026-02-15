-- Migracja: Dodanie opiekuna projektu (Manager)
-- Data: 2026-02-15

-- Krok 1: Dodaj kolumnę ManagerId (nullable tymczasowo)
ALTER TABLE Projects ADD COLUMN ManagerId INTEGER;

-- Krok 2: Przypisz pierwszego dostępnego kierownika do wszystkich istniejących projektów
-- Pobieramy pierwszego pracownika który ma rolę Manager
UPDATE Projects 
SET ManagerId = (
    SELECT e.Id 
    FROM Employees e
    INNER JOIN Users u ON e.UserId = u.Id
    WHERE u.Role = 1  -- 1 = Manager
    LIMIT 1
)
WHERE ManagerId IS NULL;

-- Krok 3: Jeśli nie ma żadnego kierownika, przypisz pierwszego admina
-- (awaryjne rozwiązanie)
UPDATE Projects 
SET ManagerId = (
    SELECT e.Id 
    FROM Employees e
    INNER JOIN Users u ON e.UserId = u.Id
    WHERE u.Role = 2  -- 2 = Admin
    LIMIT 1
)
WHERE ManagerId IS NULL;

-- Krok 4: Sprawdź czy wszystkie projekty mają przypisanego managera
-- SELECT Id, Name, ManagerId FROM Projects WHERE ManagerId IS NULL;

-- Krok 5: Ustaw kolumnę jako NOT NULL (po wypełnieniu danych)
-- SQLite nie obsługuje ALTER COLUMN, więc trzeba przekształcić tabelę

-- Twórz tabelę tymczasową
CREATE TABLE Projects_New (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    Status INTEGER NOT NULL DEFAULT 1,
    StartDate TEXT NOT NULL,
    EndDate TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    HoursBudget REAL,
    ManagerId INTEGER NOT NULL,
    FOREIGN KEY (ManagerId) REFERENCES Employees(Id) ON DELETE RESTRICT
);

-- Skopiuj dane
INSERT INTO Projects_New (Id, Name, Description, Status, StartDate, EndDate, IsActive, HoursBudget, ManagerId)
SELECT Id, Name, Description, Status, StartDate, EndDate, IsActive, HoursBudget, ManagerId
FROM Projects;

-- Usuń starą tabelę
DROP TABLE Projects;

-- Zmień nazwę nowej tabeli
ALTER TABLE Projects_New RENAME TO Projects;

-- Odtwórz relacje many-to-many (EmployeeProject)
-- Ta tabela powinna pozostać nienaruszona, ponieważ używa ProjectId jako klucza obcego

-- Krok 6: Weryfikacja
SELECT 
    p.Id,
    p.Name,
    p.ManagerId,
    e.Position,
    u.FirstName || ' ' || u.LastName as ManagerName,
    u.Role
FROM Projects p
INNER JOIN Employees e ON p.ManagerId = e.Id
INNER JOIN Users u ON e.UserId = u.Id;
