# GUHC Hierarchy System

A production-quality .NET 8 account hierarchy management system for Grand Unified Holding Corp. (GUHC). Manages organizational structures in a strict tree format with support for unlimited nesting (up to depth 5), cycle prevention, and orphan account reassignment.

## Architecture

- **Core Layer** (`GUHC.HierarchySystem.Core`): Domain entities, DTOs, and service interfaces
- **Infrastructure Layer** (`GUHC.HierarchySystem.Infrastructure`): Entity Framework Core, database context, and business logic implementation
- **API Layer** (`GUHC.HierarchySystem.Api`): ASP.NET Core Web API with 5 REST endpoints
- **Client Layer** (`GUHC.HierarchySystem.ConsoleClient`): Console application that visualizes account hierarchies as ASCII trees
- **Tests** (`GUHC.HierarchySystem.Tests`): Comprehensive unit test suite with 20 tests

## Prerequisites

- .NET 8 SDK
- SQL Server LocalDB (or any SQL Server instance)
- Visual Studio Code or Visual Studio 2022 (optional)

## Setup & Database Migration

### 1. Install Dependencies

From the solution root, restore NuGet packages:

```bash
dotnet restore
```

### 2. Create the Database & Run Migrations

The system uses Entity Framework Core with SQL Server. Create the initial migration and apply it to the database:

```bash
dotnet ef migrations add InitialCreate `
  --project GUHC.HierarchySystem.Infrastructure `
  --startup-project GUHC.HierarchySystem.Api
```

Then update the database:

```bash
dotnet ef database update `
  --project GUHC.HierarchySystem.Infrastructure `
  --startup-project GUHC.HierarchySystem.Api
```

**Database Location**: By default, the system uses SQL Server LocalDB at:
```
Server=(localdb)\MSSQLLocalDB;Database=GUHC_Hierarchy;Trusted_Connection=True;
```

To use a different SQL Server instance, modify the `ConnectionStrings.DefaultConnection` in `GUHC.HierarchySystem.Api/appsettings.json`.

## Running the Application

### Start the API Server

From the solution root, run:

```bash
dotnet run --project GUHC.HierarchySystem.Api
```

The API will start on **https://localhost:7246** with Swagger UI available at https://localhost:7246/swagger/index.html

### Run the Console Client

In a separate terminal, run:

```bash
dotnet run --project GUHC.HierarchySystem.ConsoleClient
```

The console app will prompt you:
- Press Enter to view the full account hierarchy starting from the root
- Enter an account ID to view a specific subtree

## API Endpoints

All endpoints are prefixed with `/api/accounts/`:

### 1. Create Account
```http
POST /api/accounts
Content-Type: application/json

{
  "name": "Regional Office - Asia",
  "parentId": null
}
```

**Response**: `201 Created`
```json
{
  "id": 1,
  "name": "Regional Office - Asia",
  "parentId": null,
  "depth": 0
}
```

### 2. Get Account
```http
GET /api/accounts/{id}
```

**Response**: `200 OK`
```json
{
  "id": 1,
  "name": "Regional Office - Asia",
  "parentId": null,
  "depth": 0
}
```

### 3. Move Account to Different Parent
```http
PUT /api/accounts/{id}/move
Content-Type: application/json

{
  "newParentId": 2
}
```

**Response**: `200 OK`

### 4. Get Account Subtree (Hierarchical View)
```http
GET /api/accounts/{id}/tree
```

**Response**: `200 OK`
```json
{
  "id": 1,
  "name": "Global Account",
  "depth": 0,
  "children": [
    {
      "id": 2,
      "name": "Regional Office - Asia",
      "depth": 1,
      "children": [
        {
          "id": 3,
          "name": "Country Office - India",
          "depth": 2,
          "children": []
        }
      ]
    }
  ]
}
```

### 5. Delete Account
```http
DELETE /api/accounts/{id}
```

**Response**: `200 OK`

> **Note**: Deleting an account automatically reassigns all its children to the deleted account's parent, preventing orphaned nodes.

## Sample Data Flow

### 1. Create a Root Account
```bash
curl -X POST https://localhost:7246/api/accounts `
  -H "Content-Type: application/json" `
  -d '{"name":"Global HQ","parentId":null}'
```

Response:
```json
{"id":1,"name":"Global HQ","parentId":null,"depth":0}
```

### 2. Create Regional Accounts Under Root
```bash
curl -X POST https://localhost:7246/api/accounts `
  -H "Content-Type: application/json" `
  -d '{"name":"EMEA Region","parentId":1}'

curl -X POST https://localhost:7246/api/accounts `
  -H "Content-Type: application/json" `
  -d '{"name":"APAC Region","parentId":1}'
```

### 3. Create Country Offices Under Regions
```bash
curl -X POST https://localhost:7246/api/accounts `
  -H "Content-Type: application/json" `
  -d '{"name":"Germany Office","parentId":2}'

curl -X POST https://localhost:7246/api/accounts `
  -H "Content-Type: application/json" `
  -d '{"name":"France Office","parentId":2}'
```

### 4. View Complete Hierarchy
```bash
curl https://localhost:7246/api/accounts/1/tree
```

### 5. Move an Account
```bash
# Move France Office from EMEA to APAC
curl -X PUT https://localhost:7246/api/accounts/4/move `
  -H "Content-Type: application/json" `
  -d '{"newParentId":3}'
```

### Console Client Visualization

When running the console client, the full hierarchy displays as:

```
Account Hierarchy Tree:
=======================

Global HQ (ID: 1)
├─ EMEA Region [Depth: 1] (ID: 2)
│  └─ Germany Office [Depth: 2] (ID: 4)
└─ APAC Region [Depth: 1] (ID: 3)
   └─ France Office [Depth: 2] (ID: 5)
```

## Business Rules

1. **Maximum Depth**: Accounts can be nested up to 5 levels deep (root = depth 0)
2. **Cycle Prevention**: Cannot move an account under one of its descendants
3. **Orphan Handling**: When an account is deleted, its children are automatically reassigned to the deleted account's parent
4. **Root Protection**: Cannot change a root account's parent to another account
5. **Parent Validation**: Parent account must exist and be at a valid depth level

## Testing

Run the comprehensive unit test suite:

```bash
dotnet test
```

This executes 20 tests covering:
- Account creation with depth validation
- Account retrieval and hierarchy traversal
- Move operations with cycle detection
- Account deletion with orphan reassignment
- Subtree extraction and recursive tree building

All tests pass with zero warnings.

## Project Structure

```
GUHC.HierarchySystem.sln
├── GUHC.HierarchySystem.Core/
│   ├── Entities/
│   │   └── Account.cs
│   ├── DTOs/
│   │   ├── CreateAccountDto.cs
│   │   ├── MoveAccountDto.cs
│   │   ├── AccountResponseDto.cs
│   │   └── AccountTreeResponseDto.cs
│   └── Services/
│       └── IAccountService.cs
├── GUHC.HierarchySystem.Infrastructure/
│   ├── Data/
│   │   └── AppDbContext.cs
│   └── Services/
│       └── AccountService.cs
├── GUHC.HierarchySystem.Api/
│   ├── Controllers/
│   │   └── AccountsController.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── Properties/
│       └── launchSettings.json
├── GUHC.HierarchySystem.ConsoleClient/
│   └── Program.cs
└── GUHC.HierarchySystem.Tests/
    └── AccountServiceTests.cs
```

## Configuration

### Database Connection String

Edit `GUHC.HierarchySystem.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=GUHC_Hierarchy;Trusted_Connection=True;"
  }
}
```

### Console Client API URL

Edit `GUHC.HierarchySystem.ConsoleClient/Program.cs` (line 5) to point to your API:

```csharp
var apiUrl = "https://localhost:7246";
```

## Troubleshooting

### Database Migration Fails
- Ensure SQL Server LocalDB is running: `sqllocaldb start mssqllocaldb`
- Verify the connection string in `appsettings.json` matches your SQL Server instance
- Check that the Infrastructure project has Entity Framework Core installed

### API Won't Start
- Ensure port 7246 is not in use
- Verify the database has been created with migrations
- Check `appsettings.Development.json` for any environment-specific overrides

### Console Client Connection Refused
- Ensure the API is running: `dotnet run --project GUHC.HierarchySystem.Api`
- Verify the API URL in the console client matches the running API URL
- Check firewall settings for HTTPS communication on port 7246

## Technology Stack

- **.NET 8.0**
- **ASP.NET Core Web API**
- **Entity Framework Core 8.0**
- **SQL Server**
- **xUnit 2.5.3** (testing)
- **Moq 4.18.0** (mocking)
