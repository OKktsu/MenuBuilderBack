using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuBuilderBack.Repository.Interface;
using MenuBuilderBack.Service.Interface;

namespace MenuBuilderBack.Service
{
    public class GenericService<T> : IGenericService<T> where T : class
    {
        readonly IGenericRepository<T> _repository;

        public GenericService(IGenericRepository<T> repository)
        {
            _repository = repository;
        }

        public async Task<T> CreateAsync(T entity)
            => await _repository.CreateAsync(entity);
        
        public async Task<bool> DeleteAsync(int id)
            => await _repository.DeleteAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync()
            => await _repository.GetAllAsync();

        public async Task<T?> GetByIdAsync(int id)
            => await _repository.GetByIdAsync(id);

        public async Task<T> UpdateAsync(T entity)
            => await _repository.UpdateAsync(entity);
    }
}