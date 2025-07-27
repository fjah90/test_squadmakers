# Exercises 1 & 2 – Authentication, Authorization & Joke Management

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Prerequisites](#prerequisites)
4. [Getting Started](#getting-started)
5. [Configuration](#configuration)
6. [REST API Reference](#rest-api-reference)
   1. [Authentication Endpoints](#authentication-endpoints)
   2. [User Endpoints](#user-endpoints)
   3. [Joke Endpoints](#joke-endpoints)
7. [Running Tests](#running-tests)
8. [Project Structure](#project-structure)
9. [Further Improvements](#further-improvements)

---

## Overview
This sub-project delivers **Exercise 1** (Authentication & Authorization) and **Exercise 2** (Joke Management) of the challenge.

* **Exercise 1 – Authentication & Authorization**
  * Local login issuing JSON Web Tokens (JWT).
  * External OAuth 2.0 login via Google / GitHub (authorization code flow).
  * Role-based access control (`user`, `admin`).

* **Exercise 2 – Joke Management**
  * Fetches jokes from external APIs (Chuck Norris & Dad Joke).
  * Persists local jokes with Entity Framework Core.
  * Exposes CRUD & advanced filter endpoints.
  * Executes parallel HTTP requests with `HttpClientFactory` and `async/await`.

## Architecture
Clean Architecture with clear separation of concerns.

```
┌──────────────────────────┐
│        Controllers       │  <-- REST layer (ASP.NET Core MVC)
├──────────────────────────┤
│     Application Layer    │  <-- Services / Use-cases
├──────────────────────────┤
│      Infrastructure      │  <-- EF Core, HttpClientFactory, JWT, OAuth
└──────────────────────────┘
```

* **Dependency Injection** configured in `Program.cs`.
* **Database**: SQLite by default (swapable by changing `ConnectionStrings:Default`).

## Prerequisites
* .NET 8.0 SDK (LTS)
* PowerShell 7 (Windows 11)
* Google / GitHub OAuth app credentials (for external login)

## Getting Started
```pwsh
# Restore & build
cd Exercises1_2/JokesApi

dotnet restore
dotnet build

# Apply EF Core migrations (SQLite)
dotnet ef database update

# Run API (https://localhost:7070 by default)
dotnet run
```
Open the generated Swagger UI to explore endpoints.

## Configuration
Key sections of `appsettings.json`:
```jsonc
{
  "ConnectionStrings": {
    "Default": "Data Source=jokes.db" // replace with PostgreSQL if needed
  },
  "JwtSettings": {
    "Issuer": "JokesApi",
    "Audience": "JokesApiUsers",
    "SecretKey": "YOUR_SUPER_SECRET_KEY",
    "ExpirationMinutes": 60
  },
  "OAuth": {
    "Google": {
      "ClientId": "<id>",
      "ClientSecret": "<secret>",
      "CallbackUrl": "https://localhost:7070/api/auth/external/callback"
    },
    "GitHub": {
      "ClientId": "<id>",
      "ClientSecret": "<secret>"
    }
  }
}
```

## REST API Reference
All endpoints that modify state or expose sensitive data are **JWT-protected** unless stated otherwise.

### Authentication Endpoints
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/login` | Public | Local login; returns JWT. |
| GET | `/api/auth/external/google-login` | Public | Redirect to Google OAuth. |
| GET | `/api/auth/external/github-login` | Public | Redirect to GitHub OAuth. |
| GET | `/api/auth/external/callback` | Public | OAuth callback; issues internal JWT. |

### User Endpoints
| Method | Route | Role(s) | Description |
|--------|-------|---------|-------------|
| GET | `/api/usuario` | `user`, `admin` | Returns current user info. |
| GET | `/api/admin` | `admin` | Example admin-only endpoint. |

### Joke Endpoints
| Method | Route | Query / Body | Role(s) | Description |
|--------|-------|--------------|---------|-------------|
| GET | `/api/chistes/aleatorio` | `origen` (Chuck \| Dad \| Local) | Public | Returns a random joke from specified or random source. |
| GET | `/api/chistes/emparejados` | – | `user`, `admin` | Returns 5 Chuck + 5 Dad jokes in parallel, paired & creatively combined. |
| GET | `/api/chistes/combinado` | – | `user`, `admin` | Generates a composite joke from multiple sources. |
| POST | `/api/chistes` | `{ "texto": "...", "origen": "Local" }` | `user`, `admin` | Creates a local joke; author = current user. |
| GET | `/api/chistes/filtrar` | `minPalabras`, `contiene`, `autorId`, `tematicaId` | `user`, `admin` | Advanced filtering via LINQ. |
| PUT | `/api/chistes/{id}` | `{ "texto": "updated" }` | author or `admin` | Updates joke text. |
| DELETE | `/api/chistes/{id}` | – | author or `admin` | Removes a joke. |

**External Sources**
* Chuck Norris: `https://api.chucknorris.io/jokes/random`
* Dad Joke: `https://icanhazdadjoke.com/` (requires `Accept: application/json`)

## Running Tests
```pwsh
cd Exercises1_2/JokesApi.Tests
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat="opencover"
```
Required coverage: **≥ 90 %**.

## Project Structure
```
Exercises1_2/
├── JokesApi/
│   ├── Controllers/
│   ├── Entities/
│   ├── Services/
│   ├── Notifications/        ← (shared types for Exercise 3 reference)
│   ├── Settings/
│   ├── Data/
│   ├── Program.cs
│   └── appsettings*.json
├── JokesApi.Tests/
│   ├── MathControllerTests.cs
│   └── TokenServiceTests.cs
└── README.md  ← you are here
```

## Further Improvements
* Refresh tokens & password reset flow.
* Rate-limiting & IP lockout for brute-force protection.
* Pagination & ordering for joke lists.
* Docker Compose with PostgreSQL.
* CI pipeline (GitHub Actions) to build, test & publish coverage.

---
© 2025 SquadMakers – All rights reserved. 