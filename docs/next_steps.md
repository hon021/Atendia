# Atendia — Next Steps y checklist de ejecución

## Objetivo

Definir la prioridad inmediata para convertir el proyecto de una base técnica funcional en un MVP operativo con persistencia real, seguridad y métricas útiles.

---

## Fase 1 — Cerrar el núcleo del MVP

El objetivo de esta fase es que el flujo completo use datos reales y pueda probarse de forma segura con un tenant piloto. No es suficiente que los endpoints respondan: la información debe persistir, estar aislada por tenant y ser observable.

### 0. Autenticación y contexto de tenant

- [x] Implementar registro, login y validación de sesión.
- [x] Resolver el tenant desde el usuario autenticado o la credencial pública del bot.
- [x] Rechazar `tenantId` arbitrario en chat, bots y knowledge.
- [x] Definir el rol inicial `Admin`.

## Fase 1.1 — Persistencia real

### 1. Repositorios concretos

- `ITenantRepository` y `IBotRepository` ya quedaron implementados con EF en infraestructura.
- `IKnowledgeService` ya usa `EfKnowledgeService` en lugar de almacenamiento en memoria.
- `IConversationRepository`, `IMessageRepository` e `IUsageRepository` ya están implementados y conectados al chat.

### 2. Entidades y relaciones

- Finalizar la estructura real de `Tenant`, `Bot`, `KnowledgeItem`, `Conversation`, `Message`, `Lead` y `UsageRecord`.
- Confirmar claves primarias, índices y relaciones según el caso de uso.
- [ ] Implementar embeddings, índice vectorial y búsqueda semántica con filtro obligatorio por tenant.

### 3. Migraciones adicionales

- [x] Generar migraciones para persistencia, Identity y `BotPublicKey`.
- [x] Validar que los nombres, tipos y campos cumplen el flujo actual.

---

## Fase 2 — Seguridad y multi-tenancy

### 4. Tenant enforcement

- [x] Garantizar que chat, bots y knowledge identifiquen el tenant autenticado o la credencial del bot.
- [x] No depender del `tenantId` enviado por cliente como fuente de verdad.
- [ ] Validar pertenencia del `BotId` a cada tenant y restringir creación de tenants.

### 5. Autenticación y autorización

- [x] Definir mecanismo de login y usuario asociado a tenant.
- [ ] Crear políticas para `Admin` / `Operator` / `CustomerSupport`.
- [ ] Registrar auditoría básica de eventos sensibles.

### 6. Guardrails

- Reforzar prompt y lógica de seguridad.
- Definir cuando el bot debe responder, cuándo pedir más información y cuándo escalar a humano.

---

## Fase 3 — Operación y métricas

### 7. Tracking de costos

- [x] Registrar por mensaje: tokens, modelo, costo estimado y latencia.
- [ ] Configurar tarifas por proveedor/modelo y calcular costo real por tenant y conversación.
- Preparar alertas de consumo y umbrales.

### 8. Observabilidad

- Logs estructurados.
- Health checks por servicio.
- Trazabilidad de conversaciones y errores.

### 9. Dashboard básico

- conversaciones por día,
- resolución vs escalamiento,
- leads generados,
- costo por tenant,
- disponibilidad del servicio.
- correlación por `conversationId`, `messageId`, `tenantId` y `botId`.

## Fase 4 — Widget, onboarding y handoff mínimo

### 10. Widget web y onboarding

- [x] Finalizar el widget embebido.
- [ ] Crear configuración del negocio en admin.
- [ ] Cargar Knowledge Base desde una interfaz administrativa.
- [x] Publicar mediante un script con `BotKey`.
- [ ] Validar conversaciones reales con un cliente piloto.

### 11. Handoff humano

- [ ] Definir flujo de escalamiento.
- [ ] Implementar estado `HUMAN_HANDOFF`.
- [ ] Crear revisión manual y mecanismo de contacto/notificación.

### Criterio de salida del MVP

Una PYME debe poder registrarse, configurar su bot, cargar FAQ, probarlo, publicar el widget, recibir una conversación, persistir sus mensajes y consumo, consultar el historial básico y escalar un caso a humano sin intervención de desarrollo.

---

## Fase 5 — Decisión sobre modelo local

### 12. Evaluación económica

- recopilar costo real por conversación con proveedor externo,
- calcular volumen mensual,
- comparar con el costo operativo de un modelo local en VPS,
- decidir si la inversión en infraestructura local tiene sentido.

### 13. Si se decide local

- preparar runner, GPU/CPU, modelo base y pipeline de inferencia,
- mantener la abstracción del proveedor LLM,
- validar latencia, costo y confiabilidad antes de migrar.

---

## Orden recomendado de trabajo

1. RAG con embeddings y búsqueda vectorial.
2. Autorización por recurso y validaciones de tenant restantes.
3. Onboarding administrativo y gestión de FAQ.
4. Handoff y captura de leads.
5. Costos reales, observabilidad y piloto.
6. Evaluación del modelo local.

---

## Criterio de salida de la siguiente fase

Se considera que la siguiente fase está cerrada cuando:

- el backend usa base de datos real para tenants, bots y knowledge,
- el tenant está validado en cada operación sensible,
- las conversaciones y uso son trazables,
- y existe un flujo operativo mínimo con métricas básicas.

Esto es el punto de partida para convertir el proyecto en un SaaS medible y vendible.
