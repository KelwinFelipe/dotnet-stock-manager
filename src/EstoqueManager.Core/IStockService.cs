using System.Collections.Generic;
using System.Threading.Tasks;


namespace EstoqueManager.Core
{
    public interface IStockService
    {
        List<Product> List();
        Product? GetById(Guid id);
        Product? GetByName(string name);
        Task AddAsync(Product product);
        Task<bool> UpdateQuantityAsync(Guid id, int newQuantity);
        Task<bool> UpdateProductAsync(Guid id, Product updatedProduct);
        Task<bool> RemoveAsync(Guid id);
    }

    public interface ICategoryService
    {
        List<Category> List();
        Category? GetById(Guid id);
        Task AddAsync(Category category);
        Task<bool> UpdateAsync(Guid id, string newName, string newDescription);
        Task<bool> RemoveAsync(Guid id);
    }
}
