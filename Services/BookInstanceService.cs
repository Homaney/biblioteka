using System.Collections.Generic;
using System.Linq;
using biblioteka.DAO;
using biblioteka.DTO;
using biblioteka.Entities;

namespace biblioteka.Services
{
    public class BookInstanceService
    {
        private readonly BookInstanceDAO _instanceDAO;

        public BookInstanceService()
        {
            _instanceDAO = new BookInstanceDAO();
        }

        public List<BookInstanceDto> GetAll()
        {
            var entities = _instanceDAO.GetAll();
            return entities.Select(e => MapToDto(e)).ToList();
        }

        public BookInstanceDto GetById(int id)
        {
            var entity = _instanceDAO.GetById(id);
            return entity != null ? MapToDto(entity) : null;
        }

        public List<BookInstanceDto> GetByBookId(int bookId)
        {
            var entities = _instanceDAO.GetByBookId(bookId);
            return entities.Select(e => MapToDto(e)).ToList();
        }

        public List<BookInstanceDto> GetAvailableByBookId(int bookId)
        {
            var entities = _instanceDAO.GetAvailableByBookId(bookId);
            return entities.Select(e => MapToDto(e)).ToList();
        }

        public List<BookInstanceDto> GetSellableByBookId(int bookId)
        {
            var entities = _instanceDAO.GetSellableByBookId(bookId);
            return entities.Select(e => MapToDto(e)).ToList();
        }

        public void Add(BookInstanceCreateDto dto)
        {
            var entity = new BookInstanceEntity
            {
                BookID = dto.BookId,
                InventoryNumber = dto.InventoryNumber,
                Status = dto.Status ?? "Доступна",
                CanBeSold = dto.CanBeSold,
                Price = dto.Price
            };
            _instanceDAO.Insert(entity);
        }

        public void Update(BookInstanceUpdateDto dto)
        {
            var entity = new BookInstanceEntity
            {
                ID = dto.Id,
                BookID = dto.BookId,
                InventoryNumber = dto.InventoryNumber,
                Status = dto.Status,
                CanBeSold = dto.CanBeSold,
                Price = dto.Price
            };
            _instanceDAO.Update(entity);
        }

        public void UpdateStatus(int id, string status)
        {
            _instanceDAO.UpdateStatus(id, status);
        }

        public void Delete(int id)
        {
            _instanceDAO.Delete(id);
        }

        public int GetAvailableCount(int bookId)
        {
            return _instanceDAO.GetAvailableCount(bookId);
        }

        public int GetTotalCount(int bookId)
        {
            return _instanceDAO.GetTotalCount(bookId);
        }

        public decimal? GetMinPrice(int bookId)
        {
            return _instanceDAO.GetMinPrice(bookId);
        }

        private BookInstanceDto MapToDto(BookInstanceEntity e)
        {
            return new BookInstanceDto
            {
                Id = e.ID,
                BookId = e.BookID,
                InventoryNumber = e.InventoryNumber,
                Status = e.Status,
                CanBeSold = e.CanBeSold,
                Price = e.Price
            };
        }
    }
}