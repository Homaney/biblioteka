using System.Collections.Generic;

namespace biblioteka.DTO
{
    public class BookCreateDto
    {
        public string Title { get; set; }
        public int Year { get; set; }
        public int UdkId { get; set; }
        public string Description { get; set; }
        public List<AuthorDto> Authors { get; set; } = new List<AuthorDto>();
        public int Quantity { get; set; }
        public decimal Price { get; set; }   // цена каждого экземпляра
    }
}