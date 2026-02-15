using TimeTrackerApp.Models;

namespace TimeTrackerApp.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            if (context.Users.Any())
                return;

            // 1. USERS - tworzymy użytkowników
            // ✅ FIXED: Używamy emailów zgodnych z testami (@test.com zamiast @example.com)
            var adminUser = new User
            {
                Email = "admin@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                FirstName = "Admin",
                LastName = "System",
                Role = UserRole.Admin,
                IsActive = true
            };

            var managerUser = new User
            {
                Email = "manager@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!"),
                FirstName = "Jan",
                LastName = "Kierownik",
                Role = UserRole.Manager,
                IsActive = true
            };

            var employeeUser = new User
            {
                Email = "employee@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee123!"),
                FirstName = "Piotr",
                LastName = "Pracownik",
                Role = UserRole.Employee,
                IsActive = true
            };

            context.Users.AddRange(adminUser, managerUser, employeeUser);
            context.SaveChanges();

            // 2. EMPLOYEES - tworzymy pracowników (PRZED projektami!)
            var employees = new List<Employee>
            {
                new Employee
                {
                    UserId = employeeUser.Id,
                    Position = "Developer",
                    Department = "IT",
                    HireDate = DateTime.Today.AddYears(-1),
                    IsActive = true
                },
                new Employee
                {
                    UserId = managerUser.Id,
                    Position = "Project Manager",
                    Department = "Management",
                    HireDate = DateTime.Today.AddYears(-2),
                    IsActive = true
                }
            };

            context.Employees.AddRange(employees);
            context.SaveChanges();

            // ✅ 3. CLIENTS - tworzymy klientów (PRZED projektami!)
            var clients = new List<Client>
            {
                new Client
                {
                    Name = "ABC Corporation",
                    Description = "Duża firma produkcyjna",
                    Email = "contact@abc-corp.com",
                    Phone = "+48 123 456 789",
                    Address = "ul. Główna 1",
                    City = "Warszawa",
                    PostalCode = "00-001",
                    Country = "Polska",
                    NIP = "1234567890",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Client
                {
                    Name = "TechStart Sp. z o.o.",
                    Description = "Startup technologiczny",
                    Email = "hello@techstart.pl",
                    Phone = "+48 987 654 321",
                    Address = "ul. Innowacyjna 42",
                    City = "Kraków",
                    PostalCode = "30-001",
                    Country = "Polska",
                    NIP = "0987654321",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Client
                {
                    Name = "Miasto Poznań",
                    Description = "Urząd miasta",
                    Email = "it@poznan.pl",
                    Phone = "+48 61 878 4444",
                    City = "Poznań",
                    PostalCode = "61-001",
                    Country = "Polska",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Clients.AddRange(clients);
            context.SaveChanges();

            // 4. PROJECTS - teraz możemy przypisać ManagerId i ClientId
            var projects = new List<Project>
            {
                new Project 
                { 
                    Name = "Portal E-commerce", 
                    Description = "Budowa platformy sprzedażowej", 
                    Status = ProjectStatus.Active, 
                    HoursBudget = 160,
                    StartDate = DateTime.Today.AddMonths(-2),
                    ManagerId = employees[1].Id,  // Jan Kierownik
                    ClientId = clients[0].Id,      // ✅ ABC Corporation
                    IsActive = true
                },
                new Project 
                { 
                    Name = "System CRM", 
                    Description = "Zarządzanie relacjami z klientami", 
                    Status = ProjectStatus.Active, 
                    HoursBudget = 240,
                    StartDate = DateTime.Today.AddMonths(-3),
                    ManagerId = employees[1].Id,  // Jan Kierownik
                    ClientId = clients[1].Id,      // ✅ TechStart
                    IsActive = true
                },
                new Project 
                { 
                    Name = "Modernizacja IT", 
                    Description = "Aktualizacja infrastruktury", 
                    Status = ProjectStatus.Planning, 
                    HoursBudget = 80,
                    StartDate = DateTime.Today.AddMonths(-1),
                    ManagerId = employees[1].Id,  // Jan Kierownik
                    ClientId = clients[2].Id,      // ✅ Miasto Poznań
                    IsActive = true
                }
            };

            context.Projects.AddRange(projects);
            context.SaveChanges();

            // 5. PRZYPISANIE PRACOWNIKÓW DO PROJEKTÓW
            // Piotr Pracownik pracuje w Portal E-commerce i System CRM
            employees[0].Projects.Add(projects[0]);
            employees[0].Projects.Add(projects[1]);
            context.SaveChanges();

            // 6. TIME ENTRIES - wpisy czasu
            var now = DateTime.UtcNow;
            var timeEntries = new List<TimeEntry>
            {
                new TimeEntry
                {
                    EmployeeId = employees[0].Id,
                    ProjectId = projects[0].Id,
                    EntryDate = now.Date,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    Description = "Implementacja widoku głównego",
                    CreatedBy = employeeUser.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new TimeEntry
                {
                    EmployeeId = employees[1].Id,
                    ProjectId = projects[0].Id,
                    EntryDate = now.Date.AddDays(-1),
                    StartTime = new TimeSpan(8, 30, 0),
                    EndTime = new TimeSpan(17, 30, 0),
                    Description = "Spotkanie zespołu",
                    CreatedBy = managerUser.Id,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.TimeEntries.AddRange(timeEntries);
            context.SaveChanges();
            
            Console.WriteLine("[INFO] Seed data created:");
            Console.WriteLine($"  - {context.Users.Count()} users");
            Console.WriteLine($"  - {context.Employees.Count()} employees");
            Console.WriteLine($"  - {context.Clients.Count()} clients");
            Console.WriteLine($"  - {context.Projects.Count()} projects");
            Console.WriteLine($"  - {context.TimeEntries.Count()} time entries");
        }
    }
}
