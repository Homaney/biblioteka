namespace biblioteka.Entities
{
    public class BookEntity
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public int Yearr { get; set; }
        public int UDK_ID { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableForSale { get; set; }
        public string Authors { get; set; }
    }
}