using System.Collections.Generic;

namespace biblioteka.DTO
{
    public class BookUpdateDto
    {
        public string Title { get; set; }
        public int Year { get; set; }
        public int UdkId { get; set; }
        public string Description { get; set; }
        public List<AuthorDto> Authors { get; set; } = new List<AuthorDto>();
    }
}