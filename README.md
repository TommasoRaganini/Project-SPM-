# Tourist Itinerary Management System (Project-SPM)

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Enabled-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![Testing](https://img.shields.io/badge/Tests-xUnit%20%2B%20FluentAssertions-25c2a0)](https://xunit.net/)

An enterprise-grade tourist itinerary management system developed in **C# / .NET 8** following **Clean Architecture**, **Domain-Driven Design (DDD)**, and the **State Pattern**.

---

## 🏛️ Architecture & Design Patterns

The solution is organized according to Clean Architecture principles, ensuring strict separation of concerns and testability:

```
ProjectSPM.slnx
├── src/
│   ├── Domain/                 # Core entities, Value Objects, State Pattern (zero external dependencies)
│   │   ├── Common/             # Result pattern & value containers
│   │   ├── Entities/           # Itinerary aggregate root
│   │   └── States/             # IItineraryState, DraftState, PublishedState
│   ├── Application/            # Application use cases, DTOs, service & repository interfaces
│   │   ├── DTOs/               # CreateItineraryRequest, ItineraryResponse
│   │   ├── Interfaces/         # IItineraryRepository, IItineraryService
│   │   └── Services/           # ItineraryService orchestration
│   ├── Infrastructure/         # Persistence layer (EF Core 8, Npgsql, PostgreSQL mapping)
│   │   ├── Persistence/        # ApplicationDbContext, ItineraryConfiguration
│   │   └── Repositories/       # ItineraryRepository
│   └── Api/                    # Presentation layer (ASP.NET Core REST API, Swagger/OpenAPI)
│       └── Controllers/        # ItinerariesController
├── tests/
│   └── Domain.Tests/           # Unit and integration test suite (18 automated tests)
└── Postman/
    └── Itineraries_US01.postman_collection.json # Ready-to-import Postman collection
```

### Key Patterns Applied
- **State Pattern**: The `Itinerary` aggregate encapsulates its lifecycle state (`Draft`, `Published`, etc.) via `IItineraryState`. In `DraftState`, public catalog visibility is strictly prohibited (`IsVisibleInPublicCatalog = false`), and stops can be edited.
- **Result Pattern**: Domain validations and operations return a strongly-typed `Result<T>` containing explicit error messages rather than throwing exceptions for expected business rule violations.
- **Repository Pattern**: Decouples application logic from data access technologies (Entity Framework Core).

---

## 🚀 Quick Start Guide

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (no local PostgreSQL installation required)

### 1. Start the Database Container
Run PostgreSQL 16 via Docker Compose:
```bash
docker compose up -d
```
The database will be initialized on port `5432` with credentials:
- **User**: `postgres`
- **Password**: `postgrespassword`
- **Database**: `itineraries_db`

### 2. Run the Automated Test Suite
Execute the unit and domain tests:
```bash
dotnet test
```
*All 18 tests will execute and validate the acceptance criteria.*

### 3. Launch the Backend API
Start the ASP.NET Core Web API:
```bash
dotnet run --project src/Api/Api.csproj --launch-profile "http"
```
The API starts listening on **`http://localhost:5207`**.
The database schema (`itineraries` table) is created automatically on first run via `EnsureCreated()`.

---

## 🧪 Testing the API

### Option A: Interactive Swagger UI
Open your browser and navigate to:
**[http://localhost:5207/swagger](http://localhost:5207/swagger)**

### Option B: Postman Collection
Import the collection located in `Postman/Itineraries_US01.postman_collection.json`:
1. **Create Draft Itinerary**: Sends valid payload with `X-Editor-Id` header (auto-saves ID as a variable).
2. **Reopen Saved Draft**: Retrieves the saved draft by ID (verifies AC05).
3. **Validation Failure (Missing Fields)**: Tests missing mandatory fields (verifies AC04 and `400 Bad Request`).
4. **Public Catalog**: Verifies that drafts do not appear in public listings (verifies AC02).

---

## 📋 Backlog & Acceptance Criteria Coverage

### US01: Thematic Itinerary Creation (Draft Status)
> *As an editor, I want to create a new thematic itinerary in draft status so that I can progressively define its contents before publication.*

| Criterion | Description | Implementation Details |
|---|---|---|
| **AC01** | Authenticated editor creates an itinerary with title, theme, description, and geographic area. | `POST /api/itineraries` validates mandatory domain attributes and captures editor identity via `X-Editor-Id` header or body. |
| **AC02** | On creation, itinerary is assigned `Draft` status and is excluded from the public catalog. | Enforced by the **State Pattern** (`DraftState.IsVisibleInPublicCatalog = false`) and filtered in `GET /api/itineraries/public-catalog`. |
| **AC03** | System assigns a unique identifier and records responsible editor. | Generates a unique `Guid` and sets `ResponsibleEditorId`. |
| **AC04** | If a mandatory field is missing or invalid, system rejects and does not persist. | Validates all required inputs; returns `400 Bad Request` with exact validation messages without touching the DB. |
| **AC05** | Editor can reopen the draft and retrieve previously saved data. | `GET /api/itineraries/{id}` reconstructs and returns all previously stored itinerary data from PostgreSQL. |