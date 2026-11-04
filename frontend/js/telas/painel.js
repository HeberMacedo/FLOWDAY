// Painel (UC09): indicadores e tarefas com prazo para hoje (RF42, RF43).

import { api } from '../api.js';
import { carregando, ligarAberturaDeTarefas, linhaTarefa, vazio } from '../componentes.js';
import { estado, limparFiltros, salvarFiltros } from '../estado.js';
import { esc, hojeISO, icone } from '../ui.js';

const fmtDia = new Intl.DateTimeFormat('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' });

function saudacao() {
  const hora = new Date().getHours();
  if (hora < 12) return 'Bom dia';
  if (hora < 18) return 'Boa tarde';
  return 'Boa noite';
}

function indicador({ classe, nomeIcone, valor, nome, filtro }) {
  return `
    <a class="cartao indicador ${classe}${valor > 0 ? ' tem' : ''}" href="#/lista" data-filtro='${esc(JSON.stringify(filtro))}'>
      <span class="ic">${icone(nomeIcone)}</span>
      <span><span class="valor">${valor}</span><span class="nome">${esc(nome)}</span></span>
    </a>`;
}

export async function renderizar(ctx) {
  const { el } = ctx;
  el.innerHTML = carregando();
  const painel = await api.painel.obter();
  const primeiroNome = (estado.usuario?.nome || '').split(' ')[0];

  el.innerHTML = `
    <div class="saudacao">
      <h2>${saudacao()}, ${esc(primeiroNome)}!</h2>
      <p>${esc(fmtDia.format(new Date()))}</p>
    </div>

    <div class="indicadores">
      ${indicador({ classe: 'i-afazer', nomeIcone: 'circulo', valor: painel.aFazer, nome: 'A fazer', filtro: { status: 'AFazer' } })}
      ${indicador({ classe: 'i-andamento', nomeIcone: 'andamento', valor: painel.emAndamento, nome: 'Em andamento', filtro: { status: 'EmAndamento' } })}
      ${indicador({ classe: 'i-concluidas', nomeIcone: 'sucesso', valor: painel.concluidasNaSemana, nome: 'Concluídas na semana', filtro: { status: 'Concluida' } })}
      ${indicador({ classe: 'i-atrasadas', nomeIcone: 'triangulo', valor: painel.atrasadas, nome: 'Atrasadas', filtro: { ordenarPorPrazo: true } })}
    </div>

    <section class="cartao">
      <div class="cartao-corpo" style="padding-bottom:12px">
        <div class="secao-titulo">
          <div>
            <h3>Para hoje</h3>
            <p class="cartao-sub" style="margin:2px 0 0">Tarefas com prazo para hoje</p>
          </div>
          <button type="button" class="btn btn-secundario btn-p" data-nova-hoje>${icone('mais')}Adicionar</button>
        </div>
      </div>
      <div class="lista-tarefas" style="border-top:1px solid var(--borda)">
        ${painel.tarefasDeHoje.length
          ? painel.tarefasDeHoje.map(linhaTarefa).join('')
          : vazio({ titulo: 'Nada com prazo para hoje', texto: 'Aproveite para adiantar outras tarefas ou planejar a semana.', icone: 'calendario' })}
      </div>
    </section>`;

  ligarAberturaDeTarefas(el, ctx.abrirTarefa);
  el.querySelector('[data-nova-hoje]').addEventListener('click', () => ctx.novaTarefa({ prazo: hojeISO() }));

  // Os indicadores levam à lista já filtrada.
  el.querySelectorAll('[data-filtro]').forEach((a) => a.addEventListener('click', () => {
    const filtro = JSON.parse(a.dataset.filtro);
    limparFiltros();
    if (filtro.status) estado.filtros.status = filtro.status;
    salvarFiltros();
    estado.lista = { ordenarPor: filtro.ordenarPorPrazo ? 'Prazo' : 'Criacao', ordem: filtro.ordenarPorPrazo ? 'asc' : '', pagina: 1 };
  }));
}
