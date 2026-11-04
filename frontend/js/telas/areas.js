// Áreas (UC06, RF28 a RF31): criar, listar, renomear e excluir (exceto a Geral).

import { api } from '../api.js';
import { carregando } from '../componentes.js';
import { estado, recarregarAreas } from '../estado.js';
import {
  abrirModal, comCarregamento, confirmar, esc, icone, limparErros, marcarErro, mostrarErros, toast, toastErro
} from '../ui.js';

const validarNome = (nome) => (nome.length < 1 || nome.length > 50 ? 'O nome da área deve ter de 1 a 50 caracteres.' : null);

export async function renderizar(ctx) {
  const { el } = ctx;
  el.innerHTML = `
    <section class="cartao cartao-corpo" style="margin-bottom:20px">
      <h3 class="cartao-titulo">Nova área</h3>
      <p class="cartao-sub">Separe as tarefas por contexto, como Academia, TCC ou Casa.</p>
      <form class="form-inline" novalidate data-form-area>
        <div class="campo">
          <label class="sr" for="nova-area">Nome da área</label>
          <input class="entrada" id="nova-area" name="nome" maxlength="50" placeholder="Nome da área">
          <span class="erro-campo" data-erro="nome"></span>
        </div>
        <button type="submit" class="btn btn-primario">${icone('mais')}Criar área</button>
      </form>
    </section>
    <div data-areas>${carregando()}</div>`;

  const form = el.querySelector('[data-form-area]');
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    limparErros(form);
    const nome = form.nome.value.trim();
    const erro = validarNome(nome);
    if (erro) {
      marcarErro(form, 'nome', erro);
      return;
    }
    try {
      await comCarregamento(form.querySelector('[type=submit]'), () => api.areas.criar({ nome }));
      form.reset();
      toast(`Área "${nome}" criada.`);
      await atualizar(ctx);
    } catch (falha) {
      mostrarErros(form, falha);
    }
  });

  el.addEventListener('click', (e) => {
    const renomear = e.target.closest('[data-renomear]');
    if (renomear) abrirRenomear(ctx, renomear.dataset.renomear);
    const excluir = e.target.closest('[data-excluir-area]');
    if (excluir) excluirArea(ctx, excluir.dataset.excluirArea);
  });

  await atualizar(ctx);
}

export async function atualizar(ctx) {
  const alvo = ctx.el.querySelector('[data-areas]');
  if (!alvo) return;
  const areas = await recarregarAreas();

  // Quantidade de tarefas por área (consulta de uma linha por área).
  const totais = await Promise.all(areas.map((a) =>
    api.tarefas.listar({ areaId: a.id, tamanho: 1 }).then((r) => r.total).catch(() => null)));

  alvo.innerHTML = `
    <div class="secao-titulo"><h3>Suas áreas</h3><span class="cartao-sub" style="margin:0">${areas.length} áreas</span></div>
    <div class="grade-areas">
      ${areas.map((area, i) => `
        <div class="cartao area-item">
          <span class="ic">${icone('areas')}</span>
          <div class="info">
            <div class="nome">${esc(area.nome)}</div>
            <small>${totais[i] === null ? '' : `${totais[i]} ${totais[i] === 1 ? 'tarefa' : 'tarefas'}`}${area.ehGeral ? ' · área padrão' : ''}</small>
          </div>
          <div class="acoes">
            ${area.ehGeral
              ? `<span class="selo" title="A área Geral não pode ser renomeada nem excluída">${icone('cadeado')}Fixa</span>`
              : `<button type="button" class="btn btn-fantasma btn-icone" data-renomear="${esc(area.id)}" aria-label="Renomear ${esc(area.nome)}" title="Renomear">${icone('lapis')}</button>
                 <button type="button" class="btn btn-fantasma btn-icone" data-excluir-area="${esc(area.id)}" aria-label="Excluir ${esc(area.nome)}" title="Excluir" style="color:var(--perigo)">${icone('lixeira')}</button>`}
          </div>
        </div>`).join('')}
    </div>`;
}

function abrirRenomear(ctx, id) {
  const area = estado.areas.find((a) => a.id === id);
  if (!area) return;
  const modal = abrirModal({
    titulo: 'Renomear área',
    pequeno: true,
    conteudo: `
      <form novalidate id="form-renomear">
        <div data-alerta></div>
        <div class="campo">
          <label for="nome-area">Nome</label>
          <input class="entrada" id="nome-area" name="nome" maxlength="50" value="${esc(area.nome)}" autofocus>
          <span class="erro-campo" data-erro="nome"></span>
        </div>
      </form>`,
    rodape: `<button type="button" class="btn btn-secundario" data-fechar>Cancelar</button>
             <button type="submit" form="form-renomear" class="btn btn-primario">Salvar</button>`
  });

  const form = modal.el.querySelector('form');
  form.nome.select();
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    limparErros(form);
    const nome = form.nome.value.trim();
    const erro = validarNome(nome);
    if (erro) {
      marcarErro(form, 'nome', erro);
      return;
    }
    try {
      await comCarregamento(modal.el.querySelector('[type=submit]'), () => api.areas.renomear(id, { nome }));
      modal.fechar();
      toast('Área renomeada.');
      await atualizar(ctx);
    } catch (falha) {
      mostrarErros(form, falha);
    }
  });
}

async function excluirArea(ctx, id) {
  const area = estado.areas.find((a) => a.id === id);
  if (!area) return;
  // RN18 e RN21: confirmação e tarefas movidas para a Geral.
  const ok = await confirmar({
    titulo: `Excluir a área "${area.nome}"?`,
    mensagem: 'As tarefas desta área serão movidas automaticamente para a área Geral. Nenhuma tarefa será perdida.',
    botao: 'Excluir área'
  });
  if (!ok) return;
  try {
    await api.areas.excluir(id);
    toast('Área excluída. As tarefas foram movidas para a Geral.');
    await atualizar(ctx);
  } catch (falha) {
    if (falha.status !== 401) toastErro(falha);
  }
}
