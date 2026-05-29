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
    /// Serviço responsável pela persistência, auditoria e manipulação assíncrona da coleção de produtos.
    /// </summary>
    public class StockService : IStockService
    {
        private readonly string _filePath = Path.Combine("data", "products.json");
        private ConcurrentDictionary<Guid, Product> _products = new();
        private readonly ILogService _logService;
        private readonly IStockMovementService _stockMovementService;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Construtor que inicializa o serviço com injeção de dependências e carregamento dos dados.
        /// </summary>
        public StockService(ILogService logService, IStockMovementService stockMovementService)
        {
            _logService = logService;
            _stockMovementService = stockMovementService;
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
            if (_products.Values.Any(p => p.Name.Equals(product.Name, StringComparison.OrdinalIgnoreCase) && p.IsActive))
            {
                throw new InvalidOperationException($"Já existe um produto ativo cadastrado com o nome '{product.Name}'.");
            }

            _products[product.Id] = product;
            await SaveDataAsync();

            await _logService.LogAsync($"PRODUTO ADICIONADO - ID: {product.Id} | Nome: {product.Name} | Preço: {product.Price:C2} | Qtd Inicial: {product.Quantity}");

            var movement = new StockMovement(
                product.Id,
                product.Name,
                product.Quantity,
                0,
                product.Quantity,
                "Cadastro",
                "Cadastro inicial do produto"
            );
            await _stockMovementService.AddMovementAsync(movement);
        }

        /// <summary>
        /// Obtém a listagem dos produtos cadastrados.
        /// </summary>
        /// <param name="includeInactive">Indica se deve incluir os produtos desativados (soft deleted).</param>
        /// <returns>Uma lista contendo os produtos.</returns>
        public List<Product> List(bool includeInactive = false)
        {
            return _products.Values
                .Where(p => includeInactive || p.IsActive)
                .ToList();
        }

        /// <summary>
        /// Busca um produto pelo nome exato.
        /// </summary>
        public Product? GetByName(string name)
        {
            return _products.Values
                .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Busca um produto através de seu identificador exclusivo (Guid).
        /// </summary>
        public Product? GetById(Guid id)
        {
            _products.TryGetValue(id, out var product);
            return product;
        }

        /// <summary>
        /// Atualiza a quantidade disponível em estoque de um determinado produto de forma assíncrona.
        /// </summary>
        public async Task<bool> UpdateQuantityAsync(Guid id, int newQuantity, string reason = "Ajuste manual")
        {
            if (!_products.TryGetValue(id, out var product))
                return false;

            int oldQuantity = product.Quantity;
            int delta = newQuantity - oldQuantity;
            product.Quantity = newQuantity;
            product.UpdatedAt = DateTime.Now;
            _products[id] = product;

            await SaveDataAsync();
            await _logService.LogAsync($"ESTOQUE ATUALIZADO - ID: {product.Id} | Nome: {product.Name} | Qtd: {oldQuantity} -> {newQuantity} | Motivo: {reason}");

            string type = delta >= 0 ? "Entrada" : "Saída";
            var movement = new StockMovement(
                product.Id,
                product.Name,
                delta,
                oldQuantity,
                newQuantity,
                type,
                reason
            );
            await _stockMovementService.AddMovementAsync(movement);

            return true;
        }

        /// <summary>
        /// Atualiza todos os dados de um produto existente.
        /// </summary>
        public async Task<bool> UpdateProductAsync(Guid id, Product updatedProduct)
        {
            if (!_products.TryGetValue(id, out var product))
                return false;

            // Validação de colisão de nome para edição (somente entre produtos ativos)
            if (!product.Name.Equals(updatedProduct.Name, StringComparison.OrdinalIgnoreCase) && 
                _products.Values.Any(p => p.Name.Equals(updatedProduct.Name, StringComparison.OrdinalIgnoreCase) && p.IsActive))
            {
                throw new InvalidOperationException($"Já existe um produto ativo cadastrado com o nome '{updatedProduct.Name}'.");
            }

            int oldQuantity = product.Quantity;
            int newQuantity = updatedProduct.Quantity;
            int delta = newQuantity - oldQuantity;

            product.Name = updatedProduct.Name;
            product.Price = updatedProduct.Price;
            product.Quantity = updatedProduct.Quantity;
            product.CategoryId = updatedProduct.CategoryId;
            product.Description = updatedProduct.Description;
            product.MinStockThreshold = updatedProduct.MinStockThreshold;
            product.IsActive = updatedProduct.IsActive;
            product.UpdatedAt = DateTime.Now;
            _products[id] = product;

            await SaveDataAsync();
            await _logService.LogAsync($"PRODUTO ATUALIZADO (COMPLETO) - ID: {product.Id} | Nome: {product.Name}");

            if (delta != 0)
            {
                string type = delta >= 0 ? "Entrada" : "Saída";
                var movement = new StockMovement(
                    product.Id,
                    product.Name,
                    delta,
                    oldQuantity,
                    newQuantity,
                    type,
                    "Edição manual de produto"
                );
                await _stockMovementService.AddMovementAsync(movement);
            }

            return true;
        }

        /// <summary>
        /// Desativa um produto (Soft Delete) e registra a ação e a movimentação associada.
        /// </summary>
        public async Task<bool> RemoveAsync(Guid id)
        {
            if (!_products.TryGetValue(id, out var product))
                return false;

            if (!product.IsActive)
                return false; // Já inativo

            int oldQuantity = product.Quantity;
            product.IsActive = false;
            product.UpdatedAt = DateTime.Now;
            _products[id] = product;

            await SaveDataAsync();
            await _logService.LogAsync($"PRODUTO DESATIVADO (SOFT DELETE) - ID: {id} | Nome: {product.Name}");

            var movement = new StockMovement(
                product.Id,
                product.Name,
                -oldQuantity,
                oldQuantity,
                0,
                "Remoção",
                "Desativação/Soft delete do produto"
            );
            await _stockMovementService.AddMovementAsync(movement);

            return true;
        }

        /// <summary>
        /// Restaura um produto inativo (Soft Delete) e registra a movimentação de estoque.
        /// </summary>
        public async Task<bool> RestoreAsync(Guid id)
        {
            if (!_products.TryGetValue(id, out var product))
                return false;

            if (product.IsActive)
                return false; // Já ativo

            product.IsActive = true;
            product.UpdatedAt = DateTime.Now;
            _products[id] = product;

            await SaveDataAsync();
            await _logService.LogAsync($"PRODUTO RESTAURADO - ID: {id} | Nome: {product.Name}");

            var movement = new StockMovement(
                product.Id,
                product.Name,
                product.Quantity,
                0,
                product.Quantity,
                "Entrada",
                "Restauração do produto"
            );
            await _stockMovementService.AddMovementAsync(movement);

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
                _products = new ConcurrentDictionary<Guid, Product>();
            }
        }

        /// <summary>
        /// Persiste as informações da coleção em memória para o arquivo local JSON de forma assíncrona e segura.
        /// </summary>
        private async Task SaveDataAsync()
        {
            await _semaphore.WaitAsync();
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
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
