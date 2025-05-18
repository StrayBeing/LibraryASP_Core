using System;
using System.ComponentModel.DataAnnotations;

namespace Biblioteka.Models
{
    public class Notification
    {
        public int NotificationID { get; set; }

        [Required(ErrorMessage = "Użytkownik jest wymagany.")]
        public int UserID { get; set; }
        public User? User { get; set; }

        [Required(ErrorMessage = "Wiadomość jest wymagana.")]
        [StringLength(255, ErrorMessage = "Wiadomość nie może przekraczać 255 znaków.")]
        public string Message { get; set; }

        public DateTime SentDate { get; set; }
    }
}