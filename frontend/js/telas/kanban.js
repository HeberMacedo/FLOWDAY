// Kanban (RF33, RF34): colunas por status, arrastar e soltar no computador e
// menu "Mover para" em telas de toque. Concluir com item pendente é recusado (RN10).

import { api } from '../api.js';
import { barraFiltros, carregando, ligarAberturaDeTarefas, ligarFiltros, vazioTarefas } from '../componentes.js';
import { consultaFiltros, filtrosAtivos } from '../estado.js';
import {
  barraProgresso, esc, formatarPrazo, icone, seloAtrasada, seloPrioridade, STATUS, toast, toastErro
} from '../ui.js';

let cliqueForaLigado = false;

export async function renderizar(ctx) {
  const { el } = ctx;
  el.innerHTML = `${barraFiltros()}<div data-quadro>${carregando()}</div>`;

  ligarFiltros(el, () => atualizar(ctx));
  ligarAberturaDeTarefas(el, ctx.abrirTarefa);
  ligarMenuMover(el, ctx);
  ligarArrastar(el, ctx);

  await atualizar(ctx);
}

export async function atualizar(ctx) {
  const quadro = ctx.el.querySelector('[data-quadro]');
  if (!quadro) return;
  const { colunas } = await api.tarefas.kanban(consultaFiltros());
  const total = colunas.reduce((soma, c) => soma + c.total, 0);

  if (total === 0 && filtrosAtivos()) {
    quadro.innerHTML = `<div class="cartao">${vazioTarefas({ comFiltro: true })}</div>`;
    return;
  }

  // Com filtro de status, apenas a coluna correspondente aparece (RN23).
  quadro.innerHTML = `
    <div class="kanban" style="--colunas:${colunas.length}">
      ${colunas.map((coluna) => {
        const s = STATUS.find((x) => x.valor === coluna.status);
        return `
        <section class="coluna" data-coluna="${esc(coluna.status)}" aria-label="${esc(coluna.rotulo)}">
          <header class="coluna-cabecalho">
            <span class="ponto" style="background:${s.cor}"></span>${esc(coluna.rotulo)}
            <span class="contador">${coluna.total}</span>
          </header>
          <div class="coluna-cartoes">
            ${coluna.tarefas.length
              ? coluna.tarefas.map(cartao).join('')
              : `<div class="vazio-coluna">${total === 0 && coluna.status === 'AFazer'
                ? 'Nenhuma tarefa ainda. Use “Nova tarefa” para começar.'
                : 'Solte um cartão aqui'}</div>`}
          </div>
        </section>`;
      }).join('')}
    </div>`;
}

function cartao(tarefa) {
  const concluida = tarefa.status === 'Concluida';
  return `
    <article class="cartao-tarefa${concluida ? ' concluida' : ''}${tarefa.atrasada ? ' atrasada' : ''}"
             style="--cor:${esc(tarefa.prioridadeCor)}" draggable="true"
             data-tarefa="${esc(tarefa.id)}" data-status="${esc(tarefa.status)}" tabindex="0" role="button"
             aria-label="${esc(tarefa.titulo)}">
      <div class="titulo">${esc(tarefa.titulo)}</div>
      <button type="button" class="btn btn-fantasma btn-icone btn-p mover" data-sem-abrir data-abrir-menu aria-label="Mover tarefa">${icone('opcoes')}</button>
      <div class="meta">
        ${seloPrioridade(tarefa)}
        ${seloAtrasada(tarefa)}
        ${tarefa.area ? `<span class="selo">${esc(tarefa.area.nome)}</span>` : ''}
        ${tarefa.prazo ? `<span class="selo${tarefa.atrasada ? ' selo-atrasada' : ''}">${icone('calendario')}${esc(formatarPrazo(tarefa.prazo))}</span>` : ''}
      </div>
      ${barraProgresso(tarefa.progresso)}
    </article>`;
}

async function moverTarefa(ctx, id, novoStatus) {
  try {
    await api.tarefas.alterarStatus(id, novoStatus);
    toast(`Tarefa movida para ${STATUS.find((s) => s.valor === novoStatus).rotulo}.`);
  } catch (erro) {
    // A tarefa permanece na coluna original e o motivo é exibido (CA22).
    if (erro.status !== 401) toastErro(erro);
  }
  await atualizar(ctx);
}

/* ---------------- Arrastar e soltar (mouse) ---------------- */

function ligarArrastar(el, ctx) {
  let arrastado = null;

  el.addEventListener('dragstart', (e) => {
    const card = e.target.closest('[data-tarefa]');
    if (!card) return;
    arrastado = card;
    card.classList.add('arrastando');
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', card.dataset.tarefa);
  });

  el.addEventListener('dragend', () => {
    arrastado?.classList.remove('arrastando');
    arrastado = null;
    el.querySelectorAll('.coluna.alvo').forEach((c) => c.classList.remove('alvo'));
  });

  el.addEventListener('dragover', (e) => {
    const coluna = e.target.closest('[data-coluna]');
    if (!coluna || !arrastado) return;
    e.preventDefault();
    el.querySelectorAll('.coluna.alvo').forEach((c) => c !== coluna && c.classList.remove('alvo'));
    coluna.classList.add('alvo');
  });

  el.addEventListener('dragleave', (e) => {
    const coluna = e.target.closest('[data-coluna]');
    if (coluna && !coluna.contains(e.relatedTarget)) coluna.classList.remove('alvo');
  });

  el.addEventListener('drop', (e) => {
    const coluna = e.target.closest('[data-coluna]');
    if (!coluna || !arrastado) return;
    e.preventDefault();
    coluna.classList.remove('alvo');
    const { tarefa, status } = arrastado.dataset;
    if (coluna.dataset.coluna !== status) {
      coluna.querySelector('.coluna-cartoes').prepend(arrastado);
      moverTarefa(ctx, tarefa, coluna.dataset.coluna);
    }
  });
}

/* ---------------- Menu "Mover para" (toque e teclado) ---------------- */

function ligarMenuMover(el, ctx) {
  const fecharMenus = () => document.querySelectorAll('.menu-mover').forEach((m) => m.remove());

  if (!cliqueForaLigado) {
    cliqueForaLigado = true;
    document.addEventListener('click', (e) => {
      if (!e.target.closest('.menu-mover') && !e.target.closest('[data-abrir-menu]')) fecharMenus();
    });
  }

  el.addEventListener('click', (e) => {
    const abrir = e.target.closest('[data-abrir-menu]');
    if (abrir) {
      const card = abrir.closest('[data-tarefa]');
      const jaAberto = card.querySelector('.menu-mover');
      fecharMenus();
      if (jaAberto) return;
      const menu = document.createElement('div');
      menu.className = 'menu-mover';
      menu.setAttribute('data-sem-abrir', '');
      menu.innerHTML = `<div class="titulo-menu">Mover para</div>
        ${STATUS.filter((s) => s.valor !== card.dataset.status)
          .map((s) => `<button type="button" data-mover-para="${s.valor}">${esc(s.rotulo)}</button>`).join('')}
        <div class="titulo-menu">Tarefa</div>
        <button type="button" data-abrir-detalhes>Abrir detalhes</button>`;
      card.appendChild(menu);
      menu.querySelector('button')?.focus();
      return;
    }

    const destino = e.target.closest('[data-mover-para]');
    if (destino) {
      const card = destino.closest('[data-tarefa]');
      fecharMenus();
      moverTarefa(ctx, card.dataset.tarefa, destino.dataset.moverPara);
      return;
    }

    const detalhes = e.target.closest('[data-abrir-detalhes]');
    if (detalhes) {
      const id = detalhes.closest('[data-tarefa]').dataset.tarefa;
      fecharMenus();
      ctx.abrirTarefa(id);
    }
  });
}
