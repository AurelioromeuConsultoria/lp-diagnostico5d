# ── Stage 1: Build React Admin ──────────────────────────────────────────
FROM node:22-alpine AS admin-build
WORKDIR /admin
COPY apps/Admin/package*.json ./
RUN npm ci
COPY apps/Admin .
RUN npm run build

# ── Stage 2: Build .NET API ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
COPY apps/API/Diagnostico5D.API/Diagnostico5D.API.csproj ./
RUN dotnet restore
COPY apps/API/Diagnostico5D.API .
RUN dotnet publish -c Release -o /app/publish

# ── Stage 3: Runtime ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Chromium for server-side PDF generation
RUN apt-get update && apt-get install -y \
    chromium \
    fonts-liberation \
    --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

ENV CHROMIUM_PATH=/usr/bin/chromium

COPY --from=api-build /app/publish .

# Páginas estáticas
COPY diagnostico.html ./wwwroot/diagnostico.html
COPY index.html       ./wwwroot/index.html

# Admin React build → /admin
COPY --from=admin-build /admin/dist ./wwwroot/admin

# Diretório persistente para SQLite e PDFs gerados
# ⚠️  Monte um volume neste caminho no Coolify/Docker:
#     Path: /app/data  →  Volume: diagnostico5d-data
# Sem o volume os dados são perdidos a cada redeploy.
RUN mkdir -p /app/data

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Variáveis obrigatórias — configure no Coolify em Environment Variables:
#   Jwt__Key                    = chave secreta JWT (mín. 32 chars)
#   EvolutionApi__BaseUrl       = URL base da Evolution API
#   EvolutionApi__ApiKey        = chave de API da Evolution
#   EvolutionApi__InstanceName  = nome da instância WhatsApp
#   Admin__Email                = email do admin inicial (apenas primeiro deploy)
#   Admin__Senha                = senha do admin inicial (apenas primeiro deploy)

ENTRYPOINT ["dotnet", "Diagnostico5D.API.dll"]
