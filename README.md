# Domain Copilot

Healthcare Domain Copilot for assessment variant D0T2. The project is being built in reviewed phases; this repository currently contains the Clean Architecture foundation, core domain rules and a first Application use case for starting a run.

## Architecture

```text
DomainCopilot.Api -> DomainCopilot.Application -> DomainCopilot.Domain
                 -> DomainCopilot.Infrastructure -> Application + Domain
```

`Domain` owns business rules. `Application` owns use cases and ports. `Infrastructure` will implement persistence, document processing and AI/vector provider adapters. `Api` is the composition root and HTTP boundary. Domain and Application must not reference Infrastructure, web frameworks, provider SDKs or vector-store SDKs.

## Prerequisites

- .NET 10 SDK 10.0.404 or a later 10.0 patch.

The local development environment used a portable copy of SDK 10.0.404 from the earlier workspace project. Install the SDK normally on a clean machine; the portable SDK is not copied into this repository.

## Build and verify

From this directory:

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

The `global.json` pins the SDK feature band used for the project. More detailed setup, database, model-provider, ingestion and demo instructions will be added as those features are implemented and verified.
