using System;

namespace biblioteka.DTO
{
    public class IssuedBookDto
    {
        public int Id { get; set; }
        public int InstanceId { get; set; }
        public int ReaderId { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime PlannedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }
        public string Status { get; set; }
        public bool ReturnedOnTime { get; set; }
        public string BookTitle { get; set; }
        public string ReaderName { get; set; }
        public string InventoryNumber { get; set; }
        public int DaysOverdue { get; set; }
    }
}