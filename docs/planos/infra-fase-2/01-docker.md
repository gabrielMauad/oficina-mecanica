# 01 — Docker (Dockerfile e docker-compose)

> Pré-requisito de leitura: `00-visao-geral.md` (seção 2, fatos do projeto). Conceitos em
> `docs/aulas/consolidado_03_docker.md`.

Objetivo: garantir que o Dockerfile e o docker-compose estejam revisados e funcionais, servindo
de base para a imagem que o Kubernetes e o CI/CD vão consumir.

## 1. Dockerfile — `src/Bootstrap/Api/Dockerfile`

O Dockerfile atual já é multi-stage e correto. Ajustes: fixar a porta 8080 explicitamente e
expô-la (documentação/clareza). Deixe assim:

```dockerfile
# Etapa 1: build (SDK completo, só para compilar)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY src/ ./src/
RUN dotnet restore src/Bootstrap/Api/Api.csproj
RUN dotnet publish src/Bootstrap/Api/Api.csproj -c Release -o /api --no-restore

# Etapa 2: runtime (imagem enxuta, só o necessário para executar)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /api
COPY --from=build /api ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
```

Pontos-chave (não altere sem motivo):
- **Contexto de build = raiz do repositório** (o `COPY src/ ./src/` depende disso). Sempre
  buildar com `docker build -f src/Bootstrap/Api/Dockerfile .` a partir da raiz.
- `ASPNETCORE_URLS=http://+:8080` fixa a porta que o Kestrel escuta (o K8s e o compose contam com 8080).
- `EXPOSE 8080` é documental (não publica porta sozinho).
- Migrations rodam no start da aplicação (ver `Program.cs`) — a imagem não precisa de passo
  extra para isso.

## 2. docker-compose — `docker-compose.yml` (raiz)

Revisar o compose existente para: usar a porta 8080 do container, manter o Postgres com volume
e healthcheck, e alinhar a connection string. Substitua o conteúdo por:

```yaml
services:
  api:
    build:
      context: .
      dockerfile: src/Bootstrap/Api/Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__Default=Host=postgres;Port=5432;Username=oficina;Password=oficina-dev-pass;Database=oficina_mecanica;SSL Mode=Disable
      - Jwt__Secret=docker-dev-secret-key-replace-in-prod-000
      - Auth__AdminEmail=admin@oficina.com
      - Auth__AdminSenha=admin123
    depends_on:
      postgres:
        condition: service_healthy

  postgres:
    image: postgres:16
    restart: always
    container_name: oficina-postgres
    environment:
      POSTGRES_DB: oficina_mecanica
      POSTGRES_USER: oficina
      POSTGRES_PASSWORD: oficina-dev-pass
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U oficina -d oficina_mecanica"]
      interval: 10s
      timeout: 5s
      retries: 5
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

volumes:
  postgres_data:
```

Notas:
- O host do banco no compose é `postgres` (nome do serviço). No **Kubernetes** será
  `oficina-postgres` (nome do Service) — não confunda os dois contextos.
- `depends_on ... condition: service_healthy` faz a API só subir depois do Postgres saudável
  (evita erro de migração no boot). No K8s isso é resolvido por um initContainer (ver `02`).
- Os valores aqui são de desenvolvimento local (podem ser texto plano). Os equivalentes no
  cluster vão para ConfigMap/Secret (ver `02`).

## 3. `.dockerignore` (raiz)

Garantir que artefatos de build locais não entrem no contexto (acelera o build e evita lixo):

```
**/.git
**/bin
**/obj
**/*.user
**/.vs
**/TestResults
**/coverage-results
**/coverage-report
```

## 4. Como validar

```bash
# a partir da raiz do repositório
docker compose up --build -d
# esperar o healthcheck do postgres e a API subir; então:
curl -i http://localhost:8080/healthz          # deve responder 200
# abrir a doc de API no navegador (ambiente Development):
#   http://localhost:8080/scalar
docker compose down            # (use 'docker compose down -v' para apagar o volume do banco)
```

Também valide o build isolado da imagem (é o que o CI fará):

```bash
docker build -f src/Bootstrap/Api/Dockerfile -t oficina-mecanica-api:local .
```

Se `curl /healthz` responder 200 e o build da imagem passar, esta etapa está concluída.
