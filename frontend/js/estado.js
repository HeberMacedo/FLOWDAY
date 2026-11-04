// Estado compartilhado entre as telas. Os filtros ficam salvos na sessão do navegador
// para que valham ao alternar entre lista, Kanban e calendário (RF38, RN23).

import { api } from './api.js';
import { hojeISO } from './ui.js';

const CHAVE_FILTROS = 'flowday.filtros';

export const FILTROS_VAZIOS = Object.freeze({ busca: '', areaId: '', prioridade: '', status: '', prazoDe: '', prazoAte: '' });

function carregarFiltros() {
  try {
    return { ...FILTROS_VAZIOS, ...JSON.parse(sessionStorage.getItem(CHAVE_FILTROS) || '{}') };
  } catch {
    return { ...FILTROS_VAZIOS };
  }
}

export const estado = {
  usuario: null,
  areas: [],
  filtros: carregarFiltros(),
  lista: { ordenarPor: 'Criacao', ordem: '', pagina: 1 },
  calendario: { modo: 'mes', data: hojeISO() }
};

export function salvarFiltros() {
  sessionStorage.setItem(CHAVE_FILTROS, JSON.stringify(estado.filtros));
}

export function limparFiltros() {
  estado.filtros = { ...FILTROS_VAZIOS };
  salvarFiltros();
}

export const filtrosAtivos = ({ ignorarPeriodo = false } = {}) =>
  Object.entries(estado.filtros).some(([chave, valor]) =>
    valor && !(ignorarPeriodo && (chave === 'prazoDe' || chave === 'prazoAte')));

/** Parâmetros de consulta aceitos pela API. */
export function consultaFiltros({ ignorarPeriodo = false } = {}) {
  const { busca, areaId, prioridade, status, prazoDe, prazoAte } = estado.filtros;
  return ignorarPeriodo
    ? { busca, areaId, prioridade, status }
    : { busca, areaId, prioridade, status, prazoDe, prazoAte };
}

export async function recarregarAreas() {
  estado.areas = await api.areas.listar();
  if (estado.filtros.areaId && !estado.areas.some((a) => a.id === estado.filtros.areaId)) {
    estado.filtros.areaId = '';
    salvarFiltros();
  }
  return estado.areas;
}

export const nomeArea = (id) => estado.areas.find((a) => a.id === id)?.nome || '';
export const areaGeral = () => estado.areas.find((a) => a.ehGeral);
