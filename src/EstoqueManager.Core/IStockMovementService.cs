using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EstoqueManager.Core
{
    public interface IStockMovementService
    {
        Task AddMovementAsync(StockMovement movement);
        Task<List<StockMovement>> GetMovementsByProductIdAsync(Guid productId);
        Task<List<StockMovement>> GetAllMovementsAsync();
    }
}
