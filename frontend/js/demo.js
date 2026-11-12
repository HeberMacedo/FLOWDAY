// Modo demonstração (GitHub Pages).
// O GitHub Pages só hospeda arquivos estáticos, então a API em C#/.NET e o MySQL não rodam lá.
// Este módulo responde às mesmas rotas da API, no mesmo formato (incluindo os erros em
// Problem Details), aplicando as mesmas regras de negócio, e guarda os dados no navegador.
// Com Docker (localhost), o front-end usa a API real e este arquivo não é carregado.

const CHAVE_BANCO = 'flowday.demo.banco';
const EXPIRACAO_MINUTOS = 60;
const MAXIMO_FALHAS = 5;
const BLOQUEIO_MINUTOS = 15;
const MAXIMO_ITENS = 50;
const DIAS_MAXIMOS_CALENDARIO = 62;

export const CONTA_DEMONSTRACAO = { nome: 'Maria Souza', email: 'demo@flowday.app', senha: 'demo1234' };

const PRIORIDADES = {
  Baixa: { ordem: 1, rotulo: 'Baixa', cor: '#22C55E' },
  Media: { ordem: 2, rotulo: 'Média', cor: '#EAB308' },
  Alta: { ordem: 3, rotulo: 'Alta', cor: '#F97316' },
  Urgente: { ordem: 4, rotulo: 'Urgente', cor: '#EF4444' }
};
const STATUS = {
  AFazer: { ordem: 1, rotulo: 'A fazer' },
  EmAndamento: { ordem: 2, rotulo: 'Em andamento' },
  Concluida: { ordem: 3, rotulo: 'Concluída' }
};
const AREAS_PADRAO = ['Pessoal', 'Trabalho', 'Estudos', 'Lazer', 'Geral'];
const MENSAGEM_LOGIN_INVALIDO = 'E-mail ou senha incorretos.';

/* ---------------- Utilitários ---------------- */

class Falha extends Error {
  constructor(status, titulo, detalhe, erros) {
    super(detalhe);
    this.status = status;
    this.problema = { title: titulo, status, detail: detalhe, ...(erros ? { errors: erros } : {}) };
  }
}

const validacao = (campo, mensagem) => new Falha(400, 'Dados inválidos', mensagem, { [campo]: [mensagem] });
const regra = (mensagem) => new Falha(422, 'Regra de negócio violada', mensagem);
const naoEncontrado = (mensagem) => new Falha(404, 'Não encontrado', mensagem);
const conflito = (campo, mensagem) => new Falha(409, 'Conflito', mensagem, { [campo]: [mensagem] });

/** Junta os erros de vários campos em uma única resposta, como o Validador da API. */
function validar(regras) {
  const erros = {};
  for (const [campo, mensagem] of regras) if (mensagem) erros[campo] = [mensagem];
  const campos = Object.keys(erros);
  if (campos.length === 1) throw validacao(campos[0], erros[campos[0]][0]);
  if (campos.length > 1) throw new Falha(400, 'Dados inválidos', 'Um ou mais campos estão inválidos.', erros);
}

const novoId = () => (crypto.randomUUID ? crypto.randomUUID()
  : 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    return (c === 'x' ? r : (r & 3) | 8).toString(16);
  }));

const agoraISO = () => new Date().toISOString();
const doisDigitos = (n) => String(n).padStart(2, '0');
const paraISO = (d) => `${d.getFullYear()}-${doisDigitos(d.getMonth() + 1)}-${doisDigitos(d.getDate())}`;
const hoje = () => paraISO(new Date());
const somarDias = (iso, dias) => { const [a, m, d] = iso.split('-').map(Number); return paraISO(new Date(a, m - 1, d + dias)); };
const texto = (v) => (typeof v === 'string' ? v.trim() : '');
const normalizar = (v) => texto(v).toLowerCase();
const dataValida = (v) => typeof v === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(v) && !Number.isNaN(new Date(v).getTime());

/** Senhas nunca ficam em texto puro, nem na demonstração. */
async function hash(valor) {
  const dados = new TextEncoder().encode(`flowday-demo:${valor}`);
  const resumo = await crypto.subtle.digest('SHA-256', dados);
  return [...new Uint8Array(resumo)].map((b) => b.toString(16).padStart(2, '0')).join('');
}

/* ---------------- Regras (espelham o domínio em C#) ---------------- */

const validarNome = (nome) => (texto(nome).length < 2 || texto(nome).length > 100 ? 'O nome deve ter de 2 a 100 caracteres.' : null);

function validarEmail(email) {
  const e = texto(email);
  if (!e) return 'Informe o e-mail.';
  if (e.length > 254 || !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(e)) return 'Informe um e-mail válido.';
  return null;
}

function validarSenha(senha) {
  const s = typeof senha === 'string' ? senha : '';
  if (s.length < 8 || s.length > 128) return 'A senha deve ter no mínimo 8 caracteres.';
  if (!/\p{L}/u.test(s) || !/\d/.test(s)) return 'A senha deve ter ao menos uma letra e um número.';
  return null;
}

const validarTitulo = (t) => (texto(t).length < 3 || texto(t).length > 120 ? 'O título deve ter de 3 a 120 caracteres.' : null);
const validarDescricao = (d) => (texto(d).length > 1000 ? 'A descrição deve ter no máximo 1000 caracteres.' : null);
const validarNomeArea = (n) => (texto(n).length < 1 || texto(n).length > 50 ? 'O nome da área deve ter de 1 a 50 caracteres.' : null);
const validarItem = (t) => (texto(t).length < 1 || texto(t).length > 200 ? 'O texto do item deve ter de 1 a 200 caracteres.' : null);

function prioridadeValida(p) {
  if (p === undefined || p === null) return 'Media';
  if (!PRIORIDADES[p]) throw validacao('prioridade', 'Valor ausente ou em formato inválido.');
  return p;
}

function validarNovoPrazo(prazo) {
  if (prazo === null || prazo === undefined || prazo === '') return null;
  if (!dataValida(prazo)) throw validacao('prazo', 'Valor ausente ou em formato inválido.');
  if (prazo < hoje()) throw validacao('prazo', 'O prazo não pode ser anterior à data atual.');
  return prazo;
}

/** RN14: atrasada é a tarefa não concluída com prazo anterior a hoje. */
const estaAtrasada = (t) => !!t.prazo && t.prazo < hoje() && t.status !== 'Concluida';

/* ---------------- Banco no navegador ---------------- */

function carregar() {
  try {
    const banco = JSON.parse(localStorage.getItem(CHAVE_BANCO));
    if (banco?.usuarios) return banco;
  } catch { /* banco vazio */ }
  return { usuarios: [], areas: [], tarefas: [] };
}

const salvar = (banco) => localStorage.setItem(CHAVE_BANCO, JSON.stringify(banco));

/* ---------------- Respostas ---------------- */

const usuarioResposta = (u) => ({ id: u.id, nome: u.nome, email: u.email, criadoEm: u.criadoEm });
const areaResposta = (a) => ({ id: a.id, nome: a.nome, ehGeral: a.ehGeral });

function tarefaResposta(t, banco) {
  const total = t.itens.length;
  const concluidos = t.itens.filter((i) => i.concluido).length;
  const area = banco.areas.find((a) => a.id === t.areaId);
  return {
    id: t.id,
    titulo: t.titulo,
    descricao: t.descricao,
    status: t.status,
    statusRotulo: STATUS[t.status].rotulo,
    prioridade: t.prioridade,
    prioridadeRotulo: PRIORIDADES[t.prioridade].rotulo,
    prioridadeCor: PRIORIDADES[t.prioridade].cor,
    prazo: t.prazo,
    atrasada: estaAtrasada(t),
    concluidaEm: t.concluidaEm,
    criadaEm: t.criadaEm,
    atualizadaEm: t.atualizadaEm,
    area: area ? areaResposta(area) : null,
    progresso: { concluidos, total, percentual: total === 0 ? 0 : Math.round((concluidos * 100) / total) },
    itens: [...t.itens].sort((a, b) => a.ordem - b.ordem).map((i) => ({ ...i }))
  };
}

/** Carimbo da última troca de senha, gravado no token (como o claim "sv" da API). */
const carimbo = (usuario) => String(Date.parse(usuario.senhaAlteradaEm));

function gerarToken(usuario) {
  const expiraEm = new Date(Date.now() + EXPIRACAO_MINUTOS * 60000).toISOString();
  return { token: `demo.${usuario.id}.${carimbo(usuario)}`, expiraEm, usuario: usuarioResposta(usuario) };
}

/** RN05: sessões emitidas antes de uma troca de senha deixam de valer. */
function usuarioDoToken(banco, token) {
  const [prefixo, id, carimboToken] = String(token || '').split('.');
  const usuario = prefixo === 'demo' ? banco.usuarios.find((u) => u.id === id) : null;
  if (!usuario || carimbo(usuario) !== carimboToken) {
    throw new Falha(401, 'Não autorizado', 'Sessão ausente ou expirada. Faça login novamente.');
  }
  return usuario;
}

/* ---------------- Conta e acesso (M1) ---------------- */

function criarUsuario(banco, { nome, email, senhaHash }) {
  const momento = agoraISO();
  const usuario = {
    id: novoId(), nome: texto(nome), email: texto(email), emailNormalizado: normalizar(email), senhaHash,
    falhasLogin: 0, bloqueadoAte: null, senhaAlteradaEm: momento, criadoEm: momento
  };
  banco.usuarios.push(usuario);
  // RN16: áreas criadas no cadastro.
  for (const nomeArea of AREAS_PADRAO) {
    banco.areas.push({ id: novoId(), usuarioId: usuario.id, nome: nomeArea, nomeNormalizado: nomeArea.toLowerCase(), ehGeral: nomeArea === 'Geral' });
  }
  return usuario;
}

async function cadastrar({ banco, corpo }) {
  validar([
    ['nome', validarNome(corpo.nome)],
    ['email', validarEmail(corpo.email)],
    ['senha', validarSenha(corpo.senha)],
    ['confirmacaoSenha', corpo.senha !== corpo.confirmacaoSenha ? 'A confirmação de senha não confere.' : null]
  ]);
  if (banco.usuarios.some((u) => u.emailNormalizado === normalizar(corpo.email))) {
    throw conflito('email', 'Este e-mail já está em uso.');
  }
  const usuario = criarUsuario(banco, { nome: corpo.nome, email: corpo.email, senhaHash: await hash(corpo.senha) });
  return [201, gerarToken(usuario)];
}

async function entrar({ banco, corpo }) {
  const usuario = banco.usuarios.find((u) => u.emailNormalizado === normalizar(corpo.email));
  if (!usuario) throw new Falha(401, 'Não autorizado', MENSAGEM_LOGIN_INVALIDO);

  const agora = new Date();
  if (usuario.bloqueadoAte && new Date(usuario.bloqueadoAte) > agora) {
    throw new Falha(423, 'Login bloqueado', 'Login bloqueado por 15 minutos após 5 tentativas sem sucesso. Tente novamente mais tarde.');
  }
  if (usuario.bloqueadoAte) { usuario.bloqueadoAte = null; usuario.falhasLogin = 0; }

  if (usuario.senhaHash !== await hash(corpo.senha ?? '')) {
    // RN04: 5 falhas seguidas bloqueiam o login por 15 minutos.
    usuario.falhasLogin += 1;
    if (usuario.falhasLogin >= MAXIMO_FALHAS) {
      usuario.bloqueadoAte = new Date(agora.getTime() + BLOQUEIO_MINUTOS * 60000).toISOString();
      usuario.falhasLogin = 0;
    }
    salvar(banco);
    throw new Falha(401, 'Não autorizado', MENSAGEM_LOGIN_INVALIDO);
  }
  usuario.falhasLogin = 0;
  return [200, gerarToken(usuario)];
}

function recuperarSenha() {
  return [202, {
    mensagem: 'Modo demonstração: nenhum e-mail é enviado. Na versão completa, executada com Docker, o link para redefinir a senha chega por e-mail (Mailpit).'
  }];
}

function redefinirSenha() {
  throw regra('Link inválido, expirado ou já utilizado. Solicite um novo link.');
}

/* ---------------- Perfil (UC10) ---------------- */

async function conferirSenhaAtual(usuario, senha) {
  if (!senha || usuario.senhaHash !== await hash(senha)) throw validacao('senhaAtual', 'A senha atual está incorreta.');
}

async function atualizarPerfil({ banco, usuario, corpo }) {
  validar([['nome', validarNome(corpo.nome)], ['email', validarEmail(corpo.email)]]);
  if (normalizar(corpo.email) !== usuario.emailNormalizado) {
    // RN22: trocar o e-mail exige a senha atual.
    await conferirSenhaAtual(usuario, corpo.senhaAtual);
    if (banco.usuarios.some((u) => u.id !== usuario.id && u.emailNormalizado === normalizar(corpo.email))) {
      throw conflito('email', 'Este e-mail já está em uso.');
    }
  }
  usuario.nome = texto(corpo.nome);
  usuario.email = texto(corpo.email);
  usuario.emailNormalizado = normalizar(corpo.email);
  return [200, usuarioResposta(usuario)];
}

async function alterarSenha({ usuario, corpo }) {
  await conferirSenhaAtual(usuario, corpo.senhaAtual);
  validar([
    ['novaSenha', validarSenha(corpo.novaSenha)],
    ['confirmacaoSenha', corpo.novaSenha !== corpo.confirmacaoSenha ? 'A confirmação de senha não confere.' : null]
  ]);
  usuario.senhaHash = await hash(corpo.novaSenha);
  usuario.senhaAlteradaEm = agoraISO();
  usuario.falhasLogin = 0;
  usuario.bloqueadoAte = null;
  return [200, gerarToken(usuario)];
}

async function excluirConta({ banco, usuario, corpo }) {
  await conferirSenhaAtual(usuario, corpo?.senhaAtual);
  // RN21: remove definitivamente todos os dados do usuário.
  banco.tarefas = banco.tarefas.filter((t) => t.usuarioId !== usuario.id);
  banco.areas = banco.areas.filter((a) => a.usuarioId !== usuario.id);
  banco.usuarios = banco.usuarios.filter((u) => u.id !== usuario.id);
  return [204, null];
}

/* ---------------- Áreas (UC06) ---------------- */

const areasDo = (banco, usuario) => banco.areas.filter((a) => a.usuarioId === usuario.id);

/** RN01: dado de outro usuário é tratado como inexistente. */
function areaDo(banco, usuario, id) {
  const area = banco.areas.find((a) => a.id === id && a.usuarioId === usuario.id);
  if (!area) throw naoEncontrado('Área não encontrada.');
  return area;
}

function garantirNomeDisponivel(banco, usuario, nome, exceto) {
  const erro = validarNomeArea(nome);
  if (erro) throw validacao('nome', erro);
  if (areasDo(banco, usuario).some((a) => a.id !== exceto && a.nomeNormalizado === normalizar(nome))) {
    throw conflito('nome', 'Já existe uma área com esse nome.');
  }
}

function listarAreas({ banco, usuario }) {
  const areas = areasDo(banco, usuario).sort((a, b) => (b.ehGeral - a.ehGeral) || a.nome.localeCompare(b.nome, 'pt-BR'));
  return [200, areas.map(areaResposta)];
}

function criarArea({ banco, usuario, corpo }) {
  garantirNomeDisponivel(banco, usuario, corpo.nome);
  const area = { id: novoId(), usuarioId: usuario.id, nome: texto(corpo.nome), nomeNormalizado: normalizar(corpo.nome), ehGeral: false };
  banco.areas.push(area);
  return [201, areaResposta(area)];
}

function renomearArea({ banco, usuario, corpo, params: [id] }) {
  const area = areaDo(banco, usuario, id);
  if (area.ehGeral) throw regra('A área Geral não pode ser renomeada.');
  garantirNomeDisponivel(banco, usuario, corpo.nome, area.id);
  area.nome = texto(corpo.nome);
  area.nomeNormalizado = normalizar(corpo.nome);
  return [200, areaResposta(area)];
}

function excluirArea({ banco, usuario, params: [id] }) {
  const area = areaDo(banco, usuario, id);
  if (area.ehGeral) throw regra('A área Geral não pode ser excluída.');
  // RN18: as tarefas da área vão para a Geral.
  const geral = areasDo(banco, usuario).find((a) => a.ehGeral);
  for (const t of banco.tarefas) {
    if (t.usuarioId === usuario.id && t.areaId === area.id) { t.areaId = geral.id; t.atualizadaEm = agoraISO(); }
  }
  banco.areas = banco.areas.filter((a) => a.id !== area.id);
  return [204, null];
}

/* ---------------- Tarefas (UC04, UC07, UC08) ---------------- */

function tarefaDo(banco, usuario, id) {
  const tarefa = banco.tarefas.find((t) => t.id === id && t.usuarioId === usuario.id);
  if (!tarefa) throw naoEncontrado('Tarefa não encontrada.');
  return tarefa;
}

function resolverArea(banco, usuario, areaId) {
  if (!areaId) return areasDo(banco, usuario).find((a) => a.ehGeral);
  const area = banco.areas.find((a) => a.id === areaId && a.usuarioId === usuario.id);
  if (!area) throw validacao('areaId', 'Área inválida.');
  return area;
}

/** RN23: filtros combinados; a busca ignora maiúsculas e minúsculas. */
function filtrar(banco, usuario, consulta, { usarPeriodo = true } = {}) {
  const busca = normalizar(consulta.busca);
  for (const [campo, valores] of [['prioridade', PRIORIDADES], ['status', STATUS]]) {
    if (consulta[campo] && !valores[consulta[campo]]) throw validacao(campo, `O valor informado para '${campo}' é inválido.`);
  }
  return banco.tarefas.filter((t) => t.usuarioId === usuario.id
    && (!busca || t.titulo.toLowerCase().includes(busca) || (t.descricao || '').toLowerCase().includes(busca))
    && (!consulta.areaId || t.areaId === consulta.areaId)
    && (!consulta.prioridade || t.prioridade === consulta.prioridade)
    && (!consulta.status || t.status === consulta.status)
    && (!usarPeriodo || !consulta.prazoDe || (t.prazo && t.prazo >= consulta.prazoDe))
    && (!usarPeriodo || !consulta.prazoAte || (t.prazo && t.prazo <= consulta.prazoAte)));
}

const comparar = (a, b) => (a < b ? -1 : a > b ? 1 : 0);

function listarTarefas({ banco, usuario, consulta }) {
  const ordenarPor = consulta.ordenarPor || 'Criacao';
  const ordem = String(consulta.ordem || '').toLowerCase();
  const decrescente = ordem === 'asc' ? false : ordem === 'desc' ? true : ordenarPor !== 'Prazo';
  const sinal = decrescente ? -1 : 1;

  const tarefas = filtrar(banco, usuario, consulta).sort((a, b) => {
    if (ordenarPor === 'Prazo') {
      // Tarefas sem prazo ficam sempre no fim da lista.
      if (!a.prazo !== !b.prazo) return a.prazo ? -1 : 1;
      return sinal * comparar(a.prazo || '', b.prazo || '');
    }
    if (ordenarPor === 'Prioridade') {
      return sinal * (PRIORIDADES[a.prioridade].ordem - PRIORIDADES[b.prioridade].ordem) || comparar(b.criadaEm, a.criadaEm);
    }
    return sinal * comparar(a.criadaEm, b.criadaEm);
  });

  const tamanho = Math.min(Math.max(Number(consulta.tamanho) || 20, 1), 100);
  const pagina = Math.max(Number(consulta.pagina) || 1, 1);
  const itens = tarefas.slice((pagina - 1) * tamanho, pagina * tamanho).map((t) => tarefaResposta(t, banco));
  return [200, { itens, pagina, tamanhoPagina: tamanho, total: tarefas.length, totalPaginas: Math.ceil(tarefas.length / tamanho) }];
}

function porPrioridadeEPrazo(a, b) {
  return (PRIORIDADES[b.prioridade].ordem - PRIORIDADES[a.prioridade].ordem)
    || ((!a.prazo) - (!b.prazo)) || comparar(a.prazo || '', b.prazo || '');
}

/** RF33: com o filtro de status, só a coluna correspondente aparece (RN23). */
function kanban({ banco, usuario, consulta }) {
  const tarefas = filtrar(banco, usuario, consulta).sort(porPrioridadeEPrazo);
  const status = consulta.status ? [consulta.status] : Object.keys(STATUS);
  const colunas = status.map((s) => {
    const daColuna = tarefas.filter((t) => t.status === s).map((t) => tarefaResposta(t, banco));
    return { status: s, rotulo: STATUS[s].rotulo, total: daColuna.length, tarefas: daColuna };
  });
  return [200, { colunas }];
}

/** RF35 e RN20: só tarefas com prazo; vale o período exibido. */
function calendario({ banco, usuario, consulta }) {
  const { inicio, fim } = consulta;
  const faltando = {};
  if (!dataValida(inicio)) faltando.inicio = ["O parâmetro 'inicio' é obrigatório."];
  if (!dataValida(fim)) faltando.fim = ["O parâmetro 'fim' é obrigatório."];
  if (Object.keys(faltando).length) throw new Falha(400, 'Dados inválidos', 'Verifique os campos informados.', faltando);
  if (fim < inicio) throw validacao('fim', 'A data final deve ser igual ou posterior à data inicial.');
  if ((new Date(fim) - new Date(inicio)) / 86400000 > DIAS_MAXIMOS_CALENDARIO) {
    throw validacao('fim', `O período do calendário pode ter no máximo ${DIAS_MAXIMOS_CALENDARIO} dias.`);
  }
  const tarefas = filtrar(banco, usuario, consulta, { usarPeriodo: false })
    .filter((t) => t.prazo && t.prazo >= inicio && t.prazo <= fim)
    .sort((a, b) => comparar(a.prazo, b.prazo) || (PRIORIDADES[b.prioridade].ordem - PRIORIDADES[a.prioridade].ordem));
  return [200, { inicio, fim, tarefas: tarefas.map((t) => tarefaResposta(t, banco)) }];
}

const obterTarefa = ({ banco, usuario, params: [id] }) => [200, tarefaResposta(tarefaDo(banco, usuario, id), banco)];

function adicionarItemNaTarefa(tarefa, textoItem) {
  // RN12 e RN19.
  if (tarefa.status === 'Concluida') throw regra('A tarefa está concluída. Reabra a tarefa para alterar o checklist.');
  if (tarefa.itens.length >= MAXIMO_ITENS) throw regra(`A tarefa já possui o limite de ${MAXIMO_ITENS} itens de checklist.`);
  const erro = validarItem(textoItem);
  if (erro) throw validacao('texto', erro);
  const ordem = tarefa.itens.reduce((maior, i) => Math.max(maior, i.ordem), 0) + 1;
  tarefa.itens.push({ id: novoId(), texto: texto(textoItem), concluido: false, ordem });
  tarefa.atualizadaEm = agoraISO();
}

/** RN07, RN08 e RN13: título obrigatório, padrões Média e Geral, prazo não pode estar no passado. */
function criarTarefa({ banco, usuario, corpo }) {
  const area = resolverArea(banco, usuario, corpo.areaId);
  validar([['titulo', validarTitulo(corpo.titulo)], ['descricao', validarDescricao(corpo.descricao)]]);
  const prioridade = prioridadeValida(corpo.prioridade);
  const prazo = validarNovoPrazo(corpo.prazo);
  const momento = agoraISO();
  const tarefa = {
    id: novoId(), usuarioId: usuario.id, areaId: area.id, titulo: texto(corpo.titulo),
    descricao: texto(corpo.descricao) || null, status: 'AFazer', prioridade, prazo,
    concluidaEm: null, criadaEm: momento, atualizadaEm: momento, itens: []
  };
  for (const item of corpo.itens || []) adicionarItemNaTarefa(tarefa, item);
  banco.tarefas.push(tarefa);
  return [201, tarefaResposta(tarefa, banco)];
}

/** RN13: o prazo só é validado quando muda; editar tarefa vencida é permitido. */
function atualizarTarefa({ banco, usuario, corpo, params: [id] }) {
  const tarefa = tarefaDo(banco, usuario, id);
  const area = corpo.areaId && corpo.areaId !== tarefa.areaId ? resolverArea(banco, usuario, corpo.areaId) : null;
  validar([['titulo', validarTitulo(corpo.titulo)], ['descricao', validarDescricao(corpo.descricao)]]);
  const prioridade = corpo.prioridade ? prioridadeValida(corpo.prioridade) : tarefa.prioridade;
  const novoPrazo = corpo.prazo || null;
  if (novoPrazo !== tarefa.prazo) tarefa.prazo = validarNovoPrazo(novoPrazo);
  tarefa.titulo = texto(corpo.titulo);
  tarefa.descricao = texto(corpo.descricao) || null;
  tarefa.prioridade = prioridade;
  if (area) tarefa.areaId = area.id;
  tarefa.atualizadaEm = agoraISO();
  return [200, tarefaResposta(tarefa, banco)];
}

/** RN09, RN10 e RN11. */
function alterarStatus({ banco, usuario, corpo, params: [id] }) {
  const tarefa = tarefaDo(banco, usuario, id);
  if (!STATUS[corpo.status]) throw validacao('status', 'Status inválido.');
  if (corpo.status !== tarefa.status) {
    if (corpo.status === 'Concluida') {
      if (tarefa.itens.some((i) => !i.concluido)) {
        throw regra('A tarefa só pode ser concluída quando todos os itens do checklist estiverem concluídos.');
      }
      tarefa.concluidaEm = agoraISO();
    } else {
      tarefa.concluidaEm = null;
    }
    tarefa.status = corpo.status;
    tarefa.atualizadaEm = agoraISO();
  }
  return [200, tarefaResposta(tarefa, banco)];
}

function excluirTarefa({ banco, usuario, params: [id] }) {
  const tarefa = tarefaDo(banco, usuario, id);
  banco.tarefas = banco.tarefas.filter((t) => t !== tarefa);
  return [204, null];
}

/* ---------------- Checklist (UC05) ---------------- */

function itemDa(tarefa, itemId) {
  if (tarefa.status === 'Concluida') throw regra('A tarefa está concluída. Reabra a tarefa para alterar o checklist.');
  const item = tarefa.itens.find((i) => i.id === itemId);
  if (!item) throw naoEncontrado('Item de checklist não encontrado.');
  return item;
}

function adicionarItem({ banco, usuario, corpo, params: [id] }) {
  const tarefa = tarefaDo(banco, usuario, id);
  adicionarItemNaTarefa(tarefa, corpo.texto);
  return [201, tarefaResposta(tarefa, banco)];
}

function editarItem({ banco, usuario, corpo, params: [id, itemId] }) {
  const tarefa = tarefaDo(banco, usuario, id);
  const item = itemDa(tarefa, itemId);
  const erro = validarItem(corpo.texto);
  if (erro) throw validacao('texto', erro);
  item.texto = texto(corpo.texto);
  tarefa.atualizadaEm = agoraISO();
  return [200, tarefaResposta(tarefa, banco)];
}

/** RN10: marcar o último item não conclui a tarefa automaticamente. */
function marcarItem({ banco, usuario, corpo, params: [id, itemId] }) {
  const tarefa = tarefaDo(banco, usuario, id);
  itemDa(tarefa, itemId).concluido = !!corpo.concluido;
  tarefa.atualizadaEm = agoraISO();
  return [200, tarefaResposta(tarefa, banco)];
}

function removerItem({ banco, usuario, params: [id, itemId] }) {
  const tarefa = tarefaDo(banco, usuario, id);
  const item = itemDa(tarefa, itemId);
  tarefa.itens = tarefa.itens.filter((i) => i !== item);
  tarefa.atualizadaEm = agoraISO();
  return [200, tarefaResposta(tarefa, banco)];
}

/* ---------------- Painel (UC09) ---------------- */

function painel({ banco, usuario }) {
  const minhas = banco.tarefas.filter((t) => t.usuarioId === usuario.id);
  const d = new Date();
  const inicioSemana = new Date(d.getFullYear(), d.getMonth(), d.getDate() - ((d.getDay() + 6) % 7));
  const fimSemana = new Date(inicioSemana.getFullYear(), inicioSemana.getMonth(), inicioSemana.getDate() + 7);
  const dia = hoje();
  return [200, {
    aFazer: minhas.filter((t) => t.status === 'AFazer').length,
    emAndamento: minhas.filter((t) => t.status === 'EmAndamento').length,
    concluidasNaSemana: minhas.filter((t) => t.status === 'Concluida' && t.concluidaEm
      && new Date(t.concluidaEm) >= inicioSemana && new Date(t.concluidaEm) < fimSemana).length,
    atrasadas: minhas.filter(estaAtrasada).length,
    tarefasDeHoje: minhas.filter((t) => t.prazo === dia)
      .sort((a, b) => (STATUS[a.status].ordem - STATUS[b.status].ordem) || (PRIORIDADES[b.prioridade].ordem - PRIORIDADES[a.prioridade].ordem))
      .map((t) => tarefaResposta(t, banco))
  }];
}

/* ---------------- Roteamento ---------------- */

const ID = '([^/]+)';
const ROTAS = [
  ['POST', '/auth/cadastro', cadastrar, false],
  ['POST', '/auth/login', entrar, false],
  ['POST', '/auth/logout', () => [204, null], true],
  ['POST', '/auth/recuperar-senha', recuperarSenha, false],
  ['POST', '/auth/redefinir-senha', redefinirSenha, false],
  ['GET', '/perfil', ({ usuario }) => [200, usuarioResposta(usuario)], true],
  ['PUT', '/perfil', atualizarPerfil, true],
  ['PUT', '/perfil/senha', alterarSenha, true],
  ['DELETE', '/perfil', excluirConta, true],
  ['GET', '/areas', listarAreas, true],
  ['POST', '/areas', criarArea, true],
  ['GET', `/areas/${ID}`, ({ banco, usuario, params: [id] }) => [200, areaResposta(areaDo(banco, usuario, id))], true],
  ['PUT', `/areas/${ID}`, renomearArea, true],
  ['DELETE', `/areas/${ID}`, excluirArea, true],
  ['GET', '/tarefas', listarTarefas, true],
  ['GET', '/tarefas/kanban', kanban, true],
  ['GET', '/tarefas/calendario', calendario, true],
  ['POST', '/tarefas', criarTarefa, true],
  ['GET', `/tarefas/${ID}`, obterTarefa, true],
  ['PUT', `/tarefas/${ID}`, atualizarTarefa, true],
  ['PATCH', `/tarefas/${ID}/status`, alterarStatus, true],
  ['DELETE', `/tarefas/${ID}`, excluirTarefa, true],
  ['POST', `/tarefas/${ID}/itens`, adicionarItem, true],
  ['PUT', `/tarefas/${ID}/itens/${ID}`, editarItem, true],
  ['PATCH', `/tarefas/${ID}/itens/${ID}/concluido`, marcarItem, true],
  ['DELETE', `/tarefas/${ID}/itens/${ID}`, removerItem, true],
  ['GET', '/painel', painel, true]
].map(([metodo, padrao, acao, autenticada]) => ({ metodo, regex: new RegExp(`^${padrao}$`), acao, autenticada }));

/**
 * Responde a uma requisição como a API faria. Retorna { status, dados }.
 * @param {string} metodo  GET, POST, PUT, PATCH ou DELETE.
 * @param {string} caminho Rota a partir de /api (ex.: "/tarefas").
 */
export async function responderDemo(metodo, caminho, { corpo, consulta = {}, token } = {}) {
  const banco = carregar();
  try {
    let encontrada = null;
    let params = [];
    for (const rota of ROTAS) {
      const m = rota.metodo === metodo && caminho.match(rota.regex);
      if (m) { encontrada = rota; params = m.slice(1).map(decodeURIComponent); break; }
    }
    if (!encontrada) throw naoEncontrado('Rota não encontrada.');

    const usuario = encontrada.autenticada ? usuarioDoToken(banco, token) : null;
    const [status, dados] = await encontrada.acao({ banco, usuario, corpo: corpo ?? {}, consulta, params });
    if (metodo !== 'GET') salvar(banco);
    return { status, dados };
  } catch (erro) {
    if (erro instanceof Falha) return { status: erro.status, dados: erro.problema };
    console.error(erro);
    return { status: 500, dados: { title: 'Erro interno', status: 500, detail: 'Ocorreu um erro inesperado. Tente novamente em instantes.' } };
  }
}

/* ---------------- Conta de demonstração ---------------- */

/** Cria (uma única vez) a conta de demonstração com tarefas de exemplo relativas à data de hoje. */
export async function prepararContaDemonstracao() {
  const banco = carregar();
  if (banco.usuarios.some((u) => u.emailNormalizado === CONTA_DEMONSTRACAO.email)) return CONTA_DEMONSTRACAO;

  const usuario = criarUsuario(banco, { ...CONTA_DEMONSTRACAO, senhaHash: await hash(CONTA_DEMONSTRACAO.senha) });
  const area = (nome) => banco.areas.find((a) => a.usuarioId === usuario.id && a.nome === nome)?.id;
  const academia = { id: novoId(), usuarioId: usuario.id, nome: 'Academia', nomeNormalizado: 'academia', ehGeral: false };
  banco.areas.push(academia);

  const d = hoje();
  const exemplos = [
    ['Entregar projeto de Back-end', 'Estudos', 'Urgente', 2, 'EmAndamento', ['Revisar o README', 'Gerar o documento de evidências', 'Ensaiar a apresentação'], 1, 'API REST em .NET, front-end, testes e documento de evidências.'],
    ['Estudar para a prova de Banco de Dados', 'Estudos', 'Alta', 7, 'AFazer', ['Revisar normalização', 'Exercícios de SQL', 'Resumo de índices', 'Simulado'], 3, 'Normalização, índices e transações.'],
    ['Reunião de alinhamento da sprint', 'Trabalho', 'Media', 0, 'AFazer'],
    ['Pagar a conta de luz', 'Pessoal', 'Alta', 0, 'AFazer'],
    ['Enviar relatório semanal', 'Trabalho', 'Alta', 1, 'EmAndamento', ['Consolidar indicadores', 'Revisar com a equipe'], 1],
    ['Comprar presente de aniversário', 'Pessoal', 'Media', 4, 'AFazer'],
    ['Treino de pernas', 'Academia', 'Baixa', 0, 'Concluida'],
    ['Ler 30 páginas de Clean Code', 'Estudos', 'Baixa', 3, 'AFazer'],
    ['Organizar viagem de fim de ano', 'Lazer', 'Media', null, 'AFazer', ['Pesquisar passagens', 'Reservar hospedagem', 'Montar roteiro'], 0],
    ['Atualizar currículo no LinkedIn', 'Trabalho', 'Baixa', null, 'Concluida'],
    ['Marcar consulta no dentista', 'Pessoal', 'Urgente', 1, 'AFazer'],
    ['Assistir à aula gravada de Arquitetura', 'Estudos', 'Media', 9, 'AFazer'],
    ['Renovar matrícula da academia', 'Academia', 'Alta', -2, 'AFazer'],
    ['Responder e-mail do orientador', 'Estudos', 'Urgente', -3, 'AFazer'],
    ['Cortar o cabelo', 'Pessoal', 'Baixa', 12, 'AFazer']
  ];

  exemplos.forEach(([titulo, nomeArea, prioridade, dias, status, itens = [], feitos = 0, descricao = null], i) => {
    const criadaEm = new Date(Date.now() - (exemplos.length - i) * 3600000).toISOString();
    banco.tarefas.push({
      id: novoId(), usuarioId: usuario.id, areaId: nomeArea === 'Academia' ? academia.id : area(nomeArea),
      titulo, descricao, status, prioridade, prazo: dias === null ? null : somarDias(d, dias),
      concluidaEm: status === 'Concluida' ? agoraISO() : null, criadaEm, atualizadaEm: criadaEm,
      itens: itens.map((textoItem, ordem) => ({ id: novoId(), texto: textoItem, concluido: ordem < feitos || status === 'Concluida', ordem: ordem + 1 }))
    });
  });

  salvar(banco);
  return CONTA_DEMONSTRACAO;
}
