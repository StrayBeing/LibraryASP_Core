using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Biblioteka.Models
{
    public class Copy
    {
        public int CopyID { get; set; }

        public int BookID { get; set; }
        public Book Book { get; set; }

        [Required(ErrorMessage = "Numer katalogowy jest wymagany.")]
        [StringLength(50)]
        public string CatalogNumber { get; set; }

        public bool Available { get; set; }

        public ICollection<Loan> Loans { get; set; }
    }

}