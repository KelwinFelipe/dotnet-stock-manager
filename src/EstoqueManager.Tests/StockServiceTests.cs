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
        private readonly ILogService _fakeLogService;
        private readonly IStockMovementService _fakeMovementService;

        public StockServiceTests()
        {
            _fakeLogService = new FakeLogService();
            _fakeMovementService = new FakeStockMovementService();
            CleanTestData();
        }

        public void Dispose()
        {
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
            Assert.Equal(10, product.MinStockThreshold); // default threshold
            Assert.True(product.IsActive);
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
            var service = new StockService(_fakeLogService, _fakeMovementService);
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
            var service = new StockService(_fakeLogService, _fakeMovementService);
            var product1 = new Product("Fone Ouvido", 80m, 10);
            var product2 = new Product("fone ouvido", 90m, 5);

            await service.AddAsync(product1);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddAsync(product2));
        }

        [Fact]
        public async Task StockService_UpdateQuantity_ShouldChangeValueAndPersist()
        {
            // Arrange
            var service = new StockService(_fakeLogService, _fakeMovementService);
            var product = new Product("Monitor IPS", 1200m, 5);
            await service.AddAsync(product);

            // Act
            var updated = await service.UpdateQuantityAsync(product.Id, 12, "Entrada de estoque");
            var retrieved = service.GetById(product.Id);

            // Assert
            Assert.True(updated);
            Assert.NotNull(retrieved);
            Assert.Equal(12, retrieved.Quantity);
        }

        [Fact]
        public async Task StockService_RemoveProduct_ShouldSoftDeleteSuccessfully()
        {
            // Arrange
            var service = new StockService(_fakeLogService, _fakeMovementService);
            var product = new Product("Cabo HDMI", 25m, 50);
            await service.AddAsync(product);

            // Act
            var removed = await service.RemoveAsync(product.Id);
            var listActive = service.List(includeInactive: false);
            var listAll = service.List(includeInactive: true);

            // Assert
            Assert.True(removed);
            Assert.Empty(listActive);
            Assert.Single(listAll);
            Assert.False(listAll[0].IsActive);
        }

        [Fact]
        public async Task StockService_RestoreProduct_ShouldRestoreSuccessfully()
        {
            // Arrange
            var service = new StockService(_fakeLogService, _fakeMovementService);
            var product = new Product("Mouse Pad", 50m, 20);
            await service.AddAsync(product);

            await service.RemoveAsync(product.Id);
            Assert.False(product.IsActive);

            // Act
            var restored = await service.RestoreAsync(product.Id);
            var listActive = service.List();

            // Assert
            Assert.True(restored);
            Assert.Single(listActive);
            Assert.True(listActive[0].IsActive);
        }

        [Fact]
        public async Task StockService_RemoveNonExistingProduct_ShouldReturnFalse()
        {
            // Arrange
            var service = new StockService(_fakeLogService, _fakeMovementService);

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
            ILogService logService = new LogService();

            // Act
            await logService.LogAsync(testMessage);
            var logs = await logService.ReadLastLogsAsync(5);

            // Assert
            Assert.NotEmpty(logs);
            Assert.Contains(logs, l => l.Contains(testMessage));

            // Clean up
            var logPath = Path.Combine("data", "app.log");
            if (File.Exists(logPath))
            {
                try
                {
                    File.Delete(logPath);
                }
                catch { }
            }
        }

        [Fact]
        public async Task StockService_UpdateProduct_ShouldCopyDescriptionAndThresholdFields()
        {
            // Arrange
            var service = new StockService(_fakeLogService, _fakeMovementService);
            var product = new Product("Teclado Mecânico", 299.90m, 15) { Description = "Original Description", MinStockThreshold = 5 };
            await service.AddAsync(product);

            var updatedProduct = new Product("Teclado Mecânico", 350.00m, 10) { Description = "New Description", MinStockThreshold = 2 };

            // Act
            var success = await service.UpdateProductAsync(product.Id, updatedProduct);
            var retrieved = service.GetById(product.Id);

            // Assert
            Assert.True(success);
            Assert.NotNull(retrieved);
            Assert.Equal("New Description", retrieved.Description);
            Assert.Equal(350.00m, retrieved.Price);
            Assert.Equal(10, retrieved.Quantity);
            Assert.Equal(2, retrieved.MinStockThreshold);
        }

        [Fact]
        public async Task StockService_UpdateProduct_WithDuplicateName_ShouldThrow()
        {
            // Arrange
            var service = new StockService(_fakeLogService, _fakeMovementService);
            var product1 = new Product("Teclado Mecânico", 299.90m, 15);
            var product2 = new Product("Mouse Gamer", 150m, 10);
            await service.AddAsync(product1);
            await service.AddAsync(product2);

            var updatedProduct = new Product("Mouse Gamer", 299.90m, 15);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateProductAsync(product1.Id, updatedProduct));
        }
    }
}
