using System.ComponentModel.DataAnnotations;

namespace TimeTrackerApp.Models.ViewModels
{
    public class CreateEmployeeViewModel
    {
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [MinLength(6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane")]
        [Compare("Password", ErrorMessage = "Hasła muszą być identyczne")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Imię jest wymagane")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwisko jest wymagane")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rola jest wymagana")]
        public UserRole Role { get; set; } = UserRole.Employee;

        [Required(ErrorMessage = "Stanowisko jest wymagane")]
        [MaxLength(200)]
        public string Position { get; set; } = string.Empty;

        [Required(ErrorMessage = "Departament jest wymagany")]
        [MaxLength(200)]
        public string Department { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; } = DateTime.Today;
    }
}