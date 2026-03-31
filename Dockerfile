FROM node:22-alpine

WORKDIR /app

# Install dependencies first (layer caching)
COPY package*.json ./
RUN npm install --production

# Copy all static + server files
COPY . .

EXPOSE 3000

CMD ["node", "--no-warnings=ExperimentalWarning", "server.js"]
