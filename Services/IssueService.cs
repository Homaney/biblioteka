using System;
using System.Collections.Generic;
using System.Linq;
using biblioteka.DAO;
using biblioteka.DTO;
using biblioteka.Entities;

namespace biblioteka.Services
{
    public class IssueService
    {
        private readonly IssueDAO _issueDAO;
        private readonly BookInstanceDAO _instanceDAO;
        private readonly BookDAO _bookDAO;
        private readonly ReaderDAO _readerDAO;

        public IssueService()
        {
            _issueDAO = new IssueDAO();
            _instanceDAO = new BookInstanceDAO();
            _bookDAO = new BookDAO();
            _readerDAO = new ReaderDAO();
        }

        public List<IssuedBookDto> GetAllIssues()
        {
            var entities = _issueDAO.GetAll();
            return entities.Select(e => MapToDto(e)).ToList();
        }

        public List<IssuedBookDto> GetActiveIssues()
        {
            var entities = _issueDAO.GetActiveIssues();
            return entities.Select(e => MapToDto(e)).ToList();
        }

        public List<IssuedBookDto> GetOverdueIssues()
        {
            var entities = _issueDAO.GetOverdueIssues();
            return entities.Select(e =>
            {
                var dto = MapToDto(e);
                dto.DaysOverdue = (DateTime.Now - e.PlannedReturnDate).Days;
                dto.Fine = CalculateFine(e.ID);   // расчёт штрафа для просроченных
                return dto;
            }).ToList();
        }

        public List<IssuedBookDto> GetReaderActiveIssues(int readerId)
        {
            var entities = _issueDAO.GetByReaderId(readerId, true);
            return entities.Select(e => MapToDto(e)).ToList();
        }

        public List<IssuedBookDto> GetReaderHistory(int readerId)
        {
            var entities = _issueDAO.GetHistoryByReaderId(readerId);
            return entities.Select(e => MapToDto(e)).ToList();
        }

        public IssuedBookDto GetById(int id)
        {
            var entity = _issueDAO.GetById(id);
            return entity != null ? MapToDto(entity) : null;
        }

        public void IssueBook(IssueCreateDto dto)
        {
            if (dto.InstanceId <= 0)
                throw new ArgumentException("Выберите экземпляр книги");
            if (dto.ReaderId <= 0)
                throw new ArgumentException("Выберите читателя");
            if (dto.IssueDate == default)
                throw new ArgumentException("Укажите дату выдачи");
            if (dto.IssueDate > DateTime.Today)
                throw new ArgumentException("Дата выдачи не может быть в будущем");
            if (dto.PlannedReturnDate == default)
                throw new ArgumentException("Укажите плановую дату возврата");
            if (dto.PlannedReturnDate < dto.IssueDate)
                throw new ArgumentException("Дата возврата должна быть позже даты выдачи");

            var entity = new IssuedBookEntity
            {
                InstanceID = dto.InstanceId,
                ReaderID = dto.ReaderId,
                IssueDate = dto.IssueDate,
                PlannedReturnDate = dto.PlannedReturnDate,
                Status = "Выдана"
            };

            _issueDAO.Insert(entity);
            _instanceDAO.UpdateStatus(dto.InstanceId, "Выдана");
        }

        public void ReturnBook(int issueId)
        {
            var issue = _issueDAO.GetById(issueId);
            if (issue == null)
                throw new ArgumentException("Выдача не найдена");
            if (issue.Status == "Возвращена")
                throw new InvalidOperationException("Книга уже возвращена");

            DateTime actualReturnDate = DateTime.Now;
            bool returnedOnTime = actualReturnDate <= issue.PlannedReturnDate;

            _issueDAO.ReturnBook(issueId, actualReturnDate, returnedOnTime);
            _instanceDAO.UpdateStatus(issue.InstanceID, "Доступна");
        }

        public int GetActiveIssuesCount()
        {
            return _issueDAO.GetActiveIssuesCount();
        }

        public int GetOverdueCount()
        {
            return _issueDAO.GetOverdueCount();
        }

        public decimal CalculateFine(int issueId)
        {
            var issue = _issueDAO.GetById(issueId);
            if (issue == null || issue.Status == "Возвращена")
                return 0;

            if (issue.PlannedReturnDate >= DateTime.Today)
                return 0;

            var instance = _instanceDAO.GetById(issue.InstanceID);
            if (instance == null) return 0;

            int daysOverdue = (DateTime.Today - issue.PlannedReturnDate).Days;
            decimal rate = 0.005m; // 0.5% от цены за день
            return daysOverdue * instance.Price * rate;
        }

        private IssuedBookDto MapToDto(IssuedBookEntity e)
        {
            var instance = _instanceDAO.GetById(e.InstanceID);
            var book = instance != null ? _bookDAO.GetById(instance.BookID) : null;
            var reader = _readerDAO.GetById(e.ReaderID);

            return new IssuedBookDto
            {
                Id = e.ID,
                InstanceId = e.InstanceID,
                ReaderId = e.ReaderID,
                IssueDate = e.IssueDate,
                PlannedReturnDate = e.PlannedReturnDate,
                ActualReturnDate = e.ActualReturnDate,
                Status = e.Status,
                ReturnedOnTime = e.ReturnedOnTime,
                BookTitle = book?.Title ?? "Неизвестная книга",
                ReaderName = reader?.FullName ?? "Неизвестный читатель",
                InventoryNumber = instance?.InventoryNumber ?? "—",
                DaysOverdue = e.Status == "Выдана" && e.PlannedReturnDate < DateTime.Now
                    ? (DateTime.Now - e.PlannedReturnDate).Days
                    : 0,
                Fine = 0  // по умолчанию
            };
        }
    }
}