using System;
using System.Collections.Generic;
using System.Linq;
using biblioteka.DAO;
using biblioteka.DTO;
using biblioteka.Entities;

namespace biblioteka.Services
{
    public class UDKService
    {
        private readonly UDKDAO _dao;

        public UDKService()
        {
            _dao = new UDKDAO();
        }

        public List<UDKDto> GetAll()
        {
            var entities = _dao.GetAll();
            return entities.Select(e => new UDKDto(e.ID, e.Code, e.Description)).ToList();
        }

        public UDKDto GetById(int id)
        {
            var entity = _dao.GetById(id);
            if (entity == null) return null;
            return new UDKDto(entity.ID, entity.Code, entity.Description);
        }

        public void Add(UDKCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new ArgumentException("Код УДК обязателен");
            if (string.IsNullOrWhiteSpace(dto.Description))
                throw new ArgumentException("Описание УДК обязательно");

            var all = _dao.GetAll();
            if (all.Any(e => e.Code.Equals(dto.Code, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("УДК с таким кодом уже существует");

            var entity = new UDKEntity
            {
                Code = dto.Code,
                Description = dto.Description
            };
            _dao.Insert(entity);
        }

        public void Update(int id, UDKCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new ArgumentException("Код УДК обязателен");
            if (string.IsNullOrWhiteSpace(dto.Description))
                throw new ArgumentException("Описание УДК обязательно");

            var all = _dao.GetAll();
            if (all.Any(e => e.Code.Equals(dto.Code, StringComparison.OrdinalIgnoreCase) && e.ID != id))
                throw new ArgumentException("УДК с таким кодом уже существует");

            var entity = new UDKEntity
            {
                ID = id,
                Code = dto.Code,
                Description = dto.Description
            };
            _dao.Update(entity);
        }

        public void Delete(int id)
        {
            if (_dao.IsUsedInBooks(id))
                throw new InvalidOperationException("Нельзя удалить УДК, так как он используется в книгах");
            _dao.Delete(id);
        }
    }
}