// Utilitários de interface: ícones, escape de HTML (proteção contra XSS, RNF07),
// mensagens (toasts), modais, confirmações, formulários e datas.

/* ---------------- Ícones (traço de 24x24) ---------------- */

const CAMINHOS = {
  logo: '<path d="M4 12.5 9 17.5 20 6.5" stroke-width="2.8"/>',
  painel: '<rect width="7" height="9" x="3" y="3" rx="1"/><rect width="7" height="5" x="14" y="3" rx="1"/><rect width="7" height="9" x="14" y="12" rx="1"/><rect width="7" height="5" x="3" y="16" rx="1"/>',
  lista: '<path d="m3 17 2 2 4-4"/><path d="m3 7 2 2 4-4"/><path d="M13 6h8"/><path d="M13 12h8"/><path d="M13 18h8"/>',
  kanban: '<rect width="18" height="18" x="3" y="3" rx="2"/><path d="M8 7v7"/><path d="M12 7v4"/><path d="M16 7v9"/>',
  calendario: '<rect width="18" height="18" x="3" y="4" rx="2"/><path d="M16 2v4"/><path d="M8 2v4"/><path d="M3 10h18"/>',
  areas: '<path d="M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z"/>',
  perfil: '<circle cx="12" cy="8" r="5"/><path d="M20 21a8 8 0 0 0-16 0"/>',
  mais: '<path d="M5 12h14"/><path d="M12 5v14"/>',
  busca: '<circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/>',
  sair: '<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><path d="m16 17 5-5-5-5"/><path d="M21 12H9"/>',
  olho: '<path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"/><circle cx="12" cy="12" r="3"/>',
  olhoFechado: '<path d="M9.88 9.88a3 3 0 1 0 4.24 4.24"/><path d="M10.73 5.08A10.43 10.43 0 0 1 12 5c7 0 10 7 10 7a13.16 13.16 0 0 1-1.67 2.68"/><path d="M6.61 6.61A13.53 13.53 0 0 0 2 12s3 7 10 7a9.74 9.74 0 0 0 5.39-1.61"/><path d="M2 2l20 20"/>',
  lapis: '<path d="M17 3a2.85 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z"/><path d="m15 5 4 4"/>',
  lixeira: '<path d="M3 6h18"/><path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"/><path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/><path d="M10 11v6"/><path d="M14 11v6"/>',
  fechar: '<path d="M18 6 6 18"/><path d="m6 6 12 12"/>',
  relogio: '<circle cx="12" cy="12" r="10"/><path d="M12 6v6l4 2"/>',
  alerta: '<circle cx="12" cy="12" r="10"/><path d="M12 8v4"/><path d="M12 16h.01"/>',
  triangulo: '<path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"/><path d="M12 9v4"/><path d="M12 17h.01"/>',
  sucesso: '<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><path d="M22 4 12 14.01l-3-3"/>',
  info: '<circle cx="12" cy="12" r="10"/><path d="M12 16v-4"/><path d="M12 8h.01"/>',
  esquerda: '<path d="m15 18-6-6 6-6"/>',
  direita: '<path d="m9 18 6-6-6-6"/>',
  opcoes: '<circle cx="12" cy="12" r="1"/><circle cx="12" cy="5" r="1"/><circle cx="12" cy="19" r="1"/>',
  circulo: '<circle cx="12" cy="12" r="10"/>',
  andamento: '<circle cx="12" cy="12" r="10"/><path d="m10 8 6 4-6 4Z"/>',
  check: '<path d="M20 6 9 17l-5-5"/>',
  email: '<rect width="20" height="16" x="2" y="4" rx="2"/><path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7"/>',
  cadeado: '<rect width="18" height="11" x="3" y="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>',
  raio: '<path d="M13 2 3 14h9l-1 8 10-12h-9l1-8z"/>',
  escudo: '<path d="M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z"/>',
  celular: '<rect width="14" height="20" x="5" y="2" rx="2"/><path d="M12 18h.01"/>',
  caixa: '<path d="M22 12h-6l-2 3h-4l-2-3H2"/><path d="M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z"/>',
  mover: '<path d="m5 9-3 3 3 3"/><path d="m9 5 3-3 3 3"/><path d="m15 19-3 3-3-3"/><path d="m19 9 3 3-3 3"/><path d="M2 12h20"/><path d="M12 2v20"/>'
};

export function icone(nome) {
  return `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${CAMINHOS[nome] || ''}</svg>`;
}

export function logo(comNome = true) {
  return `<span class="marca">${icone('logo')}</span>${comNome ? '<span class="nome">FlowDay</span>' : ''}`;
}

/* ---------------- Segurança de saída ---------------- */

const ESCAPES = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };

/** Escapa texto antes de inseri-lo em HTML (RNF07). */
export function esc(valor) {
  return String(valor ?? '').replace(/[&<>"']/g, (c) => ESCAPES[c]);
}

/* ---------------- Domínio: prioridade e status ---------------- */

export const PRIORIDADES = [
  { valor: 'Baixa', rotulo: 'Baixa', cor: '#22C55E' },
  { valor: 'Media', rotulo: 'Média', cor: '#EAB308' },
  { valor: 'Alta', rotulo: 'Alta', cor: '#F97316' },
  { valor: 'Urgente', rotulo: 'Urgente', cor: '#EF4444' }
];

export const STATUS = [
  { valor: 'AFazer', rotulo: 'A fazer', cor: '#6B7280', icone: 'circulo', classe: 'selo-afazer' },
  { valor: 'EmAndamento', rotulo: 'Em andamento', cor: '#2563EB', icone: 'andamento', classe: 'selo-andamento' },
  { valor: 'Concluida', rotulo: 'Concluída', cor: '#15803D', icone: 'sucesso', classe: 'selo-concluida' }
];

export const corPrioridade = (valor) => PRIORIDADES.find((p) => p.valor === valor)?.cor || '#6B7280';
export const rotuloStatus = (valor) => STATUS.find((s) => s.valor === valor)?.rotulo || valor;

/** Prioridade sempre com cor e rótulo escrito (RN15, RNF14). */
export function seloPrioridade(tarefa) {
  const cor = tarefa.prioridadeCor || corPrioridade(tarefa.prioridade);
  return `<span class="selo selo-prioridade" style="--cor:${esc(cor)}">${esc(tarefa.prioridadeRotulo)}</span>`;
}

export function seloStatus(tarefa) {
  const s = STATUS.find((x) => x.valor === tarefa.status) || STATUS[0];
  return `<span class="selo ${s.classe}">${icone(s.icone)}${esc(s.rotulo)}</span>`;
}

export function seloAtrasada(tarefa) {
  return tarefa.atrasada ? `<span class="selo selo-atrasada">${icone('triangulo')}Atrasada</span>` : '';
}

export function barraProgresso(progresso) {
  if (!progresso || progresso.total === 0) return '';
  const completo = progresso.concluidos === progresso.total ? ' completo' : '';
  return `<div class="progresso${completo}" title="${progresso.concluidos} de ${progresso.total} itens concluídos">
    <div class="barra"><span style="width:${progresso.percentual}%"></span></div>
    <span>${progresso.concluidos}/${progresso.total}</span>
  </div>`;
}

/* ---------------- Datas ---------------- */

const doisDigitos = (n) => String(n).padStart(2, '0');

/** Data local no formato AAAA-MM-DD. */
export function paraISO(data) {
  return `${data.getFullYear()}-${doisDigitos(data.getMonth() + 1)}-${doisDigitos(data.getDate())}`;
}

export function deISO(iso) {
  const [a, m, d] = iso.split('-').map(Number);
  return new Date(a, m - 1, d);
}

export const hojeISO = () => paraISO(new Date());

export function somarDias(data, dias) {
  const nova = new Date(data);
  nova.setDate(nova.getDate() + dias);
  return nova;
}

/** Segunda-feira da semana da data informada. */
export function inicioDaSemana(data) {
  const diasDesdeSegunda = (data.getDay() + 6) % 7;
  return somarDias(new Date(data.getFullYear(), data.getMonth(), data.getDate()), -diasDesdeSegunda);
}

const fmtCurta = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: 'short' });
const fmtCurtaAno = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' });
const fmtDataHora = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
const fmtData = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short' });

/** Prazo em texto amigável: Hoje, Amanhã, Ontem ou "12 de nov.". */
export function formatarPrazo(iso) {
  if (!iso) return '';
  const hoje = hojeISO();
  if (iso === hoje) return 'Hoje';
  if (iso === paraISO(somarDias(new Date(), 1))) return 'Amanhã';
  if (iso === paraISO(somarDias(new Date(), -1))) return 'Ontem';
  const data = deISO(iso);
  return (data.getFullYear() === new Date().getFullYear() ? fmtCurta : fmtCurtaAno).format(data);
}

export const formatarDataHora = (iso) => (iso ? fmtDataHora.format(new Date(iso)) : '');
export const formatarData = (iso) => (iso ? fmtData.format(new Date(iso)) : '');

/* ---------------- Toasts (RNF16) ---------------- */

function containerToasts() {
  let el = document.querySelector('.toasts');
  if (!el) {
    el = document.createElement('div');
    el.className = 'toasts';
    el.setAttribute('role', 'status');
    el.setAttribute('aria-live', 'polite');
    document.body.appendChild(el);
  }
  return el;
}

export function toast(texto, tipo = 'sucesso') {
  const el = document.createElement('div');
  el.className = `toast toast-${tipo}`;
  el.innerHTML = `${icone(tipo === 'erro' ? 'alerta' : tipo === 'info' ? 'info' : 'sucesso')}<span>${esc(texto)}</span>`;
  containerToasts().appendChild(el);
  setTimeout(() => {
    el.classList.add('saindo');
    setTimeout(() => el.remove(), 200);
  }, tipo === 'erro' ? 5200 : 3200);
}

export const toastErro = (erro) => toast(erro?.message || 'Ocorreu um erro inesperado.', 'erro');

/* ---------------- Modais ---------------- */

const pilhaModais = [];

document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape' && pilhaModais.length) pilhaModais[pilhaModais.length - 1].fechar();
});

/**
 * Abre um modal. Retorna { el, corpo, fechar }.
 * @param {{titulo: string, conteudo: string, rodape?: string, pequeno?: boolean, aoFechar?: Function}} opcoes
 */
export function abrirModal({ titulo, conteudo, rodape = '', pequeno = false, aoFechar }) {
  const fundo = document.createElement('div');
  fundo.className = 'modal-fundo';
  fundo.innerHTML = `
    <div class="modal${pequeno ? ' modal-p' : ''}" role="dialog" aria-modal="true" aria-labelledby="titulo-modal-${pilhaModais.length}">
      <div class="modal-cabecalho">
        <h2 id="titulo-modal-${pilhaModais.length}">${esc(titulo)}</h2>
        <button type="button" class="btn btn-fantasma btn-icone" data-fechar aria-label="Fechar">${icone('fechar')}</button>
      </div>
      <div class="modal-corpo">${conteudo}</div>
      ${rodape ? `<div class="modal-rodape">${rodape}</div>` : ''}
    </div>`;

  const focoAnterior = document.activeElement;
  let pressionouNoFundo = false;
  fundo.addEventListener('mousedown', (e) => { pressionouNoFundo = e.target === fundo; });
  fundo.addEventListener('click', (e) => {
    if (e.target === fundo && pressionouNoFundo) controle.fechar();
    if (e.target.closest('[data-fechar]')) controle.fechar();
  });

  const controle = {
    el: fundo,
    corpo: fundo.querySelector('.modal-corpo'),
    fechar() {
      const i = pilhaModais.indexOf(controle);
      if (i === -1) return;
      pilhaModais.splice(i, 1);
      fundo.remove();
      if (!pilhaModais.length) document.body.classList.remove('com-modal');
      focoAnterior?.focus?.();
      aoFechar?.();
    }
  };

  pilhaModais.push(controle);
  document.body.appendChild(fundo);
  document.body.classList.add('com-modal');
  setTimeout(() => fundo.querySelector('[autofocus], .modal-corpo input, .modal-corpo textarea, .modal-corpo select, .modal-rodape .btn')?.focus(), 30);
  return controle;
}

/** Confirmação para exclusões (RN21). Resolve true quando o usuário confirma. */
export function confirmar({ titulo, mensagem, botao = 'Confirmar', perigo = true }) {
  return new Promise((resolver) => {
    let confirmado = false;
    const modal = abrirModal({
      titulo,
      pequeno: true,
      conteudo: `<p>${esc(mensagem)}</p>`,
      rodape: `<button type="button" class="btn btn-secundario" data-fechar>Cancelar</button>
               <button type="button" class="btn ${perigo ? 'btn-perigo' : 'btn-primario'}" data-confirmar>${esc(botao)}</button>`,
      aoFechar: () => resolver(confirmado)
    });
    modal.el.querySelector('[data-confirmar]').addEventListener('click', () => {
      confirmado = true;
      modal.fechar();
    });
  });
}

/* ---------------- Formulários ---------------- */

export function limparErros(form) {
  form.querySelectorAll('.invalida').forEach((el) => el.classList.remove('invalida'));
  form.querySelectorAll('[data-erro]').forEach((el) => { el.textContent = ''; });
  const alerta = form.querySelector('[data-alerta]');
  if (alerta) alerta.innerHTML = '';
}

/** Mostra a mensagem geral e os erros de cada campo vindos da API (Problem Details). */
export function mostrarErros(form, erro) {
  limparErros(form);
  let algumCampo = false;
  for (const [campo, mensagem] of Object.entries(erro.erros || {})) {
    const entrada = form.querySelector(`[name="${CSS.escape(campo)}"]`);
    const alvo = form.querySelector(`[data-erro="${CSS.escape(campo)}"]`);
    if (entrada) entrada.classList.add('invalida');
    if (alvo) {
      alvo.textContent = mensagem;
      algumCampo = true;
    }
  }
  const alerta = form.querySelector('[data-alerta]');
  if (alerta && (!algumCampo || Object.keys(erro.erros || {}).length > 1)) {
    alerta.innerHTML = `<div class="alerta alerta-erro" role="alert">${icone('alerta')}<span>${esc(erro.message)}</span></div>`;
  } else if (!alerta && !algumCampo) {
    toastErro(erro);
  }
  form.querySelector('.invalida')?.focus();
}

export function marcarErro(form, campo, mensagem) {
  form.querySelector(`[name="${campo}"]`)?.classList.add('invalida');
  const alvo = form.querySelector(`[data-erro="${campo}"]`);
  if (alvo) alvo.textContent = mensagem;
}

/** Executa a ação mostrando o estado de carregamento no botão (RNF16). */
export async function comCarregamento(botao, acao) {
  botao?.classList.add('carregando');
  if (botao) botao.disabled = true;
  try {
    return await acao();
  } finally {
    botao?.classList.remove('carregando');
    if (botao) botao.disabled = false;
  }
}

/** Botão de mostrar/ocultar senha (RNF13). */
export function ativarVerSenha(raiz = document) {
  raiz.querySelectorAll('.senha-wrap').forEach((wrap) => {
    if (wrap.querySelector('.ver-senha')) return;
    const entrada = wrap.querySelector('input');
    const botao = document.createElement('button');
    botao.type = 'button';
    botao.className = 'ver-senha';
    botao.setAttribute('aria-label', 'Mostrar senha');
    botao.innerHTML = icone('olho');
    botao.addEventListener('click', () => {
      const mostrar = entrada.type === 'password';
      entrada.type = mostrar ? 'text' : 'password';
      botao.innerHTML = icone(mostrar ? 'olhoFechado' : 'olho');
      botao.setAttribute('aria-label', mostrar ? 'Ocultar senha' : 'Mostrar senha');
    });
    wrap.appendChild(botao);
  });
}

export function campoSenha({ nome, id = nome, rotulo, autocomplete = 'current-password', dica = '' }) {
  return `<div class="campo">
    <label for="${id}">${esc(rotulo)}</label>
    <div class="senha-wrap"><input class="entrada" type="password" id="${id}" name="${nome}" autocomplete="${autocomplete}" maxlength="128"></div>
    ${dica}
    <span class="erro-campo" data-erro="${nome}"></span>
  </div>`;
}

/* ---------------- Regras de validação espelhadas no cliente ---------------- */

export function validarSenha(senha) {
  if (!senha || senha.length < 8) return 'A senha deve ter no mínimo 8 caracteres.';
  if (!/\p{L}/u.test(senha) || !/\d/.test(senha)) return 'A senha deve ter ao menos uma letra e um número.';
  return null;
}

export function validarEmail(email) {
  if (!email?.trim()) return 'Informe o e-mail.';
  if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email.trim())) return 'Informe um e-mail válido.';
  return null;
}

export const iniciais = (nome) =>
  (nome || '?').trim().split(/\s+/).filter(Boolean).slice(0, 2).map((p) => p[0].toUpperCase()).join('');
