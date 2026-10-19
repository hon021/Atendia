# Atendia

Atendia es un SaaS multi-tenant para que las PYMEs creen, configuren, desplieguen y supervisen un agente de atención al cliente basado en IA.

## Estado actual del proyecto (2026-09-24)

El proyecto ya dejó atrás la etapa de "esqueleto puro" y está en una fase de MVP técnico operativo con base funcional sólida:

- La API backend en ASP.NET Core compila y su suite de pruebas pasa con éxito.
- La capa de infraestructura ya incluye EF Core, PostgreSQL/Npgsql y soporte para pgvector.
- Las migraciones de datos, Identity y `BotPublicKey` fueron generadas con EF Core.
- La abstracción del proveedor LLM está implementada con `IChatModel` y `ExternalChatModel`.
- La lógica base de tenant, bot, chat y conocimiento ya fue definida en la capa de aplicación.
- La Knowledge Base tiene una implementación EF-backed con `EfKnowledgeService` y la creación de tenant/bot persiste con repositorios EF.
- El chat público ya puede autenticarse mediante la clave pública del bot y persistir conversaciones, mensajes y uso.
- RAG vectorial, autorización por recurso, auditoría, onboarding API, gestión de FAQ, leads y handoff mínimo ya están implementados.
- Existe un prototipo inicial de frontend administrativo para registro, login, creación de bot y FAQ, además de una colección de pruebas HTTP para validar el flujo del MVP.
- La prioridad actual es cerrar la operatividad del admin, validar PostgreSQL real, resolver la vulnerabilidad de `Npgsql` y ejecutar un piloto con un cliente real.

## Estructura del repositorio

- `backend/` - API y lógica de negocio.
- `backend/src/` - código fuente de la aplicación backend.
- `backend/tests/` - pruebas del backend.
- `frontend/` - aplicación administrativa y dashboard.
- `infra/` - infraestructura, Docker, configuración de despliegue y entorno.
- `docs/` - documentación del proyecto, estrategia y guía de implementación.
- `scripts/` - scripts de automatización, despliegue y utilidades.

## Desarrollo local con PostgreSQL

Con PostgreSQL y pgvector instalados y las migraciones aplicadas, ejecuta desde la raiz del repositorio:

```powershell
.\scripts\Configure-LocalDatabase.ps1
dotnet run --project backend/src/Atendia.Api --launch-profile https
```

El script solicita la password de PostgreSQL con entrada oculta y guarda la conexion en secretos locales de .NET, fuera del repositorio. Estos secretos no estan cifrados y solo deben usarse en desarrollo. Reinicia la API despues de modificar su configuracion local.

Si PowerShell bloquea scripts, permite su ejecucion solo durante esa terminal con `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`. Si el navegador rechaza el certificado local, ejecuta `dotnet dev-certs https --trust` desde tu terminal y acepta el dialogo de confianza. No se necesita cambiar la politica de ejecucion global.

Abre `https://localhost:7028/admin.html`. En Development, la API sirve el admin y el widget desde el mismo origen. Para servir el admin por separado, configura la meta etiqueta `atendia-api-url` con el origen de la API.

Sin `ChatProvider:ApiKey`, el chat devuelve un aviso de proveedor no configurado y no genera embeddings; permite comprobar persistencia, pero no valida IA ni busqueda vectorial real. Configura la clave del proveedor mediante secretos locales, nunca en archivos versionados.

### Verificacion local (2026-10-19)

- PostgreSQL 18 y pgvector 0.8.7 instalados; las siete migraciones fueron aplicadas.
- Flujo HTTP validado contra PostgreSQL: registro/login/logout, sesion, bot/configuracion, CRUD de FAQ, chat, historial, leads, handoff, resolucion y reapertura.
- FAQ persistidas disponibles tras reiniciar la API; se corrigio el listado administrativo que devolvia vacio al buscar sin texto.
- Un segundo tenant no pudo consultar configuracion, FAQ, mensajes ni leads del primero (HTTP 404).
- Suite automatizada: 13 pruebas pasando. Se conservaron dos tenants identificados como pruebas locales; las cookies temporales fueron eliminadas.
- Pendiente: respuestas y embeddings con proveedor real, evaluacion de RAG y prueba visual del flujo completo. El certificado HTTPS local sigue requiriendo confianza en el navegador.

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

- Frontend administrativo para consumir configuración, FAQ, conversaciones y leads (prototipo funcional en curso).
- Configuración de entorno para dev/prod, CORS y cookies de sesión en navegador.
- Observabilidad y cálculo de costo real por modelo.
- Notificaciones para nuevos handoffs y revisión manual operativa.
- Validación del flujo end-to-end con PostgreSQL real y pgvector.

### ⏳ Pendiente

- Modelo local en VPS.
- Dashboard de métricas y billing.
- Piloto real con PostgreSQL, proveedor de embeddings y proveedor LLM configurados.
- Resolver la vulnerabilidad reportada por NuGet en `Npgsql` antes del piloto comercial.

## Roadmap principal

Consulta la documentación en:

- [docs/strategy.md](docs/strategy.md)
- [docs/implementation_guide.md](docs/implementation_guide.md)
- [saas_chatbot_roadmap.md](saas_chatbot_roadmap.md)

## Nota de ejecución

La compilación del backend fue verificada con `dotnet test` y las 11 pruebas automatizadas pasan. Las migraciones incluyen `KnowledgeEmbeddings`, `AuditEvents` y `LeadStatus`. El proyecto ya tiene un prototipo de admin y un conjunto de pruebas HTTP para validar el flujo del MVP. La vulnerabilidad reportada por NuGet en `Npgsql` y la validación con PostgreSQL real siguen siendo los puntos críticos antes del piloto.
