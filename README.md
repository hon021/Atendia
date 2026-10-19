# Atendia

Atendia es un SaaS multi-tenant para que las PYMEs creen, configuren, desplieguen y supervisen un agente de atención al cliente basado en IA.

## Estado actual del proyecto (2026-10-19)

Atendia tiene el núcleo técnico del MVP validado contra PostgreSQL real y una primera prueba de RAG con modelos locales. Todavía no está listo para un piloto autónomo con clientes.

- API ASP.NET Core sobre .NET 10 compilando y 17 pruebas automatizadas pasando.
- Entorno local con PostgreSQL 18, pgvector 0.8.7 y las siete migraciones aplicadas.
- Flujo HTTP comprobado: registro/login/logout, sesión, bots, configuración, CRUD de FAQ, chat, historial, leads, handoff, resolución y reapertura.
- Aislamiento comprobado entre dos tenants en configuración, FAQ, mensajes y leads.
- Ollama instalado y probado: chat con `qwen2.5:3b` y embeddings con `qwen3-embedding:4b`, usando 1536 dimensiones sin modificar el esquema.
- Una respuesta RAG recuperó un dato exclusivo de una FAQ persistida en PostgreSQL. Esta comprobación puntual no sustituye una evaluación de calidad.
- Admin funcional en desarrollo para seleccionar bots, gestionar FAQ, revisar mensajes y escalar o resolver conversaciones. La prueba visual completa de admin y widget sigue pendiente.
- Auditoría NuGet de la API sin vulnerabilidades reportadas por las fuentes consultadas; el conflicto de versiones de Pgvector y la alerta de OpenAPI fueron corregidos.

La prioridad inmediata es probar el flujo en navegador, evaluar respuestas y recuperación con preguntas reales, completar el handoff humano operativo y preparar un piloto acotado.

## Estructura del repositorio

- `backend/` - API y lógica de negocio.
- `backend/src/` - código fuente de la aplicación backend.
- `backend/tests/` - pruebas del backend.
- `frontend/` - prototipo administrativo, widget y página de demostración.
- `infra/` - carpeta reservada; todavía sin infraestructura de despliegue.
- `docs/` - documentación del proyecto, estrategia y guía de implementación.
- `scripts/` - configuración de la conexión local mediante secretos de .NET.

## Desarrollo local con PostgreSQL

Requisitos: SDK .NET 10, herramientas `dotnet ef`, PostgreSQL con pgvector instalado y una base `atendia` accesible. Desde PowerShell, en la raíz del repositorio:

```powershell
.\scripts\Configure-LocalDatabase.ps1
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project backend/src/Atendia.Api
dotnet run --project backend/src/Atendia.Api --launch-profile https
```

El script solicita la password de PostgreSQL con entrada oculta y guarda la conexion en secretos locales de .NET, fuera del repositorio. Estos secretos no estan cifrados y solo deben usarse en desarrollo. Reinicia la API despues de modificar su configuracion local.

Si PowerShell bloquea scripts, permite su ejecucion solo durante esa terminal con `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`. Si el navegador rechaza el certificado local, ejecuta `dotnet dev-certs https --trust` desde tu terminal y acepta el dialogo de confianza. No se necesita cambiar la politica de ejecucion global.

Abre `https://localhost:7028/admin.html`. En Development, la API sirve el admin y el widget desde el mismo origen. Para servir el admin por separado, configura la meta etiqueta `atendia-api-url` con el origen de la API.

Con proveedores externos, sin `ChatProvider:ApiKey` el chat devuelve un aviso de proveedor no configurado y no genera embeddings. Configura la clave del proveedor mediante secretos locales, nunca en archivos versionados. El perfil local de Ollama no requiere esa clave.

### IA local con Ollama

Instala Ollama para Windows desde su sitio oficial o con `winget install --id Ollama.Ollama --exact --scope user`. Luego descarga los modelos:

```powershell
ollama pull qwen2.5:3b
ollama pull qwen3-embedding:4b
dotnet run --project backend/src/Atendia.Api --launch-profile ollama
```

Ollama debe estar activo en `http://localhost:11434`. El perfil `ollama` usa su API compatible con OpenAI, sin enviar una API key: chat con Qwen2.5 3B y embeddings con Qwen3 Embedding 4B, solicitando 1536 dimensiones para conservar el esquema existente. Los modelos se ejecutan localmente, sin cobro por llamada; consumen RAM, CPU/GPU, disco y energia. Las descargas iniciales requieren internet.

Abre el mismo admin en `https://localhost:7028/admin.html`. Las FAQ creadas antes de activar embeddings deben volver a guardarse desde el admin para generarlos. Si cambias de modelo de embeddings, regenera todos los vectores de la base afectada, aunque tengan la misma dimension; no mezcles espacios vectoriales de modelos diferentes.

El perfil `https` conserva la configuracion del proveedor externo. No ejecutes ambos perfiles a la vez, porque usan los mismos puertos. La confianza del certificado local sigue siendo necesaria en el navegador.

Validado localmente con 31,5 GB de RAM y gráfica Intel integrada: embedding de 1536 dimensiones y respuesta RAG con un dato exclusivo de una FAQ persistida en PostgreSQL. La consulta RAG de prueba tardó unos 5 segundos; es una medida puntual, no una garantía de latencia ni de calidad. Qwen3 4B también se descargó durante la evaluación, pero no es el modelo de chat seleccionado.

### Verificación local (2026-10-19)

- PostgreSQL 18 y pgvector 0.8.7 instalados; las siete migraciones fueron aplicadas.
- Flujo HTTP validado contra PostgreSQL: registro/login/logout, sesion, bot/configuracion, CRUD de FAQ, chat, historial, leads, handoff, resolucion y reapertura.
- FAQ persistidas disponibles tras reiniciar la API; se corrigio el listado administrativo que devolvia vacio al buscar sin texto.
- Un segundo tenant no pudo consultar configuracion, FAQ, mensajes ni leads del primero (HTTP 404).
- Suite automatizada: 17 pruebas pasando, incluidas compatibilidad de Ollama sin API key, dimensiones de embeddings y regresión del proveedor externo sin credenciales. Estas pruebas son unitarias; las verificaciones HTTP contra PostgreSQL y Ollama se ejecutaron por separado.
- Embedding e historial de la prueba Ollama persistidos; el estado `HUMAN_HANDOFF` evitó nuevas llamadas al modelo.
- Se conservaron tenants identificados como pruebas locales y de Ollama; las cookies temporales fueron eliminadas. No se guardaron credenciales en el repositorio.
- Pendiente: evaluación amplia de RAG, prueba con proveedor externo configurado y prueba visual del flujo completo. El certificado HTTPS local sigue requiriendo confianza en el navegador.

Para ejecutar las comprobaciones automatizadas:

```powershell
dotnet build backend/Atendia.slnx
dotnet test backend/tests/Atendia.Tests/Atendia.Tests.csproj
dotnet list backend/src/Atendia.Api/Atendia.Api.csproj package --vulnerable --include-transitive
```

## Stack actual

- Backend: ASP.NET Core sobre .NET 10
- Frontend administrativo: HTML/JavaScript, prototipo funcional servido por la API solo en Development
- Database: PostgreSQL + pgvector
- ORM: EF Core + Npgsql
- AI abstraction: IChatModel
- IA local validada: Ollama, Qwen2.5 3B y Qwen3 Embedding 4B
- Proveedor externo integrado: OpenRouter, pendiente de prueba con credenciales reales
- Widget: JavaScript con Shadow DOM

## Estado funcional por capa

### Completado

- Esqueleto de la solución y proyectos del backend.
- Modelos principales de dominio: tenant, bot, knowledge, conversations, usage.
- Servicio de chat con abstracción de proveedor y compatibilidad con Ollama local y API externa.
- Configuración de PostgreSQL en secretos locales de desarrollo, fuera del repositorio.
- Siete migraciones de EF Core aplicadas al entorno local.
- Validación de compilación del backend.
- Persistencia EF de knowledge, conversaciones, mensajes y usage.
- Registro/login con usuarios asociados a tenant.
- Widget JavaScript instalable mediante `BotKey`.
- Diecisiete pruebas automatizadas pasando.
- Embeddings persistidos con pgvector y búsqueda semántica con filtro tenant/bot.
- Políticas `AdminOnly` y `KnowledgeRead`, además de validación tenant-scoped de bots.
- Auditoría persistente de autenticación y cambios sensibles.
- Configuración persistente del bot y CRUD de FAQ con regeneración de embeddings.
- Captura y consulta de leads por `BotKey`.
- Estado `HUMAN_HANDOFF`, revisión de conversaciones y bloqueo del modelo después del escalamiento.
- Resolución de conversaciones y reapertura en `AI_HANDLING` al recibir un nuevo mensaje.
- Recuperación de sesión, selección de bot, edición/eliminación de FAQ y consulta de mensajes desde el admin.
- Prueba RAG local con embeddings de 1536 dimensiones y recuperación de un dato exclusivo de la FAQ.

### Validación y operación pendientes

- Prueba visual end-to-end de admin y widget, incluida confianza del certificado HTTPS local y sesión en navegador.
- Evaluación de respuestas, recuperación semántica, preguntas desconocidas, guardrails y resistencia a prompt injection.
- Regeneración de embeddings de las FAQ antiguas que se crearon sin proveedor configurado.
- Notificación de handoff y respuesta humana al cliente; cambiar el estado todavía no constituye un canal de atención humano completo.
- Costos reales por modelo, límites de consumo y observabilidad mínima.
- Configuración de producción, despliegue reproducible y piloto con un cliente real.

### Fuera del núcleo actual

- Modelo local en VPS; la prueba actual se ejecuta en el equipo de desarrollo.
- Dashboard de métricas y billing.
- Integraciones con WhatsApp y CRM.

## Roadmap principal

Consulta la documentación en:

- [docs/strategy.md](docs/strategy.md)
- [docs/implementation_guide.md](docs/implementation_guide.md)
- [saas_chatbot_roadmap.md](saas_chatbot_roadmap.md)

## Nota de ejecución

La compilación de la API, las 17 pruebas y el recorrido HTTP con PostgreSQL/Ollama fueron verificados localmente. El perfil `ollama` permite continuar las pruebas sin pago por llamada ni API key, pero no implica calidad o capacidad productiva garantizadas. La aplicación requiere que PostgreSQL y Ollama estén activos. Los documentos de roadmap conservan planificación histórica; este README resume el estado comprobado al 2026-10-19.
