using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EstoqueManager.Core;

namespace EstoqueManager.Data
{
    public class StockMovementService : IStockMovementService
    {
        private readonly string _filePath = Path.Combine("data", "movements.json");
        private List<StockMovement> _movements = new();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public StockMovementService()
        {
            if (!Directory.Exists("data"))
            {
                Directory.CreateDirectory("data");
            }
            LoadData();
        }

        public async Task AddMovementAsync(StockMovement movement)
        {
            await _semaphore.WaitAsync();
            try
            {
                _movements.Add(movement);
                await SaveDataAsyncInternal();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<StockMovement>> GetMovementsByProductIdAsync(Guid productId)
        {
            await _semaphore.WaitAsync();
            try
            {
                return _movements
                    .Where(m => m.ProductId == productId)
                    .OrderByDescending(m => m.CreatedAt)
                    .ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<StockMovement>> GetAllMovementsAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return _movements
                    .OrderByDescending(m => m.CreatedAt)
                    .ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void LoadData()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    _movements = JsonSerializer.Deserialize<List<StockMovement>>(json) ?? new List<StockMovement>();
                }
            }
            catch
            {
                _movements = new List<StockMovement>();
            }
        }

        private async Task SaveDataAsyncInternal()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_movements, options);
                await File.WriteAllTextAsync(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar movimentações localmente: {ex.Message}");
            }
        }
    }
}
