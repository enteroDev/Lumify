# ---- Build-Stufe: baut das Next.js-Frontend ----
FROM node:22-alpine AS build
WORKDIR /app

# Erst nur package-Dateien + Install (Docker-Cache)
COPY package*.json ./
RUN npm ci

# Rest des Frontends kopieren
COPY . ./

# Browser-seitige API-Adresse wird beim Build fest eingebacken.
# "/api" wird vom Reverse-Proxy (Caddy) an die API weitergereicht.
ARG NEXT_PUBLIC_API_BASE=/api
ENV NEXT_PUBLIC_API_BASE=$NEXT_PUBLIC_API_BASE

RUN npm run build

# ---- Lauf-Stufe ----
ENV NODE_ENV=production
EXPOSE 3333
CMD ["npm", "start"]
