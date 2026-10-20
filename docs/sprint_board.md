# Atendia — Sprint Board

## Objetivo del sprint

Convertir la base técnica ya validada en una capa operativa mínima de producto: autenticación, persistencia real, aislamiento por tenant, RAG verificable, métricas de costo, widget funcional y preparación para un piloto real.

---

## Prioridad 0 — Alcance de aceptación del MVP

El MVP se acepta únicamente cuando el flujo `registro → configuración → FAQ → prueba → publicación → conversación → persistencia → handoff` funciona con un tenant aislado.

Quedan fuera de este sprint: WhatsApp, billing, CRM, Redis, colas, modelo local y RAG avanzado.

---

## Prioridad 1 — Autenticación y persistencia operativa

### Tareas

- [x] Implementar registro, login y rol inicial `Admin`.
- [x] Derivar el tenant del contexto autenticado o de la credencial pública del bot.
- [x] Crear repositorios para `Conversation`, `Message` y `UsageRecord`.
- [x] Conectar la lógica de chat con almacenamiento persistente de conversación.
- [x] Crear persistencia y flujo de `Lead`.
- [x] Confirmar que FAQ y retrieval usan embeddings persistidos y filtro por tenant y bot.

### Estado

Completada a nivel backend; pendiente validación con PostgreSQL real.

---

## Prioridad 2 — Multi-tenancy y seguridad

### Tareas

- [x] Definir el origen real del `tenantId` (usuario autenticado o `BotKey`).
- [x] Validar pertenencia del `BotId` en todos los endpoints críticos.
- [x] Proteger acceso inicial a knowledge y bot por tenant.
- [x] Añadir políticas de roles `Admin` / `Operator` / `CustomerSupport`.
- [x] Definir políticas simples de auditoría para eventos sensibles.
- [x] Hacer handoff automático cuando no se recupera conocimiento o el modelo indica que no puede fundamentar la respuesta.
- [x] Separar pregunta y fuentes no confiables de las instrucciones del sistema.
- [ ] Evaluar prompt injection y calibrar la recuperación con el proveedor/modelo definitivo.

### Estado

Guardrails básicos y abstención implementados; hace falta evaluación con un conjunto de ataques realista. La política de prompt reduce el riesgo, pero no garantiza por sí sola resistencia a prompt injection.

---

## Prioridad 3 — Cost tracking y observabilidad

### Tareas

- [x] Registrar tokens por request, modelo y latencia.
- [ ] Calcular costo estimado por conversación.
- [ ] Configurar tarifas por proveedor/modelo para calcular costo real.
- [ ] Agregar logs estructurados por tenant y bot.
- [x] Registrar fallos del proveedor y responder HTTP 503 con mensaje controlado.
- [x] Añadir liveness `/health` y readiness de PostgreSQL `/health/ready`.
- [ ] Exponer métricas básicas para dashboard y correlación de logs por conversación.

---

## Prioridad 3.1 — Widget y onboarding

### Tareas

- [x] Finalizar el widget embebible con `BotKey`.
- [x] Crear configuración administrativa mínima del negocio y del bot vía API.
- [x] Permitir cargar y gestionar FAQ desde el admin.
- [x] Crear página de prueba para instalación del widget.

### Estado

Backend listo; frontend administrativo pendiente.

---

## Prioridad 4 — Piloto real

### Tareas

- [ ] Preparar un caso de uso concreto para un cliente piloto.
- [ ] Crear un tenant demo con FAQ útil.
- [x] Implementar el flujo backend de conversation -> answer -> lead -> handoff.
- [x] Escalar automáticamente consultas sin respaldo y actualizar la bandeja de handoff.
- [x] Permitir que un agente responda en el admin y que el widget reciba la respuesta.
- [x] Integrar alertas SMTP al email de contacto del bot.
- [ ] Configurar y probar entrega con SMTP del entorno piloto.
- [ ] Evaluar el flujo con preguntas representativas de un cliente piloto.
- [ ] Evaluar la tasa de resolución, escalamiento y costo.
- [ ] Rediseñar la configuración mínima del bot según resultados reales.

### Estado

El flujo local de admin/widget fue validado; faltan cliente y contenido piloto, validar la entrega SMTP real y medir resultados de negocio.

---

## Prioridad 5 — Decisión sobre modelo local

### Tareas

- [ ] Medir costo real por conversación con proveedor externo.
- [ ] Calcular volumen estimado mensual.
- [ ] Comparar con la operación de un modelo local en VPS.
- [ ] Definir si conviene mantener proveedor externo o migrar localmente.

### Estado

Pendiente hasta obtener datos de volumen y costo reales.

---

## Criterio de salida del sprint

El sprint se considera listo cuando:

- todas las conversaciones y mensajes quedan persistidos,
- cada request está validado por tenant,
- el widget puede instalarse y conectarse a un bot,
- existe tracking de costo por conversación,
- existe un handoff manual mínimo y captura de leads,
- y el flujo completo puede probarse con un cliente piloto real.
