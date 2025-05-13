using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Biblioteka.Models
{
    public class User
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "Imię jest wymagane.")]
        [StringLength(50, ErrorMessage = "Imię nie może być dłuższe niż 50 znaków.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Nazwisko jest wymagane.")]
        [StringLength(50, ErrorMessage = "Nazwisko nie może być dłuższe niż 50 znaków.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email jest wymagany.")]
        [StringLength(100, ErrorMessage = "Email nie może być dłuższy niż 100 znaków.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format email.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        [StringLength(255, ErrorMessage = "Hasło nie może być dłuższe niż 255 znaków.")]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "Rola jest wymagana.")]
        [StringLength(20, ErrorMessage = "Rola nie może być dłuższa niż 20 znaków.")]
        public string Role { get; set; }

        public ICollection<Loan>? Loans { get; set; } = new List<Loan>();
        public ICollection<Notification>? Notifications { get; set; } = new List<Notification>();
    }

}