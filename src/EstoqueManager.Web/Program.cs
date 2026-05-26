using EstoqueManager.Core;
using EstoqueManager.Data;
using Microsoft.AspNetCore.Mvc;

using EstoqueManager.Export;
using System.Text;
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
builder.Services.AddSingleton<StockService>();
builder.Services.AddSingleton<CategoryService>();
builder.Services.AddSingleton<ExportService>();

var app = builder.Build();

app.UseCors("AllowAll");

// Serve arquivos estáticos da pasta wwwroot
app.UseStaticFiles();

// Configura o mapeamento de requisições de API
var api = app.MapGroup("/api/products");

api.MapGet("/", (StockService stock) =>
{
    var products = stock.List();
    return Results.Ok(products);
});

api.MapGet("/{id:guid}", (Guid id, StockService stock) =>
{
    var product = stock.GetById(id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
});

api.MapGet("/search", (string q, StockService stock) =>
{
    if (string.IsNullOrWhiteSpace(q))
        return Results.BadRequest("Termo de busca não pode ser vazio.");

    var products = stock.List()
        .Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
        .ToList();

    return Results.Ok(products);
});

api.MapPost("/", async (ProductInputModel model, StockService stock) =>
{
    try
    {
        var product = new Product(model.Name, model.Price, model.Quantity)
        {
            CategoryId = model.CategoryId
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

api.MapPut("/{id:guid}/quantity", async (Guid id, [FromBody] int quantity, StockService stock) =>
{
    if (quantity < 0)
        return Results.BadRequest(new { message = "Quantidade não pode ser negativa." });

    var success = await stock.UpdateQuantityAsync(id, quantity);
    if (!success)
        return Results.NotFound();

    return Results.Ok();
});

api.MapDelete("/{id:guid}", async (Guid id, StockService stock) =>
{
    var success = await stock.RemoveAsync(id);
    if (!success)
        return Results.NotFound();

    return Results.Ok();
});

api.MapPut("/{id:guid}", async (Guid id, ProductInputModel model, StockService stock) =>
{
    try
    {
        var product = new Product(model.Name, model.Price, model.Quantity)
        {
            CategoryId = model.CategoryId
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
api.MapGet("/export/xml", async (StockService stock, ExportService export) =>
{
    var xml = await export.GenerateXmlAsync(stock.List());
    var bytes = Encoding.UTF8.GetBytes(xml);
    return Results.File(bytes, "application/xml", "products.xml");
});

api.MapGet("/export/pdf", async (StockService stock, ExportService export) =>
{
    var pdfBytes = await export.GeneratePdfAsync(stock.List());
    return Results.File(pdfBytes, "application/pdf", "products.pdf");
});

// Configura o mapeamento de requisições de Categorias
var catApi = app.MapGroup("/api/categories");

catApi.MapGet("/", (CategoryService catService) =>
{
    return Results.Ok(catService.List());
});

catApi.MapGet("/{id:guid}", (Guid id, CategoryService catService) =>
{
    var category = catService.GetById(id);
    return category is not null ? Results.Ok(category) : Results.NotFound();
});

catApi.MapPost("/", async (CategoryInputModel model, CategoryService catService) =>
{
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

catApi.MapPut("/{id:guid}", async (Guid id, CategoryInputModel model, CategoryService catService) =>
{
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

catApi.MapDelete("/{id:guid}", async (Guid id, CategoryService catService) =>
{
    var success = await catService.RemoveAsync(id);
    if (!success)
        return Results.NotFound();
    return Results.Ok();
});

// Fallback para SPA - qualquer rota não mapeada para arquivo físico cai no index.html
app.MapFallbackToFile("index.html");

app.Run();

// Modelo para receber dados na criação
public class ProductInputModel
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public Guid? CategoryId { get; set; }
}

public class CategoryInputModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
