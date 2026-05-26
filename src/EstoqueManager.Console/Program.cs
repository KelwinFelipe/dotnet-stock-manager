using EstoqueManager.Core;
using EstoqueManager.Data;

// Inicializa o serviço de estoque com persistência local
var stock = new StockService();
bool isRunning = true;

// Loop principal de execução do console interativo
while (isRunning)
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("=========================================");
    Console.WriteLine("          GERENCIADOR DE ESTOQUE         ");
    Console.WriteLine("=========================================");
    Console.ResetColor();
    Console.WriteLine(" 1 - Cadastrar Novo Produto");
    Console.WriteLine(" 2 - Listar Todos os Produtos");
    Console.WriteLine(" 3 - Buscar Produto por Nome");
    Console.WriteLine(" 4 - Atualizar Quantidade em Estoque");
    Console.WriteLine(" 5 - Remover Produto");
    Console.WriteLine(" 0 - Sair");
    Console.WriteLine("-----------------------------------------");
    Console.Write("Selecione uma opção: ");

    var option = Console.ReadLine();

    switch (option)
    {
        case "1":
            AddProductMenu();
            break;

        case "2":
            ListProductsMenu();
            break;

        case "3":
            SearchProductMenu();
            break;

        case "4":
            UpdateStockMenu();
            break;

        case "5":
            RemoveProductMenu();
            break;

        case "0":
            isRunning = false;
            Console.WriteLine("\nEncerrando o sistema...");
            break;

        default:
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nOpção inválida! Pressione qualquer tecla para continuar.");
            Console.ResetColor();
            Console.ReadKey();
            break;
    }
}

/// <summary>
/// Exibe a tela de cadastro e realiza as validações para inserção de um novo produto.
/// </summary>
void AddProductMenu()
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(">>> CADASTRAR NOVO PRODUTO <<<\n");
    Console.ResetColor();

    Console.Write("Nome do produto: ");
    var name = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(name))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\nErro: O nome do produto não pode ser vazio.");
        Console.ResetColor();
        Console.ReadKey();
        return;
    }

    Console.Write("Preço unitário (R$): ");
    if (!decimal.TryParse(Console.ReadLine(), out decimal price) || price < 0)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\nErro: Preço inválido.");
        Console.ResetColor();
        Console.ReadKey();
        return;
    }

    Console.Write("Quantidade inicial: ");
    if (!int.TryParse(Console.ReadLine(), out int quantity) || quantity < 0)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\nErro: Quantidade inválida.");
        Console.ResetColor();
        Console.ReadKey();
        return;
    }

    // Instancia o objeto do produto com atributos sanitizados
    var newProduct = new Product
    {
        Name = name,
        Price = price,
        Quantity = quantity
    };

    // Adiciona o produto na camada de persistência
    stock.Add(newProduct);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\nProduto '{name}' cadastrado com sucesso!");
    Console.ResetColor();
    Console.WriteLine("\nPressione qualquer tecla para voltar ao menu.");
    Console.ReadKey();
}

/// <summary>
/// Lista de forma tabular todos os produtos contidos na base de dados JSON.
/// </summary>
void ListProductsMenu()
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(">>> LISTA DE PRODUTOS EM ESTOQUE <<<\n");
    Console.ResetColor();

    var products = stock.List();

    if (products.Count == 0)
    {
        Console.WriteLine("Nenhum produto cadastrado no momento.");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("{0,-38} | {1,-20} | {2,12} | {3,10}", "ID", "NOME", "PREÇO", "ESTOQUE");
        Console.WriteLine(new string('-', 90));
        Console.ResetColor();

        // Renderiza cada produto com formatação apropriada
        foreach (var p in products)
        {
            Console.WriteLine("{0,-38} | {1,-20} | {2,12:C2} | {3,10}", p.Id, p.Name, p.Price, p.Quantity);
        }
    }

    Console.WriteLine("\nPressione qualquer tecla para voltar ao menu.");
    Console.ReadKey();
}

/// <summary>
/// Permite o usuário buscar produtos pelo nome exato ou aproximação parcial de caracteres.
/// </summary>
void SearchProductMenu()
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(">>> BUSCAR PRODUTO <<<\n");
    Console.ResetColor();

    Console.Write("Digite o nome completo ou parte dele: ");
    var searchTerm = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(searchTerm))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\nBusca cancelada: Termo vazio.");
        Console.ResetColor();
        Console.ReadKey();
        return;
    }

    // Busca itens cujo nome contenha o termo digitado
    var foundProducts = stock.List()
        .Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        .ToList();

    if (foundProducts.Count == 0)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\nNenhum produto encontrado com esse termo.");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n{foundProducts.Count} produto(s) encontrado(s):");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("{0,-38} | {1,-20} | {2,12} | {3,10}", "ID", "NOME", "PREÇO", "ESTOQUE");
        Console.WriteLine(new string('-', 90));
        Console.ResetColor();

        foreach (var p in foundProducts)
        {
            Console.WriteLine("{0,-38} | {1,-20} | {2,12:C2} | {3,10}", p.Id, p.Name, p.Price, p.Quantity);
        }
    }

    Console.WriteLine("\nPressione qualquer tecla para voltar ao menu.");
    Console.ReadKey();
}

/// <summary>
/// Executa a rotina para atualizar a quantidade disponível em estoque de um produto através do Guid.
/// </summary>
void UpdateStockMenu()
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(">>> ATUALIZAR QUANTIDADE EM ESTOQUE <<<\n");
    Console.ResetColor();

    Console.Write("Digite o ID exato do produto: ");
    if (!Guid.TryParse(Console.ReadLine(), out Guid id))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\nErro: Formato de ID inválido.");
        Console.ResetColor();
        Console.ReadKey();
        return;
    }

    var product = stock.GetById(id);
    if (product == null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\nErro: Produto não localizado.");
        Console.ResetColor();
        Console.ReadKey();
        return;
    }

    Console.WriteLine($"Produto selecionado: {product.Name}");
    Console.WriteLine($"Quantidade atual: {product.Quantity}");
    Console.Write("\nDigite a nova quantidade: ");
    if (!int.TryParse(Console.ReadLine(), out int newQuantity) || newQuantity < 0)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\nErro: Quantidade inválida.");
        Console.ResetColor();
        Console.ReadKey();
        return;
    }

    // Grava as novas alterações
    stock.UpdateQuantity(id, newQuantity);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\nEstoque atualizado com sucesso!");
    Console.ResetColor();
    Console.WriteLine("\nPressione qualquer tecla para voltar ao menu.");
    Console.ReadKey();
}

/// <summary>
/// Exclui um produto do estoque permanentemente após a validação de segurança.
/// </summary>
void RemoveProductMenu()
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(">>> REMOVER PRODUTO DO SISTEMA <<<\n");
    Console.ResetColor();

    Console.Write("Digite o ID exato do produto a ser removido: ");
    if (!Guid.TryParse(Console.ReadLine(), out Guid id))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\nErro: Formato de ID inválido.");
        Console.ResetColor();
        Console.ReadKey();
        return;
    }

    var product = stock.GetById(id);
    if (product == null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\nErro: Produto não localizado.");
        Console.ResetColor();
        Console.ReadKey();
        return;
    }

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write($"Deseja realmente remover '{product.Name}' permanentemente? (S/N): ");
    Console.ResetColor();
    var confirmation = Console.ReadLine();

    if (confirmation?.Equals("S", StringComparison.OrdinalIgnoreCase) == true)
    {
        stock.Remove(id);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nProduto removido com sucesso!");
        Console.ResetColor();
    }
    else
    {
        Console.WriteLine("\nOperação cancelada pelo usuário.");
    }

    Console.WriteLine("\nPressione qualquer tecla para voltar ao menu.");
    Console.ReadKey();
}
