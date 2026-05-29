using System.Collections.Generic;
using System.Threading.Tasks;

namespace EstoqueManager.Core
{
    public interface IExportService
    {
        Task<string> GenerateXmlAsync(IEnumerable<Product> products);
        Task<byte[]> GenerateCsvAsync(IEnumerable<Product> products);
        Task<byte[]> GeneratePdfAsync(IEnumerable<Product> products);
    }
}
