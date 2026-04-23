using System;

namespace biblioteka.Entities
{
    public class WriteOffActWithCount
    {
        public int Id { get; set; }
        public string ActNumber { get; set; }
        public DateTime WriteOffDate { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public int InstanceCount { get; set; }
    }
}