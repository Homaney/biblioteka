namespace biblioteka.DTO
{
    public class BookInstanceUpdateDto
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string InventoryNumber { get; set; }
        public string Status { get; set; }
        public bool CanBeSold { get; set; }
        public decimal Price { get; set; }
    }
}