# Atendia

Atendia es un SaaS multi-tenant para que las PYMEs creen, configuren, desplieguen y supervisen un agente de atención al cliente basado en IA.

## Estado actual del proyecto (2026-09-14)

El proyecto ya dejó atrás la etapa de "esqueleto puro" y está en una fase de MVP técnico operativo:

- La API backend en ASP.NET Core compila correctamente.
- La capa de infraestructura ya incluye EF Core y PostgreSQL/Npgsql.
- Las migraciones de datos, Identity y `BotPublicKey` fueron generadas con EF Core.
- La abstracción del proveedor LLM está implementada con `IChatModel` y `ExternalChatModel`.
- La lógica base de tenant, bot, chat y conocimiento ya fue definida en la capa de aplicación.
- La Knowledge Base ya tiene una implementación EF-backed con `EfKnowledgeService` y la creación de tenant/bot ya persiste con repositorios EF.
- El chat público ya puede autenticarse mediante la clave pública del bot y persistir conversaciones, mensajes y uso.
- La prioridad actual es completar RAG vectorial, onboarding administrativo, handoff, leads y validación con piloto.

## Estructura del repositorio

- `backend/` - API y lógica de negocio.
- `backend/src/` - código fuente de la aplicación backend.
- `backend/tests/` - pruebas del backend.
- `frontend/` - aplicación administrativa y dashboard.
- `infra/` - infraestructura, Docker, configuración de despliegue y entorno.
- `docs/` - documentación del proyecto, estrategia y guía de implementación.
- `scripts/` - scripts de automatización, despliegue y utilidades.

## Stack actual

- Backend: ASP.NET Core
- Frontend administrativo: pendiente; widget actual en JavaScript autocontenido
- Database: PostgreSQL + pgvector
- ORM: EF Core + Npgsql
- AI abstraction: IChatModel
- LLM provider inicial: proveedor externo
- Widget: JavaScript con Shadow DOM

## Estado funcional por capa

### ✅ Completado

- Esqueleto de la solución y proyectos del backend.
- Modelos principales de dominio: tenant, bot, knowledge, conversations, usage.
- Servicio de chat con abstracción sobre proveedor externo.
- Configuración inicial de acceso a PostgreSQL.
- Generación de migración inicial de EF Core.
- Validación de compilación del backend.
- Persistencia EF de knowledge, conversaciones, mensajes y usage.
- Registro/login con usuarios asociados a tenant.
- Widget JavaScript instalable mediante `BotKey`.
- Cinco pruebas automatizadas pasando.

### 🔄 En progreso

- Validación completa de pertenencia de bot y autorización por recurso.
- Configuración de entorno para dev/prod y CORS restrictivo.
- Observabilidad y cálculo de costo real por modelo.
- RAG vectorial con embeddings y pgvector.

### ⏳ Pendiente

- Modelo local en VPS.
- Handoff a humano operativo y captura de leads.
- Dashboard de métricas y billing.
- Frontend admin completo y onboarding sin intervención técnica.

## Roadmap principal

Consulta la documentación en:

- [docs/strategy.md](docs/strategy.md)
- [docs/implementation_guide.md](docs/implementation_guide.md)
- [saas_chatbot_roadmap.md](saas_chatbot_roadmap.md)

## Nota de ejecución

La compilación del backend fue verificada con `dotnet build` y la migración inicial fue creada con EF Core. La advertencia actual de OpenAPI no bloquea el arranque, pero debe revisarse en una siguiente iteración por seguridad y mantenimiento.
