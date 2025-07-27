# Guía de Ejecución y Pruebas Manuales

Este documento explica paso a paso cómo levantar la API y cómo ejecutar pruebas manuales usando Postman (o REST Client) para validar los distintos endpoints.

---

## 1. Requisitos Previos

| Herramienta | Versión recomendada |
|-------------|--------------------|
| .NET SDK    | 8.0 LTS            |
| PowerShell  | 7.x (Windows)      |
| Git         | 2.x                |
| Postman     | 10.x (o REST Client / Thunder Client) |
|

---

## 2. Clonar y Compilar el Proyecto

```powershell
# 1. Clonar el repositorio
 git clone https://github.com/<TU_USUARIO>/JokesApi.git
 cd JokesApi

# 2. Restaurar y compilar
 dotnet restore
 dotnet build

# 3. Aplicar migraciones y crear la BD SQLite
 dotnet ef database update -p JokesApi/JokesApi.csproj -s JokesApi/JokesApi.csproj

# 4. Ejecutar la API
 dotnet run -p JokesApi/JokesApi.csproj
# La API escuchará en https://localhost:7070
```

---

## 3. Variables de Entorno (appsettings.Development.json)

```jsonc
{
  "JwtSettings": {
    "Key": "<CLAVE_SUPER_SECRETA>"
  },
  "Authentication": {
    "Google": {
      "ClientId": "<GOOGLE_ID>",
      "ClientSecret": "<GOOGLE_SECRET>"
    },
    "GitHub": {
      "ClientId": "<GITHUB_ID>",
      "ClientSecret": "<GITHUB_SECRET>"
    }
  }
}
```

> Si no se configuran credenciales OAuth, los endpoints externos seguirán funcionando; simplemente omite el flujo externo.

---

## 4. Importar la Colección Postman

1. Abre Postman → **Import** → selecciona `JokesApi.postman_collection.json` ubicado en la raíz.
2. Reemplaza la variable `{{base_url}}` si no usas el puerto por defecto.

---

## 5. Flujo de Pruebas Manuales

### 5.1 Login y variables

1. Ejecuta la petición **Auth – Login** con credenciales `admin@example.com` / `Admin123!`.
2. El test de la colección almacenará automáticamente:
   * `jwt_token` – JWT para endpoints protegidos.
   * `refresh_token` – Token de refresco.

### 5.2 Endpoints de Usuario

* **Usuario – Info actual** → Devuelve los datos del usuario autenticado.
* **GET /api/admin** → Ejemplo de endpoint solo para rol `admin`.

### 5.3 Ciclo de Refresh Token

1. **Auth – Refresh** → Envía el `refresh_token` y recibe nuevos `token` + `refreshToken`.
2. **Auth – Revoke** → Invalida el token de refresco. Ejecuta de nuevo **Refresh** y debe responder _Invalid refresh token_.

### 5.4 Chistes

| Prueba | Descripción |
|--------|-------------|
| **Chistes – Aleatorio** | Obtener chiste Chuck/Dad/Local.
| **Chistes – Emparejados** | 5 Chuck + 5 Dad combinados.
| **Chistes – Crear local** | Crear chiste en BD como usuario autenticado.
| **Chistes – Filtrar** | Ejemplo de query params (minPalabras, contiene, etc.). |

### 5.5 Notificaciones

* **Notificaciones – Enviar email** → Envía mensaje simulado (se registra en logs).

### 5.6 Matemáticas (nueva ruta `/api/math`)

| Endpoint | Ejemplo |
|----------|---------|
| MCM | `GET /api/math/mcm?numbers=3,4,5` |
| Next Number | `GET /api/math/next-number?number=7` |

---

## 6. Consultar Swagger

Accede a `https://localhost:7070/swagger` para visualizar y probar los endpoints de forma interactiva.

---

¡Con esto podrás verificar manualmente todas las funcionalidades requeridas por el reto!. 