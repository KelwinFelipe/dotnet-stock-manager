using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public Guid? CategoryId { get; set; }
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

class Program
{
    private static readonly HttpClient client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        while (true)
        {
            Console.WriteLine("\n=== Estoque Manager Console Client ===");
            Console.WriteLine("1. Listar produtos");
            Console.WriteLine("2. Cadastrar produto");
            Console.WriteLine("3. Atualizar quantidade");
            Console.WriteLine("4. Remover produto");
            Console.WriteLine("5. Buscar produto por nome");
            Console.WriteLine("6. Exportar XML");
            Console.WriteLine("7. Exportar PDF");
            Console.WriteLine("0. Sair");
            Console.Write("Escolha uma opção: ");
            var choice = Console.ReadLine();
            Console.WriteLine();
            switch (choice)
            {
                case "1": await ListProducts(); break;
                case "2": await AddProduct(); break;
                case "3": await UpdateQuantity(); break;
                case "4": await DeleteProduct(); break;
                case "5": await SearchProducts(); break;
                case "6": await ExportXml(); break;
                case "7": await ExportPdf(); break;
                case "0": return;
                default: Console.WriteLine("Opção inválida."); break;
            }
        }
    }

    private static async Task ListProducts()
    {
        var products = await client.GetFromJsonAsync<ProductDto[]>("/api/products");
        if (products == null) { Console.WriteLine("Nenhum produto encontrado."); return; }
        foreach (var p in products)
        {
            Console.WriteLine($"{p.Id} | {p.Name} | R${p.Price} | Qty: {p.Quantity}");
        }
    }

    private static async Task AddProduct()
    {
        Console.Write("Nome: "); var name = Console.ReadLine();
        Console.Write("Preço: "); var priceStr = Console.ReadLine();
        Console.Write("Quantidade: "); var qtyStr = Console.ReadLine();
        Console.Write("Categoria Id (opcional): "); var catStr = Console.ReadLine();
        if (!decimal.TryParse(priceStr, out var price) || !int.TryParse(qtyStr, out var qty))
        { Console.WriteLine("Preço ou quantidade inválidos."); return; }
        Guid? catId = null; if (Guid.TryParse(catStr, out var guid)) catId = guid;
        var model = new { Name = name, Price = price, Quantity = qty, CategoryId = catId };
        var response = await client.PostAsJsonAsync("/api/products", model);
        Console.WriteLine(response.IsSuccessStatusCode ? "Produto adicionado." : $"Erro: {response.StatusCode}");
    }

    private static async Task UpdateQuantity()
    {
        Console.Write("Id do produto: "); var idStr = Console.ReadLine();
        Console.Write("Nova quantidade: "); var qtyStr = Console.ReadLine();
        if (!Guid.TryParse(idStr, out var id) || !int.TryParse(qtyStr, out var qty))
        { Console.WriteLine("Id ou quantidade inválidos."); return; }
        var response = await client.PutAsJsonAsync($"/api/products/{id}/quantity", qty);
        Console.WriteLine(response.IsSuccessStatusCode ? "Quantidade atualizada." : $"Erro: {response.StatusCode}");
    }

    private static async Task DeleteProduct()
    {
        Console.Write("Id do produto a remover: "); var idStr = Console.ReadLine();
        if (!Guid.TryParse(idStr, out var id)) { Console.WriteLine("Id inválido."); return; }
        var response = await client.DeleteAsync($"/api/products/{id}");
        Console.WriteLine(response.IsSuccessStatusCode ? "Produto removido." : $"Erro: {response.StatusCode}");
    }

    private static async Task SearchProducts()
    {
        Console.Write("Termo de busca: "); var term = Console.ReadLine();
        var products = await client.GetFromJsonAsync<ProductDto[]>($"/api/products/search?q={Uri.EscapeDataString(term)}");
        if (products == null || products.Length == 0) { Console.WriteLine("Nenhum produto encontrado."); return; }
        foreach (var p in products)
        {
            Console.WriteLine($"{p.Id} | {p.Name} | R${p.Price} | Qty: {p.Quantity}");
        }
    }

    private static async Task ExportXml()
    {
        var response = await client.GetAsync("/api/products/export/xml");
        if (!response.IsSuccessStatusCode) { Console.WriteLine($"Erro ao exportar XML: {response.StatusCode}"); return; }
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var path = Path.Combine(Directory.GetCurrentDirectory(), "products.xml");
        await File.WriteAllBytesAsync(path, bytes);
        Console.WriteLine($"XML exportado para {path}");
    }

    private static async Task ExportPdf()
    {
        var response = await client.GetAsync("/api/products/export/pdf");
        if (!response.IsSuccessStatusCode) { Console.WriteLine($"Erro ao exportar PDF: {response.StatusCode}"); return; }
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var path = Path.Combine(Directory.GetCurrentDirectory(), "products.pdf");
        await File.WriteAllBytesAsync(path, bytes);
        Console.WriteLine($"PDF exportado para {path}");
    }
}

