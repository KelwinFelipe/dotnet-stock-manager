using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EstoqueManager.Core;
using EstoqueManager.Data;
using Xunit;

namespace EstoqueManager.Tests
{
    public class CategoryServiceTests : IDisposable
    {
        private readonly string _testDataDir = "data";
        private readonly string _testFilePath = Path.Combine("data", "categories.json");

        public CategoryServiceTests()
        {
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
        public async Task CategoryService_AddCategory_ShouldPersistSuccessfully()
        {
            // Arrange
            var service = new CategoryService();
            var category = new Category("Eletrônicos", "Dispositivos diversos");

            // Act
            await service.AddAsync(category);
            var list = service.List();

            // Assert
            Assert.Single(list);
            Assert.Equal("Eletrônicos", list[0].Name);
            Assert.Equal("Dispositivos diversos", list[0].Description);
        }

        [Fact]
        public async Task CategoryService_AddDuplicateCategory_ShouldThrow()
        {
            // Arrange
            var service = new CategoryService();
            var cat1 = new Category("Móveis");
            var cat2 = new Category("móveis");

            await service.AddAsync(cat1);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddAsync(cat2));
        }

        [Fact]
        public async Task CategoryService_UpdateCategory_ShouldModifyAndPersist()
        {
            // Arrange
            var service = new CategoryService();
            var cat = new Category("Escritório", "Material de escritório");
            await service.AddAsync(cat);

            // Act
            var success = await service.UpdateAsync(cat.Id, "Escritório Novo", "Nova Descrição");
            var retrieved = service.GetById(cat.Id);

            // Assert
            Assert.True(success);
            Assert.NotNull(retrieved);
            Assert.Equal("Escritório Novo", retrieved.Name);
            Assert.Equal("Nova Descrição", retrieved.Description);
        }

        [Fact]
        public async Task CategoryService_RemoveCategory_ShouldRemoveSuccessfully()
        {
            // Arrange
            var service = new CategoryService();
            var cat = new Category("Livros");
            await service.AddAsync(cat);

            // Act
            var removed = await service.RemoveAsync(cat.Id);
            var list = service.List();

            // Assert
            Assert.True(removed);
            Assert.Empty(list);
        }
    }
}
