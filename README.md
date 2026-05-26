# Gerenciador de Estoque (Estoque Manager)

Um sistema simples que expõe uma REST API desenvolvida em **C#** sobre a plataforma **.NET 10**, focado no controle de estoques de produtos.



---

## 🚀 Tecnologias Utilizadas

- **Linguagem:** C#
- **Framework:** .NET 10.0 SDK
- **Persistência:** Serialização local em JSON (`System.Text.Json`) armazenada em `products.json`
- **Arquitetura:** Camadas físicas separadas (Core, Data, Web)

---

## 🏛️ Estrutura de Projeto

```
estoque-manager/
│
├── src/
│   ├── EstoqueManager.Web/    # Camada de apresentação (REST API)
│   ├── EstoqueManager.Core/       # Camada de domínio (entidades Product, Category)
│   └── EstoqueManager.Data/       # Camada de acesso a dados (StockService, CategoryService)
│
├── README.md                     # Documentação do projeto
├── .gitignore                    # Arquivos a serem ignorados no Git
└── estoque-manager.sln            # Solução C# que reúne os módulos
```

### Detalhamento das Camadas
1. **EstoqueManager.Core** – Contém as entidades de domínio `Product` e `Category`, incluindo propriedades como `CreatedAt`, `UpdatedAt` e `CategoryId`.
2. **EstoqueManager.Data** – Implementa `StockService` e `CategoryService`, responsáveis pela persistência, auditoria e lógica de negócio (CRUD completo, validações e controle de histórico).
3. **EstoqueManager.Web** – Exposição de uma REST API com endpoints para gerenciamento de estoque, utilizando ASP.NET Core.

---

## ✨ Funcionalidades Implementadas

- **Cadastro de Produto** – Permite registrar novos itens, informando nome, preço e quantidade inicial de forma simples e rápida.
- **Listagem de Estoque** – Exibe, em uma tabela agradável, todos os produtos cadastrados com seus GUIDs, facilitando a visualização e o controle.
- **Busca por Nome** – Encontre produtos digitando parte ou a totalidade do nome, sem se preocupar com maiúsculas/minúsculas.
- **Atualização de Estoque** – Ajuste a quantidade disponível de um produto específico, utilizando seu identificador único.
- **Remoção de Produto** – Exclua itens com uma confirmação segura, evitando exclusões acidentais.
- **Persistência Não‑Volátil (JSON)** – Todos os dados são salvos automaticamente em `products.json`, garantindo que nenhuma informação seja perdida entre sessões.

---

## ⚙️ Como Executar o Projeto

### Pré-requisitos:
- Instalar o [.NET 10.0 SDK](https://dotnet.microsoft.com/download) em sua máquina.

### Passos para compilação e execução:

1. Abra seu terminal na pasta raiz do repositório (`dotnet-stock-manager`).
2. Restaure as dependências e compile a solução:
   ```bash
   dotnet build
   ```
3. Execute a API REST:
   ```bash
   dotnet run --project src/EstoqueManager.Web/EstoqueManager.Web.csproj
   ```
