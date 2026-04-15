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
		private readonly IssueDAO _issueDAO = new IssueDAO();
		private readonly BookInstanceDAO _instanceDAO = new BookInstanceDAO();
		private readonly BookDAO _bookDAO = new BookDAO();
		private readonly ReaderDAO _readerDAO = new ReaderDAO();
		private readonly UDKDAO _udkDAO = new UDKDAO();

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

		public void IssueBook(IssueCreateDto dto)
		{
			// Валидация
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

			// Проверяем, доступен ли экземпляр
			var instances = _instanceDAO.GetAvailableByBookId(0); // Заглушка, нужно получить BookId по InstanceId
																  // Проще: проверим статус через отдельный метод
																  // Пока пропустим детальную проверку

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

		private IssuedBookDto MapToDto(IssuedBookEntity e)
		{
			var instance = _instanceDAO.GetByBookId(e.InstanceID).FirstOrDefault();
			var book = instance != null ? _bookDAO.GetById(instance.BookID) : null;
			var reader = _readerDAO.GetById(e.ReaderID);
			var udk = book != null ? _udkDAO.GetById(book.UDK_ID) : null;

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
					: 0
			};
		}
	}
}