using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Biblioteka.Models
{
    public class Book
    {
        public int BookID { get; set; }

        [Required(ErrorMessage = "Tytuł jest wymagany.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Autor jest wymagany.")]
        public string Author { get; set; }

        [Required(ErrorMessage = "ISBN jest wymagany.")]
        [StringLength(20, ErrorMessage = "ISBN nie może być dłuższy niż 20 znaków.")]
        public string ISBN { get; set; }

        [Display(Name = "Rok wydania")]
        public int YearPublished { get; set; }

        public ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();
        public ICollection<Copy> Copies { get; set; } = new List<Copy>();
    }

}