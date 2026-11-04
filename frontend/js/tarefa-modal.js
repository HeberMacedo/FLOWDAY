// Modal de tarefa: criação (UC04), edição, mudança de status, checklist (UC05) e exclusão.

import { api } from './api.js';
import { areaGeral, estado } from './estado.js';
import {
  abrirModal, barraProgresso, comCarregamento, confirmar, esc, formatarDataHora, hojeISO, icone,
  limparErros, marcarErro, mostrarErros, PRIORIDADES, seloAtrasada, STATUS, toast, toastErro
} from './ui.js';

const LIMITE_ITENS = 50;
let sequencia = 0;

/* ---------------- Campos comuns ---------------- */

function camposTarefa(tarefa, { criando }) {
  const id = `t${++sequencia}`;
  const prioridadeAtual = tarefa.prioridade || 'Media';
  const areaAtual = tarefa.area?.id || tarefa.areaId || areaGeral()?.id || '';

  return `
    <div data-alerta></div>
    <div class="campo">
      <label for="${id}-titulo">Título</label>
      <input class="entrada" id="${id}-titulo" name="titulo" maxlength="120" value="${esc(tarefa.titulo || '')}" placeholder="O que precisa ser feito?" ${criando ? 'autofocus' : ''}>
      <span class="erro-campo" data-erro="titulo"></span>
    </div>
    <div class="campo">
      <label for="${id}-descricao">Descrição <span class="dica">(opcional)</span></label>
      <textarea class="entrada" id="${id}-descricao" name="descricao" maxlength="1000" placeholder="Detalhes, links ou observações">${esc(tarefa.descricao || '')}</textarea>
      <span class="erro-campo" data-erro="descricao"></span>
    </div>
    <div class="linha-campos">
      <div class="campo">
        <label for="${id}-area">Área</label>
        <select class="entrada" id="${id}-area" name="areaId">
          ${estado.areas.map((a) => `<option value="${esc(a.id)}"${a.id === areaAtual ? ' selected' : ''}>${esc(a.nome)}</option>`).join('')}
        </select>
        <span class="erro-campo" data-erro="areaId"></span>
      </div>
      <div class="campo">
        <label for="${id}-prazo">Prazo <span class="dica">(opcional)</span></label>
        <input class="entrada" type="date" id="${id}-prazo" name="prazo" value="${esc(tarefa.prazo || '')}" ${criando ? `min="${hojeISO()}"` : ''}>
        <span class="erro-campo" data-erro="prazo"></span>
      </div>
    </div>
    <div class="campo">
      <span class="rotulo" id="${id}-rot-prioridade">Prioridade</span>
      <div class="seletor-prioridade" role="radiogroup" aria-labelledby="${id}-rot-prioridade">
        ${PRIORIDADES.map((p) => `
          <input type="radio" id="${id}-p-${p.valor}" name="prioridade" value="${p.valor}"${p.valor === prioridadeAtual ? ' checked' : ''}>
          <label for="${id}-p-${p.valor}" style="--cor:${p.cor}">${esc(p.rotulo)}</label>`).join('')}
      </div>
    </div>`;
}

function lerCampos(form) {
  return {
    titulo: form.titulo.value.trim(),
    descricao: form.descricao.value.trim() || null,
    areaId: form.areaId.value || null,
    prioridade: form.querySelector('[name=prioridade]:checked')?.value || 'Media',
    prazo: form.prazo.value || null
  };
}

/** Validações do formulário espelhando a RN07 e a RN13; a API valida de novo. */
function validarCampos(form, dados, { prazoOriginal } = {}) {
  let valido = true;
  if (dados.titulo.length < 3 || dados.titulo.length > 120) {
    marcarErro(form, 'titulo', 'O título deve ter de 3 a 120 caracteres.');
    valido = false;
  }
  if (dados.prazo && dados.prazo !== prazoOriginal && dados.prazo < hojeISO()) {
    marcarErro(form, 'prazo', 'O prazo não pode ser anterior à data atual.');
    valido = false;
  }
  if (!valido) form.querySelector('.invalida')?.focus();
  return valido;
}

/* ---------------- Nova tarefa ---------------- */

/**
 * @param {{prazo?: string, areaId?: string}} padrao  Valores iniciais (ex.: dia escolhido no calendário).
 */
export function abrirNovaTarefa(padrao = {}, { aoAlterar } = {}) {
  const rascunho = [];
  const modal = abrirModal({
    titulo: 'Nova tarefa',
    conteudo: `
      <form novalidate id="form-nova-tarefa">
        ${camposTarefa({ prazo: padrao.prazo, areaId: padrao.areaId }, { criando: true })}
        <div class="campo checklist">
          <span class="rotulo">Checklist <span class="dica">(opcional)</span></span>
          <div class="novo-item">
            <input class="entrada" name="novoItem" maxlength="200" placeholder="Adicionar item e pressionar Enter">
            <button type="button" class="btn btn-secundario" data-add-item>${icone('mais')}<span class="sr">Adicionar item</span></button>
          </div>
          <span class="erro-campo" data-erro="texto"></span>
          <ul class="itens-rascunho"></ul>
        </div>
      </form>`,
    rodape: `<button type="button" class="btn btn-secundario" data-fechar>Cancelar</button>
             <button type="submit" form="form-nova-tarefa" class="btn btn-primario">Criar tarefa</button>`
  });

  const form = modal.el.querySelector('form');
  const listaRascunho = form.querySelector('.itens-rascunho');

  const desenharRascunho = () => {
    listaRascunho.innerHTML = rascunho.map((texto, i) => `
      <li><span>${esc(texto)}</span>
        <button type="button" class="btn btn-fantasma btn-icone btn-p" data-remover="${i}" aria-label="Remover item">${icone('fechar')}</button></li>`).join('');
  };

  const adicionarItem = () => {
    const entrada = form.novoItem;
    const texto = entrada.value.trim();
    form.querySelector('[data-erro="texto"]').textContent = '';
    if (!texto) return;
    if (rascunho.length >= LIMITE_ITENS) {
      marcarErro(form, 'texto', `A tarefa pode ter no máximo ${LIMITE_ITENS} itens.`);
      return;
    }
    rascunho.push(texto);
    entrada.value = '';
    desenharRascunho();
    entrada.focus();
  };

  form.novoItem.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      adicionarItem();
    }
  });
  form.querySelector('[data-add-item]').addEventListener('click', adicionarItem);
  listaRascunho.addEventListener('click', (e) => {
    const botao = e.target.closest('[data-remover]');
    if (!botao) return;
    rascunho.splice(Number(botao.dataset.remover), 1);
    desenharRascunho();
  });

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    limparErros(form);
    const dados = lerCampos(form);
    if (!validarCampos(form, dados)) return;

    // Item digitado e não adicionado também entra na tarefa.
    if (form.novoItem.value.trim()) adicionarItem();

    try {
      await comCarregamento(modal.el.querySelector('[type=submit]'), () =>
        api.tarefas.criar({ ...dados, itens: rascunho.length ? rascunho : null }));
      modal.fechar();
      toast('Tarefa criada.');
      aoAlterar?.();
    } catch (erro) {
      mostrarErros(form, erro);
    }
  });
}

/* ---------------- Tarefa existente ---------------- */

export async function abrirTarefa(id, { aoAlterar } = {}) {
  let tarefa;
  try {
    tarefa = await api.tarefas.obter(id);
  } catch (erro) {
    if (erro.status !== 401) toastErro(erro);
    if (erro.status === 404) aoAlterar?.();
    return;
  }

  let alterou = false;
  const modal = abrirModal({
    titulo: 'Detalhes da tarefa',
    conteudo: `
      <div class="status-tarefa" data-status></div>
      <form novalidate id="form-tarefa">${camposTarefa(tarefa, { criando: false })}</form>
      <div class="checklist" data-checklist></div>
      <div class="info-tarefa" data-info></div>`,
    rodape: `<button type="button" class="btn btn-fantasma esquerda" data-excluir style="color:var(--perigo)">${icone('lixeira')}Excluir</button>
             <button type="button" class="btn btn-secundario" data-fechar>Fechar</button>
             <button type="submit" form="form-tarefa" class="btn btn-primario">Salvar alterações</button>`,
    aoFechar: () => { if (alterou) aoAlterar?.(); }
  });

  const form = modal.el.querySelector('#form-tarefa');
  const caixaStatus = modal.el.querySelector('[data-status]');
  const caixaChecklist = modal.el.querySelector('[data-checklist]');
  const caixaInfo = modal.el.querySelector('[data-info]');

  /** Atualiza a parte da tela que depende do estado da tarefa. */
  const desenhar = () => {
    caixaStatus.innerHTML = `
      <div class="segmentado" role="group" aria-label="Status da tarefa">
        ${STATUS.map((s) => `<button type="button" data-novo-status="${s.valor}" class="${s.valor === tarefa.status ? 'ativo' : ''}" aria-pressed="${s.valor === tarefa.status}">${esc(s.rotulo)}</button>`).join('')}
      </div>
      ${seloAtrasada(tarefa)}`;

    const concluida = tarefa.status === 'Concluida';
    caixaChecklist.innerHTML = `
      <div class="cabeca">
        <span class="rotulo">Checklist</span>
        ${barraProgresso(tarefa.progresso) || '<span class="dica">Sem itens</span>'}
      </div>
      ${concluida ? `<div class="alerta alerta-info">${icone('info')}<span>Tarefa concluída. Reabra a tarefa para alterar o checklist.</span></div>` : ''}
      <ul>
        ${tarefa.itens.map((item) => `
          <li class="${item.concluido ? 'feito' : ''}" data-item="${esc(item.id)}">
            <input type="checkbox" ${item.concluido ? 'checked' : ''} ${concluida ? 'disabled' : ''} aria-label="Marcar item como concluído">
            <span class="texto" ${concluida ? '' : 'title="Clique para editar"'}>${esc(item.texto)}</span>
            ${concluida ? '' : `<button type="button" class="btn btn-fantasma btn-icone btn-p" data-remover-item aria-label="Excluir item">${icone('lixeira')}</button>`}
          </li>`).join('')}
      </ul>
      ${concluida ? '' : `
        <form class="novo-item" data-form-item novalidate>
          <input class="entrada" name="texto" maxlength="200" placeholder="Adicionar item ao checklist" ${tarefa.itens.length >= LIMITE_ITENS ? 'disabled' : ''}>
          <button type="submit" class="btn btn-secundario" ${tarefa.itens.length >= LIMITE_ITENS ? 'disabled' : ''}>${icone('mais')}Adicionar</button>
        </form>
        <span class="erro-campo" data-erro-item></span>`}`;

    caixaInfo.innerHTML = `
      <span>Criada em ${esc(formatarDataHora(tarefa.criadaEm))}</span>
      ${tarefa.concluidaEm ? `<span>Concluída em ${esc(formatarDataHora(tarefa.concluidaEm))}</span>` : ''}`;
  };

  /** Executa uma operação que devolve a tarefa atualizada. */
  const operar = async (acao, mensagem) => {
    try {
      tarefa = await acao();
      alterou = true;
      desenhar();
      if (mensagem) toast(mensagem);
      return true;
    } catch (erro) {
      if (erro.status !== 401) toastErro(erro);
      return false;
    }
  };

  desenhar();

  // Status (RN09, RN10, RN11): a API recusa concluir com item pendente.
  caixaStatus.addEventListener('click', (e) => {
    const botao = e.target.closest('[data-novo-status]');
    if (!botao || botao.dataset.novoStatus === tarefa.status) return;
    const rotulo = STATUS.find((s) => s.valor === botao.dataset.novoStatus).rotulo;
    operar(() => api.tarefas.alterarStatus(tarefa.id, botao.dataset.novoStatus), `Status alterado para ${rotulo}.`);
  });

  // Checklist (RN12, RN19)
  caixaChecklist.addEventListener('submit', async (e) => {
    if (!e.target.matches('[data-form-item]')) return;
    e.preventDefault();
    const entrada = e.target.texto;
    const texto = entrada.value.trim();
    const erroItem = caixaChecklist.querySelector('[data-erro-item]');
    erroItem.textContent = '';
    if (!texto) {
      erroItem.textContent = 'Digite o texto do item.';
      entrada.focus();
      return;
    }
    const ok = await operar(() => api.checklist.adicionar(tarefa.id, texto));
    if (ok) caixaChecklist.querySelector('[data-form-item] input')?.focus();
  });

  caixaChecklist.addEventListener('change', (e) => {
    if (e.target.type !== 'checkbox') return;
    const itemId = e.target.closest('[data-item]').dataset.item;
    operar(() => api.checklist.marcar(tarefa.id, itemId, e.target.checked));
  });

  caixaChecklist.addEventListener('click', async (e) => {
    const li = e.target.closest('[data-item]');
    if (!li || tarefa.status === 'Concluida') return;
    const itemId = li.dataset.item;

    if (e.target.closest('[data-remover-item]')) {
      operar(() => api.checklist.remover(tarefa.id, itemId), 'Item excluído.');
      return;
    }

    const span = e.target.closest('.texto');
    if (!span) return;
    const original = tarefa.itens.find((i) => i.id === itemId).texto;
    const entrada = document.createElement('input');
    entrada.className = 'entrada';
    entrada.maxLength = 200;
    entrada.value = original;
    span.replaceWith(entrada);
    entrada.focus();
    entrada.select();

    let finalizado = false;
    const finalizar = async (salvar) => {
      if (finalizado) return;
      finalizado = true;
      const texto = entrada.value.trim();
      if (!salvar || !texto || texto === original) {
        desenhar();
        return;
      }
      if (!(await operar(() => api.checklist.editar(tarefa.id, itemId, texto)))) desenhar();
    };
    entrada.addEventListener('keydown', (ev) => {
      if (ev.key === 'Enter') { ev.preventDefault(); finalizar(true); }
      if (ev.key === 'Escape') { ev.stopPropagation(); finalizar(false); }
    });
    entrada.addEventListener('blur', () => finalizar(true));
  });

  // Salvar campos (RF16). O prazo vencido pode ser mantido ao editar outros campos (RN13).
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    limparErros(form);
    const dados = lerCampos(form);
    if (!validarCampos(form, dados, { prazoOriginal: tarefa.prazo })) return;

    try {
      await comCarregamento(modal.el.querySelector('[type=submit][form=form-tarefa]'), () =>
        api.tarefas.atualizar(tarefa.id, dados));
      alterou = true;
      modal.fechar();
      toast('Tarefa atualizada.');
    } catch (erro) {
      mostrarErros(form, erro);
    }
  });

  // Exclusão com confirmação (RN21)
  modal.el.querySelector('[data-excluir]').addEventListener('click', async () => {
    const ok = await confirmar({
      titulo: 'Excluir tarefa?',
      mensagem: `"${tarefa.titulo}" e os itens do checklist serão excluídos. Essa ação não pode ser desfeita.`,
      botao: 'Excluir tarefa'
    });
    if (!ok) return;
    try {
      await api.tarefas.excluir(tarefa.id);
      alterou = true;
      modal.fechar();
      toast('Tarefa excluída.');
    } catch (erro) {
      if (erro.status !== 401) toastErro(erro);
    }
  });
}
