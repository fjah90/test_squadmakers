# Exercises 1 & 2 – Authentication, Authorization & Joke Management

> Spanish version available in `README.md`

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Prerequisites](#prerequisites)
4. [Getting Started](#getting-started)
   1. [Local run](#local-run)
   2. [Run with Docker](#run-with-docker)
5. [Configuration](#configuration)
6. [REST API Reference](#rest-api-reference)
   1. [Authentication Endpoints](#authentication-endpoints)
   2. [User Endpoints](#user-endpoints)
   3. [Joke Endpoints](#joke-endpoints)
   4. [Math Endpoints](#math-endpoints)
7. [Running Tests](#running-tests)
8. [CI/CD](#cicd)
9. [Project Structure](#project-structure)
10. [Security](#security)
11. [Further Improvements](#further-improvements)

---

## Overview
This project delivers **Exercise 1** (Authentication & Authorization) and **Exercise 2** (Joke Management).

* **Exercise 1 – Authentication & Authorization**
  * Local login issuing JSON Web Tokens (JWT).
  * External OAuth 2.0 login via Google / GitHub (authorization-code flow).
  * Role-based access control (`user`, `admin`).

* **Exercise 2 – Joke Management**
  * Fetches jokes from external APIs (Chuck Norris & Dad Joke).
  * Persists local jokes with Entity Framework Core (SQLite by default).
  * Exposes CRUD & advanced filter endpoints.
  * Executes parallel HTTP requests with `HttpClientFactory` and `async/await`.

## Architecture
Hexagonal Architecture with clear separation of concerns.

```
┌──────────────────────────┐
│        Controllers       │  <-- REST layer (ASP.NET Core MVC)
├──────────────────────────┤
│     Application Layer    │  <-- Services / Use-cases
├──────────────────────────┤
│      Infrastructure      │  <-- EF Core, HttpClientFactory, JWT, OAuth
└──────────────────────────┘
```

* Dependency Injection configured in `Program.cs`.
* Database: SQLite by default (can be replaced via `ConnectionStrings:Default`).

## Prerequisites
* .NET 8.0 SDK (LTS)
* PowerShell 7 (Windows 11)
* Google / GitHub OAuth app credentials (for external login)
* Docker (for running with Docker)

## Getting Started
### Local run
```pwsh
# Restore & build
 dotnet restore
 dotnet build

# Apply EF Core migrations (SQLite)
 dotnet ef database update --project JokesApi/JokesApi.csproj

# Run API (http://localhost:5278 or https://localhost:7014)
 dotnet run --project JokesApi/JokesApi.csproj
```
Open the generated Swagger UI to explore endpoints.

### Run with Docker
```pwsh
# Build Docker image
 dotnet publish -c Release -o ./publish
 docker build -t jokesapi .

# Run Docker container
 docker run -p 5278:80 -e ASPNETCORE_ENVIRONMENT=Development jokesapi
```

## Configuration
Key sections of `appsettings.json`:
```jsonc
{
  "JwtSettings": {
    "Issuer": "JokesApi",
    "Audience": "JokesApiClient",
    "Key": "YOUR_SUPER_SECRET_KEY",
    "ExpirationMinutes": 60
  },
  "ConnectionStrings": {
    "Default": "Data Source=jokes.db"
  },
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": true,
    "RealIpHeader": "X-Real-IP",
    "ClientWhitelist": [],
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1s",
        "Limit": 10
      }
    ]
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
| POST | `/api/auth/refresh-token` | Protected | Issues a new JWT using a refresh token. |
| POST | `/api/auth/revoke-token` | Protected | Revokes the current refresh token. |

### User Endpoints
| Method | Route | Role(s) | Description |
|--------|-------|---------|-------------|
| POST | `/api/users/register` | Public | Registers new user (defaults role `user`; optional `role="admin"`). |
| GET | `/api/users` | `admin` | Returns full user list. |
| GET | `/api/users/{id}` | `admin` | Returns user by ID. |
| PUT | `/api/users/{id}` | `admin` | Updates user name and/or role. |
| DELETE | `/api/users/{id}` | `admin` | Deletes user. |
| GET | `/api/admin` | `admin` | Demo admin-only ping. |

### Joke Endpoints
| Method | Route | Role(s) | Description |
|--------|-------|---------|-------------|
| GET | `/api/chistes/aleatorio` | `user`, `admin` | Returns a random joke from specified or random source. |
| GET | `/api/chistes/emparejados` | `user`, `admin` | Returns 5 Chuck + 5 Dad jokes in parallel, paired & combined. |
| GET | `/api/chistes/combinado` | `user`, `admin` | Generates a composite joke from multiple sources. |
| POST | `/api/chistes` | `user`, `admin` | Creates a local joke; author = current user. |
| PUT | `/api/chistes/{id}` | author or `admin` | Updates joke text. |
| DELETE | `/api/chistes/{id}` | author or `admin` | Removes a joke. |

### Math Endpoints
| Method | Route | Role(s) | Description |
|--------|-------|---------|-------------|
| GET | `/api/matematicas/mcm` | `user`, `admin` | Returns least common multiple. |
| GET | `/api/matematicas/siguiente-numero` | `user`, `admin` | Returns `number + 1`. |

## Running Tests
```pwsh
cd JokesApi.Tests
 dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```
Required coverage: **≥ 90 %**.

## CI/CD
GitHub Actions workflow:
1. Build: `dotnet build`
2. Test: `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover`
3. Publish: `dotnet publish -c Release -o ./publish`
4. Docker: `docker build -t jokesapi .`

## Project Structure
```
JokesApi/
  Controllers/
  Entities/
  Data/
  Services/
  Notifications/
JokesApi.Tests/
  *.cs
```

## Security
* JWT authentication & authorization.
* Rate limiting (IP-based and endpoint-based).
* Secure password hashing.
* Cross-site scripting (XSS) protection.
* Cross-site request forgery (CSRF) protection.
* Input validation.
* Secure cookie settings.

## Further Improvements
* Refresh tokens & password reset flow.  
* Rate-limiting & IP lockout for brute-force protection.  
* Pagination & ordering for joke lists.  
* Docker Compose with PostgreSQL.  
* CI pipeline (GitHub Actions) to build, test & publish coverage.

---
© 2025 SquadMakers – All rights reserved. 