using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EstoqueManager.Core;

namespace EstoqueManager.Tests
{
    public class FakeLogService : ILogService
    {
        public List<string> Logs { get; } = new();

        public Task LogAsync(string message)
        {
            Logs.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
            return Task.CompletedTask;
        }

        public Task<List<string>> ReadLastLogsAsync(int count = 50)
        {
            return Task.FromResult(Logs.Skip(Math.Max(0, Logs.Count - count)).ToList());
        }
    }

    public class FakeStockMovementService : IStockMovementService
    {
        public List<StockMovement> Movements { get; } = new();

        public Task AddMovementAsync(StockMovement movement)
        {
            Movements.Add(movement);
            return Task.CompletedTask;
        }

        public Task<List<StockMovement>> GetMovementsByProductIdAsync(Guid productId)
        {
            return Task.FromResult(Movements.Where(m => m.ProductId == productId).ToList());
        }

        public Task<List<StockMovement>> GetAllMovementsAsync()
        {
            return Task.FromResult(Movements.ToList());
        }
    }
}
