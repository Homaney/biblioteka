using System;
using System.Collections.Generic;
using System.Linq;
using biblioteka.DAO;
using biblioteka.DTO;
using biblioteka.Entities;

namespace biblioteka.Services
{
	public class SaleService
	{
		private readonly SaleDAO _saleDAO;
		private readonly BookInstanceDAO _instanceDAO;
		private readonly BookDAO _bookDAO;
		private readonly ReaderDAO _readerDAO;

		public SaleService()
		{
			_saleDAO = new SaleDAO();
			_instanceDAO = new BookInstanceDAO();
			_bookDAO = new BookDAO();
			_readerDAO = new ReaderDAO();
		}

		public List<SaleDto> GetAllSales()
		{
			var entities = _saleDAO.GetAll();
			return entities.Select(e => MapToDto(e)).ToList();
		}

		public List<SaleDto> GetSalesByDateRange(DateTime startDate, DateTime endDate)
		{
			var entities = _saleDAO.GetByDateRange(startDate, endDate);
			return entities.Select(e => MapToDto(e)).ToList();
		}

		public void AddSale(SaleCreateDto dto)
		{
			if (dto.BookId <= 0)
				throw new ArgumentException("Выберите книгу");
			if (dto.ReaderId <= 0)
				throw new ArgumentException("Выберите покупателя");
			if (dto.Quantity <= 0)
				throw new ArgumentException("Количество должно быть больше 0");
			if (dto.UnitPrice < 0)
				throw new ArgumentException("Цена не может быть отрицательной");
			if (dto.SaleDate == default)
				throw new ArgumentException("Укажите дату продажи");
			if (dto.SaleDate > DateTime.Today)
				throw new ArgumentException("Дата продажи не может быть в будущем");
			if (dto.SaleDate < new DateTime(2000, 1, 1))
				throw new ArgumentException("Дата продажи не может быть раньше 2000 года");

			int available = _instanceDAO.GetAvailableCount(dto.BookId);
			if (dto.Quantity > available)
				throw new ArgumentException($"Доступно только {available} экземпляров");

			var entity = new SaleEntity
			{
				BookID = dto.BookId,
				ReaderID = dto.ReaderId,
				Quantity = dto.Quantity,
				UnitPrice = dto.UnitPrice,
				TotalAmount = dto.Quantity * dto.UnitPrice,
				SaleDate = dto.SaleDate,
				Notes = dto.Notes
			};

			_saleDAO.Insert(entity);
			_instanceDAO.DeleteAvailable(dto.BookId, dto.Quantity);
		}

		public SaleReportDto GetReport(DateTime startDate, DateTime endDate)
		{
			var entities = _saleDAO.GetByDateRange(startDate, endDate);
			var sales = entities.Select(e => MapToDto(e)).ToList();

			decimal totalAmount = _saleDAO.GetTotalSales(startDate, endDate);
			int totalQuantity = _saleDAO.GetTotalQuantity(startDate, endDate);

			return new SaleReportDto
			{
				Sales = sales,
				TotalAmount = totalAmount,
				TotalQuantity = totalQuantity,
				StartDate = startDate,
				EndDate = endDate
			};
		}

		public SaleDto GetSaleById(int id)
		{
			var entity = _saleDAO.GetById(id);
			return entity != null ? MapToDto(entity) : null;
		}

		public void DeleteSale(int id)
		{
			_saleDAO.Delete(id);
		}

		private SaleDto MapToDto(SaleEntity e)
		{
			var book = _bookDAO.GetById(e.BookID);
			var reader = _readerDAO.GetById(e.ReaderID);

			return new SaleDto
			{
				SaleId = e.SaleID,
				BookId = e.BookID,
				ReaderId = e.ReaderID,
				SaleDate = e.SaleDate,
				Quantity = e.Quantity,
				UnitPrice = e.UnitPrice,
				TotalAmount = e.TotalAmount,
				Notes = e.Notes,
				BookTitle = book?.Title ?? "Неизвестная книга",
				Buyer = reader?.FullName ?? "Неизвестный покупатель"
			};
		}
	}

	public class SaleReportDto
	{
		public List<SaleDto> Sales { get; set; }
		public decimal TotalAmount { get; set; }
		public int TotalQuantity { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
	}
}