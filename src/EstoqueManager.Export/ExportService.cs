using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using EstoqueManager.Core;

namespace EstoqueManager.Export
{
    public class ExportService : IExportService
    {
        public ExportService()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        }
        public async Task<string> GenerateXmlAsync(IEnumerable<Product> products)
        {
            var serializer = new XmlSerializer(typeof(List<Product>));
            await using var ms = new MemoryStream();
            serializer.Serialize(ms, new List<Product>(products));
            ms.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(ms);
            return await reader.ReadToEndAsync();
        }

        public async Task<byte[]> GenerateCsvAsync(IEnumerable<Product> products)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ID,Nome,Preço,Quantidade,CategoriaId,Descrição,CriadoEm,AtualizadoEm");
            foreach (var p in products)
            {
                var name = p.Name.Contains(',') || p.Name.Contains('"') ? $"\"{p.Name.Replace("\"", "\"\"")}\"" : p.Name;
                var descValue = p.Description ?? string.Empty;
                var desc = descValue.Contains(',') || descValue.Contains('"') || descValue.Contains('\n') || descValue.Contains('\r') ? $"\"{descValue.Replace("\"", "\"\"")}\"" : descValue;
                sb.AppendLine($"{p.Id},{name},{p.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)},{p.Quantity},{p.CategoryId},{desc},{p.CreatedAt:o},{p.UpdatedAt?.ToString("o") ?? ""}");
            }
            var utf8 = new UTF8Encoding(true);
            return await Task.FromResult(utf8.GetBytes(sb.ToString()));
        }

        public async Task<byte[]> GeneratePdfAsync(IEnumerable<Product> products)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Header().Text("Produtos em Estoque").SemiBold().FontSize(20).AlignCenter();
                    page.Content().Table(table =>
                    {
                        // Header
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // ID
                            columns.RelativeColumn(3); // Nome
                            columns.RelativeColumn(2); // Preço
                            columns.RelativeColumn(2); // Quantidade
                            columns.RelativeColumn(2); // Categoria
                            columns.RelativeColumn(2); // Criado em
                            columns.RelativeColumn(2); // Atualizado em
                        });
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("ID");
                            header.Cell().Element(CellStyle).Text("Nome");
                            header.Cell().Element(CellStyle).Text("Preço");
                            header.Cell().Element(CellStyle).Text("Qtd");
                            header.Cell().Element(CellStyle).Text("Categoria");
                            header.Cell().Element(CellStyle).Text("Criado");
                            header.Cell().Element(CellStyle).Text("Atualizado");
                        });
                        foreach (var p in products)
                        {
                            table.Cell().Element(CellStyle).Text(p.Id.ToString());
                            table.Cell().Element(CellStyle).Text(p.Name);
                            table.Cell().Element(CellStyle).Text(p.Price.ToString("C"));
                            table.Cell().Element(CellStyle).Text(p.Quantity.ToString());
                            table.Cell().Element(CellStyle).Text(p.CategoryId?.ToString() ?? "-");
                            table.Cell().Element(CellStyle).Text(p.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
                            table.Cell().Element(CellStyle).Text(p.UpdatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-");
                        }
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return await Task.FromResult(pdfBytes);
        }

        private IContainer CellStyle(IContainer container)
        {
            return container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
        }
    }
}
