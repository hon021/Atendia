# Atendia — Hoja de Ruta del Proyecto SaaS

## Nombre del proyecto

**Atendia**

**Propuesta de posicionamiento:** AI Customer Service for Small Business

Atendia es un SaaS multi-tenant para que las PYMEs creen, configuren, desplieguen y supervisen un agente de atención al cliente basado en IA.

---

## Estado actual del roadmap (2026-09-14)

La ruta de desarrollo ya no está en una etapa únicamente teórica: el backend de la base del producto ya fue levantado, validado y ampliado con persistencia, autenticación básica, credencial pública por bot y un widget web inicial.

### Estado por bloque

| Fase | Estado | Detalle |
| --- | --- | --- |
| Infraestructura base | Completado | Solución backend creada, ASP.NET Core funcionando, EF Core + PostgreSQL configurado. |
| LLM abstraction | Completado | `IChatModel` + `ExternalChatModel` implementados. |
| Migración inicial | Completado | Esquema inicial generado con EF Core. |
| Persistencia de knowledge | Completado | `EfKnowledgeService` y repositorios EF para tenant/bot. |
| Persistencia de conversaciones / uso | Completado | Conversaciones, mensajes y usage se persisten mediante repositorios EF y migraciones. Leads siguen pendientes. |
| Multi-tenancy y seguridad | En progreso | Login, cookie, tenant claims y BotKey están implementados; falta autorización por recurso, roles operativos y auditoría. |
| Observabilidad y costos | En progreso | Se registran tokens, modelo y latencia; falta tarifario por modelo y métricas operativas. |
| Widget + onboarding | En progreso | Widget JavaScript y demo disponibles; falta panel administrativo y onboarding sin intervención técnica. |
| Handoff humano | Pendiente | Flujo de escalamiento aún no está operativo. |
| Modelo local en VPS | Pendiente | Se evaluará solo cuando el volumen y el costo lo justifiquen. |

### Completado

- arquitectura base del backend,
- dominio y services clave,
- abstracción LLM,
- configuración de PostgreSQL + EF Core,
- migración inicial del esquema,
- endpoints para chat / tenant / bot / knowledge,
- repositorios EF para tenant, bot y knowledge,
- validación de compilación y pruebas del backend.

### En progreso

- RAG vectorial con embeddings y búsqueda filtrada por tenant,
- validación estricta de pertenencia de bot y autorización por recurso,
- observabilidad y métricas por conversación,
- leads y handoff humano,
- despliegue y validación con cliente piloto.

### Pendiente

- modelo local en VPS,
- dashboard de administración,
- handoff humano operativo,
- billing y monetización,
- frontend productivo y seguimiento de métricas de negocio,
- pruebas end-to-end con cliente piloto.

---

# Hoja de Ruta del Proyecto: SaaS Chatbot de Atención para PYMEs

## 1. Objetivo del producto

Construir un SaaS multi-tenant que permita a las PYMEs crear, configurar, desplegar y supervisar un agente de atención al cliente basado en IA.

El producto debe permitir:

**Crear cuenta → configurar empresa → agregar conocimiento → probar bot → publicar → atender conversaciones → capturar leads → derivar a humano → medir resultados.**

El objetivo inicial no es entrenar modelos propios, sino combinar:

- Configuración del negocio.
- Base de conocimiento.
- RAG.
- Modelos LLM externos o locales.
- Reglas de comportamiento y seguridad.
- Herramientas de atención y seguimiento.

## Ajuste de alcance recomendado para el MVP

Para aumentar la probabilidad de éxito, el proyecto debe comenzar con una vertical concreta y un caso de uso bastante definido, en lugar de intentar cubrir todas las PYMEs desde el inicio.

### Recomendación

- Elegir 1 vertical inicial: restaurantes, e-commerce o servicios profesionales.
- Definir 1 caso de uso principal: atención a clientes, reservas, pedidos, dudas sobre envíos, leads, etc.
- Limitar la configuración inicial a una FAQ mínima + perfil del negocio + reglas básicas de handoff.
- Medir éxito con KPIs operativos y de negocio desde la primera semana.

### KPIs del MVP

- Tasa de resolución sin humano.
- Tasa de escalamiento a humano.
- Costo por conversación.
- Tiempo promedio de respuesta.
- Leads generados.
- Satisfacción del cliente / feedback del usuario.

Este enfoque reduce la complejidad del lanzamiento y permite aprender más rápido con clientes reales antes de ampliar funcionalidad.

---

# 2. Principios de arquitectura

## 2.1 Multi-tenancy desde el inicio

Cada organización será un tenant independiente.

Todas las entidades relacionadas con una empresa deberán estar asociadas a `tenant_id`.

La aplicación debe impedir que un usuario, consulta o proceso pueda acceder accidentalmente a información de otro tenant.

Se recomienda evaluar PostgreSQL Row Level Security (RLS) como segunda capa de protección.

## 2.2 Separación entre comportamiento, configuración y conocimiento

No se debe colocar toda la información del cliente dentro del System Prompt.

Se dividirá en:

### System Prompt

Define comportamiento y reglas:

- Rol del asistente.
- Tono.
- Restricciones.
- Qué puede y no puede hacer.
- Cuándo debe escalar a un humano.
- Política frente a información desconocida.

### Business Configuration

Datos estructurados:

- Nombre.
- Sector.
- Descripción.
- Horarios.
- Ubicación.
- Contacto.
- Canales.
- Tono.
- Configuración de handoff.

### Knowledge Base

Información recuperable mediante RAG:

- FAQ.
- Documentos.
- Precios.
- Políticas.
- Catálogos.
- URLs procesadas.
- Información específica del negocio.

La generación final seguirá conceptualmente:

`System Prompt + Business Configuration + Retrieved Knowledge + Conversation History → LLM`

## 2.3 Abstracción del proveedor LLM

La aplicación no debe quedar acoplada a un proveedor específico.

Se recomienda una interfaz equivalente a:

```csharp
public interface IChatModel
{
    Task<ChatResponse> GenerateAsync(
        ChatRequest request,
        CancellationToken cancellationToken);
}
```

Implementaciones posibles:

- OpenRouter.
- Groq.
- Together AI.
- Proveedor directo.
- vLLM/Ollama para modelos locales.

Inicialmente se utilizará **un solo proveedor**, manteniendo la abstracción para poder cambiarlo posteriormente.

## 2.4 Simplicidad antes que escala prematura

El MVP debe comenzar con:

- ASP.NET Core.
- Angular.
- PostgreSQL.
- pgvector.
- Un proveedor LLM.
- Web Widget.

No se incorporarán Redis, Qdrant, colas, microservicios u otros componentes hasta que exista una necesidad demostrable.

---

# 3. Fase 1 — MVP Core e Infraestructura Base

**Duración objetivo: semanas 1–4**

## Objetivo

Permitir que una PYME cree su cuenta, configure un bot, agregue información básica, pruebe conversaciones y publique el chatbot en su página web, enfocándose inicialmente en una vertical concreta y un caso de uso repetitivo.

### Enfoque del MVP inicial

- 1 vertical objetivo.
- 1 flujo de valor claro.
- 1 proveedor LLM.
- 1 canal de publicación: web widget.
- 1 conjunto mínimo de reglas de seguridad y handoff.

Esto evita construir un producto genérico sin validar primero si resuelve un problema real para clientes reales.

## 3.1 Autenticación y multi-tenancy

### Funcionalidades

- Registro.
- Login.
- Recuperación de contraseña.
- Organización/tenant.
- Usuario administrador.
- Roles básicos:
  - Admin.
  - Agent.
- Autorización basada en tenant.
- Auditoría básica de acciones importantes.

### Modelo conceptual

```text
Tenant
 ├── Users
 ├── Bot
 ├── Knowledge
 ├── Conversations
 ├── Messages
 └── Leads
```

### Reglas

- Todo acceso a recursos debe validar tenant.
- `tenant_id` debe ser obligatorio donde corresponda.
- No confiar en un `tenant_id` enviado libremente por el cliente.
- El tenant debe derivarse del contexto autenticado.
- Evaluar PostgreSQL RLS como defensa adicional.

---

# 4. Fase 1.2 — Onboarding básico

## Nivel 0 — Información de la empresa

Formulario inicial:

- Nombre de empresa.
- Sector.
- Descripción del negocio.
- Tono del asistente.
- Canal de derivación.
- Teléfono.
- Email.
- Sitio web.

## Nivel 1 — Información universal

Preguntas frecuentes:

- Horarios.
- Ubicación.
- Métodos de pago.
- Envíos.
- Contacto.
- Políticas básicas.

## Generación de configuración

Los datos no deben convertirse todos en prompt.

Se almacenarán estructuradamente y se utilizarán para construir el contexto del agente.

---

# 5. Fase 1.3 — LLM y pipeline RAG

## Proveedor inicial

Seleccionar un único proveedor para el MVP.

La arquitectura deberá permitir cambiar posteriormente entre:

- DeepSeek.
- Qwen.
- Llama u otros modelos.
- Proveedores API.
- Modelos locales.

## Pipeline

```text
User Message
     ↓
Validate Request
     ↓
Identify Tenant
     ↓
Load Bot Configuration
     ↓
Retrieve Relevant Knowledge
     ↓
Build Context
     ↓
LLM
     ↓
Validate Response
     ↓
Store Message + Usage
     ↓
Return Response
```

## RAG inicial

Utilizar:

**PostgreSQL + pgvector**

En lugar de incorporar inicialmente una base vectorial independiente.

Proceso:

```text
Knowledge
    ↓
Chunking
    ↓
Embeddings
    ↓
pgvector
    ↓
Similarity Search
    ↓
Relevant Context
    ↓
LLM
```

---

# 6. Fase 1.4 — Guardrails mínimos

La seguridad del agente comienza en el MVP.

## Reglas básicas

El chatbot debe:

- No inventar precios.
- No inventar horarios.
- No inventar políticas.
- No revelar instrucciones internas.
- No revelar API keys.
- No revelar información de otros tenants.
- No afirmar información que no esté autorizada.
- Indicar cuando no conoce una respuesta.
- Ofrecer derivación a humano cuando sea apropiado.

## Política de información desconocida

Ejemplo conceptual:

```text
Si la información necesaria no está presente
en la configuración ni en el contexto recuperado:

NO inventar.

Responder indicando que no se dispone de la información
y, cuando corresponda, ofrecer contacto humano.
```

## Prompt injection

Desde el MVP se debe tratar el contenido enviado por los usuarios como **input no confiable**.

Debe evitarse que un usuario pueda obtener:

- System Prompt.
- Configuración interna.
- Credenciales.
- Datos administrativos.
- Información de otros clientes.

---

# 7. Fase 1.5 — Web Widget

## Objetivo

Permitir que una PYME instale el chatbot mediante un único script.

Ejemplo conceptual:

```html
<script src="https://chat.example.com/widget.js"></script>
```

## Características iniciales

- Burbuja flotante.
- Ventana de conversación.
- Nombre/logo del negocio.
- Mensajes del usuario.
- Respuestas del bot.
- Indicador de escritura.
- Estado de conexión.
- Identificación del bot.
- Configuración básica de apariencia.

No se requiere un sistema avanzado de personalización en el MVP.

---

# 8. Fase 1.6 — Tracking de consumo y costos

Esta funcionalidad debe existir **desde el primer día**, aunque el cobro todavía no esté implementado.

Registrar como mínimo:

```text
tenant_id
conversation_id
message_id
model
input_tokens
output_tokens
total_tokens
estimated_cost
latency
timestamp
```

Esto permitirá conocer:

- Costo por conversación.
- Costo por tenant.
- Consumo mensual.
- Margen estimado.
- Modelos más económicos.
- Modelos con mejor rendimiento.

---

# 9. Fase 1 — Resultado esperado

Al finalizar la semana 4 debe ser posible:

1. Crear una cuenta.
2. Crear una empresa.
3. Configurar un bot.
4. Agregar información básica.
5. Crear FAQ.
6. Generar embeddings.
7. Consultar conocimiento mediante RAG.
8. Conversar con el LLM.
9. Probar el bot.
10. Obtener el widget.
11. Instalarlo en una web.
12. Registrar conversaciones.
13. Registrar consumo.
14. Aplicar guardrails básicos.

### Criterio de éxito

Una PYME debe poder pasar de cuenta nueva a chatbot funcionando en aproximadamente **10 minutos**, sin intervención técnica.

Además, el MVP debe demostrar que:

- responde correctamente preguntas frecuentes,
- reconoce cuándo no sabe,
- deriva a humano cuando corresponde,
- captura leads sin romper la experiencia,
- y mantiene costos por conversación bajo control.

---

# 10. Fase 2 — Productización, Knowledge Base y Dashboard

**Duración objetivo: semanas 5–8**

## 10.1 Knowledge Base avanzada

Permitir:

- Carga de PDF.
- Carga de archivos.
- URLs.
- FAQ.
- Texto manual.
- Listas de precios.
- Políticas.
- Catálogos.

Pipeline:

```text
File / URL / Text
       ↓
Extraction
       ↓
Cleaning
       ↓
Chunking
       ↓
Metadata
       ↓
Embeddings
       ↓
pgvector
```

Cada fragmento debe conservar metadata suficiente para identificar:

- Tenant.
- Documento.
- Fuente.
- Tipo.
- Fecha.
- Versión.

---

# 11. Fase 2.2 — Configuración por sector

Introducir formularios específicos según industria.

Ejemplos:

### Restaurantes

- Menú.
- Ingredientes.
- Alérgenos.
- Horarios.
- Reservas.
- Domicilios.

### E-commerce

- Productos.
- Inventario.
- Envíos.
- Tiempos de despacho.
- Devoluciones.
- Garantías.

### Servicios profesionales

- Servicios.
- Precios.
- Disponibilidad.
- Horarios.
- Proceso de contacto.

La arquitectura debe permitir agregar nuevos sectores sin modificar el núcleo del chatbot.

---

# 12. Fase 2.3 — Conversaciones

Dashboard:

- Historial.
- Conversaciones activas.
- Mensajes.
- Estado.
- Tenant.
- Fecha.
- Canal.
- Derivación.
- Lead asociado.

Estados posibles:

```text
OPEN
AI_HANDLING
WAITING_FOR_USER
HUMAN_HANDOFF
RESOLVED
CLOSED
```

---

# 13. Fase 2.4 — Captura de leads

El agente podrá solicitar:

- Nombre.
- Email.
- Teléfono.
- Necesidad/interés.

La captura debe ser contextual.

No se debe pedir información personal innecesariamente.

## Dashboard de leads

Permitir:

- Consultar.
- Filtrar.
- Ver conversación asociada.
- Exportar CSV.
- Estado del lead.

---

# 14. Fase 2.5 — Sandbox

El dashboard debe incluir un simulador para probar:

- Cambios de configuración.
- Prompt.
- Knowledge Base.
- Respuestas.
- Handoff.
- Captura de leads.

Idealmente los cambios podrán probarse antes de publicarse.

---

# 15. Fase 2.6 — Evaluación y feedback

No esperar hasta la fase de escalabilidad.

Registrar:

- 👍 Respuesta útil.
- 👎 Respuesta no útil.
- Preguntas sin respuesta.
- Escalaciones.
- Correcciones del usuario.

Crear un dataset básico:

```text
Question
Expected Answer
Actual Answer
Score
```

Esto permitirá comparar versiones de:

- Prompt.
- RAG.
- Modelo.
- Configuración.

---

# 16. Fase 2 — Resultado esperado

Al finalizar:

> Una PYME puede administrar de forma autónoma su conocimiento, conversaciones, leads y comportamiento del chatbot.

---

# 17. Fase 3 — Canales, Integraciones y Monetización

**Duración objetivo: semanas 9–12**

## 17.1 WhatsApp Business

Integrar posteriormente mediante:

- Meta Cloud API.
- Proveedor oficial/compatible cuando sea necesario.

Componentes:

- Webhooks.
- Recepción de mensajes.
- Envío de mensajes.
- Estados.
- Manejo de errores.
- Identificación del usuario.
- Conversación multicanal.

WhatsApp no debe bloquear la validación inicial del producto.

---

# 18. Fase 3.2 — Handoff humano

Flujo:

```text
Cliente
   ↓
AI
   ├── Respuesta conocida → responde
   ├── Lead → captura
   ├── Solicita humano → handoff
   └── Información insuficiente → handoff
```

## Funcionalidades

- Solicitud explícita de humano.
- Escalación automática.
- Notificación.
- Estado `HUMAN_HANDOFF`.
- Panel para que el agente tome control.
- Devolución de control a la IA.

---

# 19. Fase 3.3 — Webhooks e integraciones

Permitir enviar eventos hacia:

- Zapier.
- Make.
- CRMs.
- Sistemas propios.

Eventos iniciales:

```text
lead.created
conversation.created
conversation.resolved
human.handoff
message.received
```

---

# 20. Fase 3.4 — Monetización

Primero medir costos reales y después fijar precios definitivos.

## Plan Free / Trial

- Configuración básica.
- FAQ.
- Web Widget.
- Límite de conversaciones.
- Sin integraciones avanzadas.

## Plan Pro

- Knowledge Base.
- Documentos.
- RAG.
- Leads.
- Analytics.
- WhatsApp.
- Handoff.

## Enterprise

- Mayor volumen.
- Flujos personalizados.
- Integraciones.
- Soporte.
- SLA.
- Configuración avanzada.

Los límites comerciales pueden expresarse en conversaciones, pero internamente deben controlarse:

- Mensajes.
- Tokens.
- Costo LLM.
- Storage.
- Uso de infraestructura.

---

# 21. Fase 3 — Resultado esperado

El producto debe estar preparado para:

- Usuarios reales.
- Primeros clientes pagos.
- Web Chat.
- WhatsApp.
- Leads.
- Handoff.
- Integraciones.
- Suscripciones.

---

# 22. Fase 4 — Optimización, Seguridad Avanzada y Escalabilidad

**Semana 13 en adelante**

## 22.1 Seguridad avanzada

Fortalecer:

- Prompt injection defense.
- Protección contra extracción de prompts.
- Rate limiting.
- Validación de contenido.
- Control de acceso.
- Auditoría.
- Protección de secretos.
- Seguridad de webhooks.
- CORS.
- CSRF cuando aplique.
- Protección de endpoints.
- Encriptación de datos sensibles.

---

# 23. Fase 4.2 — RAG avanzado

Cuando el volumen lo justifique:

- Hybrid search.
- Reranking.
- Mejor chunking.
- Metadata filtering.
- Query rewriting.
- Context compression.
- Evaluación automática de retrieval.

No implementar estas técnicas por anticipado si el RAG simple ya cumple los objetivos.

---

# 24. Fase 4.3 — Caching

Introducir Redis solamente cuando exista una necesidad real.

Posibles usos:

- Respuestas frecuentes.
- Sesiones temporales.
- Rate limiting.
- Cache de configuración.
- Datos de alta frecuencia.

---

# 25. Fase 4.4 — Background Processing

Cuando la carga lo requiera, introducir procesamiento asíncrono para:

- PDFs.
- URLs.
- Embeddings.
- Indexación.
- Notificaciones.
- Webhooks.
- Procesos de evaluación.

Arquitectura futura:

```text
API
 ↓
Queue
 ↓
Worker
 ↓
Processing
```

---

# 26. Fase 4.5 — Observabilidad

Registrar:

- Latencia.
- Errores.
- Tokens.
- Costo.
- Retrieval quality.
- LLM response time.
- Rate limits.
- Fallos de proveedores.
- Estado de workers.

Crear métricas técnicas y de negocio.

---

# 27. Fase 4.6 — Evaluación continua

Construir un sistema de evaluación automática/manual.

Métricas:

- Accuracy.
- Retrieval relevance.
- Hallucination rate.
- Handoff rate.
- User feedback.
- Resolution rate.
- Lead conversion.
- Cost per conversation.

La evaluación debe permitir comparar:

```text
Model A
vs
Model B

RAG A
vs
RAG B

Prompt A
vs
Prompt B
```

---

# 28. Arquitectura tecnológica recomendada

## Frontend

- Angular.
- TypeScript.
- UI responsive.
- Dashboard administrativo.

## Backend

- ASP.NET Core.
- C#.
- REST API.
- Authentication/Authorization.
- Dependency Injection.
- Background services inicialmente cuando sea suficiente.

## Database

- PostgreSQL.
- pgvector.

## AI

Abstracción `IChatModel`.

Proveedor inicial único.

## Widget

- JavaScript/TypeScript.
- Bundle ligero.
- Comunicación HTTPS con la API.

## Infraestructura inicial

Mantener arquitectura simple:

```text
Angular
   ↓
ASP.NET Core API
   ↓
PostgreSQL + pgvector
   ↓
LLM Provider
```

Agregar componentes adicionales únicamente cuando exista una razón técnica o económica.

---

# 29. Modelo conceptual de datos

Entidades iniciales:

```text
Tenant
User
Role
Bot
BotConfiguration
KnowledgeDocument
KnowledgeChunk
Conversation
Message
Lead
UsageRecord
Feedback
AuditLog
```

Relaciones simplificadas:

```text
Tenant
 ├── Users
 ├── Bots
 │    └── BotConfiguration
 ├── KnowledgeDocuments
 │    └── KnowledgeChunks
 ├── Conversations
 │    └── Messages
 ├── Leads
 ├── UsageRecords
 ├── Feedback
 └── AuditLogs
```

---

# 30. Flujo completo del chatbot

```text
                 ┌──────────────┐
                 │    Usuario   │
                 └──────┬───────┘
                        ↓
                 ┌──────────────┐
                 │ Web / WhatsApp│
                 └──────┬───────┘
                        ↓
                 ┌──────────────┐
                 │ Chat API     │
                 └──────┬───────┘
                        ↓
                 ┌──────────────┐
                 │ Tenant       │
                 │ Resolution   │
                 └──────┬───────┘
                        ↓
              ┌─────────────────────┐
              │ Bot Configuration   │
              └─────────┬───────────┘
                        ↓
                 ┌──────────────┐
                 │ RAG Search   │
                 └──────┬───────┘
                        ↓
              ┌─────────────────────┐
              │ Context Builder     │
              └─────────┬───────────┘
                        ↓
                 ┌──────────────┐
                 │ LLM Provider │
                 └──────┬───────┘
                        ↓
                 ┌──────────────┐
                 │ Guardrails   │
                 └──────┬───────┘
                        ↓
                 ┌──────────────┐
                 │ Response     │
                 └──────┬───────┘
                        ↓
               User / Human Agent
```

---

# 31. Métricas principales del producto

## Producto

- Empresas registradas.
- Bots creados.
- Bots publicados.
- Conversaciones.
- Usuarios activos.
- Retención.

## IA

- Preguntas respondidas.
- Preguntas sin respuesta.
- Handoff rate.
- Feedback positivo/negativo.
- Accuracy.
- Hallucination rate.

## Negocio

- Trial → Paid.
- MRR.
- Churn.
- Revenue por tenant.
- AI cost por tenant.
- Gross margin.
- Cost per conversation.

## Infraestructura

- Latencia.
- Error rate.
- Tokens.
- LLM cost.
- Storage.
- Requests/minuto.

---

# 32. Riesgos principales

## Riesgo 1 — Construir demasiado

**Mitigación:** comenzar con Web Chat + configuración + FAQ + RAG.

## Riesgo 2 — Costos de IA

**Mitigación:** tracking de tokens y costos desde el MVP.

## Riesgo 3 — Fugas entre tenants

**Mitigación:** arquitectura multi-tenant estricta + autorización + RLS.

## Riesgo 4 — Alucinaciones

**Mitigación:** RAG + reglas + validación + fallback humano.

## Riesgo 5 — Prompt injection

**Mitigación:** input no confiable + aislamiento de instrucciones + validaciones.

## Riesgo 6 — Dependencia de un proveedor

**Mitigación:** interfaz `IChatModel`.

## Riesgo 7 — Complejidad prematura

**Mitigación:** evitar microservicios, Redis, Qdrant y colas hasta que sean necesarios.

## Riesgo 8 — Construir sin validar mercado

**Mitigación:** conseguir PYMEs piloto lo antes posible.

---

# 33. Prioridad de desarrollo

La prioridad debe ser:

```text
P0 — Imprescindible
P1 — Necesario para productizar
P2 — Diferenciador
P3 — Optimización
```

## P0

- Auth.
- Multi-tenancy.
- PostgreSQL.
- pgvector.
- Bot configuration.
- FAQ.
- LLM.
- RAG.
- Web Widget.
- Guardrails básicos.
- Usage/cost tracking.

## P1

- Dashboard.
- Documentos.
- URLs.
- Leads.
- Conversations.
- Sandbox.
- Handoff.
- Feedback.
- Analytics.

## P2

- WhatsApp.
- CRM.
- Webhooks.
- Billing.
- Sector-specific workflows.
- Evaluación avanzada.

## P3

- Redis.
- Reranking.
- Hybrid search.
- Queues.
- Multi-model routing.
- Escalabilidad avanzada.

---

# 34. Definición de MVP

El MVP se considera terminado cuando una PYME puede:

1. Registrarse.
2. Crear su organización.
3. Configurar su empresa.
4. Crear un bot.
5. Añadir FAQ.
6. Probar el bot.
7. Obtener respuestas mediante RAG.
8. Publicar el widget.
9. Recibir conversaciones reales.
10. Consultar conversaciones.
11. Registrar consumo y costos.
12. Escalar preguntas desconocidas.
13. Mantener los datos completamente aislados de otros tenants.

**El MVP no necesita inicialmente:**

- Múltiples verticales y formularios sectoriales complejos.
- WhatsApp.
- Stripe.
- CRM.
- Redis.
- Qdrant.
- Microservicios.
- Entrenamiento de modelos propios.
- Decenas de proveedores LLM.
- Funcionalidades de automatización avanzada antes de validar el problema.

El MVP debe estar centrado en demostrar que el bot responde con utilidad, seguridad y bajo costo para una PYME concreta.

---

# 35. Estrategia de validación

Antes de desarrollar todas las fases, se recomienda conseguir **2–5 PYMEs piloto** en la vertical elegida.

El objetivo será comprobar:

- Si el problema realmente existe.
- Qué preguntas hacen sus clientes.
- Qué información necesitan.
- Cuánto valoran la automatización.
- Cuánto estarían dispuestos a pagar.
- Cuánto cuesta atender cada conversación.
- Cuántas conversaciones terminan en humano.
- Cuántos leads genera el bot.
- Qué tan repetitivas son las consultas más comunes.

La validación debe influir directamente en el roadmap y, si los datos lo exigen, puede llevar a ajustar el enfoque vertical o el flujo de handoff antes de ampliar la plataforma.

---

# 36. Roadmap resumido

```text
SEM 1–4
────────────────────────────────
MVP CORE

Auth
Multi-tenant
PostgreSQL
pgvector
Bot Configuration
FAQ
LLM
RAG
Widget
Guardrails
Cost Tracking


SEM 5–8
────────────────────────────────
PRODUCTIZACIÓN

Dashboard
Knowledge Base
PDF / URLs
Sector Configuration
Conversations
Leads
Sandbox
Feedback
Evaluation
Human Handoff


SEM 9–12
────────────────────────────────
COMERCIALIZACIÓN

WhatsApp
Webhooks
CRM
Billing
Plans
Usage Limits
Notifications


SEM 13+
────────────────────────────────
SCALE

Advanced Security
Advanced RAG
Redis
Queues
Workers
Observability
Automated Evaluation
Multi-model Routing
Horizontal Scaling
```

---

# 37. Principio rector del proyecto

> **Primero construir un chatbot que funcione de forma confiable para una PYME. Después convertirlo en un producto completo. Finalmente optimizarlo para escalar.**

La prioridad no debe ser tener la arquitectura más sofisticada, sino conseguir que una PYME pueda decir:

> **"Lo configuré, lo instalé en mi página y realmente está resolviendo preguntas de mis clientes."**

Ese será el principal indicador de que el producto tiene sentido.
