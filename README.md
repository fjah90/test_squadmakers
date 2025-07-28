# Ejercicios 1 y 2 – Autenticación, Autorización y Gestión de Chistes

> Versión en inglés disponible en [`README_en.md`](README_en.md)

## Tabla de Contenido
1. [Descripción general](#descripción-general)
2. [Arquitectura](#arquitectura)
3. [Requisitos](#requisitos)
4. [Puesta en marcha](#puesta-en-marcha)
   1. [Ejecución local](#ejecución-local)
   2. [Ejecución con Docker](#ejecución-con-docker)
5. [Configuración](#configuración)
6. [Referencia de la API REST](#referencia-de-la-api-rest)
   1. [Endpoints de Autenticación](#endpoints-de-autenticación)
   2. [Endpoints de Usuarios](#endpoints-de-usuarios)
   3. [Endpoints de Chistes](#endpoints-de-chistes)
   4. [Endpoints Matemáticos](#endpoints-matemáticos)
7. [Ejecución de Pruebas](#ejecución-de-pruebas)
8. [CI/CD](#cicd)
9. [Estructura del Proyecto](#estructura-del-proyecto)
10. [Seguridad](#seguridad)
11. [Mejoras Futuras](#mejoras-futuras)

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
Arquitectura Hexagonal (Ports and Adapters) con clara separación de responsabilidades.

```
┌──────────────────────────┐
│        Controllers       │  <-- Capa REST (ASP.NET Core MVC)
├──────────────────────────┤
│     Application Layer    │  <-- Casos de uso / Puertos
├──────────────────────────┤
│      Infrastructure      │  <-- Adaptadores: EF Core, HttpClientFactory, JWT, OAuth
└──────────────────────────┘
```

* Inyección de dependencias configurada en `Program.cs`.
* Base de datos: SQLite por defecto, PostgreSQL en Docker.

## Requisitos
* .NET 8.0 SDK (LTS)
* PowerShell 7 (Windows 11) o Bash (Linux/macOS)
* Docker y Docker Compose (opcional, para ejecución containerizada)
* Credenciales de apps OAuth (Google / GitHub) para login externo

## Puesta en marcha

### Ejecución local
```pwsh
# Restaurar y compilar
dotnet restore
dotnet build

# Aplicar migraciones EF Core (SQLite)
dotnet ef database update --project JokesApi/JokesApi.csproj

# Ejecutar la API (http://localhost:5278 o https://localhost:7014)
dotnet run --project JokesApi/JokesApi.csproj
```

### Ejecución con Docker
```bash
# Construir y levantar los contenedores
docker-compose up -d

# La API estará disponible en http://localhost:5000
```

El archivo `docker-compose.yml` configura:
- La API en un contenedor basado en .NET 8
- PostgreSQL como base de datos
- Variables de entorno para la conexión a la base de datos

## Configuración
Fragmento relevante de `appsettings.json`:
```jsonc
{
  "ConnectionStrings": {
    "Default": "Data Source=jokes.db" // SQLite por defecto
    // Para PostgreSQL: "Host=postgres;Database=jokesdb;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "Issuer": "JokesApi",
    "Audience": "JokesApiClient",
    "Key": "<CLAVE_SUPER_SECRETA>",
    "ExpirationMinutes": 60
  },
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "GeneralRules": [
      {
        "Endpoint": "*:/api/auth/login",
        "Period": "5m",
        "Limit": 10
      }
    ]
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
| POST | `/api/auth/refresh` | Público | Refresca un token expirado usando refresh token. |
| POST | `/api/auth/revoke` | Público | Revoca un refresh token. |

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
# Ejecutar pruebas con cobertura
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Ver informe de cobertura
dotnet tool install -g reportgenerator
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

## CI/CD
El proyecto utiliza GitHub Actions para integración continua:

- **Compilación automática** en cada push a `main` o pull request
- **Ejecución de pruebas** unitarias y de integración
- **Generación de informes de cobertura** de código
- **Publicación de badges** con el porcentaje de cobertura

El archivo de configuración se encuentra en `.github/workflows/ci.yml`.

## Estructura del Proyecto
```
JokesApi/
  Application/
    Ports/         # Interfaces para adaptadores de salida
    UseCases/      # Lógica de aplicación
  Controllers/     # Controladores REST
  Domain/
    Repositories/  # Interfaces de repositorios
  Entities/        # Modelos de dominio
  Data/            # Contexto EF Core y migraciones
  Infrastructure/  # Implementaciones de adaptadores
    External/      # Clientes HTTP
    Repositories/  # Repositorios EF Core
  Middleware/      # Middleware personalizado
  Services/        # Servicios de aplicación
  Settings/        # Clases de configuración
JokesApi.Tests/    # Pruebas unitarias y de integración
```

## Seguridad
El proyecto implementa varias capas de seguridad:

- **Autenticación JWT** con tokens de acceso y refresh
- **Rate limiting** para prevenir ataques de fuerza bruta
  - Límite global de 60 peticiones por minuto a la API
  - Límite específico de 10 intentos de login cada 5 minutos
- **Bloqueo temporal** después de 5 intentos fallidos de login en 15 minutos
- **HTTPS** en entornos de producción
- **Validación de datos** en todos los endpoints

## Mejoras Futuras
* Aumentar cobertura de pruebas al 90%
* Implementar validación de revocación en middleware
* Añadir paginación y ordenación en listas de chistes
* Implementar cache distribuida con Redis
* Añadir monitoreo con Application Insights o Prometheus

---
© 2025 SquadMakers – Todos los derechos reservados. 