using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq;

namespace TimeTrackerApp.Migrations
{
    public static class MigrationRunner
    {
        public static void RunMigrations(string connectionString)
        {
            // Spróbuj różnych lokalizacji katalogu Migrations
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations"),
                Path.Combine(Directory.GetCurrentDirectory(), "Migrations"),
                Path.Combine(AppContext.BaseDirectory, "Migrations")
            };

            string? migrationsDirectory = null;
            foreach (var path in possiblePaths)
            {
                Console.WriteLine($"[DEBUG] Checking path: {path}");
                if (Directory.Exists(path))
                {
                    migrationsDirectory = path;
                    Console.WriteLine($"[INFO] Found migrations directory: {path}");
                    break;
                }
            }
            
            if (migrationsDirectory == null)
            {
                Console.WriteLine("[WARNING] Migrations directory not found. Tried:");
                foreach (var path in possiblePaths)
                {
                    Console.WriteLine($"  - {path}");
                }
                return;
            }

            var sqlFiles = Directory.GetFiles(migrationsDirectory, "*.sql")
                .OrderBy(f => Path.GetFileName(f))
                .ToList();

            if (!sqlFiles.Any())
            {
                Console.WriteLine("[INFO] No migration files found.");
                return;
            }

            Console.WriteLine($"[INFO] Found {sqlFiles.Count} migration file(s):");
            foreach (var file in sqlFiles)
            {
                Console.WriteLine($"  - {Path.GetFileName(file)}");
            }

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // Utworzenie tabeli do śledzenia migracji
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS __MigrationHistory (
                        MigrationId TEXT PRIMARY KEY,
                        AppliedAt TEXT NOT NULL
                    );
                ";
                cmd.ExecuteNonQuery();
                Console.WriteLine("[INFO] Migration history table ready.");
            }

            foreach (var sqlFile in sqlFiles)
            {
                var migrationId = Path.GetFileNameWithoutExtension(sqlFile);

                // Sprawdź czy migracja już została zastosowana
                using (var checkCmd = connection.CreateCommand())
                {
                    checkCmd.CommandText = "SELECT COUNT(*) FROM __MigrationHistory WHERE MigrationId = @id";
                    checkCmd.Parameters.AddWithValue("@id", migrationId);
                    var count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        Console.WriteLine($"[SKIP] Migration '{migrationId}' already applied.");
                        continue;
                    }
                }

                Console.WriteLine($"[RUN] Applying migration: {migrationId}");

                try
                {
                    var sql = File.ReadAllText(sqlFile);
                    Console.WriteLine($"[DEBUG] Read {sql.Length} characters from {Path.GetFileName(sqlFile)}");
                    
                    // Podziel na pojedyncze polecenia (rozdzielone przez puste linie)
                    var commands = sql.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(cmd => !string.IsNullOrWhiteSpace(cmd) && !cmd.Trim().StartsWith("--"))
                        .ToList();

                    Console.WriteLine($"[DEBUG] Parsed {commands.Count} SQL command(s)");

                    using var transaction = connection.BeginTransaction();
                    
                    int executedCount = 0;
                    foreach (var commandText in commands)
                    {
                        if (string.IsNullOrWhiteSpace(commandText)) continue;
                        
                        try
                        {
                            using var cmd = connection.CreateCommand();
                            cmd.Transaction = transaction;
                            cmd.CommandText = commandText.Trim();
                            cmd.ExecuteNonQuery();
                            executedCount++;
                        }
                        catch (SqliteException ex) when (ex.Message.Contains("already exists"))
                        {
                            // Ignoruj błędy "already exists"
                            Console.WriteLine($"[WARNING] {ex.Message} - continuing...");
                            executedCount++;
                        }
                    }

                    Console.WriteLine($"[DEBUG] Executed {executedCount}/{commands.Count} commands");

                    // Zapisz informację o zastosowanej migracji
                    using (var recordCmd = connection.CreateCommand())
                    {
                        recordCmd.Transaction = transaction;
                        recordCmd.CommandText = "INSERT INTO __MigrationHistory (MigrationId, AppliedAt) VALUES (@id, @date)";
                        recordCmd.Parameters.AddWithValue("@id", migrationId);
                        recordCmd.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("o"));
                        recordCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    Console.WriteLine($"[✓] Migration '{migrationId}' applied successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Error applying migration '{migrationId}':");
                    Console.WriteLine($"[ERROR] {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"[ERROR] Inner: {ex.InnerException.Message}");
                    }
                    throw;
                }
            }

            Console.WriteLine("[SUCCESS] All migrations completed.");
        }
    }
}
