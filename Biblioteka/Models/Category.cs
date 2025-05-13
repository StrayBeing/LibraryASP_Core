using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Biblioteka.Models
{
    public class Category
    {
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Nazwa kategorii jest wymagana.")]
        [StringLength(100)]
        public string Name { get; set; }

        public ICollection<BookCategory> BookCategories { get; set; }
    }

}