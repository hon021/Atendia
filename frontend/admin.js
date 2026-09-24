const state = {
  apiBase: 'https://localhost:5001',
  tenantId: '',
  botId: '',
  botPublicKey: '',
  botName: '',
  knowledge: [],
  conversations: [],
  leads: []
};

const ui = {
  appPanel: document.getElementById('appPanel'),
  knowledgePanel: document.getElementById('knowledgePanel'),
  widgetPanel: document.getElementById('widgetPanel'),
  opsPanel: document.getElementById('opsPanel'),
  logoutBtn: document.getElementById('logoutBtn'),
  registerForm: document.getElementById('registerForm'),
  loginForm: document.getElementById('loginForm'),
  registerStatus: document.getElementById('registerStatus'),
  loginStatus: document.getElementById('loginStatus'),
  botForm: document.getElementById('botForm'),
  configForm: document.getElementById('configForm'),
  botStatus: document.getElementById('botStatus'),
  knowledgeForm: document.getElementById('knowledgeForm'),
  knowledgeStatus: document.getElementById('knowledgeStatus'),
  knowledgeList: document.getElementById('knowledgeList'),
  widgetSnippet: document.getElementById('widgetSnippet'),
  leadsList: document.getElementById('leadsList'),
  conversationsList: document.getElementById('conversationsList')
};

function setStatus(element, message, isError = false) {
  element.textContent = message || '';
  element.classList.toggle('error', isError);
}

async function apiFetch(path, options = {}) {
  const finalOptions = {
    credentials: 'include',
    ...options,
    headers: {
      ...(options.headers || {})
    }
  };

  if (options.body && !(options.body instanceof FormData)) {
    finalOptions.headers['Content-Type'] = 'application/json';
  }

  const response = await fetch(`${state.apiBase}${path}`, finalOptions);

  if (response.status === 204) {
    return null;
  }

  const text = await response.text();
  const body = text ? JSON.parse(text) : null;

  if (!response.ok) {
    const detail = body?.message || body?.title || 'La solicitud falló.';
    throw new Error(detail);
  }

  return body;
}

function setLoggedInUI(isLoggedIn) {
  ui.logoutBtn.classList.toggle('hidden', !isLoggedIn);
  ui.appPanel.classList.toggle('hidden', !isLoggedIn);
  ui.knowledgePanel.classList.toggle('hidden', !isLoggedIn);
  ui.widgetPanel.classList.toggle('hidden', !isLoggedIn);
  ui.opsPanel.classList.toggle('hidden', !isLoggedIn);
}

async function registerCompany(event) {
  event.preventDefault();
  const formData = new FormData(ui.registerForm);
  const payload = {
    name: String(formData.get('name') || '').trim(),
    email: String(formData.get('email') || '').trim(),
    password: String(formData.get('password') || '')
  };

  try {
    setStatus(ui.registerStatus, '');
    const result = await apiFetch('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload)
    });

    state.tenantId = result.tenantId;
    setStatus(ui.registerStatus, `Empresa creada. Tenant: ${state.tenantId}`);
    ui.loginForm.email.value = payload.email;
    ui.loginForm.password.value = payload.password;
  } catch (error) {
    setStatus(ui.registerStatus, error.message, true);
  }
}

async function login(event) {
  event.preventDefault();
  const formData = new FormData(ui.loginForm);
  const payload = {
    email: String(formData.get('email') || '').trim(),
    password: String(formData.get('password') || '')
  };

  try {
    setStatus(ui.loginStatus, '');
    const result = await apiFetch('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(payload)
    });

    state.tenantId = result.tenantId || result.tenant_id || state.tenantId;
    setLoggedInUI(true);
    setStatus(ui.loginStatus, 'Sesión iniciada correctamente.');
    await refreshAll();
  } catch (error) {
    setStatus(ui.loginStatus, error.message, true);
  }
}

async function logout() {
  try {
    await apiFetch('/api/auth/logout', { method: 'POST' });
    state.tenantId = '';
    state.botId = '';
    state.botPublicKey = '';
    state.botName = '';
    state.knowledge = [];
    state.conversations = [];
    state.leads = [];
    setLoggedInUI(false);
    ui.knowledgeList.innerHTML = '';
    ui.conversationsList.innerHTML = '';
    ui.leadsList.innerHTML = '';
    ui.widgetSnippet.textContent = '';
  } catch (error) {
    console.error(error);
  }
}

async function createBot(event) {
  event.preventDefault();
  const formData = new FormData(ui.botForm);
  const payload = {
    tenantId: state.tenantId,
    name: String(formData.get('name') || '').trim(),
    businessDescription: String(formData.get('businessDescription') || '').trim() || null
  };

  try {
    setStatus(ui.botStatus, '');
    const result = await apiFetch('/api/bots', {
      method: 'POST',
      body: JSON.stringify(payload)
    });

    state.botId = result.id;
    state.botPublicKey = result.publicKey;
    state.botName = result.name;
    ui.configForm.businessName.value = result.name;
    ui.widgetSnippet.textContent = `<script src="http://localhost:8000/widget.js" data-bot-key="${result.publicKey}" data-api-url="https://localhost:5001" data-title="${result.name}"></script>`;
    setStatus(ui.botStatus, `Bot creado: ${result.name} (${result.publicKey})`);
    await refreshAll();
  } catch (error) {
    setStatus(ui.botStatus, error.message, true);
  }
}

async function saveConfig(event) {
  event.preventDefault();
  if (!state.botId) {
    setStatus(ui.botStatus, 'Primero crea un bot.', true);
    return;
  }

  const formData = new FormData(ui.configForm);
  const payload = {
    businessName: String(formData.get('businessName') || '').trim(),
    sector: String(formData.get('sector') || '').trim(),
    contactPhone: String(formData.get('contactPhone') || '').trim(),
    contactEmail: String(formData.get('contactEmail') || '').trim(),
    website: String(formData.get('website') || '').trim(),
    handOffChannel: String(formData.get('handOffChannel') || '').trim(),
    systemPrompt: `Eres el asistente de ${state.botName || 'Mi negocio'}. Responde con cortesía y ofrece derivación a humano cuando no tengas certeza.`
  };

  try {
    setStatus(ui.botStatus, '');
    await apiFetch(`/api/bots/${state.botId}/configuration`, {
      method: 'PUT',
      body: JSON.stringify(payload)
    });
    setStatus(ui.botStatus, 'Configuración guardada.');
  } catch (error) {
    setStatus(ui.botStatus, error.message, true);
  }
}

async function saveKnowledge(event) {
  event.preventDefault();
  if (!state.botId) {
    setStatus(ui.knowledgeStatus, 'Primero crea un bot.', true);
    return;
  }

  const formData = new FormData(ui.knowledgeForm);
  const payload = {
    botId: state.botId,
    title: String(formData.get('title') || '').trim(),
    content: String(formData.get('content') || '').trim(),
    sourceType: 'faq'
  };

  try {
    setStatus(ui.knowledgeStatus, '');
    await apiFetch('/api/knowledge', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
    ui.knowledgeForm.reset();
    setStatus(ui.knowledgeStatus, 'FAQ guardada correctamente.');
    await refreshKnowledge();
  } catch (error) {
    setStatus(ui.knowledgeStatus, error.message, true);
  }
}

async function refreshKnowledge() {
  if (!state.tenantId || !state.botId) return;

  try {
    const items = await apiFetch(`/api/knowledge/${state.tenantId}/${state.botId}`);
    state.knowledge = Array.isArray(items) ? items : [];
    ui.knowledgeList.innerHTML = state.knowledge.length
      ? state.knowledge.map(item => `
          <div class="item">
            <strong>${escapeHtml(item.title)}</strong>
            <div class="muted">${escapeHtml(item.sourceType || 'faq')}</div>
            <div>${escapeHtml(item.content)}</div>
          </div>
        `).join('')
      : '<div class="item">Todavía no hay FAQs cargadas.</div>';
  } catch (error) {
    ui.knowledgeList.innerHTML = '<div class="item">No se pudieron cargar las FAQs.</div>';
  }
}

async function refreshOps() {
  if (!state.botId) return;

  try {
    const leads = await apiFetch(`/api/leads/${state.botId}`);
    state.leads = Array.isArray(leads) ? leads : [];
    ui.leadsList.innerHTML = state.leads.length
      ? state.leads.map(item => `
          <div class="item">
            <strong>${escapeHtml(item.name)}</strong><br>
            <span class="muted">${escapeHtml(item.email)}</span><br>
            <span>${escapeHtml(item.interest || 'Sin interés')}</span>
          </div>
        `).join('')
      : '<div class="item">No hay leads todavía.</div>';
  } catch (error) {
    ui.leadsList.innerHTML = '<div class="item">No se pudieron cargar los leads.</div>';
  }

  try {
    const conversations = await apiFetch(`/api/conversations/${state.botId}`);
    state.conversations = Array.isArray(conversations) ? conversations : [];
    ui.conversationsList.innerHTML = state.conversations.length
      ? state.conversations.map(item => `
          <div class="item">
            <strong>${escapeHtml(item.id)}</strong><br>
            <span class="muted">Estado: ${escapeHtml(item.status || 'OPEN')}</span>
          </div>
        `).join('')
      : '<div class="item">No hay conversaciones todavía.</div>';
  } catch (error) {
    ui.conversationsList.innerHTML = '<div class="item">No se pudieron cargar las conversaciones.</div>';
  }
}

async function refreshAll() {
  await refreshKnowledge();
  await refreshOps();
}

function escapeHtml(value) {
  return String(value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

ui.registerForm.addEventListener('submit', registerCompany);
ui.loginForm.addEventListener('submit', login);
ui.botForm.addEventListener('submit', createBot);
ui.configForm.addEventListener('submit', saveConfig);
ui.knowledgeForm.addEventListener('submit', saveKnowledge);
ui.logoutBtn.addEventListener('click', logout);

setLoggedInUI(false);
