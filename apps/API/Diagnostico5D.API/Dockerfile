# ── Stage 1: Build Admin (React) ───────────────────────────────────────
FROM node:22-alpine AS admin-build
WORKDIR /admin

COPY ../../Admin/package*.json ./
RUN npm install

COPY ../../Admin .
RUN npm run build

# ── Stage 2: Build API (.NET 10) ───────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

# ── Stage 3: Runtime ───────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=api-build /app/publish .

# Static HTML pages (landing + diagnostico)
COPY ../../../index.html      ./wwwroot/index.html
COPY ../../../diagnostico.html ./wwwroot/diagnostico.html

# React Admin build output → /admin sub-path
COPY --from=admin-build /admin/dist ./wwwroot/admin

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Diagnostico5D.API.dll"]
