using TimeTrackerApp.Models;

namespace TimeTrackerApp.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            if (context.Users.Any())
                return;

            // Hasła захезані (w wersji produkcyjnej użyć BCrypt)
            var adminUser = new User
            {
                Email = "admin@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                FirstName = "Admin",
                LastName = "System",
                Role = UserRole.Admin,
                IsActive = true
            };

            var managerUser = new User
            {
                Email = "manager@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("manager123"),
                FirstName = "Jan",
                LastName = "Kierownik",
                Role = UserRole.Manager,
                IsActive = true
            };

            var employeeUser = new User
            {
                Email = "employee@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("employee123"),
                FirstName = "Piotr",
                LastName = "Pracownik",
                Role = UserRole.Employee,
                IsActive = true
            };

            context.Users.AddRange(adminUser, managerUser, employeeUser);
            context.SaveChanges();

            // Projektów
            var projects = new List<Project>
            {
                new Project { Name = "Portal E-commerce", Description = "Budowa platformy sprzedażowej", Status = ProjectStatus.Active, HoursBudget = 160 },
                new Project { Name = "System CRM", Description = "Zarządzanie relacjami z klientami", Status = ProjectStatus.Active, HoursBudget = 240 },
                new Project { Name = "Modernizacja IT", Description = "Aktualizacja infrastruktury", Status = ProjectStatus.Planning, HoursBudget = 80 }
            };

            context.Projects.AddRange(projects);
            context.SaveChanges();

            // Pracownicy
            var employees = new List<Employee>
            {
                new Employee
                {
                    UserId = employeeUser.Id,
                    Position = "Developer",
                    Department = "IT"
                },
                new Employee
                {
                    UserId = managerUser.Id,
                    Position = "Project Manager",
                    Department = "Management"
                }
            };

            context.Employees.AddRange(employees);
            context.SaveChanges();

            // Wpisy czasu
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
                    CreatedBy = employeeUser.Id
                },
                new TimeEntry
                {
                    EmployeeId = employees[1].Id,
                    ProjectId = projects[0].Id,
                    EntryDate = now.Date.AddDays(-1),
                    StartTime = new TimeSpan(8, 30, 0),
                    EndTime = new TimeSpan(17, 30, 0),
                    Description = "Spotkanie zespołu",
                    CreatedBy = managerUser.Id
                }
            };

            context.TimeEntries.AddRange(timeEntries);
            context.SaveChanges();
        }
    }
}
