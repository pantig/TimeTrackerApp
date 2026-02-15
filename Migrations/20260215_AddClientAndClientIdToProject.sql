-- Migracja: Dodanie tabeli Client i kolumny ClientId do Project
-- Data: 2026-02-15
-- Autor: System

-- 1. Utworzenie tabeli Client
CREATE TABLE IF NOT EXISTS "Clients" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Email" TEXT NULL,
    "Phone" TEXT NULL,
    "Address" TEXT NULL,
    "City" TEXT NULL,
    "PostalCode" TEXT NULL,
    "Country" TEXT NULL,
    "NIP" TEXT NULL,
    "IsActive" INTEGER NOT NULL DEFAULT 1,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NULL
);

-- 2. Dodanie domyślnego klienta (dla istniejących projektów)
INSERT INTO "Clients" ("Name", "Description", "IsActive", "CreatedAt")
VALUES ('Klient domyślny', 'Automatycznie utworzony dla istniejących projektów', 1, datetime('now'));

-- 3. Dodanie kolumny ClientId do Projects (z domyślną wartością)
-- SQLite nie obsługuje ALTER TABLE ADD COLUMN z FOREIGN KEY bezpośrednio
-- Musimy przekształcić tabelę

-- Krok 1: Utworzenie nowej tabeli z ClientId
CREATE TABLE IF NOT EXISTS "Projects_New" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Status" INTEGER NOT NULL,
    "StartDate" TEXT NOT NULL,
    "EndDate" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    "HoursBudget" REAL NULL,
    "ManagerId" INTEGER NOT NULL,
    "ClientId" INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY ("ManagerId") REFERENCES "Employees"("Id") ON DELETE RESTRICT,
    FOREIGN KEY ("ClientId") REFERENCES "Clients"("Id") ON DELETE RESTRICT
);

-- Krok 2: Kopiowanie danych (ClientId = 1 dla wszystkich istniejących projektów)
INSERT INTO "Projects_New" ("Id", "Name", "Description", "Status", "StartDate", "EndDate", "IsActive", "HoursBudget", "ManagerId", "ClientId")
SELECT "Id", "Name", "Description", "Status", "StartDate", "EndDate", "IsActive", "HoursBudget", "ManagerId", 1
FROM "Projects";

-- Krok 3: Usunięcie starej tabeli
DROP TABLE "Projects";

-- Krok 4: Zmiana nazwy nowej tabeli
ALTER TABLE "Projects_New" RENAME TO "Projects";

-- 5. Utworzenie indeksów dla wydajności
CREATE INDEX IF NOT EXISTS "IX_Projects_ClientId" ON "Projects" ("ClientId");
CREATE INDEX IF NOT EXISTS "IX_Projects_ManagerId" ON "Projects" ("ManagerId");
CREATE INDEX IF NOT EXISTS "IX_Clients_IsActive" ON "Clients" ("IsActive");

-- Koniec migracji
