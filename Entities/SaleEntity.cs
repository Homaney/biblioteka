using System;

namespace biblioteka.Entities
{
    public class SaleEntity
    {
        public int SaleID { get; set; }
        public int BookID { get; set; }
        public int ReaderID { get; set; }
        public DateTime SaleDate { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public string Notes { get; set; }
    }
}