using System.Collections.Generic;

namespace biblioteka.DTO
{
    public class BookDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
        public int UdkId { get; set; }
        public string UdkCode { get; set; }
        public string Description { get; set; }
        public List<AuthorDto> Authors { get; set; } = new List<AuthorDto>();
        public string AuthorShort { get; set; }
        public int AvailableInstances { get; set; }
        public decimal? MinPrice { get; set; }  // минимальная цена среди доступных экземпляров
    }
}