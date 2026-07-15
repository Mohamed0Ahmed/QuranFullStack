# syntax=docker/dockerfile:1
#
# Multi-stage build for the Quran Dashboard API (QuranDashboard.Api).
# Build context is Backend/ (build with: docker build -f Backend/Dockerfile Backend).
# Production configuration (connection string, CORS, port) is supplied at runtime via
# Railway environment variables — nothing secret is baked into the image.

# ---- Build stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore layer: copy the solution + ONLY the API's project-reference graph, then restore.
# API transitive graph: Api -> Application, Application.Abstractions, Infrastructure, Shared, Domain.
# tools/QuranDashboard.DataImporter and tests/QuranDashboard.Tests are intentionally NOT part of it.
COPY QuranDashboard.sln ./
COPY api/QuranDashboard.Api/QuranDashboard.Api.csproj api/QuranDashboard.Api/
COPY application/QuranDashboard.Application/QuranDashboard.Application.csproj application/QuranDashboard.Application/
COPY application/QuranDashboard.Application.Abstractions/QuranDashboard.Application.Abstractions.csproj application/QuranDashboard.Application.Abstractions/
COPY infrastructure/QuranDashboard.Infrastructure/QuranDashboard.Infrastructure.csproj infrastructure/QuranDashboard.Infrastructure/
COPY domain/QuranDashboard.Domain/QuranDashboard.Domain.csproj domain/QuranDashboard.Domain/
COPY shared/QuranDashboard.Shared/QuranDashboard.Shared.csproj shared/QuranDashboard.Shared/

RUN dotnet restore api/QuranDashboard.Api/QuranDashboard.Api.csproj

# Copy the remaining source and publish the API only (framework-dependent, no native apphost).
COPY . .
RUN dotnet publish api/QuranDashboard.Api/QuranDashboard.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---- Runtime stage ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Run as the non-root "app" user shipped with the aspnet image.
USER app

# Informational only. Railway injects $PORT at runtime and the app binds it via
# ASPNETCORE_URLS=http://0.0.0.0:${PORT} (set as a Railway environment variable).
EXPOSE 8080

ENTRYPOINT ["dotnet", "QuranDashboard.Api.dll"]
