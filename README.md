# Ejercicios 1 y 2 – Autenticación, Autorización y Gestión de Chistes

> Versión en inglés disponible en [`README_en.md`](README_en.md)

## Tabla de Contenido
1. [Descripción general](#descripción-general)
2. [Arquitectura](#arquitectura)
3. [Requisitos](#requisitos)
4. [Puesta en marcha](#puesta-en-marcha)
5. [Configuración](#configuración)
6. [Referencia de la API REST](#referencia-de-la-api-rest)
   1. [Endpoints de Autenticación](#endpoints-de-autenticación)
   2. [Endpoints de Usuarios](#endpoints-de-usuarios)
   3. [Endpoints de Chistes](#endpoints-de-chistes)
   4. [Endpoints Matemáticos](#endpoints-matemáticos)
7. [Ejecución de Pruebas](#ejecución-de-pruebas)
8. [Estructura del Proyecto](#estructura-del-proyecto)
9. [Mejoras Futuras](#mejoras-futuras)

---

## Descripción general
Este proyecto entrega los **Ejercicios 1** (Autenticación y Autorización) y **2** (Gestión de Chistes).

* **Ejercicio 1 – Autenticación y Autorización**
  * Inicio de sesión local que emite JSON Web Tokens (JWT).
  * Inicio de sesión externo con OAuth 2.0 vía Google / GitHub (flujo authorization-code).
  * Control de acceso basado en roles (`user`, `admin`).

* **Ejercicio 2 – Gestión de Chistes**
  * Obtiene chistes de APIs externas (Chuck Norris y Dad Joke).
  * Persiste chistes locales con Entity Framework Core (SQLite por defecto).
  * Expone endpoints CRUD y filtros avanzados.
  * Ejecuta peticiones HTTP en paralelo con `HttpClientFactory` y `async/await`.

## Arquitectura
Clean Architecture con clara separación de responsabilidades.

```
┌──────────────────────────┐
│        Controllers       │  <-- Capa REST (ASP.NET Core MVC)
├──────────────────────────┤
│     Application Layer    │  <-- Servicios / Casos de uso
├──────────────────────────┤
│      Infrastructure      │  <-- EF Core, HttpClientFactory, JWT, OAuth
└──────────────────────────┘
```

* Inyección de dependencias configurada en `Program.cs`.
* Base de datos: SQLite por defecto (puede sustituirse vía `ConnectionStrings:Default`).

## Requisitos
* .NET 8.0 SDK (LTS)
* PowerShell 7 (Windows 11)
* Credenciales de apps OAuth (Google / GitHub) para login externo

## Puesta en marcha
```pwsh
# Restaurar y compilar
 dotnet restore
 dotnet build

# Aplicar migraciones EF Core (SQLite)
 dotnet ef database update --project JokesApi/JokesApi.csproj

# Ejecutar la API (http://localhost:5278 o https://localhost:7014)
 dotnet run --project JokesApi/JokesApi.csproj
```
Abre Swagger generado y explora los endpoints.

## Configuración
Fragmento relevante de `appsettings.json`:
```jsonc
{
  "JwtSettings": {
    "Issuer": "JokesApi",
    "Audience": "JokesApiClient",
    "Key": "<CLAVE_SUPER_SECRETA>",
    "ExpirationMinutes": 60
  }
}
```

## Referencia de la API REST
Todos los endpoints que modifican estado o exponen datos sensibles están **protegidos con JWT** salvo que se indique lo contrario.

### Endpoints de Autenticación
| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/api/auth/login` | Público | Login local; devuelve JWT. |
| GET  | `/api/auth/external/google-login` | Público | Redirección a OAuth Google. |
| GET  | `/api/auth/external/github-login` | Público | Redirección a OAuth GitHub. |
| GET  | `/api/auth/external/callback` | Público | Callback OAuth; genera JWT interno. |

### Endpoints de Usuarios
| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| POST | `/api/users/register` | Público | Registra usuario nuevo (`role="user"` por defecto; opcional `role="admin"`). |
| GET  | `/api/users` | `admin` | Lista completa de usuarios. |
| GET  | `/api/users/{id}` | `admin` | Devuelve usuario por ID. |
| PUT  | `/api/users/{id}` | `admin` | Actualiza nombre y/o rol. |
| DELETE | `/api/users/{id}` | `admin` | Elimina usuario. |
| GET | `/api/admin` | `admin` | Ping exclusivo de administradores. |

### Endpoints de Chistes
| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| GET | `/api/chistes/aleatorio` | `user`, `admin` | Chiste aleatorio de fuente externa o local. |
| GET | `/api/chistes/emparejados` | `user`, `admin` | 5 chistes Chuck + 5 Dad en paralelo, emparejados y combinados. |
| GET | `/api/chistes/combinado` | `user`, `admin` | Chiste compuesto a partir de varias fuentes. |
| POST| `/api/chistes` | `user`, `admin` | Crea chiste local (autor = usuario actual). |
| PUT | `/api/chistes/{id}` | autor o `admin` | Actualiza texto del chiste. |
| DELETE | `/api/chistes/{id}` | autor o `admin` | Elimina chiste. |

### Endpoints Matemáticos
| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| GET | `/api/matematicas/mcm` | `user`, `admin` | Devuelve mínimo común múltiplo. |
| GET | `/api/matematicas/siguiente-numero` | `user`, `admin` | Devuelve `number + 1`. |

## Ejecución de Pruebas
```pwsh
cd JokesApi.Tests
 dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```
Cobertura requerida: **≥ 90 %**.

## Estructura del Proyecto
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

## Mejoras Futuras
* Flow de refresh tokens y recuperación de contraseña.  
* Rate-limiting y bloqueo de IP ante fuerza bruta.  
* Paginación y ordenación en listas de chistes.  
* Docker Compose con PostgreSQL.  
* Pipeline CI (GitHub Actions) para compilar, testear y publicar cobertura.

---
© 2025 SquadMakers – Todos los derechos reservados. 