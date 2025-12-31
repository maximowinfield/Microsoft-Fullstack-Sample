# ---------- 1) Build React (Vite) ----------
FROM node:18-alpine AS web-build
WORKDIR /src/web

# install deps
COPY web/package.json web/package-lock.json* ./
RUN npm ci

# copy web source
COPY web/ ./

# Build-time env for Vite (baked into bundle)
ARG VITE_API_URL=/api
ARG VITE_BASE=/

# ✅ Demo vars (baked into bundle)
ARG VITE_DEMO_ENABLED=false
ARG VITE_DEMO_USERNAME=parent1
ARG VITE_DEMO_PASSWORD=ChangeMe123
ARG VITE_DEMO_PARENT_PIN=1234

# expose to build process
ENV VITE_API_URL=${VITE_API_URL}
ENV VITE_BASE=${VITE_BASE}
ENV VITE_DEMO_ENABLED=${VITE_DEMO_ENABLED}
ENV VITE_DEMO_USERNAME=${VITE_DEMO_USERNAME}
ENV VITE_DEMO_PASSWORD=${VITE_DEMO_PASSWORD}
ENV VITE_DEMO_PARENT_PIN=${VITE_DEMO_PARENT_PIN}

# Write .env.production so Vite definitely sees them at build time
RUN echo "VITE_API_URL=${VITE_API_URL}" > .env.production \
    && echo "VITE_BASE=${VITE_BASE}" >> .env.production \
    && echo "VITE_DEMO_ENABLED=${VITE_DEMO_ENABLED}" >> .env.production \
    && echo "VITE_DEMO_USERNAME=${VITE_DEMO_USERNAME}" >> .env.production \
    && echo "VITE_DEMO_PASSWORD=${VITE_DEMO_PASSWORD}" >> .env.production \
    && echo "VITE_DEMO_PARENT_PIN=${VITE_DEMO_PARENT_PIN}" >> .env.production

RUN npm run build


# ---------- 2) Build & publish .NET API ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build
WORKDIR /src

# copy API csproj first for better caching
COPY api/*.csproj ./api/
RUN dotnet restore ./api/MsFullstackSample.Api.csproj

# copy the rest of the API source
COPY api/ ./api/

# copy built React dist into api/wwwroot
COPY --from=web-build /src/web/dist ./api/wwwroot/

# publish (SPECIFY THE PROJECT FILE)
RUN dotnet publish ./api/MsFullstackSample.Api.csproj -c Release -o /app/publish


# ---------- 3) Runtime image ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=api-build /app/publish ./

# ✅ Default PORT for local; Render will override at runtime
ENV PORT=10000
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

EXPOSE 10000
ENTRYPOINT ["dotnet", "MsFullstackSample.Api.dll"]
