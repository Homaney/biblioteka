using System.Collections.Generic;
using System.Linq;
using biblioteka.DAO;
using biblioteka.DTO;
using biblioteka.Entities;

namespace biblioteka.Services
{
    public class BookInstanceService
    {
        private readonly BookInstanceDAO _dao = new BookInstanceDAO();

        public List<BookInstanceDto> GetAvailableByBookId(int bookId)
        {
            var entities = _dao.GetAvailableByBookId(bookId);
            return entities.Select(e => new BookInstanceDto
            {
                Id = e.ID,
                BookId = e.BookID,
                InventoryNumber = e.InventoryNumber,
                Status = e.Status,
                CanBeSold = e.CanBeSold
            }).ToList();
        }
    }
}