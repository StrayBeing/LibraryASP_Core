using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Biblioteka.Models
{
    public class BookSearchViewModel
    {
        [Display(Name = "Tytuł")] public string Title { get; set; }

        [Display(Name = "Autor")]
        public string Author { get; set; }

        [Display(Name = "ISBN")]
        public string ISBN { get; set; }

        [Display(Name = "Rok wydania od")]
        [Range(1000, 9999, ErrorMessage = "Rok wydania musi być między 1000 a 9999.")]
        public int? YearFrom { get; set; }

        [Display(Name = "Rok wydania do")]
        [Range(1000, 9999, ErrorMessage = "Rok wydania musi być między 1000 a 9999.")]
        public int? YearTo { get; set; }

        [Display(Name = "Kategorie")]
        public List<int> CategoryIds { get; set; } = new List<int>();

        public List<Category> AvailableCategories { get; set; } = new List<Category>();
    }

}