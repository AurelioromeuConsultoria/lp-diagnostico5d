# ── Build stage ────────────────────────────────────────────────────────
FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
RUN npm run build

# ── Runtime: served by the .NET API as static files ────────────────────
# The dist/ folder is copied into the API's wwwroot/admin via docker-compose
# See docker-compose.yml for the full orchestration
FROM scratch AS export
COPY --from=build /app/dist /dist
