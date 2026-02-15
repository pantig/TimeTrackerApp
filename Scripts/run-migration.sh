#!/bin/bash
# Skrypt do ręcznego uruchamiania migracji

set -e

DB_PATH="timetracker.db"
MIGRATION_FILE="$1"

if [ -z "$MIGRATION_FILE" ]; then
    echo "Użycie: $0 <plik_migracji.sql>"
    echo "Przykład: $0 Migrations/20260215_AddClientAndClientIdToProject.sql"
    exit 1
fi

if [ ! -f "$MIGRATION_FILE" ]; then
    echo "Błąd: Plik $MIGRATION_FILE nie istnieje"
    exit 1
fi

echo "=== Backup bazy danych ==="
cp "$DB_PATH" "${DB_PATH}.backup.$(date +%Y%m%d_%H%M%S)"
echo "Backup utworzony"

echo "=== Uruchamianie migracji ==="
echo "Plik: $MIGRATION_FILE"
echo "Baza: $DB_PATH"

sqlite3 "$DB_PATH" < "$MIGRATION_FILE"

echo "=== Migracja zakończona ==="
echo "Sprawdzanie historii migracji:"
sqlite3 "$DB_PATH" "SELECT * FROM __MigrationHistory ORDER BY AppliedAt DESC LIMIT 5;"
