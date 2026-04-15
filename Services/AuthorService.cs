using System;
using System.Collections.Generic;
using System.Linq;
using biblioteka.DAO;
using biblioteka.DTO;
using biblioteka.Entities;

namespace biblioteka.Services
{
	public class AuthorService
	{
		private readonly AuthorDAO _dao = new AuthorDAO();

		public List<AuthorDto> GetAll()
		{
			var entities = _dao.GetAll();
			return entities.Select(e => new AuthorDto
			{
				Id = e.ID,
				FullName = e.FullName
			}).ToList();
		}

		public void Add(AuthorCreateDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.FullName))
				throw new ArgumentException("Имя автора не может быть пустым");

			var all = _dao.GetAll();
			if (all.Any(e => e.FullName.Equals(dto.FullName.Trim(), StringComparison.OrdinalIgnoreCase)))
				throw new ArgumentException("Такой автор уже существует");

			var entity = new AuthorEntity
			{
				FullName = dto.FullName.Trim()
			};
			_dao.Insert(entity);
		}

		public void Delete(int id)
		{
			if (_dao.IsUsedInBooks(id))
				throw new InvalidOperationException("Нельзя удалить автора, так как он используется в книгах");
			_dao.Delete(id);
		}
	}
}