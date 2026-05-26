namespace EstoqueManager.Core;

/// <summary>
/// Represents a product category that can be assigned to one or more products.
/// Includes basic metadata such as name, description and creation timestamp.
/// </summary>
public class Category
{
    /// <summary>
    /// Unique identifier for the category (generated automatically).
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Human‑readable name of the category.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description providing more details about the category.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the category was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    /// <summary>
    /// Parameter‑less constructor required for JSON deserialization.
    /// </summary>
    public Category() { }

    /// <summary>
    /// Creates a new category with the specified name and optional description.
    /// </summary>
    /// <param name="name">Category name – cannot be null or whitespace.</param>
    /// <param name="description">Optional description; defaults to empty string.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty or whitespace.</exception>
    public Category(string name, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome da categoria não pode ser vazio.");
        
        Name = name.Trim();
        Description = description.Trim();
    }
}
