using System;

namespace biblioteka.DTO
{
    public class IssueCreateDto
    {
        public int InstanceId { get; set; }
        public int ReaderId { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime PlannedReturnDate { get; set; }
    }
}