using System;
using System.ComponentModel.DataAnnotations;

namespace Biblioteka.Models
{
    public class Notification
    {
        public int NotificationID { get; set; }

        public int UserID { get; set; }
        public User User { get; set; }

        [Required(ErrorMessage = "Wiadomość jest wymagana.")]
        [StringLength(255)]
        public string Message { get; set; }

        public DateTime SentDate { get; set; }
    }

}