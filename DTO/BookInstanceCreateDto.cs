namespace biblioteka.DTO
{
    public class BookInstanceCreateDto
    {
        public int BookId { get; set; }
        public string InventoryNumber { get; set; }
        public string Status { get; set; }
        public bool CanBeSold { get; set; }
        public decimal Price { get; set; }
    }
}