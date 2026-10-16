const state = {
  apiBase: document.querySelector('meta[name="atendia-api-url"]')?.content || window.location.origin,
  tenantId: '',
  botId: '',
  botPublicKey: '',
  botName: '',
  bots: [],
  knowledge: [],
  conversations: [],
  leads: [],
  editingKnowledgeId: '',
  activeConversationId: ''
};

const ui = {
  authPanel: document.getElementById('authPanel'),
  botSelect: document.getElementById('botSelect'),
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
  conversationsList: document.getElementById('conversationsList'),
  conversationDetail: document.getElementById('conversationDetail'),
  conversationTitle: document.getElementById('conversationTitle'),
  conversationMessages: document.getElementById('conversationMessages'),
  handoffBtn: document.getElementById('handoffBtn'),
  resolveBtn: document.getElementById('resolveBtn')
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
  ui.authPanel.classList.toggle('hidden', isLoggedIn);
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
    setLoggedInUI(true);
    await refreshAll();
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
    if (state.editingKnowledgeId) {
      await apiFetch(`/api/knowledge/${state.botId}/${state.editingKnowledgeId}`, {
        method: 'PUT',
        body: JSON.stringify(payload)
      });
      state.editingKnowledgeId = '';
      ui.knowledgeForm.querySelector('button[type="submit"]').textContent = 'Guardar FAQ';
    } else {
      await apiFetch('/api/knowledge', {
        method: 'POST',
        body: JSON.stringify(payload)
      });
    }
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
            <div class="actions">
              <button class="ghost" type="button" data-action="edit" data-id="${escapeHtml(item.id)}">Editar</button>
              <button class="secondary" type="button" data-action="delete" data-id="${escapeHtml(item.id)}">Eliminar</button>
            </div>
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
            <div class="actions">
              <button class="ghost" type="button" data-action="view" data-id="${escapeHtml(item.id)}">Ver mensajes</button>
              ${item.status === 'RESOLVED' ? '' : `<button class="secondary" type="button" data-action="handoff" data-id="${escapeHtml(item.id)}">Escalar</button>`}
            </div>
          </div>
        `).join('')
      : '<div class="item">No hay conversaciones todavía.</div>';
  } catch (error) {
    ui.conversationsList.innerHTML = '<div class="item">No se pudieron cargar las conversaciones.</div>';
  }
}

async function refreshAll() {
  if (!state.tenantId) return;

  try {
    state.bots = await apiFetch('/api/bots');
    const currentBot = state.bots.find(bot => bot.id === state.botId) || state.bots[0];
    ui.botSelect.innerHTML = state.bots.length
      ? state.bots.map(bot => `<option value="${escapeHtml(bot.id)}">${escapeHtml(bot.name)}</option>`).join('')
      : '<option value="">Crea un bot para empezar</option>';

    if (!currentBot) {
      state.botId = '';
      state.botPublicKey = '';
      state.botName = '';
      state.knowledge = [];
      state.conversations = [];
      state.leads = [];
      ui.knowledgeList.innerHTML = '';
      ui.conversationsList.innerHTML = '';
      ui.leadsList.innerHTML = '';
      ui.conversationDetail.classList.add('hidden');
      ui.widgetSnippet.textContent = '';
      return;
    }

    state.botId = currentBot.id;
    state.botPublicKey = currentBot.publicKey;
    state.botName = currentBot.name;
    ui.botSelect.value = state.botId;
    updateWidgetSnippet();
    await Promise.all([refreshConfiguration(), refreshKnowledge(), refreshOps()]);
  } catch (error) {
    setStatus(ui.botStatus, error.message, true);
  }
}

async function refreshConfiguration() {
  if (!state.botId) return;

  try {
    const configuration = await apiFetch(`/api/bots/${state.botId}/configuration`);
    for (const field of ['businessName', 'sector', 'contactPhone', 'contactEmail', 'website', 'handOffChannel']) {
      ui.configForm.elements.namedItem(field).value = configuration[field] || '';
    }
  } catch (error) {
    setStatus(ui.botStatus, error.message, true);
  }
}

function updateWidgetSnippet() {
  if (!state.botPublicKey) {
    ui.widgetSnippet.textContent = '';
    return;
  }

  const scriptUrl = new URL('widget.js', window.location.href).href;
  ui.widgetSnippet.textContent = `<script src="${escapeHtml(scriptUrl)}" data-bot-key="${escapeHtml(state.botPublicKey)}" data-api-url="${escapeHtml(state.apiBase)}" data-title="${escapeHtml(state.botName)}"></script>`;
}

async function restoreSession() {
  try {
    const session = await apiFetch('/api/auth/session');
    state.tenantId = session.tenantId;
    setLoggedInUI(true);
    await refreshAll();
  } catch {
    setLoggedInUI(false);
  }
}

async function openConversation(conversationId) {
  if (!state.botId) return;

  try {
    const messages = await apiFetch(`/api/conversations/${state.botId}/${conversationId}/messages`);
    const conversation = state.conversations.find(item => item.id === conversationId);
    state.activeConversationId = conversationId;
    ui.conversationTitle.textContent = `Conversación ${conversationId} · ${conversation?.status || 'OPEN'}`;
    ui.conversationMessages.innerHTML = messages.map(message => `
      <div class="item">
        <strong>${escapeHtml(message.sender)}</strong>
        <div>${escapeHtml(message.content)}</div>
      </div>
    `).join('') || '<div class="item">Sin mensajes.</div>';
    ui.handoffBtn.classList.toggle('hidden', conversation?.status === 'HUMAN_HANDOFF' || conversation?.status === 'RESOLVED');
    ui.resolveBtn.classList.toggle('hidden', conversation?.status === 'RESOLVED');
    ui.conversationDetail.classList.remove('hidden');
  } catch (error) {
    setStatus(ui.botStatus, error.message, true);
  }
}

async function updateConversationStatus(action, conversationId) {
  if (!conversationId || !state.botId) return;

  try {
    await apiFetch(`/api/conversations/${conversationId}/${action}`, {
      method: 'POST',
      body: JSON.stringify({ botId: state.botId })
    });
    await refreshAll();
    await openConversation(conversationId);
  } catch (error) {
    setStatus(ui.botStatus, error.message, true);
  }
}

async function handleKnowledgeAction(event) {
  const button = event.target.closest('button[data-action]');
  if (!button) return;

  const item = state.knowledge.find(knowledge => knowledge.id === button.dataset.id);
  if (!item) return;

  if (button.dataset.action === 'edit') {
    state.editingKnowledgeId = item.id;
    ui.knowledgeForm.elements.namedItem('title').value = item.title;
    ui.knowledgeForm.elements.namedItem('content').value = item.content;
    ui.knowledgeForm.querySelector('button[type="submit"]').textContent = 'Guardar cambios';
    return;
  }

  if (button.dataset.action === 'delete' && window.confirm(`¿Eliminar la FAQ "${item.title}"?`)) {
    try {
      await apiFetch(`/api/knowledge/${state.botId}/${item.id}`, { method: 'DELETE' });
      await refreshKnowledge();
    } catch (error) {
      setStatus(ui.knowledgeStatus, error.message, true);
    }
  }
}

async function handleConversationListAction(event) {
  const button = event.target.closest('button[data-action]');
  if (!button) return;

  if (button.dataset.action === 'view') {
    await openConversation(button.dataset.id);
  } else if (button.dataset.action === 'handoff') {
    await updateConversationStatus('handoff', button.dataset.id);
  }
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
ui.botSelect.addEventListener('change', () => {
  state.botId = ui.botSelect.value;
  state.activeConversationId = '';
  ui.conversationDetail.classList.add('hidden');
  refreshAll();
});
ui.knowledgeList.addEventListener('click', handleKnowledgeAction);
ui.conversationsList.addEventListener('click', handleConversationListAction);
ui.handoffBtn.addEventListener('click', () => updateConversationStatus('handoff', state.activeConversationId));
ui.resolveBtn.addEventListener('click', () => updateConversationStatus('resolve', state.activeConversationId));
ui.logoutBtn.addEventListener('click', logout);

setLoggedInUI(false);
restoreSession();
