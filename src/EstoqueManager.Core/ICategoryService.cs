using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EstoqueManager.Core
{
    public interface ICategoryService
    {
        List<Category> List();
        Category? GetById(Guid id);
        Task AddAsync(Category category);
        Task<bool> UpdateAsync(Guid id, string newName, string newDescription);
        Task<bool> RemoveAsync(Guid id);
    }
}
