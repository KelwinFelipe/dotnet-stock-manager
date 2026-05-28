using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EstoqueManager.Core;
using EstoqueManager.Export;
using EstoqueManager.Data;
using Xunit;

namespace EstoqueManager.Tests
{
    public class CsvImportExportTests
    {
        [Fact]
        public async Task ExportService_GenerateCsv_ShouldIncludeDescriptionAndCorrectHeaders()
        {
            // Arrange
            var exportService = new ExportService();
            var products = new[]
            {
                new Product("Gamer Mouse", 150.00m, 10) { Description = "Red LED, 2400 DPI" },
                new Product("Office Keyboard", 85.50m, 5) { Description = "Silent keys, membrane" }
            };

            // Act
            var csvBytes = await exportService.GenerateCsvAsync(products);
            var csvContent = Encoding.UTF8.GetString(csvBytes);
            var lines = csvContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Assert
            Assert.NotEmpty(lines);
            
            // Check headers
            var header = lines[0];
            Assert.Contains("Descrição", header);
            Assert.Contains("Preço", header);

            // Check content
            Assert.Equal(3, lines.Length); // 1 header + 2 products
            Assert.Contains("Red LED, 2400 DPI", lines[1]);
            Assert.Contains("Silent keys, membrane", lines[2]);
        }

        [Theory]
        [InlineData("Col1,Col2,Col3", ',', new[] { "Col1", "Col2", "Col3" })]
        [InlineData("Col1;Col2;Col3", ';', new[] { "Col1", "Col2", "Col3" })]
        [InlineData("\"Name, Comma\",12.50,10", ',', new[] { "Name, Comma", "12.50", "10" })]
        [InlineData("\"Name, \\\"Escaped\\\"\",12.50,10", ',', new[] { "Name, \\\"Escaped\\\"", "12.50", "10" })]
        public void CsvParserHelper_SplitCsvLine_ShouldParseFieldsCorrectly(string line, char separator, string[] expected)
        {
            // Act
            var result = CsvParserHelper.SplitCsvLine(line, separator);

            // Assert
            Assert.Equal(expected.Length, result.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], result[i]);
            }
        }
    }
}
