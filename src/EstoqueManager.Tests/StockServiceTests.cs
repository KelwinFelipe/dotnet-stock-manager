using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EstoqueManager.Core;
using EstoqueManager.Data;
using Xunit;

namespace EstoqueManager.Tests
{
    public class StockServiceTests : IDisposable
    {
        private readonly string _testDataDir = "data";
        private readonly string _testFilePath = Path.Combine("data", "products.json");

        public StockServiceTests()
        {
            // Garantir ambiente limpo antes de cada teste
            CleanTestData();
        }

        public void Dispose()
        {
            // Limpar dados criados após os testes
            CleanTestData();
        }

        private void CleanTestData()
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
            if (Directory.Exists(_testDataDir))
            {
                try
                {
                    Directory.Delete(_testDataDir, true);
                }
                catch
                {
                    // Ignora se não for possível deletar o diretório temporário imediatamente
                }
            }
        }

        [Fact]
        public void Product_Creation_WithValidParameters_ShouldSucceed()
        {
            // Arrange & Act
            var product = new Product("Teclado Mecânico", 299.90m, 15);

            // Assert
            Assert.Equal("Teclado Mecânico", product.Name);
            Assert.Equal(299.90m, product.Price);
            Assert.Equal(15, product.Quantity);
            Assert.NotEqual(Guid.Empty, product.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Product_Creation_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Product(invalidName!, 100m, 5));
        }

        [Fact]
        public void Product_Creation_WithNegativePrice_ShouldThrowArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new Product("Produto Teste", -10m, 5));
        }

        [Fact]
        public void Product_Creation_WithNegativeQuantity_ShouldThrowArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new Product("Produto Teste", 10m, -5));
        }

        [Fact]
        public async Task StockService_AddProduct_ShouldPersistSuccessfully()
        {
            // Arrange
            var service = new StockService();
            var product = new Product("Mouse Gamer", 150m, 10);

            // Act
            await service.AddAsync(product);
            var list = service.List();

            // Assert
            Assert.Single(list);
            Assert.Equal("Mouse Gamer", list[0].Name);
        }

        [Fact]
        public async Task StockService_AddDuplicateProduct_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var service = new StockService();
            var product1 = new Product("Fone Ouvido", 80m, 10);
            var product2 = new Product("fone ouvido", 90m, 5); // Mesmo nome, caixa diferente

            await service.AddAsync(product1);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddAsync(product2));
        }

        [Fact]
        public async Task StockService_UpdateQuantity_ShouldChangeValueAndPersist()
        {
            // Arrange
            var service = new StockService();
            var product = new Product("Monitor IPS", 1200m, 5);
            await service.AddAsync(product);

            // Act
            var updated = await service.UpdateQuantityAsync(product.Id, 12);
            var retrieved = service.GetById(product.Id);

            // Assert
            Assert.True(updated);
            Assert.NotNull(retrieved);
            Assert.Equal(12, retrieved.Quantity);
        }

        [Fact]
        public async Task StockService_RemoveProduct_ShouldRemoveSuccessfully()
        {
            // Arrange
            var service = new StockService();
            var product = new Product("Cabo HDMI", 25m, 50);
            await service.AddAsync(product);

            // Act
            var removed = await service.RemoveAsync(product.Id);
            var list = service.List();

            // Assert
            Assert.True(removed);
            Assert.Empty(list);
        }

        [Fact]
        public async Task StockService_RemoveNonExistingProduct_ShouldReturnFalse()
        {
            // Arrange
            var service = new StockService();

            // Act
            var removed = await service.RemoveAsync(Guid.NewGuid());

            // Assert
            Assert.False(removed);
        }

        [Fact]
        public async Task LogService_LogAndRead_ShouldReturnLoggedMessage()
        {
            // Arrange
            var testMessage = "TEST_LOG_MESSAGE_XUNIT";

            // Act
            await LogService.LogAsync(testMessage);
            var logs = await LogService.ReadLastLogsAsync(5);

            // Assert
            Assert.NotEmpty(logs);
            Assert.Contains(logs, l => l.Contains(testMessage));

            // Clean up
            if (File.Exists("app.log"))
            {
                try
                {
                    File.Delete("app.log");
                }
                catch { }
            }
        }
    }
}
