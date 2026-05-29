using System;

namespace EstoqueManager.Core
{
    /// <summary>
    /// Representa o registro de movimentação de estoque de um produto.
    /// </summary>
    public class StockMovement
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantityDelta { get; set; }
        public int PreviousQuantity { get; set; }
        public int NewQuantity { get; set; }
        public string Type { get; set; } = string.Empty; // "Entrada", "Saída", "Ajuste", etc.
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; init; } = DateTime.Now;

        public StockMovement() { }

        public StockMovement(Guid productId, string productName, int quantityDelta, int previousQuantity, int newQuantity, string type, string reason = "")
        {
            ProductId = productId;
            ProductName = productName;
            QuantityDelta = quantityDelta;
            PreviousQuantity = previousQuantity;
            NewQuantity = newQuantity;
            Type = type;
            Reason = reason;
        }
    }
}
