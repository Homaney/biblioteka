using System;

namespace biblioteka.DTO
{
    public class SaleCreateDto
    {
        public int BookId { get; set; }
        public int ReaderId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime SaleDate { get; set; }
        public string Notes { get; set; }
    }
}