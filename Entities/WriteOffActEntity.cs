using System;

namespace biblioteka.Entities
{
    public class WriteOffActEntity
    {
        public int ID { get; set; }
        public string ActNumber { get; set; }
        public DateTime WriteOffDate { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}