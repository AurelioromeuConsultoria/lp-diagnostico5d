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

COPY --from=api-build /app/publish .

# Páginas estáticas
COPY diagnostico.html ./wwwroot/diagnostico.html
COPY index.html       ./wwwroot/index.html

# Admin React build → /admin
COPY --from=admin-build /admin/dist ./wwwroot/admin

# Diretório persistente para o SQLite (montar volume aqui no Coolify)
RUN mkdir -p /app/data

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Diagnostico5D.API.dll"]
