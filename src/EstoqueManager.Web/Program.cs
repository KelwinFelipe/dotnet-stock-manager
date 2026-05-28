using EstoqueManager.Core;
using EstoqueManager.Data;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using EstoqueManager.Export;
using System.Text;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
var builder = WebApplication.CreateBuilder(args);

// Configuração de CORS para permitir consumo do frontend no mesmo host
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Registra os serviços como Singleton, pois eles mantêm estado em memória e acessam arquivo
builder.Services.AddSingleton<IStockService, StockService>();
builder.Services.AddSingleton<ICategoryService, CategoryService>();
builder.Services.AddSingleton<ExportService>();

var app = builder.Build();

app.UseCors("AllowAll");

// Serve arquivos estáticos da pasta wwwroot
app.UseStaticFiles();

// Configura o mapeamento de requisições de API
var api = app.MapGroup("/api/products");

api.MapGet("/", (IStockService stock) =>
{
    var products = stock.List();
    return Results.Ok(products);
});

api.MapGet("/{id:guid}", (Guid id, IStockService stock) =>
{
    var product = stock.GetById(id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
});

api.MapGet("/search", (string q, IStockService stock) =>
{
    if (string.IsNullOrWhiteSpace(q))
        return Results.BadRequest("Termo de busca não pode ser vazio.");

    var products = stock.List()
        .Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
        .ToList();

    return Results.Ok(products);
});

api.MapPost("/", async (ProductInputModel model, IStockService stock) =>
{
    var valCtx = new ValidationContext(model);
    var valResults = new List<ValidationResult>();
    if (!Validator.TryValidateObject(model, valCtx, valResults, true))
    {
        return Results.BadRequest(valResults.Select(r => r.ErrorMessage));
    }

    try
    {
        var product = new Product(model.Name, model.Price, model.Quantity)
        {
            CategoryId = model.CategoryId,
            Description = model.Description
        };
        await stock.AddAsync(product);
        return Results.Created($"/api/products/{product.Id}", product);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

api.MapPut("/{id:guid}/quantity", async (Guid id, [FromBody] int quantity, IStockService stock) =>
{
    if (quantity < 0)
        return Results.BadRequest(new { message = "Quantidade não pode ser negativa." });

    var success = await stock.UpdateQuantityAsync(id, quantity);
    if (!success)
        return Results.NotFound();

    return Results.Ok();
});

api.MapDelete("/{id:guid}", async (Guid id, IStockService stock) =>
{
    var success = await stock.RemoveAsync(id);
    if (!success)
        return Results.NotFound();

    return Results.Ok();
});

api.MapPut("/{id:guid}", async (Guid id, ProductInputModel model, IStockService stock) =>
{
    var valCtx = new ValidationContext(model);
    var valResults = new List<ValidationResult>();
    if (!Validator.TryValidateObject(model, valCtx, valResults, true))
    {
        return Results.BadRequest(valResults.Select(r => r.ErrorMessage));
    }

    try
    {
        var product = new Product(model.Name, model.Price, model.Quantity)
        {
            CategoryId = model.CategoryId,
            Description = model.Description
        };
        
        var success = await stock.UpdateProductAsync(id, product);
        if (!success)
            return Results.NotFound();

        return Results.Ok();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});
// Export endpoints
api.MapGet("/export/xml", async (IStockService stock, ExportService export) =>
{
    var xml = await export.GenerateXmlAsync(stock.List());
    var bytes = Encoding.UTF8.GetBytes(xml);
    return Results.File(bytes, "application/xml", "products.xml");
});

api.MapGet("/export/csv", async (IStockService stock, ExportService export) =>
{
    var bytes = await export.GenerateCsvAsync(stock.List());
    return Results.File(bytes, "text/csv", "products.csv");
});

api.MapGet("/export/pdf", async (IStockService stock, ExportService export) =>
{
    var pdfBytes = await export.GeneratePdfAsync(stock.List());
    return Results.File(pdfBytes, "application/pdf", "products.pdf");
});

api.MapPost("/import/csv", async (HttpRequest request, IStockService stock) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest(new { message = "Formato multipart/form-data obrigatório." });

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("csvFile") ?? form.Files.FirstOrDefault();
    if (file == null || file.Length == 0)
        return Results.BadRequest(new { message = "Nenhum arquivo CSV enviado." });

    using var reader = new StreamReader(file.OpenReadStream());
    var header = await reader.ReadLineAsync();
    if (string.IsNullOrWhiteSpace(header))
        return Results.BadRequest(new { message = "Arquivo CSV vazio." });

    char separator = header.Contains(';') ? ';' : ',';
    var columns = CsvParserHelper.SplitCsvLine(header, separator).Select(c => c.Trim()).ToArray();
    
    int idxNome = Array.FindIndex(columns, c => c.Equals("Nome", StringComparison.OrdinalIgnoreCase));
    int idxPreco = Array.FindIndex(columns, c => c.Equals("Preço", StringComparison.OrdinalIgnoreCase) || c.Equals("Preco", StringComparison.OrdinalIgnoreCase));
    int idxQtd = Array.FindIndex(columns, c => c.Equals("Quantidade", StringComparison.OrdinalIgnoreCase) || c.Equals("Qtd", StringComparison.OrdinalIgnoreCase));
    int idxCatId = Array.FindIndex(columns, c => c.Equals("CategoriaId", StringComparison.OrdinalIgnoreCase));
    int idxDesc = Array.FindIndex(columns, c => c.Equals("Descrição", StringComparison.OrdinalIgnoreCase) || c.Equals("Descricao", StringComparison.OrdinalIgnoreCase) || c.Equals("Description", StringComparison.OrdinalIgnoreCase));

    if (idxNome == -1 || idxPreco == -1 || idxQtd == -1)
    {
        return Results.BadRequest(new { message = "O CSV deve conter as colunas Nome, Preço e Quantidade." });
    }

    int imported = 0;
    int skipped = 0;
    var errors = new List<string>();
    int lineNum = 1;
    string? line;

    while ((line = await reader.ReadLineAsync()) != null)
    {
        lineNum++;
        if (string.IsNullOrWhiteSpace(line)) continue;

        var parts = CsvParserHelper.SplitCsvLine(line, separator);
        int maxIndex = Math.Max(idxNome, Math.Max(idxPreco, idxQtd));
        if (parts.Length <= maxIndex)
        {
            skipped++;
            errors.Add($"Linha {lineNum}: Número insuficiente de colunas.");
            continue;
        }

        var name = parts[idxNome].Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            skipped++;
            errors.Add($"Linha {lineNum}: O Nome não pode ser vazio.");
            continue;
        }

        var priceStr = parts[idxPreco].Trim().Replace("R$", "").Trim();
        if (!decimal.TryParse(priceStr, CultureInfo.InvariantCulture, out var price) &&
            !decimal.TryParse(priceStr, new CultureInfo("pt-BR"), out price))
        {
            skipped++;
            errors.Add($"Linha {lineNum}: Preço inválido '{priceStr}'.");
            continue;
        }

        var qtyStr = parts[idxQtd].Trim();
        if (!int.TryParse(qtyStr, out var qty))
        {
            skipped++;
            errors.Add($"Linha {lineNum}: Quantidade inválida '{qtyStr}'.");
            continue;
        }

        Guid? catId = null;
        if (idxCatId != -1 && idxCatId < parts.Length)
        {
            var catIdStr = parts[idxCatId].Trim();
            if (!string.IsNullOrWhiteSpace(catIdStr) && Guid.TryParse(catIdStr, out var parsedGuid))
            {
                catId = parsedGuid;
            }
        }

        string? description = null;
        if (idxDesc != -1 && idxDesc < parts.Length)
        {
            description = parts[idxDesc].Trim();
        }

        try
        {
            var product = new Product(name, price, qty)
            {
                CategoryId = catId,
                Description = description
            };
            await stock.AddAsync(product);
            imported++;
        }
        catch (Exception ex)
        {
            skipped++;
            errors.Add($"Linha {lineNum}: {ex.Message}");
        }
    }

    return Results.Ok(new { imported, skipped, errors });
});

// Configura o mapeamento de requisições de Categorias
var catApi = app.MapGroup("/api/categories");

catApi.MapGet("/", (ICategoryService catService) =>
{
    return Results.Ok(catService.List());
});

catApi.MapGet("/{id:guid}", (Guid id, ICategoryService catService) =>
{
    var category = catService.GetById(id);
    return category is not null ? Results.Ok(category) : Results.NotFound();
});

catApi.MapPost("/", async (CategoryInputModel model, ICategoryService catService) =>
{
    var valCtx = new ValidationContext(model);
    var valResults = new List<ValidationResult>();
    if (!Validator.TryValidateObject(model, valCtx, valResults, true))
    {
        return Results.BadRequest(valResults.Select(r => r.ErrorMessage));
    }

    try
    {
        var category = new Category(model.Name, model.Description);
        await catService.AddAsync(category);
        return Results.Created($"/api/categories/{category.Id}", category);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { message = ex.Message });
    }
});

catApi.MapPut("/{id:guid}", async (Guid id, CategoryInputModel model, ICategoryService catService) =>
{
    var valCtx = new ValidationContext(model);
    var valResults = new List<ValidationResult>();
    if (!Validator.TryValidateObject(model, valCtx, valResults, true))
    {
        return Results.BadRequest(valResults.Select(r => r.ErrorMessage));
    }

    try
    {
        var success = await catService.UpdateAsync(id, model.Name, model.Description);
        if (!success)
            return Results.NotFound();
        return Results.Ok();
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { message = ex.Message });
    }
});

catApi.MapDelete("/{id:guid}", async (Guid id, ICategoryService catService) =>
{
    var success = await catService.RemoveAsync(id);
    if (!success)
        return Results.NotFound();
    return Results.Ok();
});

// Configura o mapeamento de requisições de Dashboard
var dashApi = app.MapGroup("/api/dashboard");

dashApi.MapGet("/stats", (IStockService stock) =>
{
    var products = stock.List();
    var totalItems = products.Count();
    var totalValue = products.Sum(p => p.Price * p.Quantity);
    var lowStock = products.Count(p => p.Quantity < 10);
    return Results.Ok(new { TotalItems = totalItems, TotalValue = totalValue, LowStockCount = lowStock });
});

dashApi.MapGet("/logs", async () =>
{
    var logs = await LogService.ReadLastLogsAsync(30);
    return Results.Ok(logs);
});

// Fallback para SPA - qualquer rota não mapeada para arquivo físico cai no index.html
app.MapFallbackToFile("index.html");

app.Run();

// Modelo para receber dados na criação
public class ProductInputModel
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "O preço deve ser maior que zero.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "A quantidade não pode ser negativa.")]
    public int Quantity { get; set; }
    
    public string? Description { get; set; }
    
    public Guid? CategoryId { get; set; }
}

public class CategoryInputModel
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 50 caracteres.")]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
}
