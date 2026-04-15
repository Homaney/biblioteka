public class BookInstanceDto
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string InventoryNumber { get; set; }
    public string Status { get; set; }
    public bool CanBeSold { get; set; }
}