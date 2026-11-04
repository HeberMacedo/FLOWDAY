// Calendário (RF35, RN20): visões de mês, semana e dia com as tarefas que têm prazo.
// Os filtros valem aqui, mas o período é o exibido na tela, e não o filtro de prazo (RN23).

import { api } from '../api.js';
import { barraFiltros, carregando, ligarAberturaDeTarefas, ligarFiltros, linhaTarefa, vazio } from '../componentes.js';
import { consultaFiltros, estado } from '../estado.js';
import { deISO, esc, hojeISO, icone, inicioDaSemana, paraISO, somarDias } from '../ui.js';

const DIAS_SEMANA = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb', 'Dom'];
const MAX_CHIPS_MES = 3;
const fmtMes = new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric' });
const fmtDiaLongo = new Intl.DateTimeFormat('pt-BR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
const fmtDiaCurto = new Intl.DateTimeFormat('pt-BR', { day: 'numeric', month: 'short' });

export async function renderizar(ctx) {
  const { el } = ctx;
  el.innerHTML = `
    ${barraFiltros({ semPeriodo: true })}
    <div class="cal-barra">
      <h2 data-titulo-cal></h2>
      <div class="cal-nav">
        <button type="button" class="btn btn-secundario btn-icone" data-navegar="-1" aria-label="Anterior">${icone('esquerda')}</button>
        <button type="button" class="btn btn-secundario" data-navegar="0">Hoje</button>
        <button type="button" class="btn btn-secundario btn-icone" data-navegar="1" aria-label="Próximo">${icone('direita')}</button>
      </div>
      <div class="segmentado" role="group" aria-label="Modo de exibição">
        <button type="button" data-modo="mes">Mês</button>
        <button type="button" data-modo="semana">Semana</button>
        <button type="button" data-modo="dia">Dia</button>
      </div>
    </div>
    <div data-cal>${carregando()}</div>`;

  ligarFiltros(el, () => atualizar(ctx));
  ligarAberturaDeTarefas(el, ctx.abrirTarefa);

  el.addEventListener('click', (e) => {
    const nav = e.target.closest('[data-navegar]');
    if (nav) return navegar(ctx, Number(nav.dataset.navegar));

    const modo = e.target.closest('[data-modo]');
    if (modo) {
      estado.calendario.modo = modo.dataset.modo;
      return atualizar(ctx);
    }

    // Criar tarefa a partir de um dia: o prazo vem preenchido, exceto em dias passados (RN20).
    const nova = e.target.closest('[data-nova-em]');
    if (nova) {
      const dia = nova.dataset.novaEm;
      return ctx.novaTarefa(dia >= hojeISO() ? { prazo: dia } : {});
    }

    if (e.target.closest('[data-tarefa]')) return;
    const dia = e.target.closest('[data-dia]');
    if (dia) {
      estado.calendario = { modo: 'dia', data: dia.dataset.dia };
      atualizar(ctx);
    }
  });

  el.addEventListener('keydown', (e) => {
    const dia = e.target.closest('[data-dia]');
    if (dia && e.target === dia && (e.key === 'Enter' || e.key === ' ')) {
      e.preventDefault();
      estado.calendario = { modo: 'dia', data: dia.dataset.dia };
      atualizar(ctx);
    }
  });

  await atualizar(ctx);
}

function navegar(ctx, passo) {
  const { modo, data } = estado.calendario;
  const atual = deISO(data);
  let nova;
  if (passo === 0) nova = new Date();
  else if (modo === 'mes') nova = new Date(atual.getFullYear(), atual.getMonth() + passo, 1);
  else if (modo === 'semana') nova = somarDias(atual, 7 * passo);
  else nova = somarDias(atual, passo);
  estado.calendario.data = paraISO(nova);
  atualizar(ctx);
}

function intervalo() {
  const { modo, data } = estado.calendario;
  const d = deISO(data);
  if (modo === 'mes') {
    const inicio = inicioDaSemana(new Date(d.getFullYear(), d.getMonth(), 1));
    const fim = somarDias(inicioDaSemana(new Date(d.getFullYear(), d.getMonth() + 1, 0)), 6);
    return { inicio, fim };
  }
  if (modo === 'semana') {
    const inicio = inicioDaSemana(d);
    return { inicio, fim: somarDias(inicio, 6) };
  }
  return { inicio: d, fim: d };
}

const maiuscula = (texto) => texto.charAt(0).toUpperCase() + texto.slice(1);

function tituloPeriodo({ inicio, fim }) {
  const { modo, data } = estado.calendario;
  if (modo === 'mes') return maiuscula(fmtMes.format(deISO(data)));
  if (modo === 'semana') return `${fmtDiaCurto.format(inicio)} – ${fmtDiaCurto.format(fim)} de ${fim.getFullYear()}`;
  return maiuscula(fmtDiaLongo.format(inicio));
}

export async function atualizar(ctx) {
  const alvo = ctx.el.querySelector('[data-cal]');
  if (!alvo) return;
  const periodo = intervalo();
  const { modo } = estado.calendario;

  ctx.el.querySelector('[data-titulo-cal]').textContent = tituloPeriodo(periodo);
  ctx.el.querySelectorAll('[data-modo]').forEach((b) => {
    b.classList.toggle('ativo', b.dataset.modo === modo);
    b.setAttribute('aria-pressed', b.dataset.modo === modo);
  });

  const resposta = await api.tarefas.calendario({
    inicio: paraISO(periodo.inicio),
    fim: paraISO(periodo.fim),
    ...consultaFiltros({ ignorarPeriodo: true })
  });

  const porDia = new Map();
  for (const tarefa of resposta.tarefas) {
    if (!porDia.has(tarefa.prazo)) porDia.set(tarefa.prazo, []);
    porDia.get(tarefa.prazo).push(tarefa);
  }

  if (modo === 'mes') alvo.innerHTML = visaoMes(periodo, porDia);
  else if (modo === 'semana') alvo.innerHTML = visaoSemana(periodo, porDia);
  else alvo.innerHTML = visaoDia(periodo, porDia);
}

/* ---------------- Visões ---------------- */

function chip(tarefa) {
  const concluida = tarefa.status === 'Concluida';
  const classes = ['chip', concluida ? 'concluida' : '', tarefa.atrasada ? 'atrasada' : ''].join(' ');
  const dica = `${tarefa.titulo} · ${tarefa.prioridadeRotulo}${concluida ? ' · Concluída' : ''}${tarefa.atrasada ? ' · Atrasada' : ''}`;
  return `<button type="button" class="${classes}" style="--cor:${esc(tarefa.prioridadeCor)}" data-tarefa="${esc(tarefa.id)}" title="${esc(dica)}">
    ${concluida ? icone('check') : tarefa.atrasada ? icone('triangulo') : ''}<span>${esc(tarefa.titulo)}</span></button>`;
}

function botaoAdicionar(iso, extra = '') {
  return iso >= hojeISO()
    ? `<button type="button" class="btn btn-fantasma btn-icone btn-p add" data-nova-em="${iso}" aria-label="Nova tarefa neste dia" ${extra}>${icone('mais')}</button>`
    : '';
}

function visaoMes({ inicio }, porDia) {
  const mesAtual = deISO(estado.calendario.data).getMonth();
  const hoje = hojeISO();
  const ultimoDia = somarDias(inicioDaSemana(new Date(deISO(estado.calendario.data).getFullYear(), mesAtual + 1, 0)), 6);
  const dias = [];
  for (let d = new Date(inicio); d <= ultimoDia; d = somarDias(d, 1)) dias.push(new Date(d));

  return `
    <div class="cartao cal-mes">
      <div class="cal-cabecalho">${DIAS_SEMANA.map((d) => `<div>${d}</div>`).join('')}</div>
      <div class="cal-grade">
        ${dias.map((d) => {
          const iso = paraISO(d);
          const tarefas = porDia.get(iso) || [];
          const classes = ['cal-dia', d.getMonth() !== mesAtual ? 'fora' : '', iso === hoje ? 'hoje' : ''].join(' ');
          return `
          <div class="${classes}" data-dia="${iso}" role="button" tabindex="0" aria-label="${esc(fmtDiaLongo.format(d))}: ${tarefas.length} tarefa(s)">
            <span class="num">${d.getDate()}</span>
            ${botaoAdicionar(iso)}
            ${tarefas.slice(0, MAX_CHIPS_MES).map(chip).join('')}
            ${tarefas.length > MAX_CHIPS_MES ? `<div class="mais">+${tarefas.length - MAX_CHIPS_MES} mais</div>` : ''}
            <div class="cal-pontos">${tarefas.slice(0, 6).map((t) => `<i style="--cor:${esc(t.prioridadeCor)}"></i>`).join('')}</div>
          </div>`;
        }).join('')}
      </div>
    </div>`;
}

function visaoSemana({ inicio }, porDia) {
  const hoje = hojeISO();
  const dias = Array.from({ length: 7 }, (_, i) => somarDias(inicio, i));
  return `
    <div class="cal-semana">
      ${dias.map((d, i) => {
        const iso = paraISO(d);
        const tarefas = porDia.get(iso) || [];
        return `
        <div class="dia-col${iso === hoje ? ' hoje' : ''}">
          <header data-dia="${iso}" role="button" tabindex="0" aria-label="Ver ${esc(fmtDiaLongo.format(d))}">
            <span>${DIAS_SEMANA[i]}</span><strong>${d.getDate()}</strong>
          </header>
          ${tarefas.map(chip).join('') || '<p class="dica" style="color:var(--texto-3);font-size:12.5px">Sem tarefas</p>'}
          ${iso >= hoje ? `<button type="button" class="btn btn-fantasma btn-p btn-bloco" data-nova-em="${iso}" style="margin-top:6px">${icone('mais')}Adicionar</button>` : ''}
        </div>`;
      }).join('')}
    </div>`;
}

function visaoDia({ inicio }, porDia) {
  const iso = paraISO(inicio);
  const tarefas = porDia.get(iso) || [];
  const passado = iso < hojeISO();
  return `
    <div class="cal-dia-unico">
      <div class="cabecalho-dia">
        <span class="cartao-sub" style="margin:0">${tarefas.length} ${tarefas.length === 1 ? 'tarefa' : 'tarefas'} com prazo neste dia</span>
        ${passado
          ? '<span class="dica" style="color:var(--texto-3);font-size:13px">Dias passados não recebem novas tarefas com prazo.</span>'
          : `<button type="button" class="btn btn-primario" data-nova-em="${iso}">${icone('mais')}Nova tarefa neste dia</button>`}
      </div>
      <div class="cartao lista-tarefas">
        ${tarefas.length
          ? tarefas.map(linhaTarefa).join('')
          : vazio({ titulo: 'Nenhuma tarefa com prazo neste dia', texto: 'Tarefas sem prazo continuam disponíveis na lista e no Kanban.', icone: 'calendario' })}
      </div>
    </div>`;
}
