// Casca da aplicação autenticada: menu, rotas, sessão e abertura de tarefas.

import { api, consumirAviso, definirAviso, irParaLogin, limparSessao, obterSessao } from './api.js';
import { estado, recarregarAreas } from './estado.js';
import { erroCarregamento } from './componentes.js';
import { abrirNovaTarefa, abrirTarefa } from './tarefa-modal.js';
import { esc, icone, iniciais, logo, toast } from './ui.js';
import * as painel from './telas/painel.js';
import * as lista from './telas/lista.js';
import * as kanban from './telas/kanban.js';
import * as calendario from './telas/calendario.js';
import * as areas from './telas/areas.js';
import * as perfil from './telas/perfil.js';

/** Itens do menu (RNF15): Painel, Lista, Kanban, Calendário, Áreas e Perfil. */
const ROTAS = {
  painel: { titulo: 'Painel', curto: 'Painel', sub: 'Resumo do seu dia', icone: 'painel', tela: painel },
  lista: { titulo: 'Lista de tarefas', curto: 'Lista', sub: 'Todas as suas tarefas, com busca e filtros', icone: 'lista', tela: lista },
  kanban: { titulo: 'Kanban', curto: 'Kanban', sub: 'Arraste os cartões para mudar o status', icone: 'kanban', tela: kanban },
  calendario: { titulo: 'Calendário', curto: 'Agenda', sub: 'Tarefas organizadas pelo prazo', icone: 'calendario', tela: calendario },
  areas: { titulo: 'Áreas', curto: 'Áreas', sub: 'Agrupe as tarefas por contexto', icone: 'areas', tela: areas },
  perfil: { titulo: 'Perfil', curto: 'Perfil', sub: 'Seus dados, senha e conta', icone: 'perfil', tela: perfil }
};

const sessao = obterSessao();
if (!sessao) {
  irParaLogin('Faça login para continuar.');
} else {
  iniciar();
}

async function iniciar() {
  estado.usuario = sessao.usuario;
  montarLayout();
  agendarFimDaSessao();

  try {
    await recarregarAreas();
  } catch {
    // A tela atual mostra o erro e permite tentar de novo.
  }

  window.addEventListener('hashchange', renderizarRota);
  renderizarRota();

  const aviso = consumirAviso();
  if (aviso) toast(aviso.texto, aviso.tipo === 'erro' ? 'erro' : 'sucesso');
}

/* ---------------- Layout ---------------- */

function montarLayout() {
  document.querySelectorAll('[data-logo]').forEach((el) => { el.innerHTML = logo(); });

  const itens = Object.entries(ROTAS);
  document.querySelector('[data-menu]').innerHTML = itens.map(([rota, r]) =>
    `<a class="item-menu" href="#/${rota}" data-rota="${rota}">${icone(r.icone)}<span>${esc(r.titulo.split(' ')[0])}</span></a>`).join('');
  document.querySelector('[data-menu-inferior]').innerHTML = itens.map(([rota, r]) =>
    `<a href="#/${rota}" data-rota="${rota}">${icone(r.icone)}<span>${esc(r.curto)}</span></a>`).join('');

  document.querySelectorAll('[data-nova-tarefa]').forEach((b) => { b.innerHTML = `${icone('mais')}Nova tarefa`; });
  document.querySelector('[data-nova-tarefa-fab]').innerHTML = icone('mais');
  document.querySelector('[data-sair]').innerHTML = icone('sair');

  // Botão "Nova tarefa" sempre visível, em qualquer tela (RNF15, RF39).
  document.querySelectorAll('[data-nova-tarefa], [data-nova-tarefa-fab]').forEach((b) =>
    b.addEventListener('click', () => novaTarefa()));
  document.querySelector('[data-sair]').addEventListener('click', sair);

  atualizarUsuario(estado.usuario);
}

export function atualizarUsuario(usuario) {
  estado.usuario = usuario;
  document.querySelector('[data-avatar]').textContent = iniciais(usuario.nome);
  document.querySelector('[data-nome]').textContent = usuario.nome;
  document.querySelector('[data-email]').textContent = usuario.email;
}

/* ---------------- Rotas ---------------- */

let telaAtual = null;
let contextoAtual = null;

function rotaAtual() {
  const nome = window.location.hash.replace(/^#\/?/, '').split('?')[0];
  return ROTAS[nome] ? nome : 'painel';
}

async function renderizarRota() {
  const nome = rotaAtual();
  const rota = ROTAS[nome];
  if (!window.location.hash) history.replaceState(null, '', `#/${nome}`);

  document.querySelectorAll('[data-rota]').forEach((a) => {
    const ativo = a.dataset.rota === nome;
    a.classList.toggle('ativo', ativo);
    if (ativo) a.setAttribute('aria-current', 'page'); else a.removeAttribute('aria-current');
  });
  document.querySelector('[data-titulo]').textContent = rota.titulo;
  document.querySelector('[data-subtitulo]').textContent = rota.sub;
  document.title = `${rota.titulo} · FlowDay`;

  // Cada renderização usa um contêiner novo; respostas atrasadas de telas antigas não afetam a atual.
  const conteudo = document.querySelector('#conteudo');
  const el = document.createElement('div');
  conteudo.replaceChildren(el);
  window.scrollTo(0, 0);

  telaAtual = rota.tela;
  contextoAtual = {
    el,
    abrirTarefa: (id) => abrirTarefa(id, { aoAlterar: recarregarTela }),
    novaTarefa: (padrao) => novaTarefa(padrao),
    recarregar: recarregarTela,
    atualizarUsuario
  };

  el.addEventListener('click', (e) => {
    if (e.target.closest('[data-acao-nova]')) novaTarefa();
    if (e.target.closest('[data-tentar-novamente]')) renderizarRota();
  });

  try {
    if (!estado.areas.length) await recarregarAreas();
    await telaAtual.renderizar(contextoAtual);
  } catch (erro) {
    if (erro.status !== 401) el.innerHTML = erroCarregamento(erro);
  }
}

async function recarregarTela() {
  if (!telaAtual || !contextoAtual) return;
  try {
    await (telaAtual.atualizar || telaAtual.renderizar)(contextoAtual);
  } catch (erro) {
    if (erro.status !== 401) toast(erro.message, 'erro');
  }
}

function novaTarefa(padrao = {}) {
  abrirNovaTarefa(padrao, { aoAlterar: recarregarTela });
}

/* ---------------- Sessão (RN05) ---------------- */

function agendarFimDaSessao() {
  const restante = new Date(sessao.expiraEm) - new Date();
  setTimeout(() => irParaLogin('Sua sessão expirou. Faça login novamente.'), Math.max(restante, 0));
}

async function sair() {
  try {
    await api.auth.sair();
  } catch {
    // O logout acontece no aparelho mesmo se a API não responder.
  }
  limparSessao();
  definirAviso('info', 'Você saiu da sua conta.');
  window.location.replace('index.html');
}
