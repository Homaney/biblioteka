using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace biblioteka
{
    public class Readers
    {
        public int ID { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
    }


    public class IssuedBook
    {
        public int ID { get; set; }
        public int BookID { get; set; }
        public int ReaderID { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ReturnDate { get; set; }
        public string Notes { get; set; }

        public string BookTitle { get; set; }   // Для отображения в DataGrid
        public string ReaderName { get; set; }  // Для отображения в DataGrid
    }
}

