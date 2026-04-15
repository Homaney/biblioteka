using System;

namespace biblioteka.DTO
{
    public class SaleDto
    {
        public int SaleId { get; set; }
        public int BookId { get; set; }
        public int ReaderId { get; set; }
        public DateTime SaleDate { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public string Notes { get; set; }
        public string BookTitle { get; set; }
        public string Buyer { get; set; }
    }
}