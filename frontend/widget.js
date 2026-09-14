(function () {
  'use strict';

  const script = document.currentScript;
  const botKey = script && script.dataset.botKey;
  const apiUrl = (script && script.dataset.apiUrl) || 'https://localhost:5001';
  const title = (script && script.dataset.title) || 'Atendia';

  if (!botKey) {
    console.error('Atendia Widget: data-bot-key es obligatorio.');
    return;
  }

  const host = document.createElement('div');
  host.setAttribute('aria-label', title);
  document.body.appendChild(host);
  const root = host.attachShadow({ mode: 'open' });
  let conversationId = null;
  let busy = false;

  root.innerHTML = `
    <style>
      :host { all: initial; }
      .launcher { position: fixed; right: 24px; bottom: 24px; z-index: 2147483647; width: 58px; height: 58px; border: 0; border-radius: 50%; background: #d95f39; color: #fff; cursor: pointer; box-shadow: 0 10px 28px rgba(47, 35, 29, .22); font: 700 22px Georgia, serif; }
      .panel { position: fixed; right: 24px; bottom: 94px; z-index: 2147483647; display: none; flex-direction: column; width: min(360px, calc(100vw - 32px)); height: min(560px, calc(100vh - 126px)); overflow: hidden; border: 1px solid #ead8c9; border-radius: 18px; background: #fffaf5; box-shadow: 0 18px 52px rgba(47, 35, 29, .2); color: #2f231d; font: 15px Arial, sans-serif; }
      .panel.open { display: flex; }
      .header { display: flex; align-items: center; justify-content: space-between; padding: 18px 18px 16px; background: #2f231d; color: #fffaf5; }
      .header strong { font: 700 20px Georgia, serif; }
      .close { border: 0; background: transparent; color: inherit; font-size: 24px; cursor: pointer; }
      .messages { display: flex; flex: 1; flex-direction: column; gap: 10px; overflow-y: auto; padding: 16px; }
      .message { max-width: 82%; padding: 10px 12px; border-radius: 14px; line-height: 1.4; white-space: pre-wrap; }
      .message.bot { align-self: flex-start; background: #f0e2d4; }
      .message.user { align-self: flex-end; background: #d95f39; color: #fff; }
      .message.error { align-self: center; background: #ffe5df; color: #8b2f1d; font-size: 13px; }
      .composer { display: flex; gap: 8px; padding: 12px; border-top: 1px solid #ead8c9; background: #fff; }
      .input { min-width: 0; flex: 1; padding: 11px 12px; border: 1px solid #d9c5b5; border-radius: 10px; color: #2f231d; font: inherit; }
      .send { width: 46px; border: 0; border-radius: 10px; background: #2f231d; color: #fff; cursor: pointer; font-size: 18px; }
      .send:disabled, .input:disabled { cursor: wait; opacity: .55; }
      @media (max-width: 480px) { .launcher { right: 16px; bottom: 16px; } .panel { right: 16px; bottom: 86px; } }
    </style>
    <button class="launcher" type="button" aria-label="Abrir chat">✦</button>
    <section class="panel" role="dialog" aria-label="Chat de atención">
      <header class="header"><strong>${escapeHtml(title)}</strong><button class="close" type="button" aria-label="Cerrar chat">×</button></header>
      <div class="messages" aria-live="polite"><div class="message bot">Hola. ¿En qué podemos ayudarte?</div></div>
      <form class="composer"><input class="input" autocomplete="off" placeholder="Escribe tu pregunta..." aria-label="Mensaje" /><button class="send" type="submit" aria-label="Enviar">↑</button></form>
    </section>
  `;

  const launcher = root.querySelector('.launcher');
  const panel = root.querySelector('.panel');
  const close = root.querySelector('.close');
  const messages = root.querySelector('.messages');
  const form = root.querySelector('.composer');
  const input = root.querySelector('.input');
  const send = root.querySelector('.send');

  launcher.addEventListener('click', () => {
    panel.classList.add('open');
    input.focus();
  });
  close.addEventListener('click', () => panel.classList.remove('open'));
  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    const message = input.value.trim();
    if (!message || busy) return;
    addMessage(message, 'user');
    input.value = '';
    setBusy(true);
    try {
      const response = await fetch(`${apiUrl.replace(/\/$/, '')}/api/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ botKey, message, conversationId })
      });
      if (!response.ok) throw new Error('No se pudo conectar con el asistente.');
      const data = await response.json();
      conversationId = data.conversationId || conversationId;
      addMessage(data.content || 'No recibimos una respuesta.', 'bot');
    } catch (error) {
      addMessage(error.message, 'error');
    } finally {
      setBusy(false);
    }
  });

  function addMessage(content, type) {
    const element = document.createElement('div');
    element.className = `message ${type}`;
    element.textContent = content;
    messages.appendChild(element);
    messages.scrollTop = messages.scrollHeight;
  }

  function setBusy(value) {
    busy = value;
    input.disabled = value;
    send.disabled = value;
    send.textContent = value ? '…' : '↑';
  }

  function escapeHtml(value) {
    return value.replace(/[&<>'"]/g, (character) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[character]));
  }
})();
