// Componentes de interface reutilizados pelas telas da aplicação.

import { estado, filtrosAtivos, limparFiltros, salvarFiltros } from './estado.js';
import {
  barraProgresso, esc, formatarPrazo, icone, PRIORIDADES, seloAtrasada, seloPrioridade, seloStatus, STATUS
} from './ui.js';

export const carregando = () => '<div class="carregando-bloco" aria-label="Carregando"><div class="spinner"></div></div>';

/** Estado vazio com orientação ao usuário (RNF16). */
export function vazio({ titulo, texto, icone: nomeIcone = 'caixa', acao = '' }) {
  return `<div class="vazio">${icone(nomeIcone)}<strong>${esc(titulo)}</strong><p>${esc(texto)}</p>${acao}</div>`;
}

export function vazioTarefas({ comFiltro }) {
  return comFiltro
    ? vazio({
      titulo: 'Nenhuma tarefa corresponde aos filtros',
      texto: 'Ajuste a busca ou os filtros para ver mais tarefas.',
      icone: 'busca',
      acao: '<button type="button" class="btn btn-secundario" data-limpar-filtros>Limpar filtros</button>'
    })
    : vazio({
      titulo: 'Nenhuma tarefa por aqui',
      texto: 'Crie sua primeira tarefa e comece a organizar o seu dia.',
      acao: `<button type="button" class="btn btn-primario" data-acao-nova>${icone('mais')}Nova tarefa</button>`
    });
}

export function erroCarregamento(erro) {
  return vazio({
    titulo: 'Não foi possível carregar os dados',
    texto: erro?.message || 'Tente novamente em instantes.',
    icone: 'alerta',
    acao: '<button type="button" class="btn btn-secundario" data-tentar-novamente>Tentar novamente</button>'
  });
}

/** Prazo com destaque quando a tarefa está atrasada (RN14, RF37). */
export function prazoTarefa(tarefa) {
  if (!tarefa.prazo) return '';
  return `<span class="prazo${tarefa.atrasada ? ' atrasado' : ''}">${icone('calendario')}${esc(formatarPrazo(tarefa.prazo))}</span>`;
}

/** Linha de tarefa usada na lista, no painel e no calendário (visão de dia). */
export function linhaTarefa(tarefa) {
  const concluida = tarefa.status === 'Concluida';
  return `
    <div class="linha-tarefa${concluida ? ' concluida' : ''}" style="--cor:${esc(tarefa.prioridadeCor)}" data-tarefa="${esc(tarefa.id)}" role="button" tabindex="0">
      <span class="faixa" aria-hidden="true"></span>
      <div style="min-width:0">
        <div class="titulo">${esc(tarefa.titulo)}</div>
        <div class="meta">
          ${seloPrioridade(tarefa)}
          ${seloAtrasada(tarefa)}
          ${tarefa.area ? `<span class="area">${icone('areas')}${esc(tarefa.area.nome)}</span>` : ''}
          ${prazoTarefa(tarefa)}
        </div>
      </div>
      <div class="lado">
        ${barraProgresso(tarefa.progresso)}
        ${seloStatus(tarefa)}
      </div>
    </div>`;
}

/** Abre a tarefa ao clicar ou ao pressionar Enter em um elemento com data-tarefa. */
export function ligarAberturaDeTarefas(raiz, abrir) {
  raiz.addEventListener('click', (e) => {
    const alvo = e.target.closest('[data-tarefa]');
    if (alvo && !e.target.closest('[data-sem-abrir]')) abrir(alvo.dataset.tarefa);
  });
  raiz.addEventListener('keydown', (e) => {
    if (e.key !== 'Enter' && e.key !== ' ') return;
    const alvo = e.target.closest('[data-tarefa]');
    if (alvo && e.target === alvo) {
      e.preventDefault();
      abrir(alvo.dataset.tarefa);
    }
  });
}

/* ---------------- Busca e filtros (RF40, RF41, RN23) ---------------- */

export function barraFiltros({ semPeriodo = false } = {}) {
  const f = estado.filtros;
  const opcoes = (lista, atual) => lista.map((o) =>
    `<option value="${esc(o.valor)}"${o.valor === atual ? ' selected' : ''}>${esc(o.rotulo)}</option>`).join('');

  return `
    <div class="filtros" role="search">
      <div class="busca">
        ${icone('busca')}
        <label class="sr" for="filtro-busca">Buscar tarefas</label>
        <input class="entrada" type="search" id="filtro-busca" name="busca" placeholder="Buscar no título ou na descrição" value="${esc(f.busca)}" maxlength="100">
      </div>
      <label class="sr" for="filtro-area">Área</label>
      <select class="entrada" id="filtro-area" name="areaId">
        <option value="">Área: todas</option>
        ${opcoes(estado.areas.map((a) => ({ valor: a.id, rotulo: a.nome })), f.areaId)}
      </select>
      <label class="sr" for="filtro-prioridade">Prioridade</label>
      <select class="entrada" id="filtro-prioridade" name="prioridade">
        <option value="">Prioridade: todas</option>
        ${opcoes(PRIORIDADES, f.prioridade)}
      </select>
      <label class="sr" for="filtro-status">Status</label>
      <select class="entrada" id="filtro-status" name="status">
        <option value="">Status: todos</option>
        ${opcoes(STATUS, f.status)}
      </select>
      ${semPeriodo ? '' : `
      <div class="periodo">
        <label class="sr" for="filtro-de">Prazo a partir de</label>
        <input class="entrada" type="date" id="filtro-de" name="prazoDe" value="${esc(f.prazoDe)}" title="Prazo a partir de">
        <span>até</span>
        <label class="sr" for="filtro-ate">Prazo até</label>
        <input class="entrada" type="date" id="filtro-ate" name="prazoAte" value="${esc(f.prazoAte)}" title="Prazo até">
      </div>`}
      <button type="button" class="btn btn-fantasma${filtrosAtivos({ ignorarPeriodo: semPeriodo }) ? '' : ' oculto'}" data-limpar-filtros>Limpar</button>
    </div>`;
}

/** Liga os campos de filtro; a busca por texto espera o usuário parar de digitar. */
export function ligarFiltros(raiz, aoMudar) {
  let espera;
  const botaoLimpar = raiz.querySelector('.filtros [data-limpar-filtros]');
  const atualizarBotao = () => botaoLimpar?.classList.toggle('oculto', !filtrosAtivos({ ignorarPeriodo: !raiz.querySelector('[name=prazoDe]') }));

  raiz.querySelectorAll('.filtros [name]').forEach((campo) => {
    const evento = campo.name === 'busca' ? 'input' : 'change';
    campo.addEventListener(evento, () => {
      estado.filtros[campo.name] = campo.value;
      salvarFiltros();
      atualizarBotao();
      clearTimeout(espera);
      espera = setTimeout(aoMudar, campo.name === 'busca' ? 300 : 0);
    });
  });

  raiz.addEventListener('click', (e) => {
    if (!e.target.closest('[data-limpar-filtros]')) return;
    limparFiltros();
    raiz.querySelectorAll('.filtros [name]').forEach((campo) => { campo.value = ''; });
    atualizarBotao();
    aoMudar();
  });
}
