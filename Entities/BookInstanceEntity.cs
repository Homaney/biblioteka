using System;

namespace biblioteka.Entities
{
    public class BookInstanceEntity
    {
        public int ID { get; set; }
        public int BookID { get; set; }
        public string InventoryNumber { get; set; }
        public string Status { get; set; }
        public DateTime? AcquisitionDate { get; set; }
        public string InvoiceNumber { get; set; }
        public bool CanBeSold { get; set; }
    }
}