# Quran Dashboard Backend

## Purpose

Backend foundation for the Quran Dashboard project.

## Current Scope

Empty backend foundation only.

## Folder Structure

```text
Backend/
├── QuranDashboard.sln
├── api/
│   └── QuranDashboard.Api/
├── domain/
│   └── QuranDashboard.Domain/
├── application/
│   ├── QuranDashboard.Application.Abstractions/
│   └── QuranDashboard.Application/
├── infrastructure/
│   └── QuranDashboard.Infrastructure/
└── shared/
    └── QuranDashboard.Shared/
```

## Prerequisites

- .NET 10 SDK
- PostgreSQL

## Restore

```bash
cd App/Backend
dotnet restore
```

## Build

```bash
cd App/Backend
dotnet build
```

## Run

```bash
cd App/Backend
dotnet run --project api/QuranDashboard.Api
```

## Available Endpoints

- `GET /api/health`
- `GET /api/dashboard/info`

## Notes

- No auth yet
- No CRUD yet
- No Quran features yet
- No resources reading yet
- No data import yet
