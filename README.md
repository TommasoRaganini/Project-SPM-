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
│   └── Domain.Tests/           # Unit and integration test suite (61 automated tests)
└── Postman/
    ├── Itineraries_US01.postman_collection.json # Postman collection for US01
    └── Itineraries_US02.postman_collection.json # Postman collection for US02
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
dotnet test ProjectSPM.sln
```
*All 61 tests will execute and validate the acceptance criteria for US01 and US02.*

### 3. Launch the Backend API
Start the ASP.NET Core Web API:
```bash
dotnet run --project src/Api/Api.csproj --launch-profile "http"
```
The API starts listening on **`http://localhost:5207`**.
The database schema (`itineraries` and `itinerary_stops` tables) is created automatically on first run via `EnsureCreated()`.

---

## 🧪 Testing the API

### Option A: Interactive Swagger UI
Open your browser and navigate to:
**[http://localhost:5207/swagger](http://localhost:5207/swagger)**

### Option B: Postman Collections
Collections located in the `Postman/` directory:
- **`Itineraries_US01.postman_collection.json`**:
  1. **Create Draft Itinerary**: Sends valid payload with `X-Editor-Id` header (auto-saves ID as a variable).
  2. **Reopen Saved Draft**: Retrieves the saved draft by ID (verifies AC05).
  3. **Validation Failure (Missing Fields)**: Tests missing mandatory fields (verifies AC04 and `400 Bad Request`).
  4. **Public Catalog**: Verifies that drafts do not appear in public listings (verifies AC02).
- **`Itineraries_US02.postman_collection.json`**:
  1. **Add Stop to Draft**: Adds geolocated stop (verifies AC01, AC02 and `201 Created`).
  2. **Get Itinerary Stops**: Retrieves all fixed stops for map visualization (verifies AC02).
  3. **Update Stop**: Modifies existing stop data (verifies AC03).
  4. **Validation Failure (Coordinates Out of Range)**: Validates lat [-90, 90] / lon [-180, 180] (verifies AC04 and `400 Bad Request`).
  5. **Unauthorized Editor Modification**: Rejects non-owners (verifies AC05 and `403 Forbidden`).
- **`Itineraries_US03.postman_collection.json`**:
  1. **Create Draft and Add Three Stops**: Sets up the itinerary and verifies that each new stop is appended.
  2. **Reorder Stops**: Saves a new sequence and checks that the stop data is preserved (verifies AC01 and AC05).
  3. **Validation Failure (Duplicate Stop)**: Rejects a repeated stop ID (verifies AC03 and `400 Bad Request`).
  4. **Read Saved Order**: Retrieves the saved sequence and verifies that the rejected request did not change it (verifies AC02, AC03 and AC05).
  5. **Read Consecutive Segments**: Retrieves the segments in the new sequence (verifies AC04).

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

### US02: Add and Edit Geolocated Fixed Stops
> *As an editor, I want to add and edit the geolocated fixed stops of an itinerary, so that I can identify the places of interest that make up the route.*

| Criterion | Description | Implementation Details |
|---|---|---|
| **AC01** | For a draft itinerary, editor adds a stop specifying name, description, and coordinates. | `POST /api/itineraries/{id}/stops` creates and attaches `ItineraryStop` to draft itinerary. State pattern prevents modification in non-draft states. |
| **AC02** | Added stop appears in the itinerary's stops list and map representation. | `GET /api/itineraries/{id}/stops` returns all stops with coordinates for map display and list views. |
| **AC03** | Editor can edit a stop's data and see changes after saving. | `PUT /api/itineraries/{id}/stops/{stopId}` updates and persists name, description, latitude, and longitude. |
| **AC04** | System rejects coordinates outside valid ranges (lat: [-90, 90], lon: [-180, 180]). | Domain validation inside `ItineraryStop` rejects invalid/NaN coordinates and missing mandatory fields with `400 Bad Request`. |
| **AC05** | Unauthorized editor cannot modify the itinerary's stops. | Authorization check `IsAuthorizedEditor` matches caller `X-Editor-Id` against `ResponsibleEditorId`; rejects with `403 Forbidden`. |

### US03: Define and Modify the Order of Fixed Stops
> *As an editor, I want to define and modify the order of an itinerary’s fixed stops, so that I can establish the sequence in which the places of interest are to be visited.*

| Criterion | Description | Implementation Details |
|---|---|---|
| **AC01** | Editor can set an explicit sequence for fixed stops in a draft itinerary. | `PUT /api/itineraries/{id}/stops/order` accepts the ordered `stopIds` from the responsible editor. The State Pattern restricts changes to drafts. |
| **AC02** | After reordering and saving, stops are displayed in the chosen order. | `GET /api/itineraries/{id}/stops` returns stops sorted by the persisted `Position`. |
| **AC03** | The sequence cannot contain duplicates or references to stops outside the itinerary. | Domain validation requires every stop to appear exactly once. Invalid sequences return `400 Bad Request` and preserve the saved order. |
| **AC04** | Segments are recalculated between consecutive stops. | `GET /api/itineraries/{id}/segments` derives each segment from a pair of consecutive stops in the saved order. |
| **AC05** | Reordering preserves each stop’s descriptive and geographic data. | The reorder operation updates only `Position`. |
