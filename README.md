# Atendia

Atendia es un SaaS multi-tenant para que las PYMEs creen, configuren, desplieguen y supervisen un agente de atención al cliente basado en IA.

## Estado actual del proyecto (2026-10-20)

Atendia tiene el núcleo técnico del MVP validado contra PostgreSQL real y una primera prueba de RAG con modelos locales. Todavía no está listo para un piloto autónomo con clientes.

- API ASP.NET Core sobre .NET 10 compilando y 21 pruebas automatizadas pasando.
- Entorno local con PostgreSQL 18, pgvector 0.8.7 y las siete migraciones aplicadas.
- Flujo HTTP comprobado: registro/login/logout, sesión, bots, configuración, CRUD de FAQ, chat, historial, leads, handoff, resolución y reapertura.
- Aislamiento comprobado entre dos tenants en configuración, FAQ, mensajes y leads.
- Ollama instalado y probado: chat con `qwen2.5:3b` y embeddings con `qwen3-embedding:4b`, usando 1536 dimensiones sin modificar el esquema.
- Una respuesta RAG recuperó un dato exclusivo de una FAQ persistida en PostgreSQL. Esta comprobación puntual no sustituye una evaluación de calidad.
- Admin y widget funcionales en desarrollo para configurar bots, gestionar FAQ, revisar conversaciones, escalar y responder como agente; el flujo visual se validó localmente.
- Auditoría NuGet de la API sin vulnerabilidades reportadas por las fuentes consultadas; el conflicto de versiones de Pgvector y la alerta de OpenAPI fueron corregidos.

La prueba visual del admin/widget ya se ejecutó. El flujo incluye handoff automático cuando no se recupera conocimiento, respuesta de agente desde el admin y consulta de respuestas en el widget. La política del proveedor separa pregunta/fuentes de las instrucciones internas y solicita una marca de handoff si no puede fundamentar la respuesta. Estas medidas reducen riesgos, pero no sustituyen una evaluación con un proveedor/modelo real y preguntas del negocio piloto.

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

Con proveedores externos, sin `ChatProvider:ApiKey` el chat responde HTTP 503 con un mensaje controlado de proveedor no configurado; no se envían solicitudes externas. Sin clave, la generación de embeddings se omite y la búsqueda queda limitada al texto. Configura la clave mediante secretos locales, nunca en archivos versionados. El perfil local de Ollama no requiere esa clave.

### Alertas de handoff por email

El destinatario es el email de contacto configurado en cada bot. El envío usa SMTP; configura host, remitente y credenciales en secretos locales, no en archivos versionados. Ejemplo:

```powershell
dotnet user-secrets set "HandoffEmail:Host" "smtp.example.com" --project backend\src\Atendia.Api
dotnet user-secrets set "HandoffEmail:Port" "587" --project backend\src\Atendia.Api
dotnet user-secrets set "HandoffEmail:FromAddress" "atendia@example.com" --project backend\src\Atendia.Api
dotnet user-secrets set "HandoffEmail:UserName" "usuario-smtp" --project backend\src\Atendia.Api
dotnet user-secrets set "HandoffEmail:Password" "SECRETO_SMTP" --project backend\src\Atendia.Api
dotnet user-secrets set "HandoffEmail:UseStartTls" "true" --project backend\src\Atendia.Api
```

Si el SMTP o el email de contacto no están configurados, el handoff sigue disponible en el admin y se registra el motivo; el chat no falla. La entrega por email debe validarse con el SMTP real antes del piloto.

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

### Cambios y validación (2026-10-20)

- El flujo de admin y widget se recorrió en navegador: registro, bot, configuración, FAQ, respuesta respaldada, historial, handoff y resolución.
- Una consulta sin respuesta fundamentada pasó a `HUMAN_HANDOFF`; el bot dejó de responder con información inventada.
- El admin actualiza la bandeja y el contador de handoffs cada 10 segundos. Un agente autorizado puede responder; el widget consulta esas respuestas cada 5 segundos mientras el caso está escalado.
- Pregunta del usuario y fuentes recuperadas se envían como datos no confiables, separadas de las instrucciones del sistema. La respuesta de handoff del modelo se sustituye por un mensaje seguro.
- Los fallos de chat/embeddings del proveedor se traducen a HTTP 503 con mensaje controlado y registro del servidor. `/health` indica liveness y `/health/ready` comprueba la base de datos.
- Suite automatizada: 21 pruebas pasando, incluidas abstención/handoff, separación de instrucciones y fuente, y errores de proveedor.
- La prueba inicial de prompt injection del 2026-10-20, con la política anterior, mostró que el modelo describía parte de sus instrucciones. Tras separar las fuentes no confiables y reforzar la política, una repetición local pidió copiar el prompt del sistema y el modelo respondió solo con la FAQ, sin revelar instrucciones. Es una comprobación puntual; hace falta una evaluación más amplia antes del piloto.
- La ejecución completa del piloto y la calibración de recuperación requieren seleccionar una empresa, su contenido aprobado y preguntas reales. No se hizo despliegue de producción.

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
- Veintiuna pruebas automatizadas pasando.
- Embeddings persistidos con pgvector y búsqueda semántica con filtro tenant/bot.
- Políticas `AdminOnly` y `KnowledgeRead`, además de validación tenant-scoped de bots.
- Auditoría persistente de autenticación y cambios sensibles.
- Configuración persistente del bot y CRUD de FAQ con regeneración de embeddings.
- Captura y consulta de leads por `BotKey`.
- Estado `HUMAN_HANDOFF`, revisión de conversaciones y bloqueo del modelo después del escalamiento.
- Resolución de conversaciones y reapertura en `AI_HANDLING` al recibir un nuevo mensaje.
- Recuperación de sesión, selección de bot, edición/eliminación de FAQ y consulta de mensajes desde el admin.
- Prueba RAG local con embeddings de 1536 dimensiones y recuperación de un dato exclusivo de la FAQ.
- Handoff automático cuando no se recupera conocimiento, bandeja con actualización periódica y respuesta humana transmitida al widget.
- Política de prompt con delimitación de fuentes no confiables y derivación explícita si el modelo no puede fundamentar la respuesta.
- Respuesta controlada ante fallos del proveedor de IA y health checks separados de liveness/readiness.

### Validación y operación pendientes

- Evaluación amplia y repetible con preguntas aprobadas de una empresa piloto; incluir falsos positivos/negativos de handoff y pruebas de prompt injection con el proveedor seleccionado.
- Regeneración de embeddings de las FAQ antiguas que se crearon sin proveedor configurado.
- Configurar y comprobar la entrega SMTP real de las alertas de handoff; el código está integrado, pero el envío no se probó con credenciales reales.
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

La compilación de la API y las 21 pruebas fueron verificadas localmente. El flujo visual y RAG con Ollama se probaron localmente; no se probaron las nuevas respuestas del proveedor externo con credenciales reales. El perfil `ollama` permite continuar las pruebas sin pago por llamada ni API key, pero no implica calidad o capacidad productiva garantizadas. La aplicación requiere PostgreSQL y, para generar respuestas/embeddings, el proveedor de IA seleccionado. Los documentos de roadmap conservan planificación histórica; este README resume el estado comprobado al 2026-10-20.
