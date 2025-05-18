using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Biblioteka.Models
{
    public class Book
    {
        public int BookID { get; set; }

        [Required(ErrorMessage = "Tytuł jest wymagany.")]
        [StringLength(255, ErrorMessage = "Tytuł nie może być dłuższy niż 255 znaków.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Autor jest wymagany.")]
        [StringLength(255, ErrorMessage = "Autor nie może być dłuższy niż 255 znaków.")]
        public string Author { get; set; }

        [Required(ErrorMessage = "ISBN jest wymagany.")]
        [StringLength(20, ErrorMessage = "ISBN nie może być dłuższy niż 20 znaków.")]
        public string ISBN { get; set; }

        [Display(Name = "Rok wydania")]
        [Range(1000, 9999, ErrorMessage = "Rok wydania musi być między 1000 a 9999.")]
        public int YearPublished { get; set; }

        public ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();
        public ICollection<Copy> Copies { get; set; } = new List<Copy>();
    }
}