using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EstoqueManager.Core
{
    public interface IStockService
    {
        List<Product> List(bool includeInactive = false);
        Product? GetById(Guid id);
        Product? GetByName(string name);
        Task AddAsync(Product product);
        Task<bool> UpdateQuantityAsync(Guid id, int newQuantity, string reason = "Ajuste manual");
        Task<bool> UpdateProductAsync(Guid id, Product updatedProduct);
        Task<bool> RemoveAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
    }
}
