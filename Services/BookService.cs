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
        private readonly BookDAO _bookDAO;
        private readonly UDKDAO _udkDAO;
        private readonly BookInstanceDAO _instanceDAO;

        public BookService()
        {
            _bookDAO = new BookDAO();
            _udkDAO = new UDKDAO();
            _instanceDAO = new BookInstanceDAO();
        }

        public List<BookDto> GetAllBooks()
        {
            var books = _bookDAO.GetAll();
            var result = new List<BookDto>();

            foreach (var b in books)
            {
                var udk = _udkDAO.GetById(b.UDK_ID);
                var authors = _bookDAO.GetAuthorsByBookId(b.ID);
                int available = _instanceDAO.GetAvailableCount(b.ID);
                decimal? minPrice = _instanceDAO.GetMinPrice(b.ID);

                result.Add(new BookDto
                {
                    Id = b.ID,
                    Title = b.Title ?? "Без названия",
                    Year = b.Yearr,
                    UdkId = b.UDK_ID,
                    UdkCode = udk?.Code ?? "—",
                    Description = b.Description ?? "",
                    Authors = authors.Select(a => new AuthorDto
                    {
                        Id = a.ID,
                        FullName = a.FullName
                    }).ToList(),
                    AuthorShort = ShortenAuthors(authors.Select(a => a.FullName).ToList()),
                    AvailableInstances = available,
                    MinPrice = minPrice
                });
            }

            return result;
        }

        public BookDto GetBookById(int id)
        {
            var b = _bookDAO.GetById(id);
            if (b == null) return null;

            var udk = _udkDAO.GetById(b.UDK_ID);
            var authors = _bookDAO.GetAuthorsByBookId(b.ID);
            int available = _instanceDAO.GetAvailableCount(b.ID);
            decimal? minPrice = _instanceDAO.GetMinPrice(b.ID);

            return new BookDto
            {
                Id = b.ID,
                Title = b.Title ?? "Без названия",
                Year = b.Yearr,
                UdkId = b.UDK_ID,
                UdkCode = udk?.Code ?? "—",
                Description = b.Description ?? "",
                Authors = authors.Select(a => new AuthorDto
                {
                    Id = a.ID,
                    FullName = a.FullName
                }).ToList(),
                AuthorShort = ShortenAuthors(authors.Select(a => a.FullName).ToList()),
                AvailableInstances = available,
                MinPrice = minPrice
            };
        }

        public void AddBook(BookCreateDto dto)
        {
            ValidateBook(dto);

            var bookEntity = new BookEntity
            {
                Title = dto.Title,
                Yearr = dto.Year,
                UDK_ID = dto.UdkId,
                Description = dto.Description
            };

            var authorEntities = dto.Authors.Select(a => new AuthorEntity
            {
                FullName = a.FullName
            }).ToList();

            int bookId = _bookDAO.Insert(bookEntity, authorEntities);
            _instanceDAO.InsertMultiple(bookId, dto.Quantity, dto.Price);
        }

        public void UpdateBook(int id, BookUpdateDto dto)
        {
            ValidateBookUpdate(dto);

            var bookEntity = new BookEntity
            {
                ID = id,
                Title = dto.Title,
                Yearr = dto.Year,
                UDK_ID = dto.UdkId,
                Description = dto.Description
            };

            var authorEntities = dto.Authors.Select(a => new AuthorEntity
            {
                FullName = a.FullName
            }).ToList();

            _bookDAO.Update(bookEntity, authorEntities);
        }

        public void DeleteBook(int id)
        {
            _bookDAO.Delete(id);
        }

        public List<BookDto> SearchBooks(string searchText)
        {
            var all = GetAllBooks();
            if (string.IsNullOrWhiteSpace(searchText))
                return all;

            string lower = searchText.ToLower();

            return all.Where(b =>
                (b.Title != null && b.Title.ToLower().Contains(lower)) ||
                (b.Authors != null && b.Authors.Any(a => a.FullName.ToLower().Contains(lower))) ||
                (b.UdkCode != null && b.UdkCode.ToLower().Contains(lower)) ||
                (b.Description != null && b.Description.ToLower().Contains(lower))
            ).ToList();
        }

        public void AddInstance(int bookId, decimal price)
        {
            string inventoryNumber = _instanceDAO.GenerateInventoryNumber(bookId);
            var instance = new BookInstanceEntity
            {
                BookID = bookId,
                InventoryNumber = inventoryNumber,
                Status = "Доступна",
                CanBeSold = true,
                Price = price
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
            if (dto.Quantity <= 0)
                throw new ArgumentException("Количество экземпляров должно быть больше 0");
            if (dto.Price < 0)
                throw new ArgumentException("Цена не может быть отрицательной");
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
        }

        public void AddInstances(int bookId, int quantity, decimal price, string invoiceNumber)
        {
            if (bookId <= 0)
                throw new ArgumentException("Выберите книгу");
            if (quantity <= 0)
                throw new ArgumentException("Количество должно быть больше 0");
            if (price < 0)
                throw new ArgumentException("Цена не может быть отрицательной");
            if (string.IsNullOrWhiteSpace(invoiceNumber))
                throw new ArgumentException("Введите номер накладной");

            // Проверка уникальности номера накладной
            if (_instanceDAO.IsInvoiceNumberExists(invoiceNumber))
                throw new InvalidOperationException($"Накладная с номером «{invoiceNumber}» уже существует. Номера накладных должны быть уникальными.");

            DateTime acquisitionDate = DateTime.Today;
            _instanceDAO.InsertInstances(bookId, quantity, price, invoiceNumber, acquisitionDate);
        }
        private string ShortenAuthors(List<string> authors)
        {
            if (authors == null || authors.Count == 0)
                return "—";

            var shortNames = new List<string>();

            foreach (string author in authors)
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