# Atendia — Strategy

## Estado actual del proyecto

A la fecha, la base de la solución ya fue levantada con una arquitectura clara:

- Backend en ASP.NET Core.
- Capa de dominio, aplicación e infraestructura separadas.
- Abstracción del proveedor LLM con `IChatModel` y `ExternalChatModel`.
- PostgreSQL + EF Core configurado y migración inicial creada.
- Persistencia EF de conocimiento, conversaciones, mensajes y uso.
- Identity básico asociado a tenant y credencial pública por bot.
- Widget JavaScript funcional para chat público.

Sin embargo, el proyecto aún está en una etapa de validación técnica, no de producción completa:

- la autorización por recurso y los roles operativos aún deben cerrarse,
- el RAG debe validarse con embeddings persistidos y aislamiento por tenant,
- la capa de seguridad y tenant enforcement requiere reforzarse,
- el onboarding administrativo y la captura de leads aún no existen,
- el handoff humano todavía no está operativo,
- y la operación comercial aún debe medirse con datos reales del producto.

Esto es consistente con la estrategia original: arrancar con proveedor externo, medir costo real, y solo evaluar un modelo local si el volumen lo justifica.

---

## 1. Visión

Atendia busca convertirse en un SaaS multitenant para que las PYMEs puedan ofrecer atención al cliente automatizada con IA, sin necesidad de contratar un equipo técnico especializado.

La propuesta no es reemplazar a los agentes humanos por completo, sino ayudarlos a resolver consultas repetitivas, capturar leads y escalar a humano cuando el caso lo requiere.

---

## 2. Problema que resuelve

Muchas PYMEs reciben consultas repetitivas por:

- horarios,
- precios,
- disponibilidad,
- entregas,
- pagos,
- dudas generales,
- y contacto con la empresa.

Estas consultas consumen tiempo y suelen atenderse manualmente, incluso cuando tienen respuestas estándar.

Atendia permite automatizar ese flujo, disminuir carga operativa y mejorar la velocidad de respuesta.

---

## 3. Posicionamiento

Atendia no debe posicionarse como “un chat cualquiera”.

Debe proponerse como:

- AI Customer Service for Small Business
- un agente de atención para PYMEs,
- desplegable en web,
- fácil de configurar,
- y con control de seguridad y costos.

---

## 4. Diferenciadores clave

### 1. Multi-tenancy desde el inicio

Aislar por tenant y proteger información sensible desde el diseño.

### 2. Configuración estructurada

No depender de un prompt gigante. La configuración del negocio debe vivir en modelos y metadata, no en texto libre.

### 3. RAG útil y simple

Usar pgvector con conocimiento relevante y recuperable, manteniendo calidad y bajo costo.

### 4. Guardrails básicos

El bot debe estar diseñado para no inventar, no revelar secretos ni responder fuera de contexto.

### 5. Tracking y costo

La monetización real necesita control de tokens, uso y costos desde el primer día.

---

## 5. Estrategia de producto

### Objetivo de corto plazo

Demostrar que el producto funciona para una vertical concreta y resuelve consultas reales sin intervención técnica.

### MVP recomendado

- 1 vertical objetivo.
- 1 caso de uso principal.
- 1 proveedor LLM.
- 1 canal principal: web widget.
- 1 respuesta clave: “preguntas frecuentes y bot con contexto”.
- 1 flujo mínimo de handoff manual.
- métricas de costo y resolución desde el primer piloto.

### Estrategia de crecimiento

1. Validar con PYMEs piloto.
2. Mejorar FAQ y flujo de handoff.
3. Agregar leads, dashboard y onboarding administrativo.
4. Extender a más sectores.
5. Incorporar más canales y automatizaciones.

---

## 6. Estrategia de tecnología

### Stack recomendado

- Frontend: Angular
- Backend: ASP.NET Core
- Database: PostgreSQL + pgvector
- AI abstraction: IChatModel
- Widget: JavaScript

### Proveedor inicial

- Usar un proveedor externo para arrancar.
- Mantener la abstracción para poder cambiar de modelo posteriormente.

### Política de costos

- Registrar tokens, costo estimado y latencia por mensaje.
- Medir costo total por tenant y por conversación.
- Definir thresholds para decidir si conviene un modelo local.

### Regla clave

El modelo local no se debe adoptar por “principio”, sino por necesidad económica o de control operativa.

---

## 7. Modelo de negocio

El producto debe ser rentable por tenant y por uso real.

### Planes sugeridos

- Free / Trial
- Pro
- Enterprise

### Factores de monetización

- conversaciones mensuales,
- mensajes,
- costo de LLM,
- almacenamiento,
- conocimiento base,
- lead management,
- handoff humano,
- integraciones.

---

## 8. Riesgos principales

### Riesgo 1 — construir demasiado antes de validar

Mitigación: empezar con un vertical y un MVP acotado.

### Riesgo 2 — alucinaciones

Mitigación: fuerte RAG, guardrails, transparencia y handoff.

### Riesgo 3 — fugas entre tenants

Mitigación: estricta separación lógica y autenticación por tenant.

### Riesgo 4 — costos de IA no controlados

Mitigación: tracking desde el primer día.

### Riesgo 5 — dependencia de un proveedor

Mitigación: interfaz de abstracción y modelos intercambiables.

---

## 9. Criterios de éxito

El proyecto tendrá éxito si:

- una PYME puede configurar su bot sin ayuda técnica,
- el bot responde bien a preguntas repetitivas,
- no inventa información,
- sabe escalar a humano,
- genera leads,
- registra conversaciones y consumo,
- mantiene aislamiento entre tenants,
- y la operación no se vuelve más costosa que el valor que entrega.

---

## 10. Decisión estratégica principal

La decisión más importante es esta:

- arrancar con un proveedor externo,
- medir costo real por conversación,
- y migrar a modelo local solo cuando el volumen y el margen lo justifiquen.

Esto permite lanzar rápido, aprender con datos reales, y evitar sobreinversión en infraestructura.

---

## 11. Conclusión

Atendia tiene potencial como SaaS real si se construye con disciplina, foco y minimalismo inicial.

La clave no es “tener la arquitectura más avanzada”, sino demostrar que una PYME puede instalar un bot útil, responder bien, capturar leads y operar con costos controlados.

Cuando ese punto esté validado, entonces sí hace sentido escalar, lanzar más canales, mejorar el RAG y considerar un modelo local o infraestructura más avanzada.
