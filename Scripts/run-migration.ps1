# Skrypt PowerShell do ręcznego uruchamiania migracji
param(
    [Parameter(Mandatory=$true)]
    [string]$MigrationFile
)

$ErrorActionPreference = "Stop"

$DbPath = "timetracker.db"

if (-not (Test-Path $MigrationFile)) {
    Write-Error "Błąd: Plik $MigrationFile nie istnieje"
    exit 1
}

Write-Host "=== Backup bazy danych ===" -ForegroundColor Cyan
$backupPath = "${DbPath}.backup.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item $DbPath $backupPath
Write-Host "Backup utworzony: $backupPath" -ForegroundColor Green

Write-Host "`n=== Uruchamianie migracji ===" -ForegroundColor Cyan
Write-Host "Plik: $MigrationFile"
Write-Host "Baza: $DbPath"

Get-Content $MigrationFile | sqlite3 $DbPath

Write-Host "`n=== Migracja zakończona ===" -ForegroundColor Green
Write-Host "Sprawdzanie historii migracji:"
sqlite3 $DbPath "SELECT * FROM __MigrationHistory ORDER BY AppliedAt DESC LIMIT 5;"
