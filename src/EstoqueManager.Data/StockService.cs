using System.Text.Json;
using EstoqueManager.Core;
using System.Collections.Concurrent;

namespace EstoqueManager.Data;

/// <summary>
/// Serviço responsável pela persistência, auditoria e manipulação assíncrona da coleção de produtos.
/// </summary>
public class StockService : IStockService
{
    // Caminho relativo para armazenamento local em arquivo JSON
    private readonly string _filePath = Path.Combine("data", "products.json");
    
    // Coleção principal em memória contendo a lista de produtos em forma thread‑safe
    private ConcurrentDictionary<Guid, Product> _products = new();
    // Deprecated List, replaced by ConcurrentDictionary
// private List<Product> _products = [];

    /// <summary>
    /// Construtor que inicializa o serviço carregando o histórico de dados existentes de forma síncrona.
    /// </summary>
    public StockService()
    {
        if (!Directory.Exists("data")) Directory.CreateDirectory("data");
        LoadData();
    }

    /// <summary>
    /// Adiciona um novo produto ao estoque de forma assíncrona, prevenindo duplicidade de nomes.
    /// </summary>
    /// <param name="product">Instância do produto a ser adicionada.</param>
    /// <exception cref="InvalidOperationException">Lançada caso um produto com o mesmo nome já exista.</exception>
    public async Task AddAsync(Product product)
    {
        // Validação de unicidade sem distinção de maiúsculas/minúsculas usando ConcurrentDictionary
        if (_products.Values.Any(p => p.Name.Equals(product.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Já existe um produto cadastrado com o nome '{product.Name}'.");
        }

        _products[product.Id] = product;
        await SaveDataAsync();

        // Trilha de auditoria assíncrona
        await LogService.LogAsync($"PRODUTO ADICIONADO - ID: {product.Id} | Nome: {product.Name} | Preço: {product.Price:C2} | Qtd Inicial: {product.Quantity}");
    }

    /// <summary>
    /// Obtém a listagem completa dos produtos cadastrados.
    /// </summary>
    /// <returns>Uma lista contendo todos os produtos.</returns>
    public List<Product> List()
    {
        // Return a copy to avoid external mutation
        return _products.Values.ToList();
    }

    /// <summary>
    /// Busca um produto pelo nome exato ou contendo o termo pesquisado.
    /// </summary>
    /// <param name="name">Termo para busca.</param>
    /// <returns>O primeiro produto que corresponder ao critério, ou null se não localizado.</returns>
    public Product? GetByName(string name)
    {
        return _products.Values
            .FirstOrDefault(p =>
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Busca um produto através de seu identificador exclusivo (Guid).
    /// </summary>
    /// <param name="id">ID do produto.</param>
    /// <returns>O produto encontrado, ou null se inexistente.</returns>
    public Product? GetById(Guid id)
    {
        _products.TryGetValue(id, out var product);
        return product;
    }

    /// <summary>
    /// Atualiza a quantidade disponível em estoque de um determinado produto de forma assíncrona.
    /// </summary>
    /// <param name="id">ID do produto.</param>
    /// <param name="newQuantity">Nova quantidade a ser atribuída.</param>
    /// <returns>True se atualizado com sucesso; False caso contrário.</returns>
    public async Task<bool> UpdateQuantityAsync(Guid id, int newQuantity)
    {
        if (!_products.TryGetValue(id, out var product))
            return false;

        int oldQuantity = product.Quantity;
        product.Quantity = newQuantity;
        _products[id] = product; // ensure updated entry
        
        await SaveDataAsync();
        await LogService.LogAsync($"ESTOQUE ATUALIZADO - ID: {product.Id} | Nome: {product.Name} | Qtd: {oldQuantity} -> {newQuantity}");
        
        return true;
    }

    /// <summary>
    /// Atualiza todos os dados de um produto existente.
    /// </summary>
    public async Task<bool> UpdateProductAsync(Guid id, Product updatedProduct)
    {
        if (!_products.TryGetValue(id, out var product))
            return false;

        // Validação de colisão de nome para edição
        if (!product.Name.Equals(updatedProduct.Name, StringComparison.OrdinalIgnoreCase) && 
            _products.Values.Any(p => p.Name.Equals(updatedProduct.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Já existe um produto cadastrado com o nome '{updatedProduct.Name}'.");
        }

        product.Name = updatedProduct.Name;
        product.Price = updatedProduct.Price;
        product.Quantity = updatedProduct.Quantity;
        product.CategoryId = updatedProduct.CategoryId;
        product.Description = updatedProduct.Description;
        product.UpdatedAt = DateTime.Now;
        _products[id] = product; // persist changes

        await SaveDataAsync();
        await LogService.LogAsync($"PRODUTO ATUALIZADO (COMPLETO) - ID: {product.Id} | Nome: {product.Name}");

        return true;
    }

    /// <summary>
    /// Remove um produto permanentemente do estoque e registra a ação no log de auditoria.
    /// </summary>
    /// <param name="id">ID do produto a ser deletado.</param>
    /// <returns>True se removido com sucesso; False caso contrário.</returns>
    public async Task<bool> RemoveAsync(Guid id)
    {
        var product = GetById(id);
        if (product == null)
            return false;

        _products.TryRemove(id, out var removedProduct);
        
        await SaveDataAsync();
        await LogService.LogAsync($"PRODUTO REMOVIDO - ID: {id} | Nome: {removedProduct?.Name}");
        
        return true;
    }

    /// <summary>
    /// Carrega as informações gravadas em arquivo local JSON para a coleção em memória.
    /// </summary>
    private void LoadData()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var list = JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
                _products = new ConcurrentDictionary<Guid, Product>(list.ToDictionary(p => p.Id, p => p));
            }
        }
        catch
        {
            _products = [];
        }
    }

    /// <summary>
    /// Persiste as informações da coleção em memória para o arquivo local JSON de forma assíncrona.
    /// </summary>
    private async Task SaveDataAsync()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_products.Values, options);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar dados localmente: {ex.Message}");
        }
    }
}
