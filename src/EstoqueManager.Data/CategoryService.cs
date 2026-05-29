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
    /// <summary>
    /// Serviço responsável pela persistência e manipulação assíncrona da coleção de categorias.
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly string _filePath = Path.Combine("data", "categories.json");
        private ConcurrentDictionary<Guid, Category> _categories = new();
        private readonly ILogService _logService;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public CategoryService(ILogService logService)
        {
            _logService = logService;
            if (!Directory.Exists("data")) Directory.CreateDirectory("data");
            LoadData();
        }

        public async Task AddAsync(Category category)
        {
            if (_categories.Values.Any(c => c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Já existe uma categoria cadastrada com o nome '{category.Name}'.");
            }

            _categories[category.Id] = category;
            await SaveDataAsync();
            await _logService.LogAsync($"CATEGORIA ADICIONADA - ID: {category.Id} | Nome: {category.Name}");
        }

        public List<Category> List()
        {
            return _categories.Values.ToList();
        }

        public Category? GetById(Guid id)
        {
            _categories.TryGetValue(id, out var category);
            return category;
        }

        public async Task<bool> UpdateAsync(Guid id, string newName, string newDescription)
        {
            if (!_categories.TryGetValue(id, out var category))
                return false;

            // Verifica colisão de nome (se mudou o nome para um existente)
            if (!category.Name.Equals(newName, StringComparison.OrdinalIgnoreCase) &&
                _categories.Values.Any(c => c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Já existe uma categoria cadastrada com o nome '{newName}'.");
            }

            category.Name = newName;
            category.Description = newDescription;
            _categories[id] = category;

            await SaveDataAsync();
            await _logService.LogAsync($"CATEGORIA ATUALIZADA - ID: {category.Id} | Nome: {category.Name}");

            return true;
        }

        public async Task<bool> RemoveAsync(Guid id)
        {
            if (!_categories.TryRemove(id, out var removedCategory))
                return false;

            await SaveDataAsync();
            await _logService.LogAsync($"CATEGORIA REMOVIDA - ID: {id} | Nome: {removedCategory?.Name}");

            return true;
        }

        private void LoadData()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<Category>>(json) ?? new List<Category>();
                    _categories = new ConcurrentDictionary<Guid, Category>(list.ToDictionary(c => c.Id, c => c));
                }
            }
            catch
            {
                _categories = new ConcurrentDictionary<Guid, Category>();
            }
        }

        private async Task SaveDataAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_categories.Values, options);
                await File.WriteAllTextAsync(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar categorias localmente: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
