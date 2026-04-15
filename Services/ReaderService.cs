using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using biblioteka.DAO;
using biblioteka.DTO;
using biblioteka.Entities;

namespace biblioteka.Services
{
	public class ReaderService
	{
		private readonly ReaderDAO _readerDAO;

		public ReaderService()
		{
			_readerDAO = new ReaderDAO();
		}

		public List<ReaderDto> GetAll()
		{
			var entities = _readerDAO.GetAll();
			return entities.Select(e => MapToDto(e)).ToList();
		}

		public ReaderDto GetById(int id)
		{
			var entity = _readerDAO.GetById(id);
			return entity != null ? MapToDto(entity) : null;
		}

		public List<ReaderDto> Search(string searchText)
		{
			var entities = _readerDAO.Search(searchText);
			return entities.Select(e => MapToDto(e)).ToList();
		}

		public void Add(ReaderCreateDto dto)
		{
			ValidateReader(dto);

			var entity = new ReaderEntity
			{
				FullName = dto.FullName.Trim(),
				Phone = dto.Phone?.Trim(),
				Address = dto.Address?.Trim(),
				BirthDate = dto.BirthDate,
				RegistrationDate = dto.RegistrationDate
			};

			_readerDAO.Insert(entity);
		}

		public void Update(int id, ReaderCreateDto dto)
		{
			ValidateReader(dto);

			var entity = new ReaderEntity
			{
				ID = id,
				FullName = dto.FullName.Trim(),
				Phone = dto.Phone?.Trim(),
				Address = dto.Address?.Trim(),
				BirthDate = dto.BirthDate,
				RegistrationDate = dto.RegistrationDate
			};

			_readerDAO.Update(entity);
		}

		public void Delete(int id)
		{
			// Проверяем, есть ли у читателя активные выдачи
			if (_readerDAO.HasActiveIssues(id))
				throw new InvalidOperationException("Нельзя удалить читателя, у которого есть невозвращённые книги");

			// Проверяем, есть ли у читателя покупки
			if (_readerDAO.HasSales(id))
				throw new InvalidOperationException("Нельзя удалить читателя, у которого есть история покупок");

			_readerDAO.Delete(id);
		}

		public int GetTotalCount()
		{
			return _readerDAO.GetTotalCount();
		}

		private void ValidateReader(ReaderCreateDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.FullName))
				throw new ArgumentException("ФИО обязательно");

			if (string.IsNullOrWhiteSpace(dto.Phone) || dto.Phone == "+375")
				throw new ArgumentException("Введите телефон");

			// Проверка формата телефона (только цифры, +, -, скобки, пробелы)
			if (!Regex.IsMatch(dto.Phone, @"^[\d\+\-\(\)\s]+$"))
				throw new ArgumentException("Некорректный формат телефона");

			string digits = Regex.Replace(dto.Phone, @"[^\d]", "");
			if (digits.Length < 7 || digits.Length > 15)
				throw new ArgumentException("Телефон должен содержать от 7 до 15 цифр");

			if (string.IsNullOrWhiteSpace(dto.Address))
				throw new ArgumentException("Адрес обязателен");

			if (dto.BirthDate == default)
				throw new ArgumentException("Дата рождения не указана");

			var minBirthDate = DateTime.Today.AddYears(-14);
			if (dto.BirthDate > minBirthDate)
				throw new ArgumentException("Читатель должен быть старше 14 лет");

			if (dto.BirthDate < new DateTime(1900, 1, 1))
				throw new ArgumentException("Дата рождения не может быть раньше 1900 года");

			if (dto.RegistrationDate == default)
				throw new ArgumentException("Дата регистрации не указана");

			if (dto.BirthDate >= dto.RegistrationDate)
				throw new ArgumentException("Дата рождения должна быть раньше даты регистрации");
		}

		private ReaderDto MapToDto(ReaderEntity e)
		{
			return new ReaderDto
			{
				Id = e.ID,
				FullName = e.FullName,
				Phone = e.Phone,
				Address = e.Address,
				BirthDate = e.BirthDate,
				RegistrationDate = e.RegistrationDate
			};
		}
	}
}