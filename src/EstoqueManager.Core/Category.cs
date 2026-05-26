namespace EstoqueManager.Core;

/// <summary>
/// Representa a categoria que pode ser atribuída a um ou mais produtos.
/// </summary>
public class Category
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public Category() { }

    public Category(string name, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome da categoria não pode ser vazio.");
            
        Name = name.Trim();
        Description = description.Trim();
    }
}
