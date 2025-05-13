namespace Biblioteka.Models
{
    public class BookCategory
    {
        public int BookID { get; set; }
        public Book Book { get; set; }

        public int CategoryID { get; set; }
        public Category Category { get; set; }
    }

}