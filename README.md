# Atendia

Atendia es un SaaS multi-tenant para que las PYMEs creen, configuren, desplieguen y supervisen un agente de atención al cliente basado en IA.

## Estado actual del proyecto (2026-09-16)

El proyecto ya dejó atrás la etapa de "esqueleto puro" y está en una fase de MVP técnico operativo:

- La API backend en ASP.NET Core compila correctamente.
- La capa de infraestructura ya incluye EF Core y PostgreSQL/Npgsql.
- Las migraciones de datos, Identity y `BotPublicKey` fueron generadas con EF Core.
- La abstracción del proveedor LLM está implementada con `IChatModel` y `ExternalChatModel`.
- La lógica base de tenant, bot, chat y conocimiento ya fue definida en la capa de aplicación.
- La Knowledge Base ya tiene una implementación EF-backed con `EfKnowledgeService` y la creación de tenant/bot ya persiste con repositorios EF.
- El chat público ya puede autenticarse mediante la clave pública del bot y persistir conversaciones, mensajes y uso.
- RAG vectorial, autorización por recurso, auditoría, onboarding API, gestión de FAQ, leads y handoff mínimo ya están implementados.
- La prioridad actual es conectar el frontend administrativo, configurar PostgreSQL/pgvector en un entorno real, añadir notificaciones de handoff y validar un piloto.

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
- Once pruebas automatizadas pasando.
- Embeddings persistidos con pgvector y búsqueda semántica con filtro tenant/bot.
- Políticas `AdminOnly` y `KnowledgeRead`, además de validación tenant-scoped de bots.
- Auditoría persistente de autenticación y cambios sensibles.
- Configuración persistente del bot y CRUD de FAQ con regeneración de embeddings.
- Captura y consulta de leads por `BotKey`.
- Estado `HUMAN_HANDOFF`, revisión de conversaciones y bloqueo del modelo después del escalamiento.

### 🔄 En progreso

- Frontend administrativo para consumir configuración, FAQ, conversaciones y leads.
- Configuración de entorno para dev/prod y CORS restrictivo.
- Observabilidad y cálculo de costo real por modelo.
- Notificaciones para nuevos handoffs y revisión manual operativa.

### ⏳ Pendiente

- Modelo local en VPS.
- Dashboard de métricas y billing.
- Piloto real con PostgreSQL, proveedor de embeddings y proveedor LLM configurados.

## Roadmap principal

Consulta la documentación en:

- [docs/strategy.md](docs/strategy.md)
- [docs/implementation_guide.md](docs/implementation_guide.md)
- [saas_chatbot_roadmap.md](saas_chatbot_roadmap.md)

## Nota de ejecución

La compilación del backend fue verificada con `dotnet build` y las 11 pruebas automatizadas pasan. Las migraciones incluyen `KnowledgeEmbeddings`, `AuditEvents` y `LeadStatus`. La advertencia actual de OpenAPI y la vulnerabilidad reportada de Npgsql deben revisarse antes del piloto.
