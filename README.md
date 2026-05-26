# Gerenciador de Estoque (Estoque Manager)

Um sistema robusto em linha de comando (CLI) desenvolvido em **C#** e baseado na plataforma **.NET 10**, projetado para o controle eficiente de estoques de produtos. Este projeto foi estruturado seguindo boas práticas de Engenharia de Software, utilizando o padrão de arquitetura em camadas para assegurar a separação clara de responsabilidades, alta manutenibilidade e escalabilidade.

Toda a lógica interna de variáveis, propriedades, métodos e classes foi nomeada em **inglês**, enquanto a interface do console de apresentação e interações com o usuário foram mantidas em **português**. Toda a codebase está amplamente documentada e comentada.

---

## 🚀 Tecnologias Utilizadas

- **Linguagem:** C# 12 
- **Framework:** .NET 10.0 SDK
- **Persistência:** Serialização Local em JSON (`System.Text.Json` salvando em `products.json`)
- **Arquitetura:** Camadas Físicas Separadas

---

## 🏛️ Estrutura Arquitetural

A solução está dividida em três projetos independentes, garantindo baixo acoplamento:

```txt
estoque-manager/
│
├── src/
│   ├── EstoqueManager.Console/    # Camada de Apresentação (Interface CLI e Entrada do Usuário)
│   │
│   ├── EstoqueManager.Core/       # Camada de Domínio (Entidade Product.cs e Regras de Negócio)
│   │
│   └── EstoqueManager.Data/       # Camada de Acesso a Dados (Serviço de Persistência StockService.cs)
│
├── README.md                      # Documentação do Projeto
├── .gitignore                     # Configuração de Arquivos Ignorados no Controle de Versão
└── estoque-manager.sln            # Solução C# para Integração dos Módulos
```

### Detalhamento das Camadas:
1. **EstoqueManager.Core**: Contém o modelo de domínio principal `Product.cs` com atributos essenciais em inglês (ID auto-gerado, `Name`, `Price`, `Quantity` e `CreatedAt`).
2. **EstoqueManager.Data**: Implementa `StockService.cs`, responsável por controlar e orquestrar as manipulações na coleção de dados de estoque, incluindo salvamento automático síncrono em `products.json`.
3. **EstoqueManager.Console**: Define a interface de controle do usuário em `Program.cs`, com menus coloridos interativos e proteção contra dados de entrada inválidos para mitigar quebras na execução (crashes).

---

## ✨ Funcionalidades Implementadas

- [x] **Cadastro de Produto**: Registro completo de itens informando nome, preço unitário e quantidade inicial.
- [x] **Listagem de Estoque**: Exibição tabular detalhada de todos os itens cadastrados com seus respectivos códigos únicos (GUID).
- [x] **Busca por Nome**: Pesquisa flexível e case-insensitive (pesquisa por aproximação textual).
- [x] **Atualização de Estoque**: Alteração dinâmica da quantidade disponível de um determinado item informando seu ID exato.
- [x] **Remoção de Produto**: Exclusão de itens com confirmação de segurança para evitar exclusões acidentais.
- [x] **Persistência Não-Volátil (JSON)**: Salvamento automático de todas as operações em arquivo local `products.json`.

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
3. Execute o projeto console de apresentação:
   ```bash
   dotnet run --project src/EstoqueManager.Console/EstoqueManager.Console.csproj
   ```
