using System;
using System.Collections.Generic;

namespace biblioteka.DTO
{
    public class WriteOffActUpdateDto
    {
        public int Id { get; set; }
        public string ActNumber { get; set; }
        public DateTime WriteOffDate { get; set; }
        public string Reason { get; set; }
        public List<int> InstanceIds { get; set; } = new List<int>();
    }
}