using System;

namespace biblioteka.DTO
{
    public class WriteOffActDto
    {
        public int Id { get; set; }
        public string ActNumber { get; set; }
        public DateTime WriteOffDate { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}