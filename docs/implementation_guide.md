# Atendia — Guía de Implementación por Fases

## Estado actual de ejecución

Estado al 2026-10-20: el núcleo técnico del MVP está implementado y validado localmente contra PostgreSQL, pero aún no se ha desplegado a producción ni se ha ejecutado un piloto con clientes. La validación local incluye:

- backend compilando,
- PostgreSQL + EF Core configurado,
- migraciones para persistencia, Identity y credencial pública del bot generadas,
- negocio base definido en dominio/aplicación,
- abstracción del proveedor LLM implementada,
- autenticación básica y resolución de tenant implementadas,
- conversaciones, mensajes y uso persistidos,
- widget JavaScript funcional para el piloto.

El backend también cuenta con autorización por recurso y políticas de roles, RAG con embeddings persistidos y búsqueda HNSW filtrada por tenant/bot, auditoría básica, configuración y gestión de FAQ, captura de leads y handoff humano. El panel admin y el widget se validaron localmente; se probó una recuperación RAG puntual con Ollama.

Pendiente: evaluación de RAG y guardrails con contenido aprobado del piloto y el proveedor/modelo elegido; validación real de entrega SMTP; staging y despliegue reproducible; tarifas y métricas agregadas de costo; ejecución del piloto y medición de resultados. El costo estimado y la latencia se registran por mensaje, pero aún no hay medición comercial/operativa completa.

---

## Objetivo

Esta guía define cómo construir Atendia de forma incremental, minimizando riesgo, controlando costos y validando valor real antes de ampliar la plataforma.

---

## Fase 0 — Validación del problema

### Objetivo

Confirmar que existe un problema real para una PYME concreta y entender qué tipo de consultas deben resolverse automáticamente.

### Actividades

- Elegir 1 vertical inicial.
- Buscar 2–5 PYMEs piloto.
- Revisar sus FAQs, consultas frecuentes y casos de atención.
- Definir los 10–20 casos de uso más repetitivos.
- Evaluar qué consultas podrían resolverse sin humano.
- Identificar cuándo debe producirse una escalación a humano.

### Resultado esperado

- Un mapa claro de consultas repetitivas.
- Un entendimiento del flujo real de atención al cliente.
- Reglas iniciales de respuesta y handoff.

### Criterio de salida

Se puede empezar a diseñar el MVP solo si se entiende bien:

- qué pregunta hace el cliente,
- qué información debe responder el bot,
- cuándo no debe responder,
- y cuándo debe pedir contacto humano.

---

## Fase 1 — MVP Core

### Objetivo

Permitir que una PYME cree su cuenta, configure su bot, publique un widget y responda preguntas frecuentes con RAG.

### Alcance mínimo

- Registro e inicio de sesión.
- Tenant / organización.
- Usuario administrador.
- Roles básicos y autorización por tenant.
- Bot y configuración básica.
- FAQ / conocimiento base mínima.
- PostgreSQL + pgvector.
- RAG simple con embeddings persistidos, pgvector, índice HNSW y filtro obligatorio por tenant y bot.
- LLM con proveedor externo.
- Web widget instalable.
- Configuración mínima del bot y widget JavaScript.
- Guardrails básicos.
- Tracking de consumo y costo.
- Handoff manual con estado `HUMAN_HANDOFF`, bandeja de conversaciones y captura de leads.

### Arquitectura base actual

```text
HTML/JavaScript Admin y Widget
   ↓
ASP.NET Core API
   ↓
PostgreSQL + pgvector
   ↓
LLM Provider
```

### Requisitos de calidad

- Multi-tenancy estricto.
- Validación de identidad y tenant en cada request.
- No confiar en usuario/client para elegir tenant.
- Persistencia de conversaciones, mensajes, uso, leads y auditoría.
- Respuestas sin inventar información.
- Handoff a humano cuando no haya certeza.

### Resultado esperado

Una PYME puede, mediante la API actual:

- crear una cuenta,
- configurar el bot,
- cargar FAQ,
- probar conversaciones,
- publicar el widget,
- ver conversaciones y consumo registrados,
- gestionar su configuración y FAQ,
- capturar leads,
- y escalar manualmente los casos que requieran atención humana.

---

## Fase 2 — Productización del flujo

### Objetivo

Hacer que el producto sea útil y administrable por una PYME sin intervención técnica.

### Funcionalidades

- Dashboard de conversaciones.
- Historial y filtros.
- Leads capturados.
- Feedback del usuario.
- Sandbox o prueba antes de publicar.
- Documentos y URLs en la knowledge base.
- Configuración por sector o industria.

### Requisitos

- UX simple para config.
- Definición clara de cuándo pedir datos personales.
- Reglas de lead capture contextual.
- Estado de conversación claro: OPEN, AI_HANDLING, WAITING_FOR_USER, HUMAN_HANDOFF, RESOLVED, CLOSED.

### Estado actual

Existe un panel administrativo mínimo para gestionar configuración del bot, FAQ, conversaciones, leads y handoffs, validado localmente. La productización de esta interfaz sigue pendiente: debe probarse con usuarios piloto y complementarse según las necesidades observadas. El dashboard analítico descrito en la siguiente fase todavía no está implementado.

---

## Fase 3 — Handoff humano y canales

### Objetivo

Hacer una experiencia operativa real para atención al cliente.

### Funcionalidades

- Escalación manual y automática a humano, estado `HUMAN_HANDOFF` y endpoints de revisión: implementados y validados localmente.
- Notificación SMTP al email de contacto del bot: integrada; falta configurar y verificar la entrega real en el entorno piloto.
- Integración con WhatsApp, webhooks y eventos para CRM: futuras, sujetas a validación de necesidad.

### Resultado esperado

El bot puede responder por sí mismo, pero también entregar el caso a un humano cuando corresponde.

---

## Fase 4 — Monetización y operación

### Objetivo

Preparar el producto para clientes reales y pagar por uso.

### Alcance

- Planes Free / Trial / Pro / Enterprise.
- Uso y límites por tenant.
- Costo por conversación.
- Facturación y billing.
- Alertas por consumo y errores.
- Observabilidad básica.

### Resultado esperado

El producto es medible, rentable y manejable como SaaS real.

---

## Fase 5 — Escala y optimización

### Objetivo

Mejorar seguridad, calidad y costo cuando el producto ya sea real.

### Alcance opcional

- RAG más avanzado.
- Redis para cache y rate limiting.
- Workers y background processing.
- Mejor evaluación automática.
- Optimización de prompt y retrieval.
- Modelo local en VPS si el volumen lo justifica.

### Requisito clave

Solo implementar estas mejoras cuando existan señales reales de necesidad técnica o económica.

---

## Reglas de implementación

### 1. No construir demasiado

No agregar servicios, colas, caches ni microservicios sin necesidad demostrada.

### 2. Abstraer el proveedor LLM

El negocio debe poder cambiar de proveedor sin reescribir la solución.

### 3. Medir los costos desde el primer día

Sin este dato, no se sabe si el negocio es viable.

### 4. Controlar multi-tenancy desde el diseño

Todo debe estar asociado a tenant y validado por backend.

### 5. Priorizar utilidad sobre sofisticación

La verdadera prueba del producto es si resuelve preguntas reales de clientes reales.

---

## Criterio final de MVP

El MVP se considera exitoso cuando una PYME puede:

1. registrarse,
2. crear su tenant,
3. configurar su negocio,
4. crear un bot,
5. agregar FAQ,
6. probar la conversación,
7. publicar el widget,
8. atender conversaciones reales,
9. registrar costos,
10. consultar el historial básico de conversaciones,
11. y escalar a humano cuando corresponda.

---

## Recomendación final

La implementación debe priorizar:

- producto útil,
- costo controlado,
- seguridad básica,
- y validación temprana con clientes reales.

Después de eso, se puede escalar y sofisticar sin comprometer la base del SaaS.
