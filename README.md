# 📦 Estoque Manager

> Sistema de gerenciamento de estoque moderno e responsivo, construído com **.NET 10 Minimal API** e um frontend premium com estética **Glassmorphism**.

---

## 🚀 Tecnologias Utilizadas

| Camada | Tecnologia |
|---|---|
| Backend | C# / .NET 10 / ASP.NET Core Minimal API |
| Frontend | HTML5, CSS3 (Glassmorphism), JavaScript ES2023 |
| Gráficos | Chart.js |
| Persistência | `System.Text.Json` (arquivos `.json` locais) |
| Exportação | QuestPDF (PDF), CSV nativo, XML nativo |
| Testes | xUnit (12 testes de domínio e serviço) |
| Contêiner | Docker / Docker Compose |
| Auditoria | Log em arquivo (`app.log`) |

---

## 🏛️ Estrutura do Projeto

```
dotnet-stock-manager/
│
├── src/
│   ├── EstoqueManager.Web/          # API REST + Frontend (wwwroot)
│   │   └── wwwroot/
│   │       ├── index.html           # SPA principal
│   │       ├── css/style.css        # Design system Glassmorphism
│   │       └── js/app.js            # Lógica completa da UI
│   ├── EstoqueManager.Core/         # Entidades de domínio (Product, Category)
│   ├── EstoqueManager.Data/         # Serviços de dados (StockService, CategoryService, LogService)
│   ├── EstoqueManager.Export/       # Exportação PDF/CSV/XML (QuestPDF)
│   └── EstoqueManager.Tests/        # Testes unitários (xUnit)
│
├── Dockerfile                       # Build multi-stage Docker
├── docker-compose.yml               # Orquestração com volume de dados
├── .dockerignore                    # Ignora artefatos desnecessários
└── estoque-manager.sln              # Solução C#
```

---

## ✨ Funcionalidades Implementadas

### Gestão de Produtos
- ✅ **Cadastro** de produtos com nome, preço, quantidade e categoria
- ✅ **Listagem** paginada (10 itens/página) com busca em tempo real
- ✅ **Ordenação** por coluna (Nome, Preço, Estoque) com indicador visual ▲/▼
- ✅ **Edição** completa via modal Glassmorphism
- ✅ **Atualização rápida** de quantidade em estoque
- ✅ **Remoção** com modal de confirmação customizado (sem `alert()` nativo)
- ✅ **Filtro** por categoria no select da barra de busca

### Dashboard & Analytics
- ✅ **KPIs animados** (Total de Produtos, Valor em Estoque, Estoque Baixo)
- ✅ **Gráfico Doughnut** — produtos por categoria (Chart.js)
- ✅ **Gráfico de Barras** — valor em estoque por categoria (Chart.js)
- ✅ **Barra de saúde do estoque** por produto (🔴→🟡→🟢)

### Categorias
- ✅ CRUD completo de categorias via modal dedicado

### Exportação de Dados
- ✅ **PDF** — relatório profissional com QuestPDF
- ✅ **CSV** — compatível com Excel/Sheets
- ✅ **XML** — formato estruturado para integrações

### Trilha de Auditoria
- ✅ **Log automático** de todas as operações (adição, edição, remoção)
- ✅ **Visualização em tempo real** na seção "Trilha de Auditoria" do frontend
- ✅ **Endpoint REST** `/api/dashboard/logs` para as últimas 30 entradas

### UX Premium
- ✅ **Glassmorphism** com `backdrop-filter: blur` em todos os componentes
- ✅ **Toast notifications** animadas (sucesso, erro, aviso)
- ✅ **Modal de confirmação** customizado com botão de perigo estilizado
- ✅ **Animação de contagem** nos KPIs ao carregar (ease-out cubic)
- ✅ **Responsivo** — adaptado para mobile e desktop

---

## ⚙️ Como Executar

### Pré-requisitos
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)

### Execução local
```bash
# 1. Restaurar e compilar
dotnet build

# 2. Executar a aplicação
dotnet run --project src/EstoqueManager.Web/EstoqueManager.Web.csproj
```
Acesse em: `http://localhost:5000`

---

## 🐳 Docker

```bash
# Build + Run com Docker Compose (volume persistente para dados)
docker compose up --build

# Ou manualmente
docker build -t estoque-manager .
docker run -p 8080:8080 -v estoque-data:/app/data estoque-manager
```
Acesse em: `http://localhost:8080`

---

## 🧪 Testes

```bash
dotnet test
```

- 16 testes unitários cobrindo entidades de domínio (`Product`, `Category`) e serviços (`StockService`, `LogService`, `StockMovementService`)

---

## 📡 API REST — Endpoints

### Produtos `/api/products`
| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/` | Listar todos os produtos (opcional `includeInactive=true` query parameter) |
| `GET` | `/{id}` | Buscar produto por ID |
| `GET` | `/search?q={termo}` | Buscar por nome ou descrição |
| `POST` | `/` | Cadastrar produto |
| `PUT` | `/{id}` | Atualizar produto |
| `PUT` | `/{id}/quantity` | Atualizar quantidade com motivo opcional |
| `POST` | `/{id}/restore` | Restaurar produto inativo (lixeira) |
| `GET` | `/{id}/movements` | Buscar histórico de movimentações de estoque |
| `DELETE` | `/{id}` | Desativar produto (Soft Delete) |
| `GET` | `/export/pdf` | Exportar PDF |
| `GET` | `/export/csv` | Exportar CSV |
| `GET` | `/export/xml` | Exportar XML |

### Categorias `/api/categories`
| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/` | Listar categorias |
| `POST` | `/` | Criar categoria |
| `PUT` | `/{id}` | Atualizar categoria |
| `DELETE` | `/{id}` | Remover categoria |

### Dashboard `/api/dashboard`
| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/stats` | KPIs (total, valor, estoque baixo com limite customizado) |
| `GET` | `/category-stats` | Estatísticas de produtos e valor total agrupados por categoria |
| `GET` | `/logs` | Últimas 30 entradas de auditoria |

