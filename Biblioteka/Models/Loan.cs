using System;
using System.ComponentModel.DataAnnotations;

namespace Biblioteka.Models
{
    public class Loan
    {
        public int LoanID { get; set; }

        public int UserID { get; set; }
        public User User { get; set; }

        public int CopyID { get; set; }
        public Copy Copy { get; set; }

        public DateTime LoanDate { get; set; }

        [Required(ErrorMessage = "Data zwrotu jest wymagana.")]
        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }
    }

}