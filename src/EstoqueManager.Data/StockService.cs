using System.Text.Json;
using EstoqueManager.Core;

namespace EstoqueManager.Data;

/// <summary>
/// Serviço responsável pela persistência e manipulação da coleção de produtos em estoque.
/// </summary>
public class StockService
{
    // Caminho relativo para armazenamento local em arquivo JSON
    private readonly string _filePath = "products.json";
    
    // Coleção principal em memória contendo a lista de produtos
    private List<Product> _products = [];

    /// <summary>
    /// Construtor padrão que inicializa o serviço carregando os dados persistidos.
    /// </summary>
    public StockService()
    {
        LoadData();
    }

    /// <summary>
    /// Adiciona um novo produto ao estoque e persiste as alterações.
    /// </summary>
    /// <param name="product">Instância do produto a ser adicionada.</param>
    public void Add(Product product)
    {
        _products.Add(product);
        SaveData();
    }

    /// <summary>
    /// Obtém a listagem completa dos produtos cadastrados.
    /// </summary>
    /// <returns>Uma lista contendo todos os produtos.</returns>
    public List<Product> List()
    {
        return _products;
    }

    /// <summary>
    /// Busca um produto pelo nome exato ou contendo o termo pesquisado.
    /// </summary>
    /// <param name="name">Termo para busca.</param>
    /// <returns>O primeiro produto que corresponder ao critério, ou null se não localizado.</returns>
    public Product? GetByName(string name)
    {
        return _products
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
        return _products.FirstOrDefault(p => p.Id == id);
    }

    /// <summary>
    /// Atualiza a quantidade disponível em estoque de um determinado produto.
    /// </summary>
    /// <param name="id">ID do produto.</param>
    /// <param name="newQuantity">Nova quantidade a ser atribuída.</param>
    /// <returns>True se atualizado com sucesso; False caso contrário.</returns>
    public bool UpdateQuantity(Guid id, int newQuantity)
    {
        var product = GetById(id);
        if (product == null)
            return false;

        product.Quantity = newQuantity;
        SaveData();
        return true;
    }

    /// <summary>
    /// Remove um produto permanentemente do estoque.
    /// </summary>
    /// <param name="id">ID do produto a ser deletado.</param>
    /// <returns>True se removido com sucesso; False caso contrário.</returns>
    public bool Remove(Guid id)
    {
        var product = GetById(id);
        if (product == null)
            return false;

        _products.Remove(product);
        SaveData();
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
                _products = JsonSerializer.Deserialize<List<Product>>(json) ?? [];
            }
        }
        catch
        {
            // Em caso de falha de leitura, inicializa uma coleção vazia
            _products = [];
        }
    }

    /// <summary>
    /// Persiste as informações da coleção em memória para o arquivo local JSON.
    /// </summary>
    private void SaveData()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_products, options);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar dados localmente: {ex.Message}");
        }
    }
}
