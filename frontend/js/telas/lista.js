// Lista de tarefas (RF14, RF32): paginada, ordenável e com filtros combinados.

import { api } from '../api.js';
import { barraFiltros, carregando, ligarAberturaDeTarefas, ligarFiltros, linhaTarefa, vazioTarefas } from '../componentes.js';
import { consultaFiltros, estado, filtrosAtivos } from '../estado.js';
import { icone } from '../ui.js';

const TAMANHO_PAGINA = 20;

export async function renderizar(ctx) {
  const { el } = ctx;
  const { ordenarPor, ordem } = estado.lista;

  el.innerHTML = `
    ${barraFiltros()}
    <div class="barra-lista">
      <span class="total" data-total></span>
      <div class="ordenacao">
        <label class="rotulo" for="ordenar-por">Ordenar por</label>
        <select class="entrada" id="ordenar-por">
          <option value="Criacao">Data de criação</option>
          <option value="Prazo">Prazo</option>
          <option value="Prioridade">Prioridade</option>
        </select>
        <select class="entrada" id="ordem" aria-label="Ordem">
          <option value="">Padrão</option>
          <option value="asc">Crescente</option>
          <option value="desc">Decrescente</option>
        </select>
      </div>
    </div>
    <div data-resultado></div>`;

  el.querySelector('#ordenar-por').value = ordenarPor;
  el.querySelector('#ordem').value = ordem;

  ligarFiltros(el, () => { estado.lista.pagina = 1; atualizar(ctx); });
  ligarAberturaDeTarefas(el, ctx.abrirTarefa);

  el.querySelector('#ordenar-por').addEventListener('change', (e) => {
    estado.lista.ordenarPor = e.target.value;
    estado.lista.pagina = 1;
    atualizar(ctx);
  });
  el.querySelector('#ordem').addEventListener('change', (e) => {
    estado.lista.ordem = e.target.value;
    atualizar(ctx);
  });
  el.addEventListener('click', (e) => {
    const botao = e.target.closest('[data-pagina]');
    if (!botao) return;
    estado.lista.pagina = Number(botao.dataset.pagina);
    atualizar(ctx);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  });

  await atualizar(ctx);
}

export async function atualizar(ctx) {
  const resultado = ctx.el.querySelector('[data-resultado]');
  if (!resultado) return;
  if (!resultado.children.length) resultado.innerHTML = carregando();

  const { ordenarPor, ordem, pagina } = estado.lista;
  const dados = await api.tarefas.listar({
    ...consultaFiltros(), ordenarPor, ordem, pagina, tamanho: TAMANHO_PAGINA
  });

  // Se a página atual ficou vazia (ex.: após excluir), volta para a última com dados.
  if (!dados.itens.length && dados.total > 0 && pagina > 1) {
    estado.lista.pagina = dados.totalPaginas;
    return atualizar(ctx);
  }

  ctx.el.querySelector('[data-total]').textContent =
    `${dados.total} ${dados.total === 1 ? 'tarefa encontrada' : 'tarefas encontradas'}`;

  resultado.innerHTML = dados.itens.length
    ? `<div class="cartao lista-tarefas">${dados.itens.map(linhaTarefa).join('')}</div>
       ${dados.totalPaginas > 1 ? `
         <nav class="paginacao" aria-label="Paginação">
           <button type="button" class="btn btn-secundario btn-p" data-pagina="${pagina - 1}" ${pagina <= 1 ? 'disabled' : ''}>${icone('esquerda')}Anterior</button>
           <span>Página ${dados.pagina} de ${dados.totalPaginas}</span>
           <button type="button" class="btn btn-secundario btn-p" data-pagina="${pagina + 1}" ${pagina >= dados.totalPaginas ? 'disabled' : ''}>Próxima${icone('direita')}</button>
         </nav>` : ''}`
    : `<div class="cartao">${vazioTarefas({ comFiltro: filtrosAtivos() })}</div>`;
}
