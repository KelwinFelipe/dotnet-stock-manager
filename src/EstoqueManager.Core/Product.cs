namespace EstoqueManager.Core;

/// <summary>
/// Representa a entidade de domínio de um produto no sistema com validação autônoma.
/// </summary>
public class Product
{
    private string _name = string.Empty;
    private decimal _price;
    private int _quantity;

    /// <summary>
    /// Identificador único universal do produto.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Nome ou descrição do produto.
    /// </summary>
    /// <exception cref="ArgumentException">Lançada caso o nome seja nulo, vazio ou inválido.</exception>
    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("O nome do produto não pode ser vazio ou conter apenas espaços.");
            _name = value.Trim();
        }
    }

    /// <summary>
    /// Preço unitário de comercialização do produto.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Lançada caso o preço seja negativo.</exception>
    public decimal Price
    {
        get => _price;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "O preço do produto não pode ser negativo.");
            _price = value;
        }
    }

    /// <summary>
    /// Quantidade de itens disponíveis em estoque.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Lançada caso a quantidade seja negativa.</exception>
    public int Quantity
    {
        get => _quantity;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "A quantidade em estoque não pode ser negativa.");
            _quantity = value;
        }
    }

    /// <summary>
    /// Data e hora em que o produto foi registrado no sistema.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    /// <summary>
    /// Construtor vazio necessário para processos de desserialização (System.Text.Json).
    /// </summary>
    public Product() { }

    /// <summary>
    /// Construtor parametrizado responsável por garantir a integridade do estado da entidade (Validação de Domínio).
    /// </summary>
    /// <param name="name">Nome do produto.</param>
    /// <param name="price">Preço de comercialização.</param>
    /// <param name="quantity">Quantidade em estoque.</param>
    public Product(string name, decimal price, int quantity)
    {
        Name = name;
        Price = price;
        Quantity = quantity;
    }
}
