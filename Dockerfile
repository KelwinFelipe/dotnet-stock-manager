# Estágio de Compilação
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copiar arquivos de solução e projetos para restaurar dependências com cache eficiente
COPY estoque-manager.sln ./
COPY src/EstoqueManager.Core/EstoqueManager.Core.csproj src/EstoqueManager.Core/
COPY src/EstoqueManager.Data/EstoqueManager.Data.csproj src/EstoqueManager.Data/
COPY src/EstoqueManager.Export/EstoqueManager.Export.csproj src/EstoqueManager.Export/
COPY src/EstoqueManager.Web/EstoqueManager.Web.csproj src/EstoqueManager.Web/
COPY src/EstoqueManager.ConsoleClient/EstoqueManager.ConsoleClient.csproj src/EstoqueManager.ConsoleClient/

RUN dotnet restore

# Copiar todo o código-fonte restante
COPY src/ src/

# Compilar e publicar em modo Release
WORKDIR /app/src/EstoqueManager.Web
RUN dotnet publish -c Release -o /app/publish

# Estágio de Execução
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Criar o diretório de dados para persistência se ele não existir
RUN mkdir -p data

# Expor a porta padrão do ASP.NET Core
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "EstoqueManager.Web.dll"]
