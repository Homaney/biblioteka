using System;

namespace biblioteka.Entities
{
    public class IssuedBookEntity
    {
        public int ID { get; set; }
        public int InstanceID { get; set; }
        public int ReaderID { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime PlannedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }
        public string Status { get; set; }
        public bool ReturnedOnTime { get; set; }
    }
}