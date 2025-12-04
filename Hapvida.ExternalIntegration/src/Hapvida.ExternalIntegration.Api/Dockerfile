# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar arquivos de projeto para restaurar dependências (cache layer)
COPY ["src/Hapvida.ExternalIntegration.Api/Hapvida.ExternalIntegration.Api.csproj", "src/Hapvida.ExternalIntegration.Api/"]
COPY ["src/Hapvida.ExternalIntegration.Application/Hapvida.ExternalIntegration.Application.csproj", "src/Hapvida.ExternalIntegration.Application/"]
COPY ["src/Hapvida.ExternalIntegration.Domain/Hapvida.ExternalIntegration.Domain.csproj", "src/Hapvida.ExternalIntegration.Domain/"]
COPY ["src/Hapvida.ExternalIntegration.CrossCutting/Hapvida.ExternalIntegration.CrossCutting.csproj", "src/Hapvida.ExternalIntegration.CrossCutting/"]
COPY ["src/Hapvida.ExternalIntegration.Infra/Hapvida.ExternalIntegration.Infra.csproj", "src/Hapvida.ExternalIntegration.Infra/"]

# Restaurar dependências (cache layer)
RUN dotnet restore "src/Hapvida.ExternalIntegration.Api/Hapvida.ExternalIntegration.Api.csproj"

# Copiar todo o código fonte
COPY . .

# Build do projeto
WORKDIR "/src/src/Hapvida.ExternalIntegration.Api"
RUN dotnet build "Hapvida.ExternalIntegration.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# ============================================
# Stage 2: Publish
# ============================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Hapvida.ExternalIntegration.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-build

# ============================================
# Stage 3: Runtime (imagem final reduzida)
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Criar usuário não-root para segurança
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copiar arquivos publicados
COPY --from=publish /app/publish .

# Criar diretório de logs com permissões adequadas
RUN mkdir -p /app/Logs && chown -R appuser:appuser /app

# Mudar para usuário não-root
USER appuser

# Expor portas
EXPOSE 8080
EXPOSE 8081

# Variáveis de ambiente padrão
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entrypoint
ENTRYPOINT ["dotnet", "Hapvida.ExternalIntegration.Api.dll"]
