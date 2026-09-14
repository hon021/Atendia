# Atendia — Guía de Implementación por Fases

## Estado actual de ejecución

El proyecto ya completó la fase inicial de infraestructura técnica y la base del MVP funcional:

- backend compilando,
- PostgreSQL + EF Core configurado,
- migraciones para persistencia, Identity y credencial pública del bot generadas,
- negocio base definido en dominio/aplicación,
- abstracción del proveedor LLM implementada,
- autenticación básica y resolución de tenant implementadas,
- conversaciones, mensajes y uso persistidos,
- widget JavaScript funcional para el piloto.

No obstante, aún quedan tareas críticas para dejarlo operativo como producto real:

- completar autorización por recurso y roles operativos,
- cerrar el pipeline RAG con embeddings y búsqueda filtrada por tenant,
- cerrar métricas de costo real y observabilidad,
- construir el onboarding administrativo,
- implementar leads y handoff humano,
- y validar el flujo completo con un cliente piloto.

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
- RAG simple con filtro por tenant; embeddings vectoriales quedan como siguiente bloque.
- LLM con proveedor externo.
- Web widget instalable.
- Configuración mínima del bot y widget JavaScript.
- Guardrails básicos.
- Tracking de consumo y costo.
- Preparación para handoff manual; operación completa queda pendiente.

### Arquitectura base

```text
Angular Admin
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
- Persistencia de conversaciones, mensajes y uso; leads quedan pendientes.
- Respuestas sin inventar información.
- Handoff a humano cuando no haya certeza.

### Resultado esperado

Una PYME puede:

- crear una cuenta,
- configurar el bot,
- cargar FAQ,
- probar conversaciones,
- publicar el widget,
- ver conversaciones y consumo registrados,
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

### Resultado esperado

Una PYME puede administrar:

- su conocimiento,
- su bot,
- sus conversaciones,
- y sus leads,

sin depender de un desarrollador.

---

## Fase 3 — Handoff humano y canales

### Objetivo

Hacer una experiencia operativa real para atención al cliente.

### Funcionalidades

- Escalación automática a humano.
- Notificaciones a agentes.
- Estados de handoff.
- Panel para controlar las conversaciones.
- Integración con WhatsApp cuando sea viable.
- Webhooks y eventos para CRM o automatizaciones.

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
