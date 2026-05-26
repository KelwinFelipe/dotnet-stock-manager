namespace EstoqueManager.Core;

/// <summary>
/// Representa a entidade de domínio de um produto no sistema.
/// </summary>
public class Product
{
    /// <summary>
    /// Identificador único universal do produto.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Nome ou descrição do produto.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Preço unitário de comercialização do produto.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Quantidade de itens disponíveis em estoque.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Data e hora em que o produto foi registrado no sistema.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
