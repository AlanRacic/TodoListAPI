# TodoListAPI

### ASP.NET Core application combining MVC and authenticated REST API workflows with Identity, per-user todo ownership, EF Core Code-First, and SQL Server

TodoListAPI is a **.NET 10 / ASP.NET Core** application for managing personal todo lists and tasks through both an MVC interface and authenticated REST API endpoints.

ASP.NET Core Identity provides user registration and authentication, while todo data is scoped to the authenticated user. Entity Framework Core Code-First and migrations manage the SQL Server persistence model.

---

## Application Flow

```text
                     ┌─────────────────┐
                     │ Authenticated   │
                     │      User       │
                     └────────┬────────┘
                              │
                 ┌────────────┴────────────┐
                 ↓                         ↓
          ASP.NET Core MVC           REST API
          ProfileController       TodoApiController
                 │                         │
                 └────────────┬────────────┘
                              ↓
                    ApplicationDbContext
                              ↓
                    Entity Framework Core
                              ↓
                         SQL Server
```

The same user-owned todo data is available through two application surfaces:

- Razor-based MVC workflows for interactive browser usage;
- authenticated REST endpoints for programmatic access.

Both paths enforce ownership against the currently authenticated Identity user.

---

## Authentication & User Ownership

ASP.NET Core Identity manages account registration, login, logout, and authenticated user sessions.

Todo lists are associated with an Identity user through `UserId`.

```text
IdentityUser
     │
     │ 1
     │
     └──────── *
              TodoList
                  │
                  │ 1
                  │
                  └──────── *
                           TodoTask
```

Before accessing or modifying user-owned resources, the application resolves the current user through `UserManager<IdentityUser>` and constrains database operations by that user's identifier.

For operations involving an existing list or task, ownership is verified before the modification is allowed.

```text
Authenticated user
        ↓
Resolve UserId
        ↓
Find list or task owned by UserId
        ↓
Resource exists?
   ├── No  → Not Found
   └── Yes → Perform operation
```

This ownership enforcement is applied to both the MVC and REST API workflows.

---

## MVC Workflows

The authenticated MVC area allows users to:

- view their own todo lists;
- create a new list;
- add tasks to a list;
- mark tasks as completed.

The profile view loads the authenticated user's lists together with their related tasks.

Read-only queries use `AsNoTracking()` where entity tracking is not required.

MVC state-changing actions use POST requests with anti-forgery validation:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
```

The application therefore combines authentication, resource ownership checks, and standard ASP.NET Core CSRF protection for browser-based workflows.

---

## REST API

Authenticated todo operations are also exposed through REST endpoints.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `GET` | `/api/todolists` | Get the authenticated user's lists and tasks |
| `POST` | `/api/todolists` | Create a new todo list |
| `POST` | `/api/todolists/{listId}/tasks` | Add a task to an owned list |
| `PUT` | `/api/todotasks/{taskId}/done` | Mark an owned task as completed |

The API is protected with `[Authorize]` and uses the authenticated ASP.NET Core Identity user when querying or modifying resources.

For example, list ownership is checked before a task can be added:

```csharp
var listExists = await _context.TodoLists
    .AnyAsync(list =>
        list.TodoListId == listId &&
        list.UserId == userId);
```

A user therefore cannot add tasks to another user's list simply by supplying a different list identifier.

---

## API Contracts

The REST API uses dedicated request and response contracts instead of exposing EF Core entities directly.

Request contracts include:

```text
CreateListRequest
AddTaskRequest
```

Response contracts include:

```text
TodoListResponse
TodoTaskResponse
```

The API projects database data into these response models before serialization.

```text
EF Core entities
       ↓
LINQ projection
       ↓
API response contracts
       ↓
JSON response
```

This keeps the HTTP contract separate from persistence navigation properties and avoids relying on serializer cycle-handling workarounds.

---

## Data Model

The application uses two todo entities in addition to the ASP.NET Core Identity schema.

### TodoList

Represents a collection owned by an authenticated user.

```text
TodoListId
Title
UserId
Tasks
```

### TodoTask

Represents an individual task belonging to a todo list.

```text
TodoTaskId
Title
Status
TodoListId
```

The resulting relationship is:

```text
IdentityUser
     1
     │
     └────── *
           TodoList
               1
               │
               └────── *
                     TodoTask
```

---

## EF Core Code-First & Migrations

The persistence model is managed with **Entity Framework Core Code-First**.

`ApplicationDbContext` extends the ASP.NET Core Identity database context and adds the application entities:

```csharp
public DbSet<TodoList> TodoLists => Set<TodoList>();
public DbSet<TodoTask> TodoTasks => Set<TodoTask>();
```

Database evolution is captured through EF Core migrations stored in the project.

```text
C# models
    ↓
ApplicationDbContext
    ↓
EF Core migrations
    ↓
SQL Server schema
```

This complements the Database-First approach used in the InvoiceManagementMVC project by demonstrating the opposite EF Core workflow: application models define the schema and migrations evolve it.

---

## Configuration

The SQL Server connection is configured through:

`ConnectionStrings:TodoConnection`

The application validates that the connection string exists during startup before registering `ApplicationDbContext`.

The default local configuration uses:

```text
Server: .\SQLEXPRESS
Database: TodoDB
Authentication: Windows Integrated Security
```

ASP.NET Core dependency injection provides both `ApplicationDbContext` and `UserManager<IdentityUser>` to the controllers that require them.

---

## Running Locally

### Prerequisites

- .NET 10 SDK
- SQL Server or SQL Server Express
- EF Core CLI tooling for applying migrations

### 1. Restore dependencies

```bash
dotnet restore
```

### 2. Create or update the database

From the repository root:

```bash
dotnet ef database update --project TodoListAPI/TodoListAPI.csproj
```

This applies the existing EF Core migrations and creates the Identity and todo schema in `TodoDB`.

### 3. Run the application

```bash
dotnet run --project TodoListAPI/TodoListAPI.csproj
```

Default development addresses:

```text
https://localhost:7265
http://localhost:5107
```

### 4. Use the application

Create an account through ASP.NET Core Identity, sign in, and open **My Lists** to create todo lists and tasks.

Each authenticated account works with its own user-scoped data.

---

## Technology Stack

**Backend**  
C# · .NET 10 · ASP.NET Core · ASP.NET Core MVC · REST API

**Authentication & Authorization**  
ASP.NET Core Identity · Authorization · Per-User Resource Ownership

**Data**  
Entity Framework Core · SQL Server · Code-First · EF Core Migrations · LINQ

**UI**  
Razor Views · Bootstrap

**API Design**  
Controller-based REST endpoints · Request/Response Contracts · Async EF Core Operations

---

## Design Scope

TodoListAPI is intentionally a **focused multi-user ASP.NET Core application** demonstrating how MVC and REST API workflows can operate over the same Identity-backed data model.

Key design choices include:

- ASP.NET Core Identity for registration and authentication;
- user-scoped todo lists through the Identity user identifier;
- explicit ownership validation before modifying lists or tasks;
- both Razor MVC and authenticated REST API access to todo data;
- anti-forgery validation for MVC state-changing requests;
- dedicated API request and response contracts;
- direct `ApplicationDbContext` usage without unnecessary repository abstraction;
- asynchronous EF Core database operations;
- `AsNoTracking()` for read-only queries;
- Code-First database management through EF Core migrations.

The project intentionally does not introduce service or repository layers, JWT authentication, distributed infrastructure, or cloud deployment where the current application scope does not require them.

A larger todo platform could introduce richer task lifecycle operations, list and task deletion or editing, role-based administration, independent API authentication, automated testing, service-layer abstractions, pagination, observability, containerization, or cloud deployment as those requirements become justified.

---

## License

This project is licensed under the [MIT License](LICENSE).
