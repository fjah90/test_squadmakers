# Tareas Pendientes para Cumplir 100 %

> Este archivo resume los puntos que aún faltan o están parciales respecto a los requisitos de **prueba.md**.

## 1. Arquitectura Hexagonal estricta
- [x] Extraer casos de uso a capa `Application` / `UseCases`.
- [x] Definir puertos de salida:
  - `IChuckClient`, `IDadClient` (consumo APIs externas)
  - `INotifier` ya existe (OK)
- [x] Implementar adaptadores de salida en `Infrastructure`.
- [x] Mover lógica de combinación de chistes fuera de los controladores.

## 2. Cobertura de Pruebas ≥ 90 %
- [x] Añadir tests de integración para:
  - Endpoints de **Usuarios** (`GET/PUT/DELETE`)
  - Endpoints de **Chistes** (`Create`, `Filter`, `Delete`, `Combined` happy-path)
- [x] Ejecutar `dotnet test` con coverlet y verificar porcentaje.
  - Cobertura actual: 42.07% (se necesita mejorar para llegar al 90%)

## 3. Refresh Tokens completo
- [ ] Endpoint `POST /api/auth/refresh` (ya existe) → agregar pruebas.
- [ ] Endpoint `POST /api/auth/revoke` (ya existe) → agregar pruebas.
- [ ] Validar revocación en middleware (opcional).

## 4. Docker y PostgreSQL
- [ ] Crear `docker-compose.yml` con:
  - API
  - PostgreSQL
- [ ] Ajustar `ConnectionStrings` vía variables de entorno.

## 5. CI / CD
- [ ] Workflow GitHub Actions que:
  1. Restaure, compile y ejecute tests.
  2. Publique reporte de cobertura en badge.

## 6. Seguridad avanzada
- [ ] Implementar rate-limiting por IP.
- [ ] Bloqueo temporal ante múltiples intentos fallidos de login.

## 7. Documentación
- [ ] Añadir ejemplos `200`, `400`, `401`, `403`, `404` en Swagger para todos los endpoints.
- [ ] Completar README con sección Docker y CI.

---
**Prioridad sugerida:** Arquitectura Hexagonal → Tests → CI/Coverage → Docker. 