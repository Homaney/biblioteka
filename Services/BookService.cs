using System;
using System.Collections.Generic;
using System.Linq;
using biblioteka.DAO;
using biblioteka.DTO;
using biblioteka.Entities;

namespace biblioteka.Services
{
	public class BookService
	{
		private readonly BookDAO _bookDAO = new BookDAO();
		private readonly UDKDAO _udkDAO = new UDKDAO();
		private readonly BookInstanceDAO _instanceDAO = new BookInstanceDAO();

		public List<BookDto> GetAllBooks()
		{
			var books = _bookDAO.GetAll();
			var result = new List<BookDto>();
			foreach (var b in books)
			{
				var udk = _udkDAO.GetById(b.UDK_ID);
				int available = _instanceDAO.GetAvailableCount(b.ID);
				result.Add(new BookDto
				{
					Id = b.ID,
					Title = b.Title,
					Year = b.Yearr,
					UdkId = b.UDK_ID,
					UdkCode = udk?.Code,
					Description = b.Description,
					Price = b.Price,
					Authors = b.Authors,
					AuthorShort = ShortenAuthors(b.Authors),
					AvailableInstances = available
				});
			}
			return result;
		}

		public BookDto GetBookById(int id)
		{
			var b = _bookDAO.GetById(id);
			if (b == null) return null;
			var udk = _udkDAO.GetById(b.UDK_ID);
			int available = _instanceDAO.GetAvailableCount(b.ID);
			return new BookDto
			{
				Id = b.ID,
				Title = b.Title,
				Year = b.Yearr,
				UdkId = b.UDK_ID,
				UdkCode = udk?.Code,
				Description = b.Description,
				Price = b.Price,
				Authors = b.Authors,
				AuthorShort = ShortenAuthors(b.Authors),
				AvailableInstances = available
			};
		}

		public void AddBook(BookCreateDto dto)
		{
			ValidateBook(dto);

			var entity = new BookEntity
			{
				Title = dto.Title,
				Yearr = dto.Year,
				UDK_ID = dto.UdkId,
				Description = dto.Description,
				Price = dto.Price,
				Authors = string.Join(", ", dto.Authors)
			};

			int bookId = _bookDAO.Insert(entity);
			_instanceDAO.InsertMultiple(bookId, dto.Quantity);
		}

		public void UpdateBook(int id, BookUpdateDto dto)
		{
			ValidateBookUpdate(dto);

			var entity = new BookEntity
			{
				ID = id,
				Title = dto.Title,
				Yearr = dto.Year,
				UDK_ID = dto.UdkId,
				Description = dto.Description,
				Price = dto.Price,
				Authors = string.Join(", ", dto.Authors)
			};
			_bookDAO.Update(entity);
		}

		public void DeleteBook(int id)
		{
			_bookDAO.Delete(id);
		}

		public List<BookDto> SearchBooks(string searchText)
		{
			var all = GetAllBooks();
			if (string.IsNullOrWhiteSpace(searchText)) return all;
			string lower = searchText.ToLower();
			return all.Where(b =>
				(b.Title != null && b.Title.ToLower().Contains(lower)) ||
				(b.Authors != null && b.Authors.ToLower().Contains(lower)) ||
				(b.UdkCode != null && b.UdkCode.ToLower().Contains(lower)) ||
				(b.Description != null && b.Description.ToLower().Contains(lower))
			).ToList();
		}

		public void AddInstance(int bookId)
		{
			var instance = new BookInstanceEntity
			{
				BookID = bookId,
				InventoryNumber = $"BK-{bookId}-{DateTime.Now:yyyyMMdd-HHmmss}",
				Status = "Доступна",
				CanBeSold = true
			};
			_instanceDAO.Insert(instance);
		}

		public void RemoveAvailableInstance(int bookId)
		{
			int available = _instanceDAO.GetAvailableCount(bookId);
			if (available == 0)
				throw new InvalidOperationException("Нет доступных экземпляров для удаления");
			_instanceDAO.DeleteAvailable(bookId, 1);
		}

		private void ValidateBook(BookCreateDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.Title))
				throw new ArgumentException("Название книги обязательно");
			if (dto.Authors == null || dto.Authors.Count == 0)
				throw new ArgumentException("Добавьте хотя бы одного автора");
			if (dto.UdkId <= 0)
				throw new ArgumentException("Выберите УДК");
			if (dto.Year < 1000 || dto.Year > DateTime.Now.Year + 5)
				throw new ArgumentException($"Год должен быть от 1000 до {DateTime.Now.Year + 5}");
			if (dto.Price < 0)
				throw new ArgumentException("Цена не может быть отрицательной");
			if (dto.Quantity <= 0)
				throw new ArgumentException("Количество экземпляров должно быть больше 0");
		}

		private void ValidateBookUpdate(BookUpdateDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.Title))
				throw new ArgumentException("Название книги обязательно");
			if (dto.Authors == null || dto.Authors.Count == 0)
				throw new ArgumentException("Добавьте хотя бы одного автора");
			if (dto.UdkId <= 0)
				throw new ArgumentException("Выберите УДК");
			if (dto.Year < 1000 || dto.Year > DateTime.Now.Year + 5)
				throw new ArgumentException($"Год должен быть от 1000 до {DateTime.Now.Year + 5}");
			if (dto.Price < 0)
				throw new ArgumentException("Цена не может быть отрицательной");
		}

		private string ShortenAuthors(string authors)
		{
			if (string.IsNullOrEmpty(authors) || authors == "—")
				return "—";

			string[] authorList = authors.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			var shortNames = new List<string>();

			foreach (string author in authorList)
			{
				string trimmed = author.Trim();
				string[] parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				if (parts.Length >= 2)
				{
					string lastName = parts[0];
					string firstNameInitial = parts[1].Length > 0 ? parts[1][0].ToString() : "";
					string result = $"{lastName} {firstNameInitial}.";

					if (parts.Length >= 3)
					{
						string middleInitial = parts[2].Length > 0 ? parts[2][0].ToString() : "";
						result = $"{lastName} {firstNameInitial}.{middleInitial}.";
					}
					shortNames.Add(result);
				}
				else
				{
					shortNames.Add(trimmed);
				}
			}
			return string.Join(", ", shortNames);
		}
	}
}