using System.Text.Json;
using EstoqueManager.Core;

namespace EstoqueManager.Data;

/// <summary>
/// Serviço responsável pela persistência e manipulação assíncrona da coleção de categorias.
/// </summary>
public class CategoryService
{
    private readonly string _filePath = "categories.json";
    private List<Category> _categories = [];

    public CategoryService()
    {
        LoadData();
    }

    public async Task AddAsync(Category category)
    {
        if (_categories.Any(c => c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Já existe uma categoria cadastrada com o nome '{category.Name}'.");
        }

        _categories.Add(category);
        await SaveDataAsync();
        await LogService.LogAsync($"CATEGORIA ADICIONADA - ID: {category.Id} | Nome: {category.Name}");
    }

    public List<Category> List()
    {
        return _categories;
    }

    public Category? GetById(Guid id)
    {
        return _categories.FirstOrDefault(c => c.Id == id);
    }

    public async Task<bool> UpdateAsync(Guid id, string newName, string newDescription)
    {
        var category = GetById(id);
        if (category == null)
            return false;

        // Verifica colisão de nome (se mudou o nome para um existente)
        if (!category.Name.Equals(newName, StringComparison.OrdinalIgnoreCase) && 
            _categories.Any(c => c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Já existe uma categoria cadastrada com o nome '{newName}'.");
        }

        category.Name = newName;
        category.Description = newDescription;

        await SaveDataAsync();
        await LogService.LogAsync($"CATEGORIA ATUALIZADA - ID: {category.Id} | Nome: {category.Name}");
        
        return true;
    }

    public async Task<bool> RemoveAsync(Guid id)
    {
        var category = GetById(id);
        if (category == null)
            return false;

        _categories.Remove(category);
        
        await SaveDataAsync();
        await LogService.LogAsync($"CATEGORIA REMOVIDA - ID: {category.Id} | Nome: {category.Name}");
        
        return true;
    }

    private void LoadData()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _categories = JsonSerializer.Deserialize<List<Category>>(json) ?? [];
            }
        }
        catch
        {
            _categories = [];
        }
    }

    private async Task SaveDataAsync()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_categories, options);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar categorias localmente: {ex.Message}");
        }
    }
}
