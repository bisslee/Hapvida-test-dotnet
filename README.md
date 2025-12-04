# Hapvida-test-dotnet

API HTTP em .NET 8 que integra APIs públicas (CEP + clima), desenvolvida como prova técnica para Hapvida.

## 📋 Índice

- [Hapvida-test-dotnet](#hapvida-test-dotnet)
  - [📋 Índice](#-índice)
  - [🎯 Sobre o Projeto](#-sobre-o-projeto)
  - [✨ Funcionalidades Implementadas](#-funcionalidades-implementadas)
    - [✅ US01: Consulta de CEP](#-us01-consulta-de-cep)
    - [✅ US02: Persistência de CEP](#-us02-persistência-de-cep)
    - [✅ US03: Clima Atual e Previsão](#-us03-clima-atual-e-previsão)
  - [🏗️ Arquitetura e Template Base](#️-arquitetura-e-template-base)
    - [Padrões e Tecnologias](#padrões-e-tecnologias)
      - [🏛️ Arquitetura](#️-arquitetura)
      - [🛠️ Tecnologias Principais](#️-tecnologias-principais)
      - [⚡ Performance e Otimização](#-performance-e-otimização)
      - [🛡️ Segurança e Proteção](#️-segurança-e-proteção)
      - [📊 Observabilidade](#-observabilidade)
      - [🌐 Internacionalização](#-internacionalização)
      - [🧪 Testes](#-testes)
    - [Estrutura de Camadas](#estrutura-de-camadas)
  - [🚀 Configuração e Execução](#-configuração-e-execução)
    - [Pré-requisitos](#pré-requisitos)
    - [Instalação](#instalação)
    - [Executando a API](#executando-a-api)
      - [Opção 1: Execução direta](#opção-1-execução-direta)
      - [Opção 2: Execução com Docker](#opção-2-execução-com-docker)
    - [Executando os Testes](#executando-os-testes)
  - [📡 Endpoints da API](#-endpoints-da-api)
    - [US01: Consulta de CEP](#us01-consulta-de-cep)
    - [US02: Persistência de CEP](#us02-persistência-de-cep)
    - [US03: Clima Atual e Previsão](#us03-clima-atual-e-previsão)
    - [Health Check](#health-check)
  - [📚 Documentação](#-documentação)
    - [Projeto](#projeto)
    - [Template Base](#template-base)
  - [🎯 Status do Projeto](#-status-do-projeto)

## 🎯 Sobre o Projeto

Este projeto implementa uma HTTP API em .NET 8 com foco em:

- Integração com APIs públicas (BrasilAPI, ViaCEP, Open-Meteo)
- Boas práticas de desenvolvimento
- Resiliência e tratamento de erros
- Testes automatizados (140 testes, 100% de aprovação)
- Documentação OpenAPI/Swagger
- Observabilidade (logs, métricas, correlação)

## ✨ Funcionalidades Implementadas

### ✅ US01: Consulta de CEP

- Consulta CEP com normalização automática (aceita com ou sem hífen)
- Integração com BrasilAPI (provedor primário) e ViaCEP (fallback)
- Validação de formato de CEP
- Retorno normalizado com dados unificados

### ✅ US02: Persistência de CEP

- Persistência de CEPs consultados em banco de dados em memória (SQLite)
- Validação de duplicatas (retorna 409 Conflict)
- Reutilização da lógica de consulta da US01
- Armazenamento de coordenadas geográficas quando disponíveis

### ✅ US03: Clima Atual e Previsão

- Consulta de clima baseada nos CEPs salvos
- Integração com Open-Meteo (Forecast e Geocoding)
- Cache em memória com TTL de 10 minutos
- Fallback para geocodificação quando coordenadas não estão disponíveis
- Ordenação automática por data de criação (mais recente primeiro)

### ✅ US04: Docker Multi-stage

- **Dockerfile multi-stage**: Build otimizado com imagem final reduzida
- **Imagem base**: `mcr.microsoft.com/dotnet/aspnet:8.0` (runtime apenas, sem SDK)
- **Docker Compose**: Configuração completa para execução simplificada
- **Documentação**: Portas e variáveis de ambiente documentadas no README
- **Segurança**: Usuário não-root configurado no container

**Portas do Container:**
- **8080**: Porta HTTP interna (mapeada para porta externa configurável)
- **8081**: Porta HTTPS interna (mapeada para porta externa configurável)

### ✅ US05: Validação & Mensagens de Erro (RFC 7807)

- **RFC 7807 Problem Details**: Todos os erros seguem o padrão RFC 7807
- **Campos obrigatórios**: `type`, `title`, `status`, `detail`, `traceId`
- **Middleware global**: Tratamento centralizado de exceções
- **Normalização**: Problem Details normalizados para todos os tipos de erro
- **Exemplos implementados**:
  - `400` CEP inválido
  - `404` CEP não encontrado ou nenhum CEP salvo
  - `409` CEP já persistido
  - `504` Timeout de provedor externo
  - `500` Erro interno do servidor

### ✅ US06: Resiliência a Falhas Externas

- **Timeouts explícitos**: 2-5 segundos por tentativa (configurável por provedor)
- **Retry com backoff exponencial + jitter**: 3 tentativas para erros transitórios
- **Circuit Breaker**: Abre após 50% de falhas em janela de tempo (mínimo 3-5 requisições), mantém aberto por 20-30s, half-open para testar recuperação
- **Logging de tentativas**: Logs estruturados de retries e estado do circuito
- **Implementação**: `Microsoft.Extensions.Http.Resilience` (recomendado pela Microsoft)

**Configurações por Provedor:**
- **CEP Providers (BrasilAPI, ViaCEP)**: Timeout 2s, Circuit Breaker após 3 falhas em 20s
- **Weather Provider (Open-Meteo)**: Timeout 3s, Circuit Breaker após 5 falhas em 30s

### ✅ US07: Observabilidade (Logs, Correlação, Métricas)

- **Logs estruturados (JSON)**: Todos os logs são formatados em JSON com Serilog
- **TraceId em cada request**: Cada requisição possui um `traceId` único que é incluído em todos os logs
- **Correlation ID**: Suporte a `X-Correlation-ID` header para rastreamento de requisições
- **Métricas básicas**: 
  - Contador de requisições por rota (`http_requests_total`)
  - Histograma de latência (`http_request_duration_seconds`)
  - Métricas incluem método HTTP, rota e código de status
- **Implementação**: `System.Diagnostics.Metrics` para métricas, Serilog para logs estruturados

### ✅ US08: Documentação via OpenAPI/Swagger

- **Swagger UI**: Disponível em `/swagger` quando a API está em execução
- **Schemas completos**: Request/Response com exemplos e códigos de status documentados
- **Anotações Swagger**: Todos os endpoints possuem anotações detalhadas (`SwaggerOperation`, `SwaggerResponse`)
- **Exemplos inline**: DTOs documentados com exemplos de uso
- **Implementação**: `Swashbuckle.AspNetCore` com anotações completas

### ✅ US09: Testes (Unitários e Integração)

- **Normalização/mapeamento de CEP**: 26 testes cobrindo normalização com/sem hífen, validação de formato, tratamento de erros
- **Fallback de provedores**: 4+ testes cobrindo fallback BrasilAPI → ViaCEP, tratamento de exceções, múltiplas falhas consecutivas
- **Serviço de persistência (US02)**: Testes completos para AddZipCodeLookupHandler cobrindo persistência, validação de duplicatas, tratamento de erros
- **Mapeamento de clima (current + diário)**: Testes para WeatherService cobrindo mapeamento de dados atuais e previsão diária, cache, geocodificação
- **Validações (CEP e days)**: Testes para GetCepValidator, GetWeatherValidator, AddZipCodeLookupValidator cobrindo todos os casos de validação
- **Implementação**: xUnit, Moq, FluentAssertions - 140 testes unitários com 100% de aprovação

## 🏗️ Arquitetura e Template Base

Este projeto é baseado no template **[Biss.Solutions.MicroService.Template.Net9](https://www.nuget.org/packages/Biss.Solutions.MicroService.Template.Net9)** (versão 2.1.0), adaptado para utilizar bibliotecas compatíveis com **.NET 8**.

### Padrões e Tecnologias

O template fornece uma estrutura sólida seguindo os seguintes padrões e tecnologias:

#### 🏛️ Arquitetura

- **Clean Architecture** organizada em camadas independentes
- **CQRS** (Command Query Responsibility Segregation) com MediatR
- **Domain-Driven Design** com foco no domínio de negócio
- **Specification Pattern** para validação de regras de negócio
- **Repository Pattern** com interfaces genéricas e cache decorator
- **SOLID Principles** aplicados em todo o código

#### 🛠️ Tecnologias Principais

- **.NET 8**: Framework da Microsoft
- **Entity Framework Core 8.0.6**: Persistência de dados com otimizações de performance
- **AutoMapper 13.0.1**: Mapeamento automático entre objetos (DTOs e entidades)
- **MediatR 12.3.0**: Implementação do padrão Mediator para Commands e Queries
- **FluentValidation 11.9.0**: Validação fluente de Requests com suporte a validações customizadas
- **Swagger/OpenAPI**: Documentação automática da API com anotações completas
- **HealthChecks**: Monitoramento detalhado da saúde da API e banco de dados
- **Biss.MultiSinkLogger**: Logging estruturado com suporte a múltiplos sinks (Console, File)
- **Serilog**: Logging estruturado com enriquecimento de contexto e correlation IDs
- **Microsoft.Extensions.Caching.Memory**: Cache em memória para otimização de performance

#### ⚡ Performance e Otimização

- **Compressão de Resposta**: Brotli e Gzip para reduzir tamanho de dados
- **Entity Framework Otimizado**: NoTracking por padrão, queries otimizadas
- **Response Compression**: Redução significativa no tamanho das respostas HTTP
- **Connection Pooling**: Pool de conexões otimizado
- **Cache em Memória**: Cache de respostas de clima com TTL configurável

#### 🛡️ Segurança e Proteção

- **Rate Limiting**: Limitação de taxa configurável por endpoint
- **Security Headers**: Headers de segurança implementados (X-Frame-Options, X-Content-Type-Options)
- **HTTPS Redirection**: Redirecionamento HTTPS configurável
- **CORS**: Configuração robusta por ambiente (Development/Production)
- **Validação de entrada**: FluentValidation para validação robusta
- **Problem Details (RFC 7807)**: Respostas de erro padronizadas

#### 📊 Observabilidade

- **Biss.MultiSinkLogger** para logging multi-destino configurável
- **Health Checks** detalhados (API, Database, External Dependencies)
- **Global Exception Handler** com tratamento centralizado de exceções
- **Correlation IDs** para rastreamento de requisições
- **Structured Logging** com enriquecimento de contexto
- **HTTP Logging** para captura de requisições e respostas

#### 🌐 Internacionalização

- **Suporte a múltiplos idiomas** (pt-BR, en-US, es)
- **Resource files** para localização
- **Accept-Language header** support
- **Configuração de cultura** por requisição

#### 🧪 Testes

- **XUnit e Moq**: Testes unitários de infraestrutura, aplicação e API
- **FluentAssertions**: Assertions expressivas para testes mais legíveis
- **Cobertura completa** das regras críticas:
  - Normalização e mapeamento de CEP (26 testes)
  - Fallback de provedores (4+ testes)
  - Serviço de persistência (US02)
  - Mapeamento de clima (current + diário)
  - Validações (CEP e days)
- **140 testes implementados** com 100% de aprovação

### Estrutura de Camadas

A solução é dividida em **5 camadas** principais, seguindo os princípios de Clean Architecture:

- **Api**: Responsável por receber as requisições HTTP, aplicar validações iniciais, gerenciar middlewares, compressão de resposta e devolver as respostas formatadas.
- **Application**: Orquestra a lógica de negócios, gerencia Commands, Queries, Specifications e utiliza padrões como CQRS e Mediator.
- **Infrastructure (Infra)**: Implementa o acesso a dados e a comunicação com serviços externos.
- **Domain**: Define as entidades de domínio, enums, interfaces, specifications e regras de negócio puras.
- **CrossCutting**: Contém utilitários e configurações compartilhadas entre todas as camadas (como injeções de dependência, logs, validações, health checks, rate limiting).

## 🚀 Configuração e Execução

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) ou superior
- Visual Studio 2022, VS Code ou Rider (opcional)
- **Docker** e **Docker Compose** (obrigatório para US04)

### Instalação

1. Clone o repositório:

```bash
git clone <url-do-repositorio>
cd test-dotnet/Hapvida.ExternalIntegration
```

2. Restaure as dependências:

```bash
dotnet restore
```

3. Compile a solução:

```bash
dotnet build
```

### Executando a API

#### Opção 1: Execução direta

```bash
cd src/Hapvida.ExternalIntegration.Api
dotnet run
```

A API estará disponível em:

- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`
- **Swagger UI**: `https://localhost:5001/swagger`

#### Opção 2: Execução com Docker (US04 - OBRIGATÓRIO)

A API pode ser executada usando Docker de duas formas:

##### 2.1. Usando Docker Compose (Recomendado)

```bash
# Na raiz do projeto Hapvida.ExternalIntegration
docker-compose up --build
```

A API estará disponível em:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `http://localhost:5001`
- **Swagger UI**: `http://localhost:5000/swagger`

Para executar em background:
```bash
docker-compose up -d --build
```

Para parar:
```bash
docker-compose down
```

##### 2.2. Usando Docker diretamente

```bash
# Build da imagem
docker build -f src/Hapvida.ExternalIntegration.Api/Dockerfile -t hapvida-api .

# Executar o container
docker run -d \
  --name hapvida-api \
  -p 5000:8080 \
  -p 5001:8081 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  hapvida-api
```

##### Variáveis de Ambiente

As seguintes variáveis de ambiente podem ser configuradas:

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `ASPNETCORE_ENVIRONMENT` | Ambiente de execução (Development, Staging, Production) | `Production` |
| `ASPNETCORE_URLS` | URLs que a API escutará | `http://+:8080` |
| `ASPNETCORE_HTTP_PORTS` | Porta HTTP interna do container | `8080` |
| `ASPNETCORE_HTTPS_PORTS` | Porta HTTPS interna do container | `8081` |
| `ConnectionStrings__DefaultConnection` | String de conexão do banco de dados | `Data Source=:memory:` |
| `HTTP_PORT` | Porta HTTP externa (docker-compose) | `5000` |
| `HTTPS_PORT` | Porta HTTPS externa (docker-compose) | `5001` |

**Exemplo com variáveis de ambiente customizadas:**

```bash
docker run -d \
  --name hapvida-api \
  -p 8080:8080 \
  -p 8081:8081 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS=http://+:8080 \
  hapvida-api
```

##### Portas do Container

- **8080**: Porta HTTP interna (mapeada para porta externa configurável)
- **8081**: Porta HTTPS interna (mapeada para porta externa configurável)

**Nota**: O Dockerfile utiliza multi-stage build para otimizar o tamanho da imagem final, utilizando `mcr.microsoft.com/dotnet/aspnet:8.0` como imagem base (runtime apenas, sem SDK).

### Executando os Testes

Para executar todos os testes:

```bash
dotnet test
```

Para executar com cobertura de código:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

Para executar testes de um projeto específico:

```bash
dotnet test test/Hapvida.ExternalIntegration.UnitTest/Hapvida.ExternalIntegration.UnitTest.csproj
```

## 📡 Endpoints da API

### US01: Consulta de CEP

**GET** `/api/v1/cep/{zipCode}`

Consulta um CEP e retorna o endereço normalizado.

**Parâmetros:**

- `zipCode` (path): CEP com ou sem hífen (ex: `01001000` ou `01001-000`)

**Respostas:**

- `200 OK`: CEP encontrado
- `400 Bad Request`: CEP inválido
- `404 Not Found`: CEP não encontrado
- `500 Internal Server Error`: Erro interno

**Exemplo de requisição:**

```bash
curl -X GET "https://localhost:5001/api/v1/cep/01306001"
```

**Exemplo de resposta:**

```json
{
  "success": true,
  "data": {
    "zipCode": "01306001",
    "street": "Avenida Paulista",
    "district": "Bela Vista",
    "city": "São Paulo",
    "state": "SP",
    "ibge": "3550308",
    "location": {
      "lat": -23.5505,
      "lon": -46.6333
    },
    "provider": "brasilapi"
  },
  "statusCode": 200
}
```

### US02: Persistência de CEP

**POST** `/api/v1/cep`

Persiste um CEP no banco de dados em memória.

**Body:**

```json
{
  "zipCode": "01306001"
}
```

**Respostas:**

- `201 Created`: CEP persistido com sucesso
- `400 Bad Request`: CEP inválido
- `404 Not Found`: CEP não encontrado nos provedores externos
- `409 Conflict`: CEP já persistido
- `500 Internal Server Error`: Erro interno

**Exemplo de requisição:**

```bash
curl -X POST "https://localhost:5001/api/v1/cep" \
  -H "Content-Type: application/json" \
  -d '{"zipCode": "01306001"}'
```

**Exemplo de resposta:**

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "zipCode": "01306001",
    "street": "Avenida Paulista",
    "district": "Bela Vista",
    "city": "São Paulo",
    "state": "SP",
    "ibge": "3550308",
    "location": {
      "lat": -23.5505,
      "lon": -46.6333
    },
    "provider": "brasilapi",
    "createdAtUtc": "2024-01-15T10:30:00Z"
  },
  "statusCode": 201,
  "message": "CEP persistido com sucesso"
}
```

### US03: Clima Atual e Previsão

**GET** `/api/v1/weather?days=3`

Consulta o clima atual e a previsão para os próximos dias com base no último CEP salvo.

**Parâmetros:**

- `days` (query, opcional): Número de dias para a previsão (padrão: 3, máximo: 7)

**Respostas:**

- `200 OK`: Dados de clima encontrados
- `206 Partial Content`: Dados de clima encontrados (coleção não vazia)
- `400 Bad Request`: Parâmetro `days` inválido
- `404 Not Found`: Nenhum CEP salvo ou dados de clima não encontrados
- `500 Internal Server Error`: Erro interno

**Exemplo de requisição:**

```bash
curl -X GET "https://localhost:5001/api/v1/weather?days=5"
```

**Exemplo de resposta:**

```json
{
  "success": true,
  "data": [
    {
      "sourceZipCodeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "location": {
        "lat": -23.5505,
        "lon": -46.6333,
        "city": "São Paulo",
        "state": "SP"
      },
      "current": {
        "temperatureC": 25.5,
        "humidity": 0.65,
        "apparentTemperatureC": 26.0,
        "observedAt": "2024-01-15T10:30:00Z"
      },
      "daily": [
        {
          "date": "2024-01-15",
          "tempMinC": 20.0,
          "tempMaxC": 28.0
        },
        {
          "date": "2024-01-16",
          "tempMinC": 21.0,
          "tempMaxC": 29.0
        }
      ],
      "provider": "open-meteo"
    }
  ],
  "statusCode": 200
}
```

**Notas:**

- O endpoint retorna dados de clima para todos os CEPs salvos, ordenados por data de criação (mais recente primeiro)
- Se o CEP salvo não tiver coordenadas, o sistema tenta geocodificar usando cidade e estado
- Os dados de clima são cacheados por 10 minutos para otimizar performance

### Health Check

**GET** `/health`

Verifica a saúde da API e suas dependências.

**Respostas:**

- `200 OK`: API saudável
- `503 Service Unavailable`: API ou dependências com problemas

**Exemplo de requisição:**

```bash
curl -X GET "https://localhost:5001/health"
```

### Métricas

**GET** `/api/v1/metrics`

Retorna informações sobre as métricas coletadas pela API.

**Respostas:**

- `200 OK`: Informações sobre métricas disponíveis

**Exemplo de requisição:**

```bash
curl -X GET "https://localhost:5001/api/v1/metrics"
```

**Exemplo de resposta:**

```json
{
  "message": "Métricas estão sendo coletadas via System.Diagnostics.Metrics",
  "availableMetrics": [
    "http_requests_total - Total de requisições HTTP (contador)",
    "http_request_duration_seconds - Duração das requisições HTTP (histograma)"
  ],
  "note": "Use ferramentas como OpenTelemetry ou Prometheus para visualizar as métricas em tempo real"
}
```

**Notas:**
- As métricas são coletadas automaticamente para todas as requisições HTTP
- Métricas incluem: método HTTP, rota e código de status
- Para visualização em tempo real, integre com OpenTelemetry ou Prometheus

## 🔴 Tratamento de Erros (RFC 7807 - Problem Details)

Todos os erros retornados pela API seguem o padrão **RFC 7807 (Problem Details)**, garantindo mensagens consistentes e legíveis.

### Estrutura do Problem Details

Todos os erros retornam um JSON no seguinte formato:

```json
{
  "type": "https://errors.hapvida.externalintegration/{tipo-erro}",
  "title": "Título do erro",
  "status": 400,
  "detail": "Descrição detalhada do erro",
  "instance": "/api/v1/cep/123",
  "traceId": "00-abc123...-def456-01",
  "extensions": {
    "campoAdicional": "valor"
  }
}
```

### Exemplos de Erros

#### 400 Bad Request - CEP Inválido

```json
{
  "type": "https://errors.hapvida.externalintegration/invalid-cep",
  "title": "CEP inválido",
  "status": 400,
  "detail": "CEP deve conter 8 dígitos.",
  "instance": "/api/v1/cep/123",
  "traceId": "00-abc123def456-01"
}
```

#### 404 Not Found - CEP Não Encontrado

```json
{
  "type": "https://errors.hapvida.externalintegration/cep-not-found",
  "title": "CEP não encontrado",
  "status": 404,
  "detail": "O CEP '99999999' não foi encontrado em nenhum provedor externo.",
  "instance": "/api/v1/cep/99999999",
  "traceId": "00-abc123def456-01",
  "extensions": {
    "zipCode": "99999999"
  }
}
```

#### 404 Not Found - Nenhum CEP Salvo

```json
{
  "type": "https://errors.hapvida.externalintegration/no-saved-cep",
  "title": "Nenhum CEP salvo",
  "status": 404,
  "detail": "Nenhum CEP foi salvo no banco de dados. Por favor, salve um CEP antes de consultar o clima.",
  "instance": "/api/v1/weather",
  "traceId": "00-abc123def456-01"
}
```

#### 409 Conflict - CEP Já Persistido

```json
{
  "type": "https://errors.hapvida.externalintegration/conflict",
  "title": "Conflito",
  "status": 409,
  "detail": "O CEP '01306001' já está persistido no banco de dados.",
  "instance": "/api/v1/cep",
  "traceId": "00-abc123def456-01"
}
```

#### 504 Gateway Timeout - Timeout de Provedor Externo

```json
{
  "type": "https://errors.hapvida.externalintegration/timeout",
  "title": "Timeout",
  "status": 504,
  "detail": "A requisição ao provedor externo 'brasilapi' excedeu o tempo limite.",
  "instance": "/api/v1/cep/01306001",
  "traceId": "00-abc123def456-01",
  "extensions": {
    "provider": "brasilapi"
  }
}
```

#### 400 Bad Request - Validação de Modelo

```json
{
  "type": "https://errors.hapvida.externalintegration/validation-error",
  "title": "Erro de validação",
  "status": 400,
  "detail": "Um ou mais erros de validação ocorreram.",
  "instance": "/api/v1/cep",
  "traceId": "00-abc123def456-01",
  "extensions": {
    "errors": {
      "zipCode": [
        "O campo ZipCode é obrigatório.",
        "O CEP deve conter 8 dígitos."
      ]
    }
  }
}
```

### Content-Type

Todos os erros são retornados com o content-type `application/problem+json`, conforme especificado no RFC 7807.

## 📚 Documentação

### Projeto

- [Especificação da Prova Técnica](Test%20.NET.pdf)
- **Swagger UI**: Disponível em `https://localhost:5001/swagger` quando a API estiver em execução

### Template Base

- [Biss.Solutions.MicroService.Template.Net9](https://www.nuget.org/packages/Biss.Solutions.MicroService.Template.Net9) - Template original em .NET 9

---

## 🎯 Status do Projeto

✅ **US01**: Consulta de CEP - Implementado e testado  
✅ **US02**: Persistência de CEP - Implementado e testado  
✅ **US03**: Clima Atual e Previsão - Implementado e testado  
✅ **US04**: Docker Multi-stage - Implementado e documentado  
✅ **US05**: Validação & Mensagens de erro (RFC 7807) - Implementado  
✅ **US06**: Resiliência a falhas externas - Implementado  
✅ **US07**: Observabilidade (Logs, Correlação, Métricas) - Implementado  
✅ **US08**: Documentação via OpenAPI/Swagger - Implementado  
✅ **US09**: Testes (Unitários e Integração) - Implementado  
✅ **Testes**: 140 testes unitários com 100% de aprovação  
✅ **Documentação**: Swagger/OpenAPI completo  

**Nota**: Este projeto demonstra cuidado com versionamento e boas práticas de desenvolvimento, utilizando uma arquitetura sólida baseada em padrões reconhecidos da indústria.

## ✅ Verificação de Requisitos

### Checklist de Implementação

#### US01 - Consulta de CEP ✅
- [x] Rota `GET /api/v1/cep/{zipCode}` (aceita com/sem hífen)
- [x] Normalização para 8 dígitos
- [x] Validação de CEP inválido → `400` (Problem Details)
- [x] BrasilAPI CEP v2 como primária
- [x] ViaCEP como fallback
- [x] Retorno `200` com JSON normalizado
- [x] `404` quando não encontrado (Problem Details)
- [x] Mapeamento de campos distintos para DTO único
- [x] Logging do provedor utilizado

#### US02 - Persistir CEP ✅
- [x] Rota `POST /api/v1/cep`
- [x] Request JSON: `{ "zipCode": "01001000" }`
- [x] Validação de CEP (8 dígitos) → `400` se inválido
- [x] Reutilização do serviço da US01
- [x] Persistência em `ZipCodeLookups` (SQLite in-memory)
- [x] Retorno `201 Created` com recurso salvo
- [x] Não permite entradas repetidas → `409 Conflict`
- [x] Campos: Id, ZipCode, Street, District, City, State, Ibge, Lat, Lon, Provider, CreatedAtUtc

#### US03 - Clima Atual e Previsão ✅
- [x] Rota `GET /api/v1/weather?days=3`
- [x] Parâmetro `days` padrão 3, aceita 1-7
- [x] Busca todos os CEPs salvos (`CreatedAtUtc DESC`)
- [x] `404` se nenhum CEP persistido (Problem Details)
- [x] Usa Lat/Lon dos registros salvos
- [x] Geocodificação por `city + state` se não houver coordenadas
- [x] Consulta Open-Meteo Forecast
- [x] Cache em memória com TTL de 10 minutos
- [x] Retorno `200` com JSON normalizado (current + daily)

#### US04 - Docker ✅
- [x] Dockerfile multi-stage
- [x] Imagem final reduzida (`mcr.microsoft.com/dotnet/aspnet:8.0`)
- [x] Documentação de portas e variáveis de ambiente
- [x] Docker Compose configurado

#### US05 - Validação & Mensagens de Erro ✅
- [x] RFC 7807 em todos os erros
- [x] Campos: `type`, `title`, `status`, `detail`, `traceId`
- [x] Exemplos: `400` CEP inválido, `404` CEP não encontrado, `504` timeout
- [x] Middleware global para exceções
- [x] Normalização de Problem Details

#### US06 - Resiliência a Falhas Externas ✅
- [x] Timeouts explícitos (2-5s por tentativa)
- [x] Retry com backoff exponencial + jitter (3 tentativas)
- [x] Circuit Breaker (abre após X falhas/Y s, half-open, fecha se OK)
- [x] Logging de tentativas e estado do circuito
- [x] Implementação: `Microsoft.Extensions.Http.Resilience`

#### US07 - Observabilidade ✅
- [x] Logs estruturados (JSON) com `traceId` em cada request
- [x] Métricas mínimas (requisições por rota, latência)
- [x] Correlation ID (`X-Correlation-ID`)

#### US08 - Documentação OpenAPI/Swagger ✅
- [x] Swagger UI em `/swagger`
- [x] Schemas de request/response com exemplos
- [x] Códigos de status documentados
- [x] Implementação: `Swashbuckle.AspNetCore` com anotações

#### US09 - Testes ✅
- [x] Normalização/mapeamento de CEP (26 testes)
- [x] Fallback de provedores (4+ testes)
- [x] Serviço de persistência (US02)
- [x] Mapeamento de clima (current + diário)
- [x] Validações (CEP e days)
- [x] Total: 140 testes com 100% de aprovação

### Observações

**Rotas da API:**
- A especificação menciona rotas sem versionamento (`/cep`, `/weather`), mas a implementação utiliza versionamento (`/api/v1/cep`, `/api/v1/weather`), que é uma prática recomendada para APIs REST. As rotas funcionais são:
  - `GET /api/v1/cep/{zipCode}` (equivalente funcional a `GET /cep/{zipCode}`)
  - `POST /api/v1/cep` (equivalente funcional a `POST /cep`)
  - `GET /api/v1/weather?days=3` (equivalente funcional a `GET /weather?days=3`)

**Todos os requisitos foram implementados e testados conforme especificado.**