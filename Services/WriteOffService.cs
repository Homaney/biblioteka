using System;
using System.Collections.Generic;
using biblioteka.DAO;
using biblioteka.DTO;
using biblioteka.Entities;

namespace biblioteka.Services
{
    public class WriteOffService
    {
        private readonly WriteOffDAO _writeOffDAO = new WriteOffDAO();
        private readonly BookInstanceDAO _instanceDAO = new BookInstanceDAO();

        public void CreateWriteOffAct(WriteOffActCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ActNumber))
                throw new ArgumentException("Введите номер акта");
            if (dto.InstanceIds == null || dto.InstanceIds.Count == 0)
                throw new ArgumentException("Выберите экземпляры для списания");

            // Проверяем, что все экземпляры доступны
            foreach (var id in dto.InstanceIds)
            {
                var instance = _instanceDAO.GetById(id);
                if (instance == null)
                    throw new ArgumentException($"Экземпляр с ID {id} не найден");
                if (instance.Status != "Доступна")
                    throw new ArgumentException($"Экземпляр {instance.InventoryNumber} недоступен для списания");
            }

            var act = new WriteOffActEntity
            {
                ActNumber = dto.ActNumber,
                WriteOffDate = dto.WriteOffDate,
                Reason = dto.Reason
            };

            _writeOffDAO.Insert(act, dto.InstanceIds);
        }

        public void UpdateAct(WriteOffActUpdateDto dto)
        {
            if (dto.Id <= 0)
                throw new ArgumentException("Неверный ID акта");
            if (string.IsNullOrWhiteSpace(dto.ActNumber))
                throw new ArgumentException("Введите номер акта");
            if (dto.InstanceIds == null || dto.InstanceIds.Count == 0)
                throw new ArgumentException("Выберите экземпляры для списания");

            var act = new WriteOffActEntity
            {
                ID = dto.Id,
                ActNumber = dto.ActNumber,
                WriteOffDate = dto.WriteOffDate,
                Reason = dto.Reason
            };

            _writeOffDAO.UpdateAct(act, dto.InstanceIds);
        }
        public List<WriteOffActWithCount> GetAllActsWithCount()
        {
            return _writeOffDAO.GetAllWithCount();
        }
        public List<WriteOffActDto> GetAllActs()
        {
            var entities = _writeOffDAO.GetAll();
            var result = new List<WriteOffActDto>();
            foreach (var e in entities)
            {
                result.Add(new WriteOffActDto
                {
                    Id = e.ID,
                    ActNumber = e.ActNumber,
                    WriteOffDate = e.WriteOffDate,
                    Reason = e.Reason,
                    CreatedAt = e.CreatedAt
                });
            }
            return result;
        }
    }
}