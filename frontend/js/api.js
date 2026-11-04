// Cliente HTTP da API REST do FlowDay.
// Centraliza o token JWT, o tratamento das respostas de erro (Problem Details,
// RFC 7807) e o redirecionamento ao login quando a sessão expira (RF12, RN05).

import { API_URL } from './config.js';

const CHAVE_SESSAO = 'flowday.sessao';
const CHAVE_AVISO = 'flowday.aviso';

/* ---------------- Sessão ---------------- */

export function obterSessao() {
  try {
    const sessao = JSON.parse(localStorage.getItem(CHAVE_SESSAO));
    if (!sessao?.token) return null;
    if (new Date(sessao.expiraEm) <= new Date()) {
      localStorage.removeItem(CHAVE_SESSAO);
      return null;
    }
    return sessao;
  } catch {
    return null;
  }
}

export function salvarSessao({ token, expiraEm, usuario }) {
  localStorage.setItem(CHAVE_SESSAO, JSON.stringify({ token, expiraEm, usuario }));
}

export function atualizarUsuarioDaSessao(usuario) {
  const sessao = obterSessao();
  if (sessao) salvarSessao({ ...sessao, usuario });
}

export function limparSessao() {
  localStorage.removeItem(CHAVE_SESSAO);
}

/** Mensagem exibida uma única vez na próxima tela (ex.: "Sua sessão expirou"). */
export function definirAviso(tipo, texto) {
  sessionStorage.setItem(CHAVE_AVISO, JSON.stringify({ tipo, texto }));
}

export function consumirAviso() {
  const valor = sessionStorage.getItem(CHAVE_AVISO);
  sessionStorage.removeItem(CHAVE_AVISO);
  try { return valor ? JSON.parse(valor) : null; } catch { return null; }
}

export function irParaLogin(mensagem) {
  limparSessao();
  if (mensagem) definirAviso('info', mensagem);
  window.location.replace('index.html');
}

/* ---------------- Erros ---------------- */

const MENSAGENS_PADRAO = {
  0: 'Não foi possível conectar à API. Verifique se ela está em execução.',
  400: 'Verifique os dados informados.',
  401: 'Sessão ausente ou expirada. Faça login novamente.',
  403: 'Você não tem permissão para esta ação.',
  404: 'Registro não encontrado.',
  409: 'Já existe um registro com esses dados.',
  422: 'A operação não é permitida.',
  423: 'Login bloqueado temporariamente.',
  429: 'Muitas tentativas em pouco tempo. Aguarde um minuto.',
  500: 'Ocorreu um erro inesperado. Tente novamente em instantes.'
};

export class ErroApi extends Error {
  constructor(status, problema) {
    super(problema?.detail || problema?.title || MENSAGENS_PADRAO[status] || MENSAGENS_PADRAO[500]);
    this.status = status;
    this.titulo = problema?.title;
    this.erros = normalizarErros(problema?.errors);
  }
}

/** Converte { "Titulo": ["msg"], "$.prazo": ["msg"] } em { titulo: "msg", prazo: "msg" }. */
function normalizarErros(errors) {
  const resultado = {};
  if (!errors) return resultado;
  for (const [chave, valor] of Object.entries(errors)) {
    const campo = chave.replace(/^\$\.?/, '').split('.').pop() || 'geral';
    const nome = campo.charAt(0).toLowerCase() + campo.slice(1);
    resultado[nome] = Array.isArray(valor) ? valor[0] : String(valor);
  }
  return resultado;
}

/* ---------------- Requisições ---------------- */

/**
 * Faz uma requisição à API.
 * @param {string} caminho  Rota a partir de /api (ex.: "/tarefas").
 * @param {{metodo?: string, corpo?: any, consulta?: object, autenticado?: boolean}} opcoes
 */
export async function requisitar(caminho, { metodo = 'GET', corpo, consulta, autenticado = true } = {}) {
  const url = new URL(`${API_URL}/api${caminho}`);
  for (const [chave, valor] of Object.entries(consulta || {})) {
    if (valor !== undefined && valor !== null && valor !== '') url.searchParams.set(chave, valor);
  }

  const cabecalhos = { Accept: 'application/json' };
  if (corpo !== undefined) cabecalhos['Content-Type'] = 'application/json';
  if (autenticado) {
    const sessao = obterSessao();
    if (!sessao) {
      irParaLogin('Sua sessão expirou. Faça login novamente.');
      throw new ErroApi(401);
    }
    cabecalhos.Authorization = `Bearer ${sessao.token}`;
  }

  let resposta;
  try {
    resposta = await fetch(url, {
      method: metodo,
      headers: cabecalhos,
      body: corpo !== undefined ? JSON.stringify(corpo) : undefined
    });
  } catch {
    throw new ErroApi(0);
  }

  if (resposta.status === 204) return null;

  const tipo = resposta.headers.get('content-type') || '';
  const dados = tipo.includes('json') ? await resposta.json().catch(() => null) : null;

  if (!resposta.ok) {
    if (resposta.status === 401 && autenticado) {
      irParaLogin('Sua sessão expirou. Faça login novamente.');
    }
    throw new ErroApi(resposta.status, dados);
  }
  return dados;
}

/* ---------------- Rotas da API ---------------- */

export const api = {
  auth: {
    cadastrar: (dados) => requisitar('/auth/cadastro', { metodo: 'POST', corpo: dados, autenticado: false }),
    entrar: (dados) => requisitar('/auth/login', { metodo: 'POST', corpo: dados, autenticado: false }),
    sair: () => requisitar('/auth/logout', { metodo: 'POST' }),
    recuperarSenha: (dados) => requisitar('/auth/recuperar-senha', { metodo: 'POST', corpo: dados, autenticado: false }),
    redefinirSenha: (dados) => requisitar('/auth/redefinir-senha', { metodo: 'POST', corpo: dados, autenticado: false })
  },
  perfil: {
    obter: () => requisitar('/perfil'),
    atualizar: (dados) => requisitar('/perfil', { metodo: 'PUT', corpo: dados }),
    alterarSenha: (dados) => requisitar('/perfil/senha', { metodo: 'PUT', corpo: dados }),
    excluir: (dados) => requisitar('/perfil', { metodo: 'DELETE', corpo: dados })
  },
  areas: {
    listar: () => requisitar('/areas'),
    criar: (dados) => requisitar('/areas', { metodo: 'POST', corpo: dados }),
    renomear: (id, dados) => requisitar(`/areas/${id}`, { metodo: 'PUT', corpo: dados }),
    excluir: (id) => requisitar(`/areas/${id}`, { metodo: 'DELETE' })
  },
  tarefas: {
    listar: (consulta) => requisitar('/tarefas', { consulta }),
    kanban: (consulta) => requisitar('/tarefas/kanban', { consulta }),
    calendario: (consulta) => requisitar('/tarefas/calendario', { consulta }),
    obter: (id) => requisitar(`/tarefas/${id}`),
    criar: (dados) => requisitar('/tarefas', { metodo: 'POST', corpo: dados }),
    atualizar: (id, dados) => requisitar(`/tarefas/${id}`, { metodo: 'PUT', corpo: dados }),
    alterarStatus: (id, status) => requisitar(`/tarefas/${id}/status`, { metodo: 'PATCH', corpo: { status } }),
    excluir: (id) => requisitar(`/tarefas/${id}`, { metodo: 'DELETE' })
  },
  checklist: {
    adicionar: (tarefaId, texto) => requisitar(`/tarefas/${tarefaId}/itens`, { metodo: 'POST', corpo: { texto } }),
    editar: (tarefaId, itemId, texto) => requisitar(`/tarefas/${tarefaId}/itens/${itemId}`, { metodo: 'PUT', corpo: { texto } }),
    marcar: (tarefaId, itemId, concluido) =>
      requisitar(`/tarefas/${tarefaId}/itens/${itemId}/concluido`, { metodo: 'PATCH', corpo: { concluido } }),
    remover: (tarefaId, itemId) => requisitar(`/tarefas/${tarefaId}/itens/${itemId}`, { metodo: 'DELETE' })
  },
  painel: {
    obter: () => requisitar('/painel')
  }
};
